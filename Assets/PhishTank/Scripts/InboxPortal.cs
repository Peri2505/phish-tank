using UnityEngine;

namespace PhishTank
{
    /// <summary>
    /// Where the scam emails come from. If the room scan finds a real TV/monitor (MRUK "SCREEN"),
    /// RoomSetup moves this portal onto it and hides the virtual laptop — so fish burst out of
    /// YOUR real screen. Otherwise a glowing virtual laptop floats in front of you.
    /// </summary>
    public class InboxPortal : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint;      // just in front of the screen surface
        [SerializeField] private GameObject virtualLaptop;  // hidden when a real screen is used
        [SerializeField] private Transform glow;            // pulsing ring
        [SerializeField] private TMPro.TMP_Text inboxLabel;

        public static InboxPortal Instance { get; private set; }
        public bool OnRealScreen { get; private set; }

        private float _pulse;

        private void Awake() => Instance = this;

        public Vector3 SpawnPosition => spawnPoint ? spawnPoint.position : transform.position;
        public Vector3 Forward => transform.forward * -1f; // portal faces the player (its -Z looks at them)

        /// <summary>Called by RoomSetup when a real screen is found.</summary>
        public void AttachToRealScreen(Vector3 center, Vector3 towardsPlayer, Vector2 size)
        {
            OnRealScreen = true;
            transform.position = center + towardsPlayer.normalized * 0.03f;
            transform.rotation = Quaternion.LookRotation(-towardsPlayer.normalized, Vector3.up);
            if (virtualLaptop) virtualLaptop.SetActive(false);
            if (glow)
            {
                float s = Mathf.Clamp(Mathf.Max(size.x, size.y), 0.3f, 1.6f);
                glow.localScale = new Vector3(s, s * 0.6f, 1f);
            }
        }

        public void SetUnread(int count)
        {
            if (inboxLabel) inboxLabel.text = count > 0 ? $"INBOX  ({count})" : "INBOX";
        }

        /// <summary>Visual + sound when a fish swims out.</summary>
        public void Emit()
        {
            _pulse = 1f;
            Fx.Bubbles(SpawnPosition);
            SoundBank.Play(SoundBank.Sfx.Bubble, SpawnPosition);
        }

        private void Update()
        {
            if (!glow) return;
            _pulse = Mathf.MoveTowards(_pulse, 0f, Time.deltaTime * 2.5f);
            float breathe = 1f + 0.04f * Mathf.Sin(Time.time * 2f) + 0.12f * _pulse;
            var r = glow.GetComponent<Renderer>();
            if (r) r.material.color = Color.Lerp(new Color(0.2f, 0.6f, 1f), new Color(0.6f, 0.9f, 1f), _pulse) * breathe;
        }
    }
}
