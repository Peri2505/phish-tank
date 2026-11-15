using Oculus.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace PhishTank
{
    /// <summary>
    /// A floating glowing orb that acts as a button: pinch or grab it to trigger it.
    /// Fully hands-first (no controller, no laser pointer needed), and it floats back home when released.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ActionOrb : MonoBehaviour
    {
        [SerializeField] private Grabbable grabbable;
        [SerializeField] private TMP_Text label;
        [SerializeField] private UnityEvent onActivated = new UnityEvent();
        [SerializeField] private float bobHeight = 0.015f;

        private Vector3 _home;
        private Quaternion _homeRot;
        private Rigidbody _rb;
        private bool _held;

        public void SetLabel(string text) { if (label != null) label.text = text; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.isKinematic = true;
            if (grabbable == null) grabbable = GetComponent<Grabbable>();
        }

        private void OnEnable()
        {
            _home = transform.localPosition;
            _homeRot = transform.localRotation;
            if (grabbable != null) grabbable.WhenPointerEventRaised += OnPointer;
        }

        private void OnDisable()
        {
            if (grabbable != null) grabbable.WhenPointerEventRaised -= OnPointer;
            _held = false;
            transform.localPosition = _home;
            transform.localRotation = _homeRot;
        }

        private void OnPointer(PointerEvent evt)
        {
            if (evt.Type == PointerEventType.Select && !_held)
            {
                _held = true;
                onActivated.Invoke();
            }
            else if (evt.Type == PointerEventType.Unselect || evt.Type == PointerEventType.Cancel)
            {
                _held = false;
            }
        }

        private void Update()
        {
            if (_held) return;
            // Drift home and bob gently.
            Vector3 target = _home + Vector3.up * Mathf.Sin(Time.time * 2f) * bobHeight;
            transform.localPosition = Vector3.Lerp(transform.localPosition, target, 8f * Time.deltaTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _homeRot, 8f * Time.deltaTime);
        }

        /// <summary>Lets the editor/simulator trigger it too (DevShortcuts).</summary>
        public void Activate() => onActivated.Invoke();
    }
}
