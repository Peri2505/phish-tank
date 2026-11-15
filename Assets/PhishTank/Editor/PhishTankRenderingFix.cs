#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.OpenXR;

namespace PhishTank.EditorTools
{
    /// <summary>
    /// Menu: PhishTank > Fix Rendering
    /// Gives the active quality level (Meta Quest build profile) our own URP asset + renderer
    /// with Tile-Only mode OFF, so transparent things (text, UI, effects) render.
    /// </summary>
    public static class PhishTankRenderingFix
    {
        const string Dir = "Assets/PhishTank/Rendering";

        [MenuItem("PhishTank/Fix Rendering")]
        public static void Fix()
        {
            System.IO.Directory.CreateDirectory(Dir);
            string rendererPath = Dir + "/PhishTank_Renderer.asset";
            string assetPath = Dir + "/PhishTank_URP.asset";

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            var rso = new SerializedObject(renderer);
            var tile = rso.FindProperty("m_TileOnlyMode");
            if (tile != null) tile.boolValue = false;
            var mode = rso.FindProperty("m_RenderingMode");
            if (mode != null) mode.intValue = 0; // Forward
            rso.ApplyModifiedPropertiesWithoutUndo();

            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (urp == null)
            {
                urp = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(urp, assetPath);
            }
            urp.msaaSampleCount = 4;
            urp.supportsHDR = false;
            urp.renderScale = 1f;
            var aso = new SerializedObject(urp);
            var list = aso.FindProperty("m_RendererDataList");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            aso.FindProperty("m_DefaultRendererIndex").intValue = 0;
            aso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urp);
            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();

            QualitySettings.renderPipeline = urp;   // active level = Meta Quest (Build Profile)

            // Mac editor / Meta XR Simulator: TextMeshPro shaders need Multi-Pass stereo here.
            var sa = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            if (sa != null) { sa.renderMode = OpenXRSettings.RenderMode.MultiPass; EditorUtility.SetDirty(sa); }
            AssetDatabase.SaveAssets();
            Debug.Log($"[PhishTank] Rendering fixed: quality level '{QualitySettings.names[QualitySettings.GetQualityLevel()]}' now uses {assetPath} (Tile-Only OFF). " +
                      $"Default pipeline={(GraphicsSettings.defaultRenderPipeline ? GraphicsSettings.defaultRenderPipeline.name : "none")}, current={(GraphicsSettings.currentRenderPipeline ? GraphicsSettings.currentRenderPipeline.name : "none")}");
        }
    }
}
#endif
