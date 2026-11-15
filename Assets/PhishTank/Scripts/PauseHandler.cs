using UnityEngine;

namespace PhishTank
{
    /// <summary>
    /// "Easy to get into and out of": pauses cleanly when the headset is taken off
    /// or the Meta menu opens, and resumes where you left off.
    /// </summary>
    public class PauseHandler : MonoBehaviour
    {
#if !UNITY_EDITOR
        // Only on the headset: in the Editor, focus moves to the XR Simulator window constantly.
        private void OnApplicationPause(bool paused) => SetPaused(paused);
        private void OnApplicationFocus(bool focused) => SetPaused(!focused);
#endif

        private static void SetPaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
        }
    }
}
