using TMPro;
using UnityEngine;

namespace PhishTank
{
    /// <summary>
    /// Your aquarium grows as you catch more scams — visible progress between sessions.
    /// Add decorations (coral, shells, a little castle...) as children, each disabled,
    /// and list them here with the number of correct sorts needed to unlock them.
    /// </summary>
    public class AquariumGrowth : MonoBehaviour
    {
        [System.Serializable]
        public class Unlock
        {
            public GameObject decoration;
            public int correctSortsNeeded = 10;
            public string label = "Coral";
        }

        [SerializeField] private Unlock[] unlocks;
        [SerializeField] private TMP_Text statsText; // optional: "142 scams caught • 3-day streak"

        private void OnEnable()
        {
            ProgressStore.OnProgressChanged += Refresh;
            Refresh();
        }

        private void OnDisable() => ProgressStore.OnProgressChanged -= Refresh;

        private void Refresh()
        {
            int total = ProgressStore.TotalCorrect;
            foreach (var u in unlocks)
                if (u.decoration != null) u.decoration.SetActive(total >= u.correctSortsNeeded);

            if (statsText != null)
            {
                string next = "";
                foreach (var u in unlocks)
                    if (total < u.correctSortsNeeded) { next = $"\nNext: {u.label} at {u.correctSortsNeeded}"; break; }
                statsText.text = $"{total} correct sorts  •  {ProgressStore.Streak}-day streak  •  best {ProgressStore.BestScore:N0}{next}";
            }
        }
    }
}
