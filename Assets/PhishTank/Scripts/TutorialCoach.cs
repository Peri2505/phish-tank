using TMPro;
using UnityEngine;

namespace PhishTank
{
    /// <summary>
    /// The first five minutes: one short hint at a time, driven by what the player actually does.
    /// Grab -> Flip -> Throw -> done. Shown once (remembered), and can be replayed from the menu.
    /// </summary>
    public class TutorialCoach : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text text;
        [SerializeField] private Transform arrow;

        public static TutorialCoach Instance { get; private set; }
        const string Key = "pt_tutorial_done";

        enum Step { Off, Grab, Flip, Throw, Done }
        Step _step = Step.Off;
        float _doneAt;

        private void Awake() { Instance = this; Hide(); }

        public static bool Completed => PlayerPrefs.GetInt(Key, 0) == 1;

        public void Begin()
        {
            if (Completed) { Hide(); return; }
            Set(Step.Grab, "Pinch a fish to catch it");
        }

        public void OnGrab(EmailFish f)   { if (_step == Step.Grab) Set(Step.Flip, "Turn your hand over:\nthe fish's belly shows the TRUTH"); }
        public void OnFlip(EmailFish f)   { if (_step == Step.Flip || _step == Step.Grab) Set(Step.Throw, "Scam?  Throw it in the RED net\nSafe?  Toss it in the GREEN inbox"); }
        public void OnJudged(bool correct)
        {
            if (_step == Step.Off || _step == Step.Done) return;
            Set(Step.Done, correct ? "You've got it!  Catch the rest" : "Every mistake teaches a trick.  Keep going!");
            _doneAt = Time.time;
            PlayerPrefs.SetInt(Key, 1);
            PlayerPrefs.Save();
        }

        public static void ResetTutorial() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }

        private void Update()
        {
            if (_step == Step.Done && Time.time - _doneAt > 3f) Hide();
            if (arrow && root && root.activeSelf)
                arrow.localPosition = new Vector3(0, -95f + Mathf.Sin(Time.time * 4f) * 12f, 0);
        }

        void Set(Step s, string msg)
        {
            _step = s;
            if (root) root.SetActive(true);
            if (text) text.text = msg;
        }

        void Hide()
        {
            if (_step != Step.Done) _step = Step.Off;
            if (root) root.SetActive(false);
        }
    }
}
