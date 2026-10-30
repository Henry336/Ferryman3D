using System.Collections.Generic;
using UnityEngine;

// 2-3 ghostly arms that rise from the river and grab the rim at the front, back or middle.
public class HandGroup : MonoBehaviour
{
    public enum Slot { Back, Middle, Front }
    enum State { Rising, Holding, Hit, Leaving }

    public Slot slot;
    public float LocalX;
    public int Count;
    public bool Done;

    Boat boat;
    State state;
    float timer, ramp;
    readonly List<Transform> arms = new List<Transform>();
    Material mat;
    static readonly Color Ghost = new Color(0.62f, 0.78f, 0.86f, 1f);

    public bool Grabbing => state == State.Holding || state == State.Hit;
    public bool Hittable => state == State.Rising || state == State.Holding;
    public int MiddleCount => slot == Slot.Middle && Grabbing ? Count : 0;

    public void Init(Boat b, Slot s, float localX, int count)
    {
        boat = b; slot = s; LocalX = localX; Count = count;
        transform.SetParent(b.transform, false);
        transform.localPosition = new Vector3(localX, Tuning.I.rimY, 0f);
        mat = Art.Glow(Ghost, 0.45f, 1.6f);
        for (int i = 0; i < count; i++)
        {
            var arm = new GameObject("Arm").transform;
            arm.SetParent(transform, false);
            float off = (i - (count - 1) * 0.5f) * 0.38f;
            arm.localPosition = new Vector3(off, -3.2f, (i % 2 == 0 ? -0.75f : 0.75f) + (i * 0.05f));
            var fore = Art.Obj("Fore", arm, Art.Capsule, mat).transform;
            fore.localScale = new Vector3(0.16f, 1.4f, 0.16f);
            fore.localPosition = new Vector3(0f, -1.3f, 0f);
            var palm = Art.Obj("Palm", arm, Art.Sphere, mat).transform;
            palm.localScale = new Vector3(0.32f, 0.22f, 0.3f);
            palm.localPosition = new Vector3(0f, 0.05f, 0f);
            for (int f = 0; f < 3; f++)
            {
                var fin = Art.Obj("Finger", arm, Art.Capsule, mat).transform;
                fin.localScale = new Vector3(0.06f, 0.13f, 0.06f);
                fin.localPosition = new Vector3(-0.09f + f * 0.09f, 0.18f, (arm.localPosition.z < 0 ? 0.12f : -0.12f));
                fin.localRotation = Quaternion.Euler(arm.localPosition.z < 0 ? 50f : -50f, 0f, 0f);
            }
            arms.Add(arm);
        }
        state = State.Rising; timer = 0f;
        CameraRig.Shake(0.12f);
    }

    // Pull acts like extra weight at this end.
    public float Torque(float cos)
    {
        if (slot == Slot.Middle || !Grabbing) return 0f;
        return -Count * Tuning.I.handPull * ramp * LocalX * cos;
    }

    public void Hit()
    {
        if (!Hittable) return;
        state = State.Hit; timer = 0f;
        mat.SetColor("_Color", new Color(1f, 0.15f, 0.12f, 1f));
        ramp = 1.3f; // one last pull
        CameraRig.Shake(0.22f);
    }

    public void ForceLeave() { if (state != State.Leaving) { state = State.Leaving; timer = 0f; } }

    void Update()
    {
        if (!boat) return;
        float dt = Time.deltaTime;
        timer += dt;
        var T = Tuning.I;
        switch (state)
        {
            case State.Rising:
            {
                float p = Mathf.Clamp01(timer / T.handRiseTime);
                float e = 1f - Mathf.Pow(1f - p, 3f);
                SetArmHeight(Mathf.Lerp(-3.2f, 0f, e));
                if (p >= 1f) { state = State.Holding; timer = 0f; CameraRig.Shake(0.18f); }
                break;
            }
            case State.Holding:
                ramp = Mathf.Clamp01(timer / T.handPullRamp);
                // Straining tremble.
                for (int i = 0; i < arms.Count; i++)
                    arms[i].localPosition = new Vector3(arms[i].localPosition.x, Mathf.Sin(Time.time * 18f + i) * 0.02f - ramp * 0.06f, arms[i].localPosition.z);
                break;
            case State.Hit:
            {
                float jolt = Mathf.Sin(timer * 40f) * 0.08f * (1f - timer / 0.4f);
                for (int i = 0; i < arms.Count; i++)
                    arms[i].localPosition = new Vector3(arms[i].localPosition.x + jolt * 0.2f, -0.12f + jolt, arms[i].localPosition.z);
                if (timer > 0.4f) { state = State.Leaving; timer = 0f; ramp = 0f; }
                break;
            }
            case State.Leaving:
            {
                float p = Mathf.Clamp01(timer / 0.6f);
                SetArmHeight(Mathf.Lerp(-0.1f, -3.4f, p * p));
                var c = mat.GetColor("_Color"); c.a = 1f - p; mat.SetColor("_Color", c);
                if (p >= 1f) { Done = true; Destroy(gameObject); }
                break;
            }
        }
    }

    void SetArmHeight(float y)
    {
        foreach (var a in arms) a.localPosition = new Vector3(a.localPosition.x, y, a.localPosition.z);
    }
}
