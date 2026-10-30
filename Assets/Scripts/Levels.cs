using UnityEngine;

// Per-leg settings. Edit here (or move to ScriptableObjects later).
[System.Serializable]
public class LevelDef
{
    public string from, to;
    public float duration = 60f;
    [Tooltip("Wave height multiplier.")] public float waveAmp = 0.2f;
    [Tooltip("Seconds between hand grabs (min, max).")] public Vector2 handInterval = new Vector2(6f, 9f);
    public int maxHandsPerGroup = 2;
    public bool middleHands;
    public int calmSouls = 2, vengefulSouls;
    [Tooltip("River speed multiplier.")] public float speed = 1f;
    public Color skyTop, skyBottom, waterDeep, waterLight;

    public static LevelDef[] All()
    {
        return new[]
        {
            new LevelDef { from = "Charon's Steps", to = "Asphodel Shallows", duration = 60f, waveAmp = 0.15f, handInterval = new Vector2(7f, 10f),
                maxHandsPerGroup = 2, middleHands = false, calmSouls = 2, vengefulSouls = 0, speed = 1f,
                skyTop = Art.Hex("#0b1220"), skyBottom = Art.Hex("#2a4552"), waterDeep = Art.Hex("#06161c"), waterLight = Art.Hex("#1f6e72") },
            new LevelDef { from = "Asphodel Shallows", to = "Lethe Narrows", duration = 60f, waveAmp = 0.22f, handInterval = new Vector2(5f, 8f),
                maxHandsPerGroup = 3, middleHands = true, calmSouls = 2, vengefulSouls = 1, speed = 1.15f,
                skyTop = Art.Hex("#120e22"), skyBottom = Art.Hex("#3b3a5c"), waterDeep = Art.Hex("#0a1022"), waterLight = Art.Hex("#2c5e86") },
            new LevelDef { from = "Lethe Narrows", to = "Acheron Gate", duration = 90f, waveAmp = 0.26f, handInterval = new Vector2(4.5f, 7f),
                maxHandsPerGroup = 3, middleHands = true, calmSouls = 2, vengefulSouls = 1, speed = 1.2f,
                skyTop = Art.Hex("#1a0f14"), skyBottom = Art.Hex("#5a3a3a"), waterDeep = Art.Hex("#120a0e"), waterLight = Art.Hex("#7a4a3e") },
            new LevelDef { from = "Acheron Gate", to = "The Far Shore", duration = 150f, waveAmp = 0.4f, handInterval = new Vector2(3f, 5.5f),
                maxHandsPerGroup = 3, middleHands = true, calmSouls = 2, vengefulSouls = 2, speed = 1.4f,
                skyTop = Art.Hex("#05070c"), skyBottom = Art.Hex("#1d2a33"), waterDeep = Art.Hex("#02080a"), waterLight = Art.Hex("#16424a") },
        };
    }
}
