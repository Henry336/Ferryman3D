using System.Collections.Generic;
using UnityEngine;

// Procedural placeholder meshes so the game needs no imported art yet.
public static class MeshKit
{
    // Flat grid in XZ for the water surface.
    public static Mesh Grid(float x0, float x1, float z0, float z1, int nx, int nz)
    {
        var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        var v = new Vector3[(nx + 1) * (nz + 1)];
        for (int j = 0; j <= nz; j++)
            for (int i = 0; i <= nx; i++)
                v[j * (nx + 1) + i] = new Vector3(Mathf.Lerp(x0, x1, i / (float)nx), 0f, Mathf.Lerp(z0, z1, j / (float)nz));
        var t = new int[nx * nz * 6];
        int k = 0;
        for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                int a = j * (nx + 1) + i, b = a + 1, c = a + nx + 1, d = c + 1;
                t[k++] = a; t[k++] = c; t[k++] = b;
                t[k++] = b; t[k++] = c; t[k++] = d;
            }
        m.vertices = v; m.triangles = t;
        m.RecalculateNormals();
        m.bounds = new Bounds(new Vector3((x0 + x1) / 2, 0, (z0 + z1) / 2), new Vector3(x1 - x0, 10f, z1 - z0));
        return m;
    }

    // Convex 2D outline (XY, counter-clockwise) extruded along Z with flat side normals.
    public static Mesh Extrude(Vector2[] poly, float depth)
    {
        var v = new List<Vector3>();
        var t = new List<int>();
        float h = depth * 0.5f;
        int n = poly.Length;
        // Caps (fan)
        int f = v.Count;
        for (int i = 0; i < n; i++) v.Add(new Vector3(poly[i].x, poly[i].y, -h));
        for (int i = 1; i < n - 1; i++) { t.Add(f); t.Add(f + i + 1); t.Add(f + i); }
        int b = v.Count;
        for (int i = 0; i < n; i++) v.Add(new Vector3(poly[i].x, poly[i].y, h));
        for (int i = 1; i < n - 1; i++) { t.Add(b); t.Add(b + i); t.Add(b + i + 1); }
        // Sides
        for (int i = 0; i < n; i++)
        {
            var p0 = poly[i]; var p1 = poly[(i + 1) % n];
            int s = v.Count;
            v.Add(new Vector3(p0.x, p0.y, -h)); v.Add(new Vector3(p1.x, p1.y, -h));
            v.Add(new Vector3(p0.x, p0.y, h)); v.Add(new Vector3(p1.x, p1.y, h));
            t.Add(s); t.Add(s + 1); t.Add(s + 2);
            t.Add(s + 1); t.Add(s + 3); t.Add(s + 2);
        }
        var m = new Mesh();
        m.SetVertices(v); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // Smooth cone / tapered cylinder along +Y, closed at the bottom.
    public static Mesh Cone(float rBottom, float rTop, float height, int seg = 20)
    {
        var v = new List<Vector3>();
        var nrm = new List<Vector3>();
        var t = new List<int>();
        float slope = (rBottom - rTop) / height;
        for (int i = 0; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            var n = (dir + Vector3.up * slope).normalized;
            v.Add(dir * rBottom); nrm.Add(n);
            v.Add(dir * rTop + Vector3.up * height); nrm.Add(n);
        }
        for (int i = 0; i < seg; i++)
        {
            int a = i * 2;
            t.Add(a); t.Add(a + 1); t.Add(a + 2);
            t.Add(a + 1); t.Add(a + 3); t.Add(a + 2);
        }
        int c = v.Count; v.Add(Vector3.zero); nrm.Add(Vector3.down);
        int r0 = v.Count;
        for (int i = 0; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            v.Add(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rBottom); nrm.Add(Vector3.down);
        }
        for (int i = 0; i < seg; i++) { t.Add(c); t.Add(r0 + i); t.Add(r0 + i + 1); }
        var m = new Mesh();
        m.SetVertices(v); m.SetNormals(nrm); m.SetTriangles(t, 0);
        m.RecalculateBounds();
        return m;
    }

    // Crescent blade in XY, pointing toward +X from the origin, extruded thin.
    public static Mesh Blade(float length, float width, float depth, int seg = 14)
    {
        var outer = new List<Vector2>();
        var inner = new List<Vector2>();
        for (int i = 0; i <= seg; i++)
        {
            float u = i / (float)seg;
            float x = u * length;
            float curve = -Mathf.Pow(u, 1.6f) * length * 0.45f;
            float w = width * Mathf.Sin(Mathf.PI * Mathf.Min(1f, u * 1.05f + 0.05f)) * (1f - u * 0.6f);
            outer.Add(new Vector2(x, curve + w * 0.5f));
            inner.Add(new Vector2(x, curve - w * 0.5f));
        }
        // Build as a strip (non-convex), caps + sides.
        var v = new List<Vector3>(); var t = new List<int>();
        float h = depth * 0.5f;
        for (int side = 0; side < 2; side++)
        {
            float z = side == 0 ? -h : h;
            int s = v.Count;
            for (int i = 0; i <= seg; i++) { v.Add(new Vector3(outer[i].x, outer[i].y, z)); v.Add(new Vector3(inner[i].x, inner[i].y, z)); }
            for (int i = 0; i < seg; i++)
            {
                int a = s + i * 2;
                if (side == 0) { t.Add(a); t.Add(a + 2); t.Add(a + 1); t.Add(a + 1); t.Add(a + 2); t.Add(a + 3); }
                else { t.Add(a); t.Add(a + 1); t.Add(a + 2); t.Add(a + 1); t.Add(a + 3); t.Add(a + 2); }
            }
        }
        var m = new Mesh();
        m.SetVertices(v); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // Jagged silhouette strip (XY) from y=-height to a random ridge, for background cliffs.
    public static Mesh Ridge(float width, float baseY, float minH, float maxH, int peaks, int seed)
    {
        var rnd = new System.Random(seed);
        int n = peaks * 2 + 1;
        var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i <= n; i++)
        {
            float x = width * i / n;
            float hgt = (i == 0 || i == n) ? (minH + maxH) * 0.5f
                : (i % 2 == 1 ? Mathf.Lerp(minH, maxH, (float)rnd.NextDouble()) : Mathf.Lerp(minH * 0.6f, minH, (float)rnd.NextDouble()));
            v.Add(new Vector3(x, baseY, 0f));
            v.Add(new Vector3(x, baseY + hgt, 0f));
        }
        for (int i = 0; i < n; i++)
        {
            int a = i * 2;
            t.Add(a); t.Add(a + 1); t.Add(a + 2);
            t.Add(a + 1); t.Add(a + 3); t.Add(a + 2);
        }
        var m = new Mesh();
        m.SetVertices(v); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    public static Mesh Quad(float w, float h)
    {
        var m = new Mesh();
        m.vertices = new[] { new Vector3(-w / 2, -h / 2), new Vector3(w / 2, -h / 2), new Vector3(-w / 2, h / 2), new Vector3(w / 2, h / 2) };
        m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        m.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
}
