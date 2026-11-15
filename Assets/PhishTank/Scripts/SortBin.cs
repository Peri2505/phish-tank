using UnityEngine;

namespace PhishTank
{
    /// <summary>
    /// A bin you throw fish into. Needs a Collider with "Is Trigger" ticked.
    /// Make two: one with isScamBin = true (red "SCAM" net), one false (green "SAFE" inbox).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SortBin : MonoBehaviour
    {
        [SerializeField] private bool isScamBin;
        [SerializeField] private Animator animator; // optional: plays "Catch" trigger

        public bool IsScamBin => isScamBin;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var fish = other.GetComponentInParent<EmailFish>();
            if (fish == null || fish.Judged || fish.Data == null || !fish.CanBeSorted) return;

            if (animator != null) animator.SetTrigger("Catch");
            GameManager.Instance?.Judge(fish, isScamBin);
        }
    }
}
