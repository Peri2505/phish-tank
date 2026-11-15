using TMPro;
using UnityEngine;

namespace PhishTank
{
    /// <summary>
    /// The small world-space "email" that pops up above a fish while you hold it.
    /// Put this on a World Space Canvas (child of the fish), about 0.25 m wide.
    /// </summary>
    public class EmailCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text fromText;
        [SerializeField] private TMP_Text subjectText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text linkText;
        [SerializeField] private TMP_Text attachmentText;
        [Tooltip("If set, the whole email is written into this ONE text block (lines flow, never overlap).")]
        [SerializeField] private TMP_Text contentText;
        [SerializeField] private Vector3 offsetAboveFish = new Vector3(0f, 0.14f, 0f);

        private Transform _head;
        private Transform _fish;

        private void Awake()
        {
            _fish = transform.parent;
            if (Camera.main != null) _head = Camera.main.transform;
        }

        private EmailData _data;
        private EmailFish _owner;
        private bool _lastRevealed;

        public void Show(EmailData d) => Show(d, null);

        public void Show(EmailData d, EmailFish owner)
        {
            _data = d;
            _owner = owner;
            _lastRevealed = owner != null && owner.Revealed;
            Render();
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Front of the fish = what an email client shows (display name, link text).
        /// Belly (flip the fish) = the truth: real sender address and where the link really goes.
        /// </summary>
        private void Render()
        {
            var d = _data;
            if (d == null) return;
            bool truth = _owner == null || _owner.Revealed;
            if (contentText != null)
            {
                var sb = new System.Text.StringBuilder();
                if (truth) sb.Append("<size=80%><color=#FFB86B><b>TRUE COLORS</b></color></size>\n");
                sb.Append($"<size=85%><color=#9AA4B2>From:</color> <b>{d.fromName}</b>\n");
                sb.Append(truth ? $"<color=#FFB86B>{d.fromAddress}</color>" : "<color=#6C7A8C><i>flip the fish to see the real address</i></color>");
                sb.Append("</size>\n");
                sb.Append($"<size=110%><b>{d.subject}</b></size>\n");
                sb.Append($"{d.body}\n");
                if (!string.IsNullOrEmpty(d.linkText))
                    sb.Append($"\n<u><color=#4EA8FF>{d.linkText}</color></u>\n<size=80%>" +
                              (truth ? $"<color=#FFB86B>goes to: {d.linkUrl}</color>" : "<color=#6C7A8C><i>flip to see where it really goes</i></color>") + "</size>\n");
                if (!string.IsNullOrEmpty(d.attachment))
                    sb.Append($"\n<color=#9AA4B2>Attachment:</color> <b>{d.attachment}</b>");
                contentText.text = sb.ToString();
                return;
            }
            // legacy multi-field layout
            fromText.text = $"<b>{d.fromName}</b>  <color=#9AA4B2>{d.fromAddress}</color>";
            subjectText.text = d.subject;
            bodyText.text = d.body;
            bool hasLink = !string.IsNullOrEmpty(d.linkText);
            linkText.gameObject.SetActive(hasLink);
            if (hasLink) linkText.text = $"<u><color=#4EA8FF>{d.linkText}</color></u>\n<size=70%><color=#9AA4B2>goes to: {d.linkUrl}</color></size>";
            bool hasAttachment = !string.IsNullOrEmpty(d.attachment);
            attachmentText.gameObject.SetActive(hasAttachment);
            if (hasAttachment) attachmentText.text = $"<color=#9AA4B2>Attachment:</color> {d.attachment}";
        }

        public void Hide() => gameObject.SetActive(false);

        private void LateUpdate()
        {
            if (_owner != null && _owner.Revealed != _lastRevealed) { _lastRevealed = _owner.Revealed; Render(); }
            if (_fish == null || _head == null) return;

            // Float above the fish in world space (ignore the fish's rotation).
            transform.position = _fish.position + offsetAboveFish;

            // Always face the player so it's readable.
            Vector3 away = transform.position - _head.position;
            if (away.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }
    }
}
