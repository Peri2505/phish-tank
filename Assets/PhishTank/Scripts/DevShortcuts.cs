using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PhishTank
{
    /// <summary>
    /// Editor/simulator helpers so you can test without a headset:
    ///   Enter = Start / Next level / Play again
    ///   R     = reset saved progress
    /// Only active in the Unity Editor.
    /// </summary>
    public class DevShortcuts : MonoBehaviour
    {
#if UNITY_EDITOR
        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            if (Pressed(true))
            {
                if (gm.State == GameState.Menu || gm.State == GameState.GameOver) gm.StartGame();
                else if (gm.State == GameState.LevelComplete) gm.NextLevel();
            }
            if (Pressed(false)) { ProgressStore.ResetAll(); Debug.Log("[PhishTank] Progress reset."); }
        }

        private static bool Pressed(bool enter)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            return enter ? kb.enterKey.wasPressedThisFrame : kb.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(enter ? KeyCode.Return : KeyCode.R);
#endif
        }
#endif
    }
}
