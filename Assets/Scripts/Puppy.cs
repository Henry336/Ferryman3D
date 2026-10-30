using UnityEngine;

// The skeleton puppy: hops aboard at the end of leg 3, then trots at the Reaper's heels for the rest of the trip.
public class Puppy : MonoBehaviour
{
    public Reaper reaper;
    public Boat boat;
    public Transform model;
    public bool Aboard;

    float localX, hopT = -1f, whimperCd;
    Vector3 hopFrom;
    int facing = 1;

    const float HopTime = 1.1f;

    // Leaps from a world point (the pier) onto the deck beside the Reaper.
    public void Arrive(Vector3 from)
    {
        gameObject.SetActive(true);
        hopFrom = from; hopT = 0f; Aboard = true;
        localX = Mathf.Clamp(reaper.LocalX - 0.9f, -Tuning.I.halfLength + 0.8f, Tuning.I.halfLength - 0.8f);
        transform.position = from;
    }

    // Skip the leap (player pressed on early, or a retry in a later leg).
    public void Settle()
    {
        gameObject.SetActive(true);
        Aboard = true; hopT = -1f;
        localX = Mathf.Clamp(reaper.LocalX - 0.9f * reaper.Facing, -Tuning.I.halfLength + 0.8f, Tuning.I.halfLength - 0.8f);
    }

    void LateUpdate()
    {
        if (!Aboard || !boat) return;
        float dt = Time.deltaTime;
        var deck = boat.DeckPoint(localX);

        if (hopT >= 0f)
        {
            hopT += dt;
            float p = Mathf.Clamp01(hopT / HopTime);
            var pos = Vector3.Lerp(hopFrom, deck, p);
            pos.y += Mathf.Sin(p * Mathf.PI) * 1.6f;
            transform.position = pos;
            facing = deck.x < hopFrom.x ? -1 : 1;
            if (p >= 1f) { hopT = -1f; Sfx.Play("sfx_dog_bark"); CameraRig.Shake(0.05f); }
        }
        else if (boat.Failed)
        {
            // Bob in the water next to the Reaper.
            var target = reaper.transform.position + new Vector3(-0.8f, 0.05f, 0.3f);
            transform.position = Vector3.Lerp(transform.position, target, dt * 3f);
        }
        else
        {
            // Trot to stay a step behind the Reaper; little hops while moving.
            float want = reaper.LocalX - 0.9f * reaper.Facing;
            want = Mathf.Clamp(want, -Tuning.I.halfLength + 0.6f, Tuning.I.halfLength - 0.6f);
            float before = localX;
            localX = Mathf.MoveTowards(localX, want, dt * 3.2f);
            float speed = Mathf.Abs(localX - before) / Mathf.Max(dt, 1e-4f);
            if (speed > 0.2f) facing = localX > before ? 1 : -1;
            float hop = speed > 0.2f ? Mathf.Abs(Mathf.Sin(Time.time * 11f)) * 0.12f : 0f;
            transform.position = boat.DeckPoint(localX) + Vector3.up * hop;

            whimperCd -= dt;
            if (boat.TiltFraction > 0.8f && whimperCd <= 0f) { Sfx.Play("sfx_dog_whimper", 0.7f); whimperCd = 6f; }
        }

        transform.rotation = Quaternion.Euler(0f, 0f, boat.Angle * 0.5f);
        if (model)
        {
            // Model faces +Z; turn it toward travel and three-quarters to the camera, with a small breathing squash.
            model.localRotation = Quaternion.Slerp(model.localRotation, Quaternion.Euler(0f, facing > 0 ? 120f : -120f, 0f), dt * 10f);
            float b = Mathf.Sin(Time.time * 3f) * 0.025f;
            model.localScale = new Vector3(1f + b, 1f - b, 1f + b);
        }
    }

    public float Torque(float cos) => Aboard && hopT < 0f ? -0.15f * localX * cos : 0f;
}
