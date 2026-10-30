using UnityEngine;

// The Reaper: eased walking on the deck, facing, downhill slide, scythe swing, emotes.
public class Reaper : MonoBehaviour
{
    public Boat boat;
    public Transform visual, scythePivot, body;
    public Renderer[] eyes;
    public Material trailMat;
    public Animation anim;   // legacy clips from the Meshy rig (null with placeholder art)
    public string Emote;     // set by Game for story beats, e.g. "Happy_Sway"; empty = normal locomotion
    string playing;
    Transform handBone;

    public float LocalX, Velocity;
    public int Facing = 1;
    public bool Controllable = true;

    float cooldown, slashTimer = -1f, floatTime, idle, slashBuffer, stepTimer;
    Vector3 floatVel;
    GameObject trail;

    public void ResetState(float startX)
    {
        LocalX = startX; Velocity = 0f; Facing = 1;
        cooldown = 0f; slashTimer = -1f; floatTime = 0f; idle = 0f;
        Controllable = true;
        if (scythePivot) scythePivot.localRotation = Quaternion.identity;
        if (trail) trail.SetActive(false);
        Place();
    }

    public float Torque(float cos) => -Tuning.I.reaperWeight * LocalX * cos;

    // Returns true on the frame a swing starts.
    public bool Step(float input, bool slashPressed, float dt)
    {
        var T = Tuning.I;
        cooldown -= dt;
        bool swung = false;
        if (!boat.Failed)
        {
            if (!Controllable) input = 0f;
            if (input != 0f) Facing = input > 0f ? 1 : -1;
            float target = input * T.maxSpeed;
            bool speeding = input != 0f && Velocity * input >= 0f;
            Velocity = Mathf.MoveTowards(Velocity, target, (speeding ? T.accel : T.decel) * dt);

            float a = Mathf.Sign(boat.Angle) * Mathf.Max(0f, Mathf.Abs(boat.Angle) - T.slideDeadZone);
            float slide = -Mathf.Sin(a * Mathf.Deg2Rad) * 9.81f * T.slideFactor;
            LocalX += (Velocity + slide) * dt;
            float lim = T.halfLength - T.edgePadding;
            if (LocalX > lim) { LocalX = lim; if (Velocity > 0) Velocity = 0; }
            if (LocalX < -lim) { LocalX = -lim; if (Velocity < 0) Velocity = 0; }

            // Buffer presses made during the cooldown so mashing never drops a swing.
            if (slashPressed) slashBuffer = 0.15f;
            if (slashBuffer > 0f && Controllable && cooldown <= 0f)
            {
                cooldown = T.slashCooldown; slashTimer = 0f; swung = true; slashBuffer = 0f;
            }
            slashBuffer -= dt;
            idle = Mathf.Abs(Velocity) < 0.05f ? idle + dt : 0f;
            stepTimer -= dt;
            if (Mathf.Abs(Velocity) > 0.6f && stepTimer <= 0f) { Sfx.Play("sfx_footstep", 0.35f, 0.1f); stepTimer = 0.34f; }
        }
        AnimateSlash(dt);
        return swung;
    }

    void AnimateSlash(float dt)
    {
        if (slashTimer < 0f) return;
        slashTimer += dt;
        float p = slashTimer / Tuning.I.slashDuration;
        if (p >= 1f)
        {
            slashTimer = -1f;
            if (scythePivot) scythePivot.localRotation = Quaternion.identity;
            if (trail) trail.SetActive(false);
            return;
        }
        float swing = p < 0.35f ? 1f - Mathf.Pow(1f - p / 0.35f, 3f) : 1f - Mathf.SmoothStep(0f, 1f, (p - 0.35f) / 0.65f);
        if (scythePivot) scythePivot.localRotation = Quaternion.Euler(0f, 0f, -115f * swing);
        if (trail)
        {
            trail.SetActive(p < 0.5f);
            if (trailMat) trailMat.SetColor("_Color", new Color(0.7f, 0.95f, 1f, 1f - p * 2f));
        }
    }

    public void SetTrail(GameObject t) { trail = t; trail.SetActive(false); }

    void LateUpdate()
    {
        if (!boat) return;
        float t = Time.time;
        if (boat.Failed)
        {
            floatTime += Time.deltaTime;
            var target = new Vector3(transform.position.x, Waves.Height(transform.position.x, 0f) - 0.55f + Mathf.Sin(floatTime * 2.2f) * 0.06f, 0f);
            transform.position = Vector3.SmoothDamp(transform.position, target, ref floatVel, 0.6f);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(floatTime * 1.3f) * 6f);
        }
        else Place();

        if (visual)
        {
            visual.localScale = new Vector3(Facing, 1f, 1f);
            visual.localRotation = Quaternion.Euler(0f, 0f, -Velocity * 2.5f);
        }
        if (anim && scythePivot)
        {
            if (!handBone)
                foreach (var bone in anim.GetComponentsInChildren<Transform>())
                {
                    string n = bone.name.ToLowerInvariant();
                    if (n.Contains("righthand") || n.Contains("hand_r") || n.Contains("r_hand")) { handBone = bone; break; }
                }
            // Keep the scythe in the animated hand (stays on the fixed grip if no bone was found).
            if (handBone) scythePivot.position = handBone.position;
        }
        if (anim)
        {
            string want = boat.Failed ? "Fall_Down"
                : slashTimer >= 0f ? "Left_Slash"
                : !string.IsNullOrEmpty(Emote) ? Emote
                : Mathf.Abs(Velocity) > 0.3f ? "Walk"
                : idle > 8f && idle % 14f < 3f ? "Catching_Breath" : "Idle"; // an occasional weary breather
            PlayClip(want, want == "Left_Slash" ? 0.05f : 0.2f);
        }
        else if (body)
        {
            // Breathing / walk squash.
            float walk = Mathf.Abs(Velocity) / Tuning.I.maxSpeed;
            float b = Mathf.Sin(t * (walk > 0.1f ? 12f : 2f)) * (walk > 0.1f ? 0.035f : 0.02f);
            body.localScale = new Vector3(1f - b, 1f + b, 1f - b);
        }
    }

    // Clip names come from Meshy (e.g. "Armature|Left_Slash"), so match by substring.
    void PlayClip(string key, float fade)
    {
        if (playing == key) return;
        foreach (AnimationState s in anim)
        {
            if (s.name.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            bool once = key == "Left_Slash" || key == "Fall_Down";
            s.wrapMode = once ? WrapMode.ClampForever : WrapMode.Loop;
            if (key == "Left_Slash") { s.speed = s.length / Mathf.Max(0.05f, Tuning.I.slashDuration * 1.6f); anim.Rewind(s.name); }
            anim.CrossFade(s.name, fade);
            playing = key;
            return;
        }
    }

    void Place()
    {
        transform.position = boat.DeckPoint(LocalX);
        transform.rotation = Quaternion.Euler(0f, 0f, boat.Angle * 0.4f);
    }

    public float IdleTime => idle;
}
