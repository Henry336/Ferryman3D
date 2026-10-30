using UnityEngine;

// One wave function shared by C# (boat bob/tilt) and Water.shader. Keep both formulas identical.
public static class Waves
{
    public static float Amp = 0.25f;
    public static float Scroll;
    public static float Time;

    public static float Height(float x, float z)
    {
        float X = x + Scroll, t = Time;
        return Amp * (0.55f * Mathf.Sin(0.55f * X + 1.3f * t)
                    + 0.30f * Mathf.Sin(1.1f * X + 0.4f * z - 1.9f * t)
                    + 0.15f * Mathf.Sin(2.3f * X - 0.7f * z + 2.6f * t));
    }

    public static void Push()
    {
        Shader.SetGlobalFloat("_WaveAmp", Amp);
        Shader.SetGlobalFloat("_WaveScroll", Scroll);
        Shader.SetGlobalFloat("_WaveTime", Time);
    }
}
