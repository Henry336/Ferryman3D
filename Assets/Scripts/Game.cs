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
    float elapsed, stateTime, handTimer, speedNow;
    bool confirmOpen, paused;

    Boat boat;
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

    void Awake()
    {
        Application.targetFrameRate = 60;
        GetComponent<Tuning>(); // Awake sets Tuning.I
        if (!Tuning.I) Tuning.I = GetComponent<Tuning>();
        levels = LevelDef.All();
        BuildWorld();
        ApplyLevelLook(levels[0]);
        ResetLevel(0, State.Title);
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
        cam.target = boat.transform;
    }

    void BuildBoat()
    {
        var root = new GameObject("Boat");
        boat = root.AddComponent<Boat>();
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
    void ResetLevel(int index, State next)
    {
        levelIndex = index;
        foreach (var h in hands) if (h) Destroy(h.gameObject);
        hands.Clear();
        foreach (var s in souls) if (s) Destroy(s.gameObject);
        souls.Clear();
        boat.ResetState();
        reaper.ResetState(-0.6f);
        Waves.Amp = L.waveAmp;
        elapsed = 0f; handTimer = Random.Range(L.handInterval.x, L.handInterval.y) * 0.6f;
        speedNow = Tuning.I.baseSpeed * L.speed;
        pierX = 60f; pier.position = new Vector3(pierX, 0f, 0f);
        ApplyLevelLook(L);
        SpawnSouls();
        SetState(next);
    }

    void SpawnSouls()
    {
        int total = L.calmSouls + L.vengefulSouls;
        float lim = Tuning.I.halfLength - 0.9f;
        for (int i = 0; i < total; i++)
        {
            bool veng = i >= L.calmSouls;
            var col = veng ? new Color(1f, 0.32f, 0.25f, 1f) : new Color(0.75f, 0.92f, 1f, 1f);
            var mat = Art.Glow(col, 0.55f, 1.4f);
            var g = Art.Obj(veng ? "VengefulSoul" : "Soul", null, Art.Sphere, mat);
            g.transform.localScale = new Vector3(0.42f, 0.55f, 0.42f);
            var tail = Art.Obj("Tail", g.transform, MeshKit.Cone(0.5f, 0.05f, 1.2f, 12), mat).transform;
            tail.localRotation = Quaternion.Euler(180f, 0f, 0f);
            tail.localPosition = new Vector3(0, 0.2f, 0);
            var s = g.AddComponent<Soul>();
            s.mat = mat;
            float x = Mathf.Lerp(-lim, lim, total == 1 ? 0.5f : i / (float)(total - 1));
            if (Mathf.Abs(x - reaper.LocalX) < 0.6f) x += 0.8f;
            s.Init(boat, veng, Mathf.Clamp(x, -lim, lim), col);
            souls.Add(s);
        }
    }

    void SetState(State s) { state = s; stateTime = 0f; }

    void Update()
    {
        GameInput.Update();
        float dt = Time.deltaTime;
        stateTime += Time.unscaledDeltaTime;

        if (confirmOpen)
        {
            if (GameInput.Yes) { confirmOpen = false; Time.timeScale = 1f; ResetLevel(levelIndex, State.Card); }
            else if (GameInput.No) { confirmOpen = false; Time.timeScale = 1f; }
            return;
        }
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
                if (GameInput.Confirm || TappedAnywhere()) SetState(State.Card);
                break;
            case State.Card:
                StepIdleBoat(dt);
                if (stateTime > 2.2f || (stateTime > 0.4f && (GameInput.Confirm || TappedAnywhere()))) SetState(State.Play);
                break;
            case State.Play: StepPlay(dt); break;
            case State.Dock: StepDock(dt); break;
            case State.Docked:
                StepIdleBoat(dt);
                if (stateTime > 1.2f && (GameInput.Confirm || TappedAnywhere()))
                {
                    if (levelIndex + 1 < levels.Length) ResetLevel(levelIndex + 1, State.Card);
                    else SetState(State.End);
                }
                break;
            case State.Failed:
                StepBoatOnly(dt);
                if (stateTime > 1.5f && (GameInput.Confirm || GameInput.Restart)) ResetLevel(levelIndex, State.Card);
                break;
            case State.End:
                StepIdleBoat(dt);
                break;
        }

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
        float torque = reaper.Torque(cos);
        foreach (var s in souls) { s.Step(dt); torque += s.Torque(cos); }
        int middle = 0;
        foreach (var h in hands) { torque += h.Torque(cos); middle += h.MiddleCount; }
        boat.Step(torque, middle, dt);

        if (boat.Failed) { SetState(State.Failed); return; }
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
        if (hit > 0) StartCoroutine(HitPause());
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
    GUIStyle big, mid, small, btn;

    void Styles()
    {
        float u = Screen.height / 720f;
        if (big == null)
        {
            big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            mid = new GUIStyle(big) { fontStyle = FontStyle.Normal };
            small = new GUIStyle(mid);
            btn = new GUIStyle(GUI.skin.button);
        }
        big.fontSize = Mathf.RoundToInt(64 * u);
        mid.fontSize = Mathf.RoundToInt(30 * u);
        small.fontSize = Mathf.RoundToInt(20 * u);
        btn.fontSize = Mathf.RoundToInt(28 * u);
        big.normal.textColor = mid.normal.textColor = small.normal.textColor = new Color(0.85f, 0.95f, 1f, 0.95f);
    }

    void OnGUI()
    {
        Styles();
        float W = Screen.width, H = Screen.height;
        switch (state)
        {
            case State.Title:
                GUI.Label(new Rect(0, H * 0.06f, W, H * 0.15f), "FERRYMAN", big);
                GUI.Label(new Rect(0, H * 0.19f, W, H * 0.06f), "One last crossing.", mid);
                GUI.Label(new Rect(0, H * 0.82f, W, H * 0.06f), Blink() ? "Press Z or tap to begin" : "", mid);
                GUI.Label(new Rect(0, H * 0.89f, W, H * 0.05f), "Arrows  move     Z  slash / poke     R  restart", small);
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
                GUI.Label(new Rect(0, H * 0.31f, W, H * 0.06f), "The souls step into the light.", mid);
                GUI.Label(new Rect(0, H * 0.82f, W, H * 0.06f), Blink() ? "Press Z or tap to continue" : "", mid);
                break;
            case State.Failed:
                if (stateTime > 1.2f)
                {
                    GUI.Label(new Rect(0, H * 0.24f, W, H * 0.1f), "You failed!", big);
                    GUI.Label(new Rect(0, H * 0.35f, W, H * 0.06f), boat.FailReason, mid);
                    if (GUI.Button(new Rect(W / 2 - 140 * H / 720f, H * 0.5f, 280 * H / 720f, 70 * H / 720f), "Try again", btn))
                        ResetLevel(levelIndex, State.Card);
                }
                break;
            case State.End:
                GUI.Label(new Rect(0, H * 0.3f, W, H * 0.1f), "The last crossing.", big);
                GUI.Label(new Rect(0, H * 0.42f, W, H * 0.06f), "To be continued...", mid);
                break;
        }

        if (confirmOpen)
        {
            GUI.Box(new Rect(W / 2 - 220 * H / 720f, H * 0.35f, 440 * H / 720f, 200 * H / 720f), "");
            GUI.Label(new Rect(0, H * 0.38f, W, H * 0.08f), "Restart level?  Y / N", mid);
            float bw = 150 * H / 720f, bh = 60 * H / 720f;
            if (GUI.Button(new Rect(W / 2 - bw - 10, H * 0.48f, bw, bh), "Yes", btn)) { confirmOpen = false; Time.timeScale = 1f; ResetLevel(levelIndex, State.Card); }
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
        float bw = W * 0.5f, x0 = (W - bw) / 2f, y = 24 * u;
        GUI.Label(new Rect(x0 - 160 * u, y - 14 * u, 150 * u, 30 * u), L.from, small);
        GUI.Label(new Rect(x0 + bw + 10 * u, y - 14 * u, 150 * u, 30 * u), L.to, small);
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
