#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.XR.OpenXR;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace PhishTank.EditorTools
{
    /// <summary>
    /// Menu: PhishTank > Fix Android OpenXR
    /// Enables the OpenXR features a hands-first Quest build needs on Android
    /// (Meta Quest support, hand tracking subsystem, Meta hand aim) and then applies
    /// every automatic fix from OpenXR Project Validation, so the APK build passes.
    /// </summary>
    public static class PhishTankAndroidFix
    {
        static readonly string[] WantedFeatureIdParts =
        {
            "metaquest", "handtracking", "handaim", "metahandtrackingaim", "handinteraction"
        };

        static readonly string[] BlockedVendorParts =
        {
            "hololens", "microsoft", "pico", "magicleap", "htc", "vive", "huawei", "androidxr", "snapdragon", "spaces"
        };

        [MenuItem("PhishTank/Fix Android OpenXR")]
        public static void Fix()
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null) { Debug.LogError("[PhishTank] No Android OpenXR settings found. Is OpenXR enabled for Android in XR Plug-in Management?"); return; }

            foreach (var f in settings.GetFeatures<OpenXRFeature>())
            {
                if (f == null) continue;
                string id = (f.GetType().Name + " " + f.name).ToLowerInvariant().Replace(" ", "").Replace("_", "");
                bool otherVendor = BlockedVendorParts.Any(p => id.Contains(p));
                if (otherVendor && f.enabled)
                {
                    f.enabled = false;
                    EditorUtility.SetDirty(f);
                    Debug.Log($"[PhishTank] Disabled non-Quest feature: {f.GetType().Name}");
                    continue;
                }
                if (!otherVendor && WantedFeatureIdParts.Any(p => id.Contains(p)) && !f.enabled)
                {
                    f.enabled = true;
                    EditorUtility.SetDirty(f);
                    Debug.Log($"[PhishTank] Enabled Android OpenXR feature: {f.GetType().Name}");
                }
            }
            foreach (var f in settings.GetFeatures<OpenXRFeature>())
                if (f != null && f.enabled) Debug.Log($"[PhishTank] Android feature ON: {f.GetType().Name}");
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            var issues = new List<OpenXRFeature.ValidationRule>();
            for (int pass = 0; pass < 4; pass++)
            {
                issues.Clear();
                OpenXRProjectValidation.GetCurrentValidationIssues(issues, BuildTargetGroup.Android);
                var fixable = issues.Where(i => i.fixIt != null).ToList();
                if (fixable.Count == 0) break;
                foreach (var rule in fixable)
                {
                    try { rule.fixIt(); Debug.Log($"[PhishTank] Fixed: {rule.message}"); }
                    catch (System.Exception e) { Debug.LogWarning($"[PhishTank] Could not auto-fix '{rule.message}': {e.Message}"); }
                }
                AssetDatabase.SaveAssets();
            }

            issues.Clear();
            OpenXRProjectValidation.GetCurrentValidationIssues(issues, BuildTargetGroup.Android);
            var errors = issues.Where(i => i.error).ToList();
            if (errors.Count == 0)
                Debug.Log($"[PhishTank] ANDROID OPENXR OK: no validation errors left ({issues.Count} warnings).");
            else
                foreach (var e in errors) Debug.LogError($"[PhishTank] Still failing: {e.message}");
        }
    }
}
#endif
