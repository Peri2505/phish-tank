using UnityEngine;

namespace PhishTank
{
    /// <summary>
    /// Decides WHERE fish swim. Default: a box in front of the seated player's head.
    /// If MRUK is set up (see MrukSpawnArea), it moves the box above your real table.
    /// </summary>
    public class SpawnArea : MonoBehaviour
    {
        [Tooltip("How far in front of the head the box starts and ends (metres).")]
        [SerializeField] private float nearDistance = 0.35f;
        [SerializeField] private float farDistance = 0.75f;
        [Tooltip("Width / height of the swim box. Keep it small: the 'airplane seat' test.")]
        [SerializeField] private float width = 0.7f;
        [SerializeField] private float height = 0.3f;
        [SerializeField] private float belowEyes = 0.0f;

        private Bounds? _override;

        /// <summary>Lets the MRUK script place the area over a real table.</summary>
        public void SetOverride(Bounds b) => _override = b;
        public void ClearOverride() => _override = null;

        public Bounds GetBounds()
        {
            if (_override.HasValue) return _override.Value;

            Transform head = Camera.main != null ? Camera.main.transform : transform;
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;

            float mid = (nearDistance + farDistance) * 0.5f;
            Vector3 center = head.position + forward * mid + Vector3.down * belowEyes;
            // Axis-aligned box is fine here: it's small and roughly in front of you.
            Vector3 size = new Vector3(width, height, farDistance - nearDistance);
            size = Quaternion.LookRotation(forward) * size;
            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            return new Bounds(center, size);
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            var b = GetBounds();
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.4f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}
