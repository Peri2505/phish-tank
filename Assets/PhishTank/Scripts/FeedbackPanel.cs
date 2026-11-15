using System.Collections;
using TMPro;
using UnityEngine;

namespace PhishTank
{
    /// <summary>
    /// The teaching moment. After each sort, shows "Caught it!" / "Oops" plus the clues
    /// and one-sentence explanation. Put on a World Space Canvas ~0.7 m in front of the player.
    /// </summary>
    public class FeedbackPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text cluesText;
        [SerializeField] private TMP_Text explanationText;
        [SerializeField] private float showSeconds = 3.5f;
        [SerializeField] private Color goodColor = new Color(0.35f, 0.9f, 0.55f);
        [SerializeField] private Color badColor = new Color(1f, 0.45f, 0.45f);

        private Coroutine _routine;

        private void Awake() { if (root) root.SetActive(false); }

        public void ShowResult(bool correct, EmailData email, int multiplier, int bonus = 0)
        {
            string verdict = email.isScam ? "SCAM" : "SAFE";
            titleText.color = correct ? goodColor : badColor;
            titleText.text = correct
                ? (multiplier > 1 ? $"Caught it!  x{multiplier} combo" : "Caught it!") + (bonus > 0 ? $"   <color=#FFB86B>+{bonus} Inspector</color>" : "")
                : $"Oops — that one was {verdict}";

            cluesText.text = (email.clues != null && email.clues.Length > 0)
                ? "Clues: " + string.Join("  •  ", email.clues)
                : "";
            explanationText.text = email.explanation;

            Flash();
        }

        public void ShowBanner(string text)
        {
            titleText.color = Color.white;
            titleText.text = text;
            cluesText.text = "";
            explanationText.text = "Grab a fish, read it, then throw it: SAFE or SCAM.";
            Flash();
        }

        private void Flash()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            if (root) root.SetActive(true);
            yield return new WaitForSeconds(showSeconds);
            if (root) root.SetActive(false);
        }
    }
}
