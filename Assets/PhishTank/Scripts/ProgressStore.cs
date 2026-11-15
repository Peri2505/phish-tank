using System;
using UnityEngine;

namespace PhishTank
{
    /// <summary>
    /// Saves progress between sessions (the "reason to come back"):
    /// total scams caught, best score, highest level, and a daily streak.
    /// </summary>
    public static class ProgressStore
    {
        private const string KTotalCorrect = "pt_total_correct";
        private const string KBestScore = "pt_best_score";
        private const string KHighestLevel = "pt_highest_level";
        private const string KLastDay = "pt_last_day";
        private const string KStreak = "pt_streak";

        public static event Action OnProgressChanged;

        public static int TotalCorrect => PlayerPrefs.GetInt(KTotalCorrect, 0);
        public static int BestScore => PlayerPrefs.GetInt(KBestScore, 0);
        public static int HighestLevel => PlayerPrefs.GetInt(KHighestLevel, -1);
        public static int Streak => PlayerPrefs.GetInt(KStreak, 0);

        public static void RecordSort(bool correct)
        {
            if (correct) PlayerPrefs.SetInt(KTotalCorrect, TotalCorrect + 1);
            TouchStreak();
            PlayerPrefs.Save();
            OnProgressChanged?.Invoke();
        }

        public static void RecordLevelComplete(int levelIndex, int score)
        {
            if (levelIndex > HighestLevel) PlayerPrefs.SetInt(KHighestLevel, levelIndex);
            if (score > BestScore) PlayerPrefs.SetInt(KBestScore, score);
            PlayerPrefs.Save();
            OnProgressChanged?.Invoke();
        }

        private static void TouchStreak()
        {
            int today = (int)(DateTime.Now.Date - new DateTime(2020, 1, 1)).TotalDays;
            int last = PlayerPrefs.GetInt(KLastDay, -1);
            if (last == today) return;
            PlayerPrefs.SetInt(KStreak, last == today - 1 ? Streak + 1 : 1);
            PlayerPrefs.SetInt(KLastDay, today);
        }

        /// <summary>For testing in the Editor: Tools-style reset.</summary>
        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(KTotalCorrect); PlayerPrefs.DeleteKey(KBestScore);
            PlayerPrefs.DeleteKey(KHighestLevel); PlayerPrefs.DeleteKey(KLastDay);
            PlayerPrefs.DeleteKey(KStreak); PlayerPrefs.Save();
            OnProgressChanged?.Invoke();
        }
    }
}
