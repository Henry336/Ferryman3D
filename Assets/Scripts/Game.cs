using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Builds the whole scene in code and runs the level flow. The only object in the scene is this one.
[RequireComponent(typeof(Tuning))]
public class Game : MonoBehaviour
{
    enum State { Title, Card, Play, Dock, Docked, Failed, End }

    State state = State.Title;
    LevelDef[] levels;
    int levelIndex;
    float elapsed, stateTime, handTimer, speedNow, failElapsed;
    bool confirmOpen, paused;

    Boat boat;
    Puppy puppy;
    Reaper reaper;
    CameraRig cam;
    Material waterMat, skyMat, hullMat;
    readonly List<Soul> souls = new List<Soul>();
    readonly List<HandGroup> hands = new List<HandGroup>();
    readonly List<Transform> ridges = new List<Transform>();
    readonly List<float> ridgeFactor = new List<float>();
    readonly List<Material> ridgeMats = new List<Material>();
    readonly List<Transform> embers = new List<Transform>();
    Transform pier;
    float pierX;
    LevelDef L => levels[levelIndex];
    const int PuppyLeg = 2; // the puppy joins at the end of leg 3 and rides along after that

    void Awake()
    {
        Application.targetFrameRate = 60;
        GetComponent<Tuning>(); // Awake sets Tuning.I
        if (!Tuning.I) Tuning.I = GetComponent<Tuning>();
        levels = LevelDef.All();
        BuildWorld();
        ApplyLevelLook(levels[0]);
        ResetLevel(0, State.Title);
        Sfx.Ambience("sfx_river_amb");
    }

    // ---------- World ----------
    void BuildWorld()
    {
        Shader.SetGlobalFloat("_FogStart", 18f);
        Shader.SetGlobalFloat("_FogEnd", 75f);
        Shader.SetGlobalFloat("_BoatHalf", Tuning.I.halfLength);

        var camGo = new GameObject("Camera");
        var c = camGo.AddComponent<Camera>();
        c.clearFlags = CameraClearFlags.SolidColor;
        c.fieldOfView = 34f; c.nearClipPlane = 0.3f; c.farClipPlane = 200f;
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        cam = camGo.AddComponent<CameraRig>();
        camGo.transform.position = cam.offset;

        var lightGo = new GameObject("Moon");
        var li = lightGo.AddComponent<Light>();
        li.type = LightType.Directional; li.color = new Color(0.8f, 0.9f, 1f); li.intensity = 1f;
        lightGo.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

        skyMat = Art.Unlit(Color.black, Color.black);
        skyMat.SetFloat("_Y0", -2f); skyMat.SetFloat("_Y1", 40f);
        var sky = Art.Obj("Sky", null, MeshKit.Quad(400f, 160f), skyMat).transform;
        sky.position = new Vector3(0f, 20f, 120f);

        // Parallax cliffs: two tiles per layer so they wrap.
        float[] zs = { 95f, 70f, 45f };
        float[] fac = { 0.12f, 0.25f, 0.45f };
        for (int layer = 0; layer < 3; layer++)
        {
            var mat = Art.Unlit(Color.black, Color.black);
            ridgeMats.Add(mat);
            for (int tile = 0; tile < 2; tile++)
            {
                var r = Art.Obj("Ridge" + layer, null, MeshKit.Ridge(200f, -12f, 8f + layer * -1.5f, 22f - layer * 4f, 9 + layer * 3, 7 + layer * 13 + tile), mat).transform;
                r.position = new Vector3(-100f + tile * 200f, -1f, zs[layer]);
                ridges.Add(r); ridgeFactor.Add(fac[layer]);
            }
        }

        waterMat = Art.Water();
        Art.Obj("Water", null, MeshKit.Grid(-45f, 45f, -30f, 60f, 220, 120), waterMat);

        // Embers / soul motes drifting in the air.
        var emberMat = Art.Glow(new Color(1f, 0.75f, 0.45f, 0.9f), 0.8f, 1.5f);
        for (int i = 0; i < 28; i++)
        {
            var e = Art.Obj("Ember", null, Art.Sphere, emberMat).transform;
            e.localScale = Vector3.one * Random.Range(0.05f, 0.12f);
            e.position = new Vector3(Random.Range(-22f, 22f), Random.Range(-0.5f, 9f), Random.Range(2f, 30f));
            embers.Add(e);
        }

        BuildBoat();
        BuildReaper();
        BuildPier();
        BuildPuppy();
        cam.target = boat.transform;
    }

    void BuildBoat()
    {
        var root = new GameObject("Boat");
        boat = root.AddComponent<Boat>();
        // Meshy boat: length runs along X in the file; yaw 180 puts the lantern at the bow (+X).
        var model = Art.Model("boat", root.transform, 8.8f, 180f, true);
        if (model)
        {
            model.transform.localPosition = new Vector3(0f, -0.75f, 0f);
            hullMat = model.GetComponentInChildren<Renderer>().sharedMaterial;
            hullMat.SetFloat("_Outline", 0.03f);
            boat.hullMat = hullMat;
            var glow = new GameObject("LanternGlow").transform;
            glow.SetParent(root.transform, false);
            glow.localPosition = new Vector3(4.2f, 1.9f, 0f);
            var h = Art.Obj("Halo", glow, MeshKit.Quad(1f, 1f), Art.Halo(new Color(0.4f, 0.9f, 1f, 0.35f))).transform;
            h.localScale = Vector3.one * 6f;
            h.localPosition = new Vector3(0, 0, -0.6f);
            return;
        }
        hullMat = Art.Toon(Art.Hex("#5a4030"), Art.Hex("#24170f"), 0.05f);
        boat.hullMat = hullMat;
        var hull = new[] { new Vector2(-3.6f, -0.1f), new Vector2(-2.8f, -0.55f), new Vector2(0f, -0.68f), new Vector2(2.8f, -0.55f),
                           new Vector2(3.6f, -0.1f), new Vector2(4.1f, 0.55f), new Vector2(-4.1f, 0.55f) };
        Art.Obj("Hull", root.transform, MeshKit.Extrude(hull, 1.6f), hullMat);
        var trim = Art.Toon(Art.Hex("#7a5a40"), Art.Hex("#3a2618"), 0.03f);
        var rim = Art.Obj("Rim", root.transform, Art.Cylinder, trim).transform;
        rim.localScale = new Vector3(0.09f, 4.1f, 0.09f);
        rim.localRotation = Quaternion.Euler(0, 0, 90f);
        rim.localPosition = new Vector3(0, 0.58f, -0.8f);
        // Curled stern and bow posts with a lantern at the bow.
        foreach (var s in new[] { -1f, 1f })
        {
            var post = Art.Obj("Post", root.transform, Art.Cylinder, trim).transform;
            post.localScale = new Vector3(0.14f, 0.6f, 0.14f);
            post.localPosition = new Vector3(4.05f * s, 1.05f, 0f);
            post.localRotation = Quaternion.Euler(0, 0, -18f * s);
        }
        var lantern = Art.Obj("Lantern", root.transform, Art.Sphere, Art.Glow(new Color(1f, 0.78f, 0.4f, 1f), 0.9f, 1.2f)).transform;
        lantern.localScale = Vector3.one * 0.32f;
        lantern.localPosition = new Vector3(4.35f, 1.65f, 0f);
        var halo = Art.Obj("Halo", lantern, MeshKit.Quad(1f, 1f), Art.Halo(new Color(1f, 0.6f, 0.25f, 0.35f))).transform;
        halo.localScale = Vector3.one * 7f;
        halo.localPosition = new Vector3(0, 0, -0.5f);
    }

    void BuildReaper()
    {
        var root = new GameObject("Reaper");
        reaper = root.AddComponent<Reaper>();
        reaper.boat = boat;
        var visual = new GameObject("Visual").transform; visual.SetParent(root.transform, false);
        var body = new GameObject("Body").transform; body.SetParent(visual, false);
        reaper.visual = visual; reaper.body = body;

        var trailMatM = Art.Glow(new Color(0.7f, 0.95f, 1f, 1f), 0.6f, 0.8f);
        // Meshy reaper faces +Z; yaw 120 turns it to the right and three-quarters toward the camera.
        var model = Art.Model("reaper", body, 1.55f, 120f);
        if (model)
        {
            reaper.anim = model.GetComponentInChildren<Animation>();
            reaper.eyes = new Renderer[0];
            var piv = new GameObject("ScythePivot").transform;
            piv.SetParent(visual, false);
            piv.localPosition = new Vector3(0.38f, 0.65f, -0.35f);
            reaper.scythePivot = piv;
            var sc = Art.Model("scythe", piv, 1.9f, 0f);
            if (sc) sc.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            var tr = Art.Obj("Trail", piv, MeshKit.Blade(1.5f, 0.6f, 0.01f), trailMatM);
            tr.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            reaper.trailMat = trailMatM;
            reaper.SetTrail(tr);
            return;
        }

        var robe = Art.Toon(Art.Hex("#1b1b24"), Art.Hex("#08080c"), 0.035f);
        robe.SetColor("_RimColor", new Color(0.35f, 0.75f, 0.8f, 1f));
        Art.Obj("Robe", body, MeshKit.Cone(0.5f, 0.2f, 0.95f), robe);
        var hood = Art.Obj("Hood", body, Art.Sphere, robe).transform;
        hood.localScale = new Vector3(0.62f, 0.66f, 0.62f);
        hood.localPosition = new Vector3(0f, 1.12f, 0f);
        var tip = Art.Obj("HoodTip", body, MeshKit.Cone(0.2f, 0.01f, 0.5f, 12), robe).transform;
        tip.localPosition = new Vector3(-0.12f, 1.35f, 0f);
        tip.localRotation = Quaternion.Euler(0, 0, 40f);
        var face = Art.Obj("Face", body, Art.Sphere, Art.Toon(Color.black, Color.black, 0f)).transform;
        face.localScale = new Vector3(0.36f, 0.42f, 0.42f);
        face.localPosition = new Vector3(0.17f, 1.08f, 0f);

        var eyeMat = Art.Glow(new Color(0.75f, 0.95f, 1f, 1f), 1f, 1f);
        var eyes = new List<Renderer>();
        foreach (var z in new[] { -0.12f, 0.08f })
        {
            var e = Art.Obj("Eye", body, Art.Sphere, eyeMat);
            e.transform.localScale = new Vector3(0.06f, 0.1f, 0.06f);
            e.transform.localPosition = new Vector3(0.33f, 1.1f, z);
            eyes.Add(e.GetComponent<Renderer>());
        }
        reaper.eyes = eyes.ToArray();

        var bone = Art.Toon(Art.Hex("#e8e0cc"), Art.Hex("#9a917e"), 0.025f);
        var pivot = new GameObject("ScythePivot").transform;
        pivot.SetParent(visual, false);
        pivot.localPosition = new Vector3(0.32f, 0.6f, -0.42f);
        reaper.scythePivot = pivot;
        var hand = Art.Obj("Hand", pivot, Art.Sphere, bone).transform;
        hand.localScale = Vector3.one * 0.14f;
        var shaft = Art.Toon(Art.Hex("#2e2622"), Art.Hex("#120e0c"), 0.025f);
        var pole = Art.Obj("Pole", pivot, Art.Cylinder, shaft).transform;
        pole.localScale = new Vector3(0.055f, 1.2f, 0.055f);
        pole.localPosition = new Vector3(0f, 0.55f, 0f);
        var blade = Art.Obj("Blade", pivot, MeshKit.Blade(1.0f, 0.22f, 0.04f), bone).transform;
        blade.localPosition = new Vector3(0f, 1.7f, 0f);
        var trailMat = Art.Glow(new Color(0.7f, 0.95f, 1f, 1f), 0.6f, 0.8f);
        var trail = Art.Obj("Trail", pivot, MeshKit.Blade(1.5f, 0.6f, 0.01f), trailMat);
        trail.transform.localPosition = new Vector3(0f, 1.65f, 0f);
        reaper.trailMat = trailMat;
        reaper.SetTrail(trail);

        var other = Art.Obj("Hand2", visual, Art.Sphere, bone).transform;
        other.localScale = Vector3.one * 0.13f;
        other.localPosition = new Vector3(0.28f, 0.45f, 0.3f);
    }

    void BuildPuppy()
    {
        var root = new GameObject("Puppy");
        puppy = root.AddComponent<Puppy>();
        puppy.reaper = reaper; puppy.boat = boat;
        var model = Art.Model("puppy", root.transform, 0.75f, 0f); // Puppy.cs turns it toward travel
        if (model) puppy.model = model.transform;
        root.SetActive(false);
    }

    void BuildPier()
    {
        pier = new GameObject("Pier").transform;
        var wood = Art.Toon(Art.Hex("#3d3028"), Art.Hex("#16100c"), 0.04f);
        var deck = Art.Obj("Deck", pier, Art.Cylinder, wood).transform;
        deck.localScale = new Vector3(0.6f, 6f, 1.2f);
        deck.localRotation = Quaternion.Euler(0, 0, 90f);
        deck.localPosition = new Vector3(5f, 0.6f, 2.2f);
        for (int i = 0; i < 4; i++)
        {
            var p = Art.Obj("Pile", pier, Art.Cylinder, wood).transform;
            p.localScale = new Vector3(0.25f, 1.6f, 0.25f);
            p.localPosition = new Vector3(i * 3.3f, 0f, 1.6f);
        }
        var lamp = Art.Obj("Lamp", pier, Art.Sphere, Art.Glow(new Color(0.6f, 0.95f, 1f, 1f), 0.9f, 1.2f)).transform;
        lamp.localScale = Vector3.one * 0.45f;
        lamp.localPosition = new Vector3(0f, 2.4f, 1.6f);
        var halo = Art.Obj("Halo", lamp, MeshKit.Quad(1f, 1f), Art.Halo(new Color(0.4f, 0.9f, 1f, 0.3f))).transform;
        halo.localScale = Vector3.one * 9f;
        pier.position = new Vector3(60f, 0f, 0f);
    }

    void ApplyLevelLook(LevelDef d)
    {
        skyMat.SetColor("_Top", d.skyTop);
        skyMat.SetColor("_Bottom", d.skyBottom);
        Shader.SetGlobalColor("_FogColor", d.skyBottom);
        Camera.main.backgroundColor = d.skyBottom;
        for (int i = 0; i < ridgeMats.Count; i++)
        {
            float k = 0.35f + i * 0.25f; // nearer layers are darker
            ridgeMats[i].SetColor("_Top", Color.Lerp(d.skyBottom, d.skyTop * 0.4f, k));
            ridgeMats[i].SetColor("_Bottom", Color.Lerp(d.skyBottom, Color.black, k));
        }
        waterMat.SetColor("_Deep", d.waterDeep);
        waterMat.SetColor("_Light", d.waterLight);
    }

    // ---------- Level flow ----------
    void ResetLevel(int index, State next, float resumeAt = 0f)
    {
        levelIndex = index;
        foreach (var h in hands) if (h) Destroy(h.gameObject);
        hands.Clear();
        foreach (var s in souls) if (s) Destroy(s.gameObject);
        souls.Clear();
        boat.ResetState();
        reaper.ResetState(-0.6f);
        Waves.Amp = L.waveAmp;
        elapsed = resumeAt; handTimer = Random.Range(L.handInterval.x, L.handInterval.y) * 0.6f;
        speedNow = Tuning.I.baseSpeed * L.speed;
        pierX = 60f; pier.position = new Vector3(pierX, 0f, 0f);
        ApplyLevelLook(L);
        SpawnSouls();
        if (puppy) { if (index > PuppyLeg) puppy.Settle(); else { puppy.Aboard = false; puppy.gameObject.SetActive(false); } }
        SetState(next);
    }

    // Failing late in a long leg only costs ~20 s: resume before the failure, calm and straight into play.
    void Retry() => ResetLevel(levelIndex, State.Play, Mathf.Max(0f, failElapsed - 20f));

    void SpawnSouls()
    {
        int total = L.calmSouls + L.vengefulSouls;
        float lim = Tuning.I.halfLength - 0.9f;
        for (int i = 0; i < total; i++)
        {
            bool veng = i >= L.calmSouls;
            var col = veng ? new Color(1f, 0.32f, 0.25f, 1f) : new Color(0.75f, 0.92f, 1f, 1f);
            Material mat;
            GameObject g;
            var soulTex = Resources.Load<Texture2D>("Models/soul_tex");
            if (soulTex)
            {
                // Sky-style glowing figure: painted texture pushed bright, warm for calm souls, red for vengeful ones.
                col = veng ? new Color(1f, 0.45f, 0.38f, 1f) : new Color(1f, 0.97f, 0.85f, 1f);
                mat = Art.ToonTex(soulTex, 0.015f);
                mat.SetFloat("_Emission", 0.85f);
                mat.SetColor("_RimColor", veng ? new Color(1f, 0.3f, 0.2f, 1f) : new Color(1f, 0.9f, 0.6f, 1f));
                mat.SetColor("_OutlineColor", veng ? new Color(0.35f, 0.05f, 0.05f, 1f) : new Color(0.45f, 0.35f, 0.15f, 1f));
                g = new GameObject(veng ? "VengefulSoul" : "Soul");
                var m = Art.Model("soul", g.transform, 1.0f, Random.Range(150f, 210f), false, mat);
                if (m) m.transform.localPosition = new Vector3(0f, -0.7f, 0f);
                var halo = Art.Obj("Halo", g.transform, MeshKit.Quad(1f, 1f), Art.Halo(veng ? new Color(1f, 0.25f, 0.2f, 0.35f) : new Color(1f, 0.85f, 0.5f, 0.3f))).transform;
                halo.localScale = Vector3.one * 2.4f;
                halo.localPosition = new Vector3(0f, -0.2f, 0.4f);
            }
            else
            {
                mat = Art.Glow(col, 0.55f, 1.4f);
                g = Art.Obj(veng ? "VengefulSoul" : "Soul", null, Art.Sphere, mat);
                g.transform.localScale = new Vector3(0.42f, 0.55f, 0.42f);
                var tail = Art.Obj("Tail", g.transform, MeshKit.Cone(0.5f, 0.05f, 1.2f, 12), mat).transform;
                tail.localRotation = Quaternion.Euler(180f, 0f, 0f);
                tail.localPosition = new Vector3(0, 0.2f, 0);
            }
            var s = g.AddComponent<Soul>();
            s.mat = mat;
            float x = Mathf.Lerp(-lim, lim, total == 1 ? 0.5f : i / (float)(total - 1));
            if (Mathf.Abs(x - reaper.LocalX) < 0.6f) x += 0.8f;
            s.Init(boat, veng, Mathf.Clamp(x, -lim, lim), col);
            souls.Add(s);
        }
    }

    void SetState(State s)
    {
        state = s; stateTime = 0f;
        cam.menu = s == State.Title;
        if (reaper) reaper.Emote = s == State.Docked || s == State.End ? "Happy_Sway" : "";
        if (s == State.Failed) Sfx.Play("sfx_capsize");
        if (s == State.Dock) Sfx.Play("sfx_dock_bell");
        if (s == State.Card) Sfx.Play("sfx_card_whoosh", 0.8f);
        if (s == State.Docked) Sfx.Play("sfx_soul_ascend");
        bool inLevel = s == State.Card || s == State.Play || s == State.Dock || s == State.Failed;
        Sfx.Music(s == State.End ? "mus_epilogue" : !inLevel ? "mus_title" : levelIndex == levels.Length - 1 ? "mus_final" : "mus_level");
    }

    void Update()
    {
        GameInput.Update();
        if (Input.GetKeyDown(KeyCode.M)) Sfx.SetOn(!Sfx.On);
        if (Input.GetKeyDown(KeyCode.F9)) ResetLevel(PuppyLeg, State.Play, levels[PuppyLeg].duration - 4f); // dev: jump to the puppy moment
        float dt = Time.deltaTime;
        stateTime += Time.unscaledDeltaTime;

        if (confirmOpen)
        {
            if (GameInput.Yes) { confirmOpen = false; Time.timeScale = 1f; ResetLevel(levelIndex, State.Play); }
            else if (GameInput.No) { confirmOpen = false; Time.timeScale = 1f; }
            return;
        }
        if (GameInput.Pause && (state == State.Play || state == State.Dock))
        {
            paused = !paused; Time.timeScale = paused ? 0f : 1f; AudioListener.pause = paused;
        }
        if (paused) return;
        if (GameInput.Restart && (state == State.Play || state == State.Dock))
        {
            confirmOpen = true; Time.timeScale = 0f; return;
        }

        Waves.Time += dt;
        Waves.Scroll += speedNow * dt;
        Waves.Push();
        UpdateBackdrop(dt);

        switch (state)
        {
            case State.Title:
                StepIdleBoat(dt);
                if (GameInput.Confirm) SetState(State.Card);
                break;
            case State.Card:
                StepIdleBoat(dt);
                if (stateTime > 2.2f || (stateTime > 0.4f && (GameInput.Confirm || TappedAnywhere()))) SetState(State.Play);
                break;
            case State.Play: StepPlay(dt); break;
            case State.Dock: StepDock(dt); break;
            case State.Docked:
                StepIdleBoat(dt);
                if (levelIndex == PuppyLeg)
                {
                    // Puppy moment: wordless, about 3 s, skippable with the same continue press.
                    if (!puppy.Aboard && stateTime > 1.0f) { Sfx.Play("sfx_dog_whimper", 0.8f); puppy.Arrive(pier.position + new Vector3(2.6f, 1.4f, 1.6f)); }
                    reaper.Emote = stateTime > 2.0f && stateTime < 4.2f ? "happy_jump_m" : "Happy_Sway";
                }
                if (stateTime > 1.2f && (GameInput.Confirm || TappedAnywhere()))
                {
                    if (levelIndex == PuppyLeg) puppy.Settle();
                    if (levelIndex + 1 < levels.Length) ResetLevel(levelIndex + 1, State.Card);
                    else SetState(State.End);
                }
                break;
            case State.Failed:
                StepBoatOnly(dt);
                if (stateTime > 1.5f && (GameInput.Confirm || GameInput.Restart)) Retry();
                break;
            case State.End:
                StepIdleBoat(dt);
                break;
        }

        Sfx.Danger(state == State.Play && !paused ? Mathf.Max(Mathf.InverseLerp(0.5f, 1f, boat.TiltFraction), boat.FloodFraction) : 0f);
        cam.intensity = Mathf.Clamp01(Mathf.Max(boat.TiltFraction - 0.4f, boat.SinkMeter) * 1.4f);
    }

    bool TappedAnywhere() => Input.GetMouseButtonDown(0) && !confirmOpen;

    void StepIdleBoat(float dt)
    {
        reaper.Step(0f, false, dt);
        StepBoatOnly(dt);
    }

    void StepBoatOnly(float dt)
    {
        float cos = Mathf.Cos(boat.Angle * Mathf.Deg2Rad);
        float torque = reaper.Torque(cos);
        foreach (var s in souls) { s.Step(dt); torque += s.Torque(cos); }
        boat.Step(torque, 0, dt);
    }

    void StepPlay(float dt)
    {
        elapsed += dt;
        bool swung = reaper.Step(GameInput.Move, GameInput.Slash, dt);
        if (swung) ResolveSwing();

        hands.RemoveAll(h => h == null);
        handTimer -= dt;
        if (handTimer <= 0f && elapsed < L.duration - 4f)
        {
            SpawnHands();
            handTimer = Random.Range(L.handInterval.x, L.handInterval.y);
        }

        float cos = Mathf.Cos(boat.Angle * Mathf.Deg2Rad);
        float torque = reaper.Torque(cos) + puppy.Torque(cos);
        foreach (var s in souls) { s.Step(dt); torque += s.Torque(cos); }
        int middle = 0;
        foreach (var h in hands) { torque += h.Torque(cos); middle += h.MiddleCount; }
        boat.Step(torque, middle, dt);
        if (boat.TiltFraction > 0.6f) Sfx.Play("sfx_boat_creak", 0.6f, 2.5f);
        if (boat.SinkMeter > 0.35f) Sfx.Play("sfx_sink_warn", 0.7f, 3f);

        if (boat.Failed) { failElapsed = elapsed; SetState(State.Failed); return; }
        if (elapsed >= L.duration)
        {
            foreach (var h in hands) if (h) h.ForceLeave();
            boat.Docked = true;
            SetState(State.Dock);
        }
    }

    void StepDock(float dt)
    {
        float p = Mathf.Clamp01(stateTime / 3f);
        speedNow = Tuning.I.baseSpeed * L.speed * (1f - Mathf.SmoothStep(0f, 1f, p));
        pierX = Mathf.Lerp(26f, Tuning.I.halfLength - 1f, 1f - Mathf.Pow(1f - p, 3f));
        pier.position = new Vector3(pierX, -0.3f, 0f);
        reaper.Step(GameInput.Move, false, dt);
        if (p >= 1f && stateTime < 3.1f) foreach (var s in souls) s.Leave();
        StepBoatOnly(dt);
        hands.RemoveAll(h => h == null);
        if (stateTime > 5f) SetState(State.Docked);
    }

    void ResolveSwing()
    {
        var T = Tuning.I;
        int hit = 0;
        foreach (var h in hands)
        {
            if (!h || !h.Hittable) continue;
            float dx = (h.LocalX - reaper.LocalX) * reaper.Facing;
            if (dx >= -T.slashBehind && dx <= T.slashRange) { h.Hit(); hit++; }
        }
        foreach (var s in souls)
        {
            if (!s.vengeful) continue;
            float dx = (s.LocalX - reaper.LocalX) * reaper.Facing;
            if (dx >= -0.2f && dx <= T.pokeRange) s.Poke(reaper.Facing);
        }
        Sfx.Play("sfx_scythe_swing", 0.8f);
        if (hit > 0) { Sfx.Play("sfx_slash_hit"); StartCoroutine(HitPause()); }
    }

    IEnumerator HitPause()
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(Tuning.I.hitPause);
        if (!confirmOpen) Time.timeScale = 1f;
    }

    void SpawnHands()
    {
        var T = Tuning.I;
        var options = new List<HandGroup.Slot> { HandGroup.Slot.Back, HandGroup.Slot.Front };
        if (L.middleHands) options.Add(HandGroup.Slot.Middle);
        foreach (var h in hands) options.Remove(h.slot);
        if (options.Count == 0) return;
        var slot = options[Random.Range(0, options.Count)];
        float end = T.halfLength - 0.7f;
        float x = slot == HandGroup.Slot.Front ? end : slot == HandGroup.Slot.Back ? -end : Random.Range(-1.4f, 1.4f);
        var g = new GameObject("Hands").AddComponent<HandGroup>();
        g.Init(boat, slot, x, Random.Range(2, L.maxHandsPerGroup + 1));
        hands.Add(g);
        Sfx.Play("sfx_hand_rise", 0.7f, 0.3f);
    }

    void UpdateBackdrop(float dt)
    {
        for (int i = 0; i < ridges.Count; i++)
        {
            var r = ridges[i];
            float x = -100f + (i % 2) * 200f - Waves.Scroll * ridgeFactor[i];
            x = Mathf.Repeat(x + 200f, 400f) - 200f;
            r.position = new Vector3(x, r.position.y, r.position.z);
        }
        foreach (var e in embers)
        {
            var p = e.position;
            p.x -= speedNow * dt * (0.3f + p.z * 0.01f);
            p.y += dt * 0.25f;
            if (p.x < -24f) p.x += 48f;
            if (p.y > 9f) p.y = -0.5f;
            e.position = p;
        }
        if (state != State.Dock && state != State.Docked && pier.position.x < 50f)
            pier.position += Vector3.left * speedNow * dt;
    }

    // ---------- UI ----------
    GUIStyle big, mid, small, btn, logo, eyebrow, menuBtn, menuLeft;
    static readonly Color Bone = new Color(0.95f, 0.91f, 0.8f), Teal = new Color(0.45f, 0.82f, 0.8f);

    static Texture2D Tex(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }

    void Styles()
    {
        float u = Screen.height / 720f;
        if (big == null)
        {
            var title = Resources.Load<Font>("Fonts/Title");
            var body = Resources.Load<Font>("Fonts/Body");
            big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, font = title };
            mid = new GUIStyle(big) { font = body };
            small = new GUIStyle(mid);
            btn = new GUIStyle(GUI.skin.button) { font = body };
            logo = new GUIStyle(big) { alignment = TextAnchor.MiddleLeft };
            eyebrow = new GUIStyle(logo) { font = body, fontStyle = FontStyle.Bold };
            menuLeft = new GUIStyle(logo) { font = body };
            menuBtn = new GUIStyle(GUI.skin.button) { font = body, alignment = TextAnchor.MiddleCenter, border = new RectOffset(0, 0, 0, 0) };
            menuBtn.normal.background = Tex(new Color(0.04f, 0.09f, 0.1f, 0.75f));
            menuBtn.hover.background = menuBtn.focused.background = Tex(new Color(0.12f, 0.24f, 0.25f, 0.9f));
            menuBtn.active.background = Tex(new Color(0.9f, 0.75f, 0.45f, 0.95f));
            menuBtn.normal.textColor = menuBtn.hover.textColor = Bone;
            menuBtn.active.textColor = new Color(0.05f, 0.05f, 0.05f);
        }
        big.fontSize = Mathf.RoundToInt(64 * u);
        mid.fontSize = Mathf.RoundToInt(30 * u);
        small.fontSize = Mathf.RoundToInt(20 * u);
        btn.fontSize = Mathf.RoundToInt(28 * u);
        logo.fontSize = Mathf.RoundToInt(96 * u);
        eyebrow.fontSize = Mathf.RoundToInt(16 * u);
        menuLeft.fontSize = Mathf.RoundToInt(28 * u);
        menuBtn.fontSize = Mathf.RoundToInt(26 * u);
        big.normal.textColor = mid.normal.textColor = small.normal.textColor = logo.normal.textColor = menuLeft.normal.textColor = Bone;
        eyebrow.normal.textColor = Teal;
    }

    void DrawMenu(float W, float H)
    {
        float u = H / 720f, x = Mathf.Max(24 * u, W * 0.08f), w = Mathf.Min(W - 2 * x, 520 * u);
        var old = GUI.color;
        // Darken the left side so the text reads over the river.
        float panel = x + w * 0.75f;
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(new Rect(0, 0, panel, H), Texture2D.whiteTexture);
        for (int i = 0; i < 24; i++) // soft fade-out edge
        {
            GUI.color = new Color(0, 0, 0, 0.5f * (1f - i / 24f));
            GUI.DrawTexture(new Rect(panel + i * 8 * u, 0, 8 * u, H), Texture2D.whiteTexture);
        }
        GUI.color = old;

        float y = H * 0.24f;
        GUI.Label(new Rect(x, y, w, 24 * u), "THE RIVER STYX  ·  ONE LAST CROSSING", eyebrow);
        GUI.Label(new Rect(x, y + 20 * u, W, 110 * u), "FERRYMAN", logo);
        GUI.Label(new Rect(x, y + 125 * u, w, 40 * u), "The reaper's final shift.", menuLeft);
        GUI.color = new Color(Bone.r, Bone.g, Bone.b, 0.4f);
        GUI.DrawTexture(new Rect(x, y + 175 * u, w * 0.8f, 1.5f * u), Texture2D.whiteTexture);
        GUI.color = old;

        float by = y + 200 * u, bw = Mathf.Min(w * 0.8f, 340 * u), bh = 56 * u;
        if (GUI.Button(new Rect(x, by, bw, bh), "Begin crossing", menuBtn)) { Sfx.Play("sfx_ui_click"); SetState(State.Card); }
        if (GUI.Button(new Rect(x, by + bh + 14 * u, bw, bh * 0.8f), Sfx.On ? "Sound: On" : "Sound: Off", menuBtn)) { Sfx.SetOn(!Sfx.On); Sfx.Play("sfx_ui_click"); }
        if (GUI.Button(new Rect(x, by + bh * 1.8f + 28 * u, bw, bh * 0.8f), CameraRig.ShakeOn ? "Screen shake: On" : "Screen shake: Off", menuBtn)) { CameraRig.ShakeOn = !CameraRig.ShakeOn; Sfx.Play("sfx_ui_click"); }

        GUI.color = new Color(1, 1, 1, 0.7f);
        GUI.Label(new Rect(x, H - 70 * u, W - x, 30 * u), "Arrows / A D  move     Z  slash     R  restart     Esc  pause     M  mute", eyebrow);
        GUI.Label(new Rect(x, H - 44 * u, W - x, 30 * u), Blink() ? "Press Z or Enter to begin" : "", eyebrow);
        GUI.color = old;
    }

    void OnGUI()
    {
        Styles();
        float W = Screen.width, H = Screen.height;
        switch (state)
        {
            case State.Title:
                DrawMenu(W, H);
                break;
            case State.Card:
                GUI.Label(new Rect(0, H * 0.1f, W, H * 0.1f), L.from + "  to  " + L.to, big);
                break;
            case State.Play:
            case State.Dock:
                DrawProgress(W, H);
                break;
            case State.Docked:
                GUI.Label(new Rect(0, H * 0.2f, W, H * 0.1f), L.to, big);
                if (levelIndex != PuppyLeg) GUI.Label(new Rect(0, H * 0.31f, W, H * 0.06f), "The souls step into the light.", mid);
                GUI.Label(new Rect(0, H * 0.82f, W, H * 0.06f), Blink() ? "Press Z or tap to continue" : "", mid);
                break;
            case State.Failed:
                if (stateTime > 1.2f)
                {
                    GUI.Label(new Rect(0, H * 0.24f, W, H * 0.1f), "You failed!", big);
                    GUI.Label(new Rect(0, H * 0.35f, W, H * 0.06f), boat.FailReason, mid);
                    if (GUI.Button(new Rect(W / 2 - 140 * H / 720f, H * 0.5f, 280 * H / 720f, 70 * H / 720f), "Try again", btn))
                        Retry();
                }
                break;
            case State.End:
                GUI.Label(new Rect(0, H * 0.3f, W, H * 0.1f), "The last crossing.", big);
                GUI.Label(new Rect(0, H * 0.42f, W, H * 0.06f), "To be continued...", mid);
                break;
        }

        if (paused)
        {
            GUI.Box(new Rect(W / 2 - 220 * H / 720f, H * 0.35f, 440 * H / 720f, 200 * H / 720f), "");
            GUI.Label(new Rect(0, H * 0.4f, W, H * 0.08f), "Paused", big);
            GUI.Label(new Rect(0, H * 0.5f, W, H * 0.06f), "Esc to resume", mid);
        }

        if (confirmOpen)
        {
            GUI.Box(new Rect(W / 2 - 220 * H / 720f, H * 0.35f, 440 * H / 720f, 200 * H / 720f), "");
            GUI.Label(new Rect(0, H * 0.38f, W, H * 0.08f), "Restart level?  Y / N", mid);
            float bw = 150 * H / 720f, bh = 60 * H / 720f;
            if (GUI.Button(new Rect(W / 2 - bw - 10, H * 0.48f, bw, bh), "Yes", btn)) { confirmOpen = false; Time.timeScale = 1f; ResetLevel(levelIndex, State.Play); }
            if (GUI.Button(new Rect(W / 2 + 10, H * 0.48f, bw, bh), "No", btn)) { confirmOpen = false; Time.timeScale = 1f; }
        }

        if (GameInput.TouchMode && (state == State.Play || state == State.Dock))
        {
            GUI.Box(GameInput.LeftBtn, "<", btn);
            GUI.Box(GameInput.RightBtn, ">", btn);
            GUI.Box(GameInput.SlashBtn, "Slash", btn);
            GUI.Box(GameInput.ActBtn, "Act", btn);
            if (GUI.Button(new Rect(W - 90 * H / 720f, 10, 80 * H / 720f, 50 * H / 720f), "R", btn)) { confirmOpen = true; Time.timeScale = 0f; }
        }
    }

    bool Blink() => Mathf.Repeat(Time.unscaledTime, 1.2f) < 0.8f;

    void DrawProgress(float W, float H)
    {
        float u = H / 720f;
        float bw = W * 0.4f, x0 = (W - bw) / 2f, y = 24 * u;
        small.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(x0 - 270 * u, y - 14 * u, 260 * u, 30 * u), L.from, small);
        small.alignment = TextAnchor.MiddleLeft;
        GUI.Label(new Rect(x0 + bw + 10 * u, y - 14 * u, 260 * u, 30 * u), L.to, small);
        small.alignment = TextAnchor.MiddleCenter;
        var old = GUI.color;
        GUI.color = new Color(1, 1, 1, 0.25f);
        GUI.DrawTexture(new Rect(x0, y, bw, 3 * u), Texture2D.whiteTexture);
        GUI.color = new Color(0.75f, 0.95f, 1f, 0.9f);
        float p = Mathf.Clamp01(elapsed / L.duration);
        GUI.DrawTexture(new Rect(x0, y, bw * p, 3 * u), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x0 + bw * p - 6 * u, y - 5 * u, 12 * u, 12 * u), Texture2D.whiteTexture);
        if (boat.SinkMeter > 0.01f)
        {
            GUI.color = new Color(0.4f, 0.8f, 1f, 0.9f);
            GUI.Label(new Rect(0, y + 14 * u, W, 30 * u), "Sinking " + Mathf.RoundToInt(boat.SinkMeter * 100f) + "%", small);
        }
        GUI.color = old;
    }
}
