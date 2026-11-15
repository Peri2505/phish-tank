#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PhishTank.EditorTools
{
    /// <summary>
    /// PhishTank > Open Tester (play with buttons on a Mac, no headset or hands needed).
    /// 1) Press Play  2) Start Game  3) Grab Next Fish  4) read the email  5) SAFE or SCAM
    /// </summary>
    public class PhishTankTester : EditorWindow
    {
        private EmailFish _held;
        private Vector2 _scroll;

        [MenuItem("PhishTank/Open Tester")]
        public static void Open()
        {
            var w = GetWindow<PhishTankTester>("Phish Tank Tester");
            w.minSize = new Vector2(320, 420);
            w.Show();
        }

        private void OnInspectorUpdate() => Repaint();

        private void OnGUI()
        {
            var big = new GUIStyle(GUI.skin.button) { fontSize = 16, fixedHeight = 40 };
            var title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
            var wrap = new GUIStyle(EditorStyles.label) { wordWrap = true, fontSize = 13, richText = true };

            GUILayout.Label("🐟 Phish Tank Tester", title);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Step 1: press the ▶ Play button at the top of Unity.", MessageType.Info);
                if (GUILayout.Button("▶  Play", big)) EditorApplication.EnterPlaymode();
                return;
            }

            var gm = GameManager.Instance;
            if (gm == null) { EditorGUILayout.HelpBox("Loading…", MessageType.None); return; }

            // Status
            EditorGUILayout.LabelField($"Level: {(gm.CurrentLevel ? gm.CurrentLevel.levelName : "-")}   Score: {gm.Score}   Hearts: {new string('♥', Mathf.Max(0, gm.Lives))}   Combo: x{Mathf.Max(1, gm.Combo)}", wrap);
            EditorGUILayout.LabelField($"State: {gm.State}   Fish left: {CountFish()}", wrap);
            GUILayout.Space(6);

            if (gm.State == GameState.Menu || gm.State == GameState.GameOver)
            {
                if (GUILayout.Button(gm.State == GameState.Menu ? "Start Game" : "Play Again", big)) gm.StartGame();
                return;
            }
            if (gm.State == GameState.LevelComplete)
            {
                EditorGUILayout.HelpBox("Level complete (or wait 2 seconds for the result)…", MessageType.Info);
                if (GUILayout.Button("Next Level", big)) gm.NextLevel();
                return;
            }

            // Playing
            if (_held == null || _held.Judged)
            {
                _held = null;
                EditorGUILayout.HelpBox("Step 2: grab a fish to read its email.", MessageType.Info);
                if (GUILayout.Button("✋  Grab Next Fish", big)) GrabNext();
                return;
            }

            var d = _held.Data;
            EditorGUILayout.HelpBox("Step 3: read it. Is it SAFE or a SCAM?", MessageType.Info);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(190));
            bool truth = _held.Revealed;
            GUILayout.Label($"<b>From:</b> {d.fromName}\n" + (truth ? $"<color=#E0A060>‹{d.fromAddress}›</color>" : "<i>(flip the fish to see the real address)</i>"), wrap);
            GUILayout.Label($"<b>Subject:</b> {d.subject}", wrap);
            GUILayout.Label(d.body, wrap);
            if (!string.IsNullOrEmpty(d.linkText)) GUILayout.Label($"<b>Link:</b> {d.linkText}\n" + (truth ? $"<color=#E0A060>goes to: {d.linkUrl}</color>" : "<i>(flip to see where it really goes)</i>"), wrap);
            if (!string.IsNullOrEmpty(d.attachment)) GUILayout.Label($"<b>Attachment:</b> {d.attachment}", wrap);
            EditorGUILayout.EndScrollView();

            GUILayout.Space(6);
            if (!truth && GUILayout.Button("🔄  Flip the fish (reveal the truth, +50 bonus)", big))
            {
                _held.ForceReveal();
                SoundBank.Play(SoundBank.Sfx.Flip, _held.transform.position);
                TutorialCoach.Instance?.OnFlip(_held);
            }
            GUILayout.BeginHorizontal();
            var old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f);
            if (GUILayout.Button("✅  SAFE", big)) Sort(false);
            GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
            if (GUILayout.Button("🚩  SCAM", big)) Sort(true);
            GUI.backgroundColor = old;
            GUILayout.EndHorizontal();
        }

        private static int CountFish()
        {
            int n = 0;
            foreach (var f in FindObjectsByType<EmailFish>(FindObjectsSortMode.None)) if (!f.Judged) n++;
            return n;
        }

        private void GrabNext()
        {
            var cam = Camera.main;
            EmailFish best = null; float bd = float.MaxValue;
            foreach (var f in FindObjectsByType<EmailFish>(FindObjectsSortMode.None))
            {
                if (f.Judged || f.Data == null) continue;
                float d = cam ? Vector3.Distance(cam.transform.position, f.transform.position) : 0f;
                if (d < bd) { bd = d; best = f; }
            }
            if (best == null) { ShowNotification(new GUIContent("No fish yet — wait a second")); return; }
            _held = best;
            _held.OnGrabbed();   // opens the email card in the simulator too
            _scroll = Vector2.zero;
        }

        private void Sort(bool scam)
        {
            if (_held == null) return;
            _held.OnReleased();
            GameManager.Instance.Judge(_held, scam);   // same scoring + feedback as a real throw
            _held = null;
        }
    }
}
#endif
