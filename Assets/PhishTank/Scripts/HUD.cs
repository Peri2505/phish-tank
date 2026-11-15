using TMPro;
using UnityEngine;

namespace PhishTank
{
    /// <summary>Small score / lives / combo readout. World Space Canvas, lower-left of view.</summary>
    public class HUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private GameObject root;

        private void OnEnable()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnStatsChanged += Refresh;
            Refresh();
        }

        private void Start()
        {
            // In case GameManager woke after us.
            GameManager.Instance.OnStatsChanged -= Refresh;
            GameManager.Instance.OnStatsChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnStatsChanged -= Refresh;
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (root) root.SetActive(gm.State == GameState.Playing);
            scoreText.text = gm.Score.ToString("N0");
            livesText.text = new string('♥', Mathf.Max(0, gm.Lives));
            comboText.text = gm.Combo > 1 ? $"x{Mathf.Min(gm.Combo, 5)}" : "";
            levelText.text = gm.CurrentLevel != null ? gm.CurrentLevel.levelName : "";
        }
    }
}
