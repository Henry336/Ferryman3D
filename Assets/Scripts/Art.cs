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

    public static Material ToonTex(Texture tex, float outline = 0.02f)
    {
        var m = new Material(Resources.Load<Shader>("Shaders/ToonTex"));
        if (tex) m.SetTexture("_MainTex", tex);
        m.SetFloat("_Outline", outline);
        return m;
    }

    // Spawns Resources/Models/<name>, gives it a ToonTex material, rotates it by yaw, scales its
    // height (or length along X when byLength) to size and sits its bottom centre on the parent's origin.
    // Returns null if the model isn't in the build, so callers can fall back to placeholder shapes.
    public static GameObject Model(string name, Transform parent, float size, float yaw = 0f, bool byLength = false, Material mat = null)
    {
        var prefab = Resources.Load<GameObject>("Models/" + name);
        if (!prefab) return null;
        var holder = new GameObject(name).transform;
        holder.SetParent(parent, false);
        var g = Object.Instantiate(prefab, holder);
        g.transform.localPosition = Vector3.zero;
        g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        if (!mat) mat = ToonTex(Resources.Load<Texture2D>("Models/" + name + "_tex"));
        var rends = g.GetComponentsInChildren<Renderer>();
        foreach (var r in rends)
        {
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (r is SkinnedMeshRenderer s) s.updateWhenOffscreen = true;
        }
        // Fit using bounds measured in the holder's space.
        var b = new Bounds();
        bool first = true;
        // Measure the mesh's own corners: a fresh SkinnedMeshRenderer reports stale bounds.
        foreach (var r in rends)
        {
            Mesh mesh = r is SkinnedMeshRenderer sk ? sk.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
            if (!mesh) continue;
            var mb = mesh.bounds;
            for (int c = 0; c < 8; c++)
            {
                var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                var p = holder.InverseTransformPoint(r.transform.TransformPoint(corner));
                if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p);
            }
        }
        float current = byLength ? b.size.x : b.size.y;
        float k = current > 1e-4f ? size / current : 1f;
        g.transform.localScale *= k;
        g.transform.localPosition = -new Vector3(b.center.x, b.min.y, b.center.z) * k;
        return holder.gameObject;
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
