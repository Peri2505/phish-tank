using System;
using System.Collections;
using UnityEngine;

namespace PhishTank
{
    public enum GameState { Menu, Playing, LevelComplete, GameOver }

    /// <summary>
    /// The brain of the game: levels, score, combos, lives, and the judging of each fish.
    /// Put on an empty "GameManager" object in the scene.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private EmailDatabase database;
        [SerializeField] private FishSpawner spawner;
        [SerializeField] private FeedbackPanel feedback;
        [SerializeField] private GameObject startMenu;
        [SerializeField] private GameObject levelCompleteMenu;
        [SerializeField] private GameObject gameOverMenu;

        [Header("Levels (in order)")]
        [SerializeField] private LevelConfig[] levels;

        [Header("Rules")]
        [SerializeField] private int startingLives = 3;
        [SerializeField] private int pointsPerCorrect = 100;
        [SerializeField] private int maxComboMultiplier = 5;
        [SerializeField] private int inspectorBonus = 50;

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource sfx;
        [SerializeField] private AudioClip correctClip;
        [SerializeField] private AudioClip wrongClip;
        [SerializeField] private AudioClip grabClip;
        [SerializeField] private AudioClip levelCompleteClip;

        public GameState State { get; private set; } = GameState.Menu;
        public int Score { get; private set; }
        public int Lives { get; private set; }
        public int Combo { get; private set; }
        public int LevelIndex { get; private set; }
        public LevelConfig CurrentLevel => levels[LevelIndex];

        // UI listens to these.
        public event Action OnStatsChanged;
        public event Action<GameState> OnStateChanged;

        private int _judgedThisLevel;
        private int _fishThisLevel;
        private int _correctThisLevel;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start() => SetState(GameState.Menu);

        // ---------- Buttons (wire Poke buttons' "When Select" to these) ----------

        public void StartGame()
        {
            if (PlayAreaAnchor.Instance != null) PlayAreaAnchor.Instance.Recenter();
            Score = 0;
            Lives = startingLives;
            Combo = 0;
            LevelIndex = 0;
            StartLevel();
        }

        public void NextLevel()
        {
            LevelIndex = Mathf.Min(LevelIndex + 1, levels.Length - 1);
            StartLevel();
        }

        public void BackToMenu()
        {
            spawner.ClearAll();
            SetState(GameState.Menu);
        }

        // ---------- Level flow ----------

        private void StartLevel()
        {
            spawner.ClearAll();
            var level = CurrentLevel;
            var batch = database.GetBatch(level.maxDifficulty, level.fishCount, level.scamRatio);
            _fishThisLevel = batch.Count;
            _judgedThisLevel = 0;
            _correctThisLevel = 0;

            SetState(GameState.Playing);
            feedback?.ShowBanner(level.levelName);
            if (LevelIndex == 0) TutorialCoach.Instance?.Begin();
            spawner.SpawnBatch(batch, level);
            OnStatsChanged?.Invoke();
        }

        public void NotifyFishGrabbed(EmailFish fish) => Play(grabClip);

        /// <summary>Called by a SortBin when a fish lands in it.</summary>
        public void Judge(EmailFish fish, bool putInScamBin)
        {
            if (State != GameState.Playing || fish.Judged) return;

            var email = fish.Data;
            bool correct = putInScamBin == email.isScam;
            Vector3 at = fish.transform.position;
            bool inspected = fish.WasFlipped;
            fish.MarkJudged();
            SoundBank.Play(SoundBank.Sfx.Splash, at, 0.6f);
            TutorialCoach.Instance?.OnJudged(correct);
            _judgedThisLevel++;

            if (correct)
            {
                Combo++;
                _correctThisLevel++;
                int multiplier = Mathf.Clamp(Combo, 1, maxComboMultiplier);
                Score += pointsPerCorrect * multiplier + (inspected ? inspectorBonus : 0);
                Play(correctClip);
                SoundBank.Play(multiplier > 1 ? SoundBank.Sfx.Combo : SoundBank.Sfx.Correct, at);
                if (inspected) SoundBank.Play(SoundBank.Sfx.Bonus, at, 0.7f);
                if (multiplier >= 3) Fx.Combo(at); else Fx.Correct(at);
                feedback?.ShowResult(true, email, multiplier, inspected ? inspectorBonus : 0);
            }
            else
            {
                Combo = 0;
                Lives--;
                Play(wrongClip);
                SoundBank.Play(SoundBank.Sfx.Wrong, at);
                Fx.Wrong(at);
                feedback?.ShowResult(false, email, 1);
            }

            ProgressStore.RecordSort(correct);
            OnStatsChanged?.Invoke();

            if (Lives <= 0) { StartCoroutine(EndAfter(GameState.GameOver, 2.5f)); return; }
            if (_judgedThisLevel >= _fishThisLevel) StartCoroutine(EndAfter(GameState.LevelComplete, 2.5f));
        }

        private IEnumerator EndAfter(GameState next, float delay)
        {
            State = GameState.LevelComplete; // freeze judging while the last feedback shows
            yield return new WaitForSeconds(delay);
            spawner.ClearAll();

            if (next == GameState.LevelComplete)
            {
                Play(levelCompleteClip);
                SoundBank.Play(SoundBank.Sfx.LevelUp, Camera.main ? Camera.main.transform.position : Vector3.zero);
                ProgressStore.RecordLevelComplete(LevelIndex, Score);
                // Last level finished = victory; show game over screen as the "results" screen.
                if (LevelIndex >= levels.Length - 1) next = GameState.GameOver;
            }
            else
            {
                ProgressStore.RecordLevelComplete(LevelIndex - 1, Score);
            }
            SetState(next);
        }

        private void SetState(GameState s)
        {
            State = s;
            if (startMenu) startMenu.SetActive(s == GameState.Menu);
            if (levelCompleteMenu) levelCompleteMenu.SetActive(s == GameState.LevelComplete);
            if (gameOverMenu) gameOverMenu.SetActive(s == GameState.GameOver);
            OnStateChanged?.Invoke(s);
            OnStatsChanged?.Invoke();
        }

        public int CorrectThisLevel => _correctThisLevel;
        public int FishThisLevel => _fishThisLevel;

        private void Play(AudioClip clip)
        {
            if (sfx != null && clip != null) sfx.PlayOneShot(clip);
        }
    }
}
