#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PhishTank.EditorTools
{
    /// <summary>Procedural meshes for a friendly cartoon fish (saved as assets so prefabs keep them).</summary>
    public static class PhishTankMeshes
    {
        const string Dir = "Assets/PhishTank/Generated/Meshes";

        /// <summary>Teardrop body along +Z (nose at +Z), length ~1, max radius ~0.32.</summary>
        public static Mesh FishBody()
        {
            const int rings = 18, segs = 20;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            for (int i = 0; i <= rings; i++)
            {
                float t = i / (float)rings;                 // 0 tail -> 1 nose
                float z = Mathf.Lerp(-0.5f, 0.5f, t);
                // fat near the front, tapering to a narrow tail stalk
                float r = Mathf.Sin(Mathf.Pow(t, 0.75f) * Mathf.PI) * 0.32f + 0.02f * (1f - t);
                for (int j = 0; j <= segs; j++)
                {
                    float a = j / (float)segs * Mathf.PI * 2f;
                    float x = Mathf.Cos(a) * r * 0.78f;       // slightly flat sideways
                    float y = Mathf.Sin(a) * r;
                    v.Add(new Vector3(x, y, z));
                    n.Add(new Vector3(x, y, 0f).normalized);
                    uv.Add(new Vector2(j / (float)segs, t));
                }
            }
            int row = segs + 1;
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < segs; j++)
                {
                    int a = i * row + j, b = a + row;
                    tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
            var m = new Mesh { name = "FishBody" };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return Save(m);
        }

        /// <summary>Forked tail fin in the YZ plane (both faces), root at origin, pointing -Z.</summary>
        public static Mesh TailFin()
        {
            var v = new[] { new Vector3(0, 0, 0), new Vector3(0, 0.32f, -0.34f), new Vector3(0, 0.05f, -0.2f),
                            new Vector3(0, -0.32f, -0.34f), new Vector3(0, -0.05f, -0.2f) };
            return DoubleSided("TailFin", v, new[] { 0, 1, 2, 0, 2, 4, 0, 4, 3 });
        }

        /// <summary>Little triangular fin (dorsal / side), root along +Z edge.</summary>
        public static Mesh Fin()
        {
            var v = new[] { new Vector3(0, 0, 0.12f), new Vector3(0, 0, -0.1f), new Vector3(0, 0.16f, -0.12f) };
            return DoubleSided("Fin", v, new[] { 0, 1, 2 });
        }

        static Mesh DoubleSided(string name, Vector3[] verts, int[] tris)
        {
            var v = new List<Vector3>(verts); v.AddRange(verts);
            var t = new List<int>(tris);
            for (int i = 0; i < tris.Length; i += 3)
                t.AddRange(new[] { tris[i] + verts.Length, tris[i + 2] + verts.Length, tris[i + 1] + verts.Length });
            var m = new Mesh { name = name };
            m.SetVertices(v); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return Save(m);
        }

        static Mesh Save(Mesh m)
        {
            System.IO.Directory.CreateDirectory(Dir);
            string path = $"{Dir}/{m.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(m, existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
#endif
