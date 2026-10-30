using UnityEngine;

// A passenger soul on the deck. Calm souls drift; vengeful souls walk to the low end on purpose.
public class Soul : MonoBehaviour
{
    public Boat boat;
    public bool vengeful;
    public float LocalX;
    public Material mat;
    public Color baseColor;

    float targetX, retarget, stun, phase, leaving = -1f, lastX;

    public float Weight => vengeful ? Tuning.I.vengefulWeight : Tuning.I.calmWeight;
    public float Torque(float cos) => leaving >= 0f ? 0f : -Weight * LocalX * cos;
    public bool Gone => leaving > 1.6f;

    public void Init(Boat b, bool veng, float x, Color c)
    {
        boat = b; vengeful = veng; LocalX = targetX = x; baseColor = c;
        phase = Random.value * 10f; retarget = Random.Range(1f, 3f);
    }

    public void Poke(int dir)
    {
        targetX = 0f;
        stun = Tuning.I.pokeStun;
        Sfx.Play("sfx_soul_poke", 0.8f, 0.2f);
        LocalX += dir * 0.25f;
        retarget = stun;
    }

    public void Leave() { if (leaving < 0f) leaving = 0f; }

    public void Step(float dt)
    {
        var T = Tuning.I;
        float lim = T.halfLength - 0.7f;
        if (leaving >= 0f)
        {
            leaving += dt;
            transform.position += Vector3.up * dt * 1.6f;
            float a = Mathf.Clamp01(1f - leaving / 1.6f);
            mat.SetColor("_Color", baseColor * new Color(1, 1, 1, a));
            return;
        }

        stun -= dt;
        retarget -= dt;
        if (retarget <= 0f)
        {
            if (vengeful && stun <= 0f)
            {
                // Head to whichever end is already low, or pick an end.
                float s = Mathf.Abs(boat.Angle) > 2f ? -Mathf.Sign(boat.Angle) : (Random.value < 0.5f ? -1f : 1f);
                targetX = s * lim;
                retarget = Random.Range(2.5f, 4.5f);
            }
            else
            {
                targetX = Random.Range(-lim, lim);
                retarget = Random.Range(3f, 6f);
            }
        }
        float speed = vengeful && stun <= 0f ? T.vengefulSpeed : T.calmSpeed;
        LocalX = Mathf.MoveTowards(LocalX, targetX, speed * dt);
        LocalX = Mathf.Clamp(LocalX, -lim, lim);

        float bob = Mathf.Sin(Time.time * 2.3f + phase) * 0.08f;
        float moving = Mathf.Abs(LocalX - lastX) / Mathf.Max(dt, 1e-4f);
        lastX = LocalX;
        float hop = moving > 0.1f ? Mathf.Abs(Mathf.Sin(Time.time * 9f + phase)) * 0.1f : 0f;
        transform.position = boat.DeckPoint(LocalX) + Vector3.up * (0.95f + bob + hop) + new Vector3(0, 0, 0.2f);
        float sway = Mathf.Sin(Time.time * 1.7f + phase) * 6f + boat.Angle * 0.6f + (stun > 0f ? Mathf.Sin(Time.time * 30f) * 8f : 0f);
        transform.rotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 0.6f + phase) * 25f, sway);
        float br = Mathf.Sin(Time.time * 2.6f + phase) * 0.04f;
        transform.localScale = new Vector3(1f + br, 1f - br, 1f + br);

        Color c = baseColor;
        if (vengeful && stun > 0f) c = Color.Lerp(baseColor, new Color(0.75f, 0.85f, 1f, baseColor.a), 0.6f);
        mat.SetColor("_Color", c);
    }
}
