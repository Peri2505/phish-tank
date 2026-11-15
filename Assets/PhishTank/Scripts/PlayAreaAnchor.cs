using System.Collections;
using UnityEngine;

namespace PhishTank
{
    /// <summary>
    /// Keeps the whole game (menus, bins, HUD, aquarium) arranged around the player's head,
    /// whatever their height (seated or standing) and wherever they face when the game starts.
    /// The "Play Area" root's origin = the player's eyes.
    /// </summary>
    public class PlayAreaAnchor : MonoBehaviour
    {
        [SerializeField] private float startDelay = 0.4f; // let head tracking settle

        public static PlayAreaAnchor Instance { get; private set; }

        private void Awake() => Instance = this;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(startDelay);
            Recenter();
        }

        /// <summary>Move the play area in front of the player's current head pose (yaw only).</summary>
        public void Recenter()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            transform.SetPositionAndRotation(cam.transform.position, Quaternion.LookRotation(fwd.normalized, Vector3.up));
        }
    }
}
