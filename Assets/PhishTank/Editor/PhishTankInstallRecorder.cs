#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace PhishTank.EditorTools
{
    /// <summary>Menu: PhishTank > Install Recorder (adds Unity Recorder for clean Game-view videos).</summary>
    public static class PhishTankInstallRecorder
    {
        static AddRequest _req;

        [MenuItem("PhishTank/Install Recorder")]
        public static void Install()
        {
            _req = Client.Add("com.unity.recorder");
            EditorApplication.update += Poll;
            Debug.Log("[PhishTank] Installing Unity Recorder…");
        }

        static void Poll()
        {
            if (_req == null || !_req.IsCompleted) return;
            EditorApplication.update -= Poll;
            if (_req.Status == StatusCode.Success) Debug.Log($"[PhishTank] RECORDER INSTALLED: {_req.Result.version}");
            else Debug.LogError($"[PhishTank] Recorder install failed: {_req.Error?.message}");
            _req = null;
        }
    }
}
#endif
