#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

namespace PhishTank.EditorTools
{
    public static class PhishTankDiagnostics
    {
        [MenuItem("PhishTank/Reset Progress + Tutorial")]
        public static void ResetAll()
        {
            ProgressStore.ResetAll();
            TutorialCoach.ResetTutorial();
            Debug.Log("[PhishTank] Progress, streak and tutorial reset — next game starts fresh.");
        }

        [MenuItem("PhishTank/Play Mode: Start Game")]
        public static void StartGame() { if (GameManager.Instance) GameManager.Instance.StartGame(); else Debug.LogWarning("[PhishTank] Enter Play mode first."); }

        [MenuItem("PhishTank/Play Mode: Auto-Sort All Fish Correctly")]
        public static void AutoSort()
        {
            var gm = GameManager.Instance;
            if (gm == null) { Debug.LogWarning("[PhishTank] Enter Play mode first."); return; }
            int n = 0;
            foreach (var f in Object.FindObjectsByType<EmailFish>(FindObjectsSortMode.None))
                if (!f.Judged && f.Data != null) { f.OnGrabbed(); f.ForceReveal(); gm.Judge(f, f.Data.isScam); n++; }
            Debug.Log($"[PhishTank][Test] Auto-sorted {n} fish. Level={gm.CurrentLevel?.levelName} Score={gm.Score} Lives={gm.Lives} State={gm.State}");
        }

        [MenuItem("PhishTank/Play Mode: Next Level")]
        public static void NextLevel() { if (GameManager.Instance) GameManager.Instance.NextLevel(); }

        [MenuItem("PhishTank/Play Mode: Show Card On Nearest Fish")]
        public static void ShowCard()
        {
            var cam = Camera.main;
            var fishes = Object.FindObjectsByType<EmailFish>(FindObjectsSortMode.None);
            EmailFish best = null; float bd = float.MaxValue;
            foreach (var f in fishes) { float d = Vector3.Distance(cam.transform.position, f.transform.position); if (d < bd) { bd = d; best = f; } }
            if (best != null) { best.OnGrabbed(); Debug.Log($"[PhishTank] Showing card for '{best.Data?.subject}' ({fishes.Length} fish alive)"); }
        }

        [MenuItem("PhishTank/Spawn Render Test")]
        public static void SpawnRenderTest()
        {
            var cam = Camera.main;
            if (cam == null) { Debug.LogError("[PhishTank][Diag] no camera"); return; }
            Vector3 p = cam.transform.position + cam.transform.forward * 0.6f;

            // 1) transparent URP cube (left)
            var c1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c1.name = "TEST transparent cube";
            c1.transform.position = p - cam.transform.right * 0.2f;
            c1.transform.localScale = Vector3.one * 0.1f;
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            m.color = new Color(1, 0, 1, 0.8f);
            c1.GetComponent<Renderer>().material = m;

            // 2) opaque cube (center)
            var c2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c2.name = "TEST opaque cube";
            c2.transform.position = p;
            c2.transform.localScale = Vector3.one * 0.1f;

            // 3) 3D TMP text (right)
            var tgo = new GameObject("TEST tmp");
            var t = tgo.AddComponent<TextMeshPro>();
            t.text = "HELLO";
            t.fontSize = 1f;
            t.color = Color.yellow;
            t.alignment = TextAlignmentOptions.Center;
            tgo.transform.position = p + cam.transform.right * 0.2f;
            tgo.transform.rotation = Quaternion.LookRotation(cam.transform.forward);
            Debug.Log("[PhishTank][Diag] Spawned render test at " + p.ToString("F2") +
                      " | queue tmp=" + t.fontSharedMaterial.renderQueue +
                      " | quality=" + QualitySettings.names[QualitySettings.GetQualityLevel()] +
                      " | pipeline=" + (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline ? UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name : "none"));
        }

        [MenuItem("PhishTank/Diagnose Text")]
        public static void DiagnoseText()
        {
            var texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Debug.Log($"[PhishTank][Diag] Active TMP texts: {texts.Length}. Default font: {(TMP_Settings.defaultFontAsset ? TMP_Settings.defaultFontAsset.name : "NULL")}");
            int n = 0;
            foreach (var t in texts)
            {
                if (n++ >= 4) break;
                var r = t.GetComponent<Renderer>();
                var mat = t.fontSharedMaterial;
                var sh = mat ? mat.shader : null;
                Debug.Log($"[PhishTank][Diag] '{t.name}' text='{t.text}' font={(t.font ? t.font.name : "NULL")} size={t.fontSize} " +
                          $"verts={(t.mesh ? t.mesh.vertexCount : -1)} chars={t.textInfo?.characterCount} " +
                          $"shader={(sh ? sh.name : "NULL")} supported={(sh ? sh.isSupported : false)} " +
                          $"renderer={(r ? r.GetType().Name : "none")} enabled={(r ? r.enabled : false)} visible={(r ? r.isVisible : false)} " +
                          $"bounds={(r ? r.bounds.size.ToString("F3") : "-")} layer={t.gameObject.layer} pos={t.transform.position.ToString("F2")} lossyScale={t.transform.lossyScale.ToString("F4")}");
            }
            var cam = Camera.main;
            Debug.Log($"[PhishTank][Diag] Camera.main={(cam ? cam.name : "NULL")} cullingMask={(cam ? cam.cullingMask : 0)} pos={(cam ? cam.transform.position.ToString("F2") : "-")}");
        }
    }
}
#endif
