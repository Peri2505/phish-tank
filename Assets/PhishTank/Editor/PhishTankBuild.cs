#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PhishTank.EditorTools
{
    /// <summary>
    /// PhishTank > Prepare Quest Build   — sets the store-ready Android/Quest player settings.
    /// PhishTank > Build Quest APK       — builds Builds/PhishTank.apk (upload this to the "Competition" channel).
    /// </summary>
    public static class PhishTankBuild
    {
        public const string ApkPath = "Builds/PhishTank.apk";
        const string Scene = "Assets/PhishTank/Scenes/PhishTank.unity";

        [MenuItem("PhishTank/Prepare Quest Build")]
        public static void Prepare()
        {
            PlayerSettings.companyName = "Aiperi Moldobaeva";
            PlayerSettings.productName = "Phish Tank";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.aiperimoldobaeva.phishtank");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;   // required by the Meta Horizon store
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            EditorUserBuildSettings.development = false;
            UseReleaseKey();

            var scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            EditorBuildSettings.scenes = scenes;
            AssetDatabase.SaveAssets();
            Debug.Log("[PhishTank] Quest build settings ready: 'Phish Tank' 1.0.0 (com.aiperimoldobaeva.phishtank), IL2CPP, ARM64.");
        }

        /// <summary>Signs with Keystore/phishtank.keystore (the store rejects debug-signed APKs).</summary>
        static void UseReleaseKey()
        {
            string ks = Path.GetFullPath("Keystore/phishtank.keystore");
            string pw = "Keystore/keystore_password.txt";
            if (!File.Exists(ks) || !File.Exists(pw)) { Debug.LogWarning("[PhishTank] No release keystore found - building with the DEBUG key."); return; }
            string pass = File.ReadAllText(pw).Trim();
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = ks;
            PlayerSettings.Android.keystorePass = pass;
            PlayerSettings.Android.keyaliasName = "phishtank";
            PlayerSettings.Android.keyaliasPass = pass;
            Debug.Log("[PhishTank] Signing with the release key (Keystore/phishtank.keystore).");
        }

        [MenuItem("PhishTank/Build Quest APK")]
        public static void BuildApk()
        {
            Prepare();
            Directory.CreateDirectory("Builds");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            if (s.result == BuildResult.Succeeded)
                Debug.Log($"[PhishTank] APK BUILD SUCCEEDED: {Path.GetFullPath(ApkPath)}  ({s.totalSize / (1024f * 1024f):F1} MB, {s.totalTime.TotalMinutes:F1} min)");
            else
                Debug.LogError($"[PhishTank] APK BUILD {s.result}: {s.totalErrors} errors. See the Console above for details.");
        }
    }
}
#endif
