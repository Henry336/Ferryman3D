using UnityEngine;

// Materials and small builders for placeholder visuals. Shaders live in Resources so they ship in the build.
public static class Art
{
    static Shader toon, glow, water, unlit;

    public static Material Toon(Color c, Color shade, float outline = 0.035f)
    {
        if (!toon) toon = Resources.Load<Shader>("Shaders/Toon");
        var m = new Material(toon);
        m.SetColor("_Color", c);
        m.SetColor("_Shade", shade);
        m.SetFloat("_Outline", outline);
        return m;
    }

    public static Material Glow(Color c, float core = 0.35f, float power = 2f)
    {
        if (!glow) glow = Resources.Load<Shader>("Shaders/Glow");
        var m = new Material(glow);
        m.SetColor("_Color", c);
        m.SetFloat("_Core", core);
        m.SetFloat("_Power", power);
        return m;
    }

    // Soft radial glow for camera-facing quads.
    public static Material Halo(Color c)
    {
        var m = Glow(c);
        m.SetFloat("_Radial", 1f);
        return m;
    }

    public static Material Water()
    {
        if (!water) water = Resources.Load<Shader>("Shaders/Water");
        return new Material(water);
    }

    public static Material Unlit(Color top, Color bottom)
    {
        if (!unlit) unlit = Resources.Load<Shader>("Shaders/Unlit");
        var m = new Material(unlit);
        m.SetColor("_Top", top);
        m.SetColor("_Bottom", bottom);
        return m;
    }

    public static GameObject Obj(string name, Transform parent, Mesh mesh, Material mat)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return g;
    }

    static Mesh sphere, capsule, cylinder;
    static Mesh Builtin(ref Mesh cache, PrimitiveType type)
    {
        if (!cache)
        {
            var g = GameObject.CreatePrimitive(type);
            cache = g.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(g);
        }
        return cache;
    }
    public static Mesh Sphere => Builtin(ref sphere, PrimitiveType.Sphere);
    public static Mesh Capsule => Builtin(ref capsule, PrimitiveType.Capsule);
    public static Mesh Cylinder => Builtin(ref cylinder, PrimitiveType.Cylinder);

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
}
