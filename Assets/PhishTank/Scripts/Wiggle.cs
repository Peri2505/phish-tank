using UnityEngine;

namespace PhishTank
{
    /// <summary>Tail swish / seaweed sway / gentle bob. Cheap sine animation, no Animator needed.</summary>
    public class Wiggle : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        public float angle = 25f;
        public float speed = 6f;
        public float phase;
        [Tooltip("Speed up with the parent rigidbody's velocity (fish swim faster = tail beats faster).")]
        public bool followVelocity;

        private Quaternion _base;
        private Rigidbody _rb;
        private float _t;

        private void Awake()
        {
            _base = transform.localRotation;
            if (phase == 0f) phase = Random.value * 10f;
            if (followVelocity) _rb = GetComponentInParent<Rigidbody>();
        }

        private void Update()
        {
            float mult = 1f;
            if (_rb != null) mult = 1f + Mathf.Clamp(_rb.linearVelocity.magnitude * 4f, 0f, 3f);
            _t += Time.deltaTime * speed * mult;
            transform.localRotation = _base * Quaternion.AngleAxis(Mathf.Sin(_t + phase) * angle, axis);
        }
    }
}
