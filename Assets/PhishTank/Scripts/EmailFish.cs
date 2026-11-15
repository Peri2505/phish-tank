using System.Collections;
using Oculus.Interaction;
using UnityEngine;

namespace PhishTank
{
    /// <summary>
    /// A swimming email. Swims around the spawn area until you grab it.
    /// While held, its email card opens. Throw it into the SAFE or SCAM bin.
    ///
    /// Grabbing comes from Meta's Interaction SDK: the fish prefab has a Grabbable
    /// plus HandGrab / DistanceHandGrab interactables (added by PhishTank > Build Game Scene).
    /// This script listens to the Grabbable's pointer events directly.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class EmailFish : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EmailCard card;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private ParticleSystem popEffect;
        [Tooltip("Meta Interaction SDK Grabbable on this fish. Found automatically if left empty.")]
        [SerializeField] private Grabbable grabbable;

        [Header("Swimming")]
        [SerializeField] private float swimSpeed = 0.12f;      // metres per second
        [SerializeField] private float steering = 1.5f;        // how quickly it turns
        [SerializeField] private float resumeSwimDelay = 1.5f; // after a throw that missed

        public EmailData Data { get; private set; }
        public bool IsGrabbed { get; private set; }
        public bool Judged { get; private set; }
        /// <summary>True while held or for a few seconds after a throw — swimming fish can't fall into a bin by accident.</summary>
        /// <summary>Belly up = the fish shows its "true colors" (real sender + real link).</summary>
        public bool IsFlipped => IsGrabbed && Vector3.Dot(transform.up, Vector3.up) < -0.25f;
        /// <summary>Did the player inspect this fish before sorting it? (Inspector bonus)</summary>
        public bool WasFlipped { get; private set; }
        public void ForceReveal() { WasFlipped = true; _forceReveal = true; }
        public bool Revealed => _forceReveal || IsFlipped;
        private bool _forceReveal;
        public bool CanBeSorted => IsGrabbed || (Time.time - _releasedAt) < 3f;
        private float _releasedAt = -999f;

        private Rigidbody _rb;
        private Bounds _area;
        private Vector3 _target;
        private float _speedMultiplier = 1f;
        private bool _swimming;
        private float _bobOffset;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;          // fish float
            _rb.linearDamping = 0.6f;        // thrown fish slow down instead of flying forever
            _rb.angularDamping = 2f;
            _bobOffset = Random.value * 10f;
            if (card != null) card.Hide();
            if (grabbable == null) grabbable = GetComponent<Grabbable>();
        }

        private void OnEnable()
        {
            if (grabbable != null) grabbable.WhenPointerEventRaised += HandlePointerEvent;
        }

        private void OnDisable()
        {
            if (grabbable != null) grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }

        private int _selectCount;

        // Hand grab / distance grab -> our game events.
        private void HandlePointerEvent(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Select:
                    if (_selectCount++ == 0) OnGrabbed();
                    break;
                case PointerEventType.Unselect:
                case PointerEventType.Cancel:
                    if (_selectCount > 0 && --_selectCount == 0) OnReleased();
                    break;
            }
        }

        public void Init(EmailData data, Bounds swimArea, float speedMultiplier, Color tint)
        {
            Data = data;
            _area = swimArea;
            _speedMultiplier = speedMultiplier;
            if (bodyRenderer != null) bodyRenderer.material.color = tint;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r.name.StartsWith("Fin") || r.name.StartsWith("Tail"))
                    r.material.color = Color.Lerp(tint, Color.white, 0.35f);
            PickNewTarget();
            _swimming = true;
        }

        private void FixedUpdate()
        {
            if (!_swimming || IsGrabbed || Judged) return;

            Vector3 toTarget = _target - transform.position;
            if (toTarget.magnitude < 0.08f) PickNewTarget();

            Vector3 desired = toTarget.normalized * swimSpeed * _speedMultiplier;
            desired.y += Mathf.Sin(Time.time * 2f + _bobOffset) * 0.02f; // gentle bobbing
            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, desired, steering * Time.fixedDeltaTime);

            if (_rb.linearVelocity.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(_rb.linearVelocity.normalized, Vector3.up);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, look, steering * Time.fixedDeltaTime));
            }

            // Keep it from wandering off: gently pull back inside the area.
            if (!_area.Contains(transform.position)) _target = _area.center;
        }

        private void PickNewTarget()
        {
            _target = new Vector3(
                Random.Range(_area.min.x, _area.max.x),
                Random.Range(_area.min.y, _area.max.y),
                Random.Range(_area.min.z, _area.max.z));
        }

        // ---- Called by the Interaction SDK event wrapper ----

        public void OnGrabbed()
        {
            if (Judged) return;
            IsGrabbed = true;
            _swimming = false;
            if (card != null) card.Show(Data, this);
            GameManager.Instance?.NotifyFishGrabbed(this);
            SoundBank.Play(SoundBank.Sfx.Grab, transform.position);
            TutorialCoach.Instance?.OnGrab(this);
        }

        private bool _wasFlippedLastFrame;
        private void Update()
        {
            bool flipped = IsFlipped;
            if (flipped && !_wasFlippedLastFrame)
            {
                if (!WasFlipped) SoundBank.Play(SoundBank.Sfx.Flip, transform.position);
                WasFlipped = true;
                TutorialCoach.Instance?.OnFlip(this);
            }
            _wasFlippedLastFrame = flipped;
        }

        /// <summary>Swim out of the inbox portal toward the player.</summary>
        public void Launch(Vector3 velocity)
        {
            _rb.linearVelocity = velocity;
            if (velocity.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
        }

        [Header("Throw assist")]
        [Tooltip("Release (or flick) the fish to the left/right and it glides into that net. Makes seated hand play forgiving.")]
        [SerializeField] private bool throwAssist = true;
        [SerializeField] private float assistDeadZone = 0.07f;   // metres from centre: release here = put it back

        public void OnReleased()
        {
            IsGrabbed = false;
            _releasedAt = Time.time;
            if (card != null) card.Hide();
            if (Judged) return;
            if (throwAssist && TryAssistThrow()) return;
            StartCoroutine(ResumeSwimmingAfterDelay());
        }

        private bool TryAssistThrow()
        {
            var head = Camera.main;
            if (head == null) return false;
            Vector3 local = head.transform.InverseTransformPoint(transform.position);
            float sideSpeed = Vector3.Dot(_rb.linearVelocity, head.transform.right);
            float lateral = local.x + sideSpeed * 0.25f;            // where you let go + which way you flicked
            if (Mathf.Abs(lateral) < assistDeadZone) return false;  // released in the middle: it swims away
            bool scam = lateral > 0f;                               // SCAM net is on the right, SAFE on the left
            SortBin target = null;
            foreach (var b in FindObjectsByType<SortBin>(FindObjectsSortMode.None))
                if (b.IsScamBin == scam) { target = b; break; }
            if (target == null) return false;
            StartCoroutine(GlideInto(target, scam));
            return true;
        }

        private IEnumerator GlideInto(SortBin bin, bool scam)
        {
            Vector3 start = transform.position;
            Vector3 end = bin.GetComponent<Collider>().bounds.center;
            Quaternion r0 = transform.rotation;
            Quaternion dive = Quaternion.LookRotation((end - start).normalized, Vector3.up);
            _rb.isKinematic = true;
            for (float t = 0f; t < 1f && !Judged; t += Time.deltaTime / 0.65f)
            {
                float k = Mathf.SmoothStep(0f, 1f, t);
                transform.SetPositionAndRotation(
                    Vector3.Lerp(start, end, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.1f,
                    Quaternion.Slerp(r0, dive, k));
                yield return null;
            }
            if (!Judged) GameManager.Instance?.Judge(this, scam);
        }

        private IEnumerator ResumeSwimmingAfterDelay()
        {
            yield return new WaitForSeconds(resumeSwimDelay);
            if (!Judged && !IsGrabbed)
            {
                PickNewTarget();
                _swimming = true;
            }
        }

        /// <summary>Called by GameManager after the fish lands in a bin.</summary>
        public void MarkJudged()
        {
            Judged = true;
            _swimming = false;
            if (card != null) card.Hide();
            if (popEffect != null)
            {
                popEffect.transform.SetParent(null);
                popEffect.Play();
                Destroy(popEffect.gameObject, 2f);
            }
            Destroy(gameObject, 0.05f);
        }
    }
}
