using UnityEngine;

// Seesaw tilt with simple math (no Rigidbody). Positive Angle lifts the front (+X).
public class Boat : MonoBehaviour
{
    public float Angle, AngVel, SinkMeter;
    public bool Failed;
    public string FailReason = "";
    public float TiltFraction => Mathf.Abs(Angle) / Mathf.Max(0.01f, Tuning.I.maxSafeAngle);
    public bool Docked; // can't fail while docking

    public Material hullMat;
    public Transform visual;

    float sinkOffset, failTimer, failStartAngle, failStartY, floodTimer;
    public float FloodFraction => floodTimer / Mathf.Max(0.01f, Tuning.I.floodGrace);
    Color outlineSafe = new Color(0.02f, 0.02f, 0.03f);

    public void ResetState()
    {
        Angle = AngVel = SinkMeter = sinkOffset = floodTimer = 0f;
        Failed = false; Docked = false; FailReason = "";
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        if (hullMat) hullMat.SetColor("_OutlineColor", outlineSafe);
    }

    // torque: sum of weight x offset contributions (already signed). middleHands: hands grabbing the middle.
    public void Step(float torque, int middleHands, float dt)
    {
        var T = Tuning.I;
        if (Failed) { StepFailing(dt); return; }

        float L = T.halfLength;
        float waveT = T.waveTorque * (Waves.Height(L, 0f) - Waves.Height(-L, 0f));
        float lev = T.leveling * (Docked ? 3f : 1f);
        float acc = (torque + waveT) * T.torqueToAccel - lev * Angle;
        AngVel = (AngVel + acc * dt) * Mathf.Exp(-T.angularDamping * dt);
        AngVel = Mathf.Clamp(AngVel, -T.maxAngularSpeed, T.maxAngularSpeed);
        Angle += AngVel * dt;

        if (middleHands > 0) SinkMeter += middleHands * T.sinkFillPerHand * dt;
        else SinkMeter -= T.sinkDrain * dt;
        SinkMeter = Mathf.Clamp01(SinkMeter);
        sinkOffset = Mathf.MoveTowards(sinkOffset, SinkMeter * T.sinkDepth, 0.6f * dt);

        if (Mathf.Abs(Angle) <= T.maxSafeAngle) floodTimer = Mathf.Max(0f, floodTimer - dt * 2f);
        if (Docked) Angle = Mathf.Clamp(Angle, -T.maxSafeAngle * 0.8f, T.maxSafeAngle * 0.8f);
        else if (Mathf.Abs(Angle) > T.maxSafeAngle)
        {
            // Taking on water: recoverable until the grace window runs out.
            if (floodTimer == 0f) { Sfx.Play("sfx_sink_warn", 0.9f, 0.8f); CameraRig.Shake(0.15f); }
            floodTimer += dt;
            if (floodTimer >= T.floodGrace) Fail("The boat tipped too far and flooded.");
        }
        else if (SinkMeter >= 1f) Fail("The hands dragged the boat under.");

        float bob = (Waves.Height(L * 0.6f, 0f) + Waves.Height(-L * 0.6f, 0f)) * 0.5f;
        float lift = T.tiltLift * Mathf.Abs(Mathf.Sin(Angle * Mathf.Deg2Rad));
        transform.position = new Vector3(0f, bob - sinkOffset + lift - FloodFraction * 0.25f, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, Angle);
        UpdateOutline();
    }

    void Fail(string reason)
    {
        Failed = true; FailReason = reason;
        failTimer = 0f; failStartAngle = Angle; failStartY = transform.position.y;
        CameraRig.Shake(0.5f);
    }

    void StepFailing(float dt)
    {
        failTimer += dt;
        float p = Mathf.Clamp01(failTimer / 3f);
        float dir = Mathf.Abs(failStartAngle) > Tuning.I.maxSafeAngle * 0.8f ? Mathf.Sign(failStartAngle) : 0f;
        Angle = failStartAngle + dir * 14f * Mathf.SmoothStep(0, 1, p);
        transform.position = new Vector3(0f, failStartY - 3.5f * p * p, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, Angle);
    }

    void UpdateOutline()
    {
        if (!hullMat) return;
        float f = TiltFraction;
        Color c = outlineSafe;
        if (f > 0.5f) c = Color.Lerp(outlineSafe, new Color(1f, 0.55f, 0.1f), Mathf.InverseLerp(0.5f, 0.8f, f));
        if (f > 0.8f)
        {
            c = Color.Lerp(new Color(1f, 0.55f, 0.1f), new Color(1f, 0.12f, 0.08f), Mathf.InverseLerp(0.8f, 1f, f));
            c = Color.Lerp(c, Color.white, (Mathf.Sin(Time.time * 14f) * 0.5f + 0.5f) * 0.3f);
        }
        hullMat.SetColor("_OutlineColor", c);
    }

    public Vector3 DeckPoint(float localX) => transform.TransformPoint(new Vector3(localX, Tuning.I.deckY, 0f));
}
