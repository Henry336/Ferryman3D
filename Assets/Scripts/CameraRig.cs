using UnityEngine;

// Side-on follow camera with gentle sway, intensity pull-back and trauma-based shake.
public class CameraRig : MonoBehaviour
{
    static float trauma;
    public static void Shake(float amount) { trauma = Mathf.Min(1f, trauma + amount); }

    public Transform target;
    public Vector3 offset = new Vector3(0.6f, 1.6f, -15f);
    [Tooltip("Extra distance when the boat is near tipping.")] public float pullBack = 2.5f;
    public float intensity; // 0..1, set by Game

    Vector3 vel;
    float pull;

    void LateUpdate()
    {
        float t = Time.time;
        pull = Mathf.Lerp(pull, intensity, 1.5f * Time.deltaTime);
        var ty = target ? target.position.y * 0.4f : 0f;
        var goal = new Vector3(offset.x, offset.y + ty + pull * 0.6f, offset.z - pull * pullBack);
        goal += new Vector3(Mathf.Sin(t * 0.37f) * 0.15f, Mathf.Sin(t * 0.53f) * 0.1f, 0f);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref vel, 0.35f);

        var look = new Vector3(transform.position.x - 0.1f, ty + 0.3f, 0f);
        var rot = Quaternion.LookRotation(look - transform.position);
        float s = trauma * trauma;
        if (s > 0f)
        {
            float n = t * 30f;
            rot *= Quaternion.Euler((Mathf.PerlinNoise(n, 0f) - 0.5f) * 3f * s,
                                    (Mathf.PerlinNoise(0f, n) - 0.5f) * 3f * s,
                                    (Mathf.PerlinNoise(n, n) - 0.5f) * 4f * s);
            transform.position += new Vector3((Mathf.PerlinNoise(n, 7f) - 0.5f) * 0.6f * s, (Mathf.PerlinNoise(7f, n) - 0.5f) * 0.6f * s, 0f);
        }
        transform.rotation = rot;
        trauma = Mathf.Max(0f, trauma - Time.unscaledDeltaTime * 1.4f);
    }
}
