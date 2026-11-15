using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace PhishTank
{
    /// <summary>
    /// Mixed reality: reads the room scan (Meta MR Utility Kit).
    ///  - Real TV / monitor found  -> fish burst out of it (InboxPortal moves onto the screen).
    ///  - Real table found         -> the aquarium sits on your table.
    /// Falls back gracefully when there is no scan (simulator without a room, or skipped permission).
    /// </summary>
    public class RoomSetup : MonoBehaviour
    {
        [SerializeField] private InboxPortal portal;
        [SerializeField] private Transform aquarium;
        [SerializeField] private float maxScreenDistance = 4f;

        public static bool HasRoom { get; private set; }

        private void Start()
        {
            if (MRUK.Instance == null) { Debug.Log("[PhishTank] No MRUK in scene — virtual inbox mode."); return; }
            MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
        }

        private void OnSceneLoaded()
        {
            var room = MRUK.Instance.GetCurrentRoom();
            if (room == null) return;
            HasRoom = true;
            var cam = Camera.main;
            Vector3 head = cam ? cam.transform.position : Vector3.zero;
            Vector3 fwd = cam ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;

            MRUKAnchor screen = null, table = null;
            float bestScreen = float.MaxValue, bestTable = float.MaxValue;
            foreach (var a in room.Anchors)
            {
                Vector3 p = AnchorCenter(a);
                float d = Vector3.Distance(head, p);
                if (a.HasAnyLabel(MRUKAnchor.SceneLabels.SCREEN) && d < maxScreenDistance)
                {
                    // prefer screens in front of the player
                    float score = d - Vector3.Dot((p - head).normalized, fwd);
                    if (score < bestScreen) { bestScreen = score; screen = a; }
                }
                if (a.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE) && d < bestTable) { bestTable = d; table = a; }
            }

            if (screen != null && portal != null)
            {
                Vector3 c = AnchorCenter(screen);
                Vector2 size = screen.PlaneRect.HasValue ? screen.PlaneRect.Value.size
                             : screen.VolumeBounds.HasValue ? new Vector2(screen.VolumeBounds.Value.size.x, screen.VolumeBounds.Value.size.y) : new Vector2(0.6f, 0.4f);
                Vector3 toPlayer = Vector3.ProjectOnPlane(head - c, Vector3.up);
                if (toPlayer.sqrMagnitude < 0.01f) toPlayer = -fwd;
                portal.transform.SetParent(null, true);   // stays on the real screen, doesn't follow the head
                portal.AttachToRealScreen(c, toPlayer, size);
                Debug.Log($"[PhishTank] Real screen found {Vector3.Distance(head, c):F1} m away — fish will swim out of it.");
            }
            else Debug.Log("[PhishTank] No screen in the room scan — using the virtual laptop.");

            if (table != null && aquarium != null)
            {
                Vector3 top = AnchorCenter(table);
                if (table.VolumeBounds.HasValue) top.y = table.transform.position.y; // volume anchor sits on the top surface
                aquarium.SetParent(null, true);
                aquarium.position = top + Vector3.up * 0.01f;
                Vector3 toPlayer = Vector3.ProjectOnPlane(head - top, Vector3.up);
                if (toPlayer.sqrMagnitude > 0.01f) aquarium.rotation = Quaternion.LookRotation(-toPlayer.normalized, Vector3.up);
                Debug.Log("[PhishTank] Aquarium placed on your real table.");
            }
        }

        static Vector3 AnchorCenter(MRUKAnchor a)
        {
            if (a.VolumeBounds.HasValue) return a.transform.TransformPoint(a.VolumeBounds.Value.center);
            if (a.PlaneRect.HasValue) return a.transform.TransformPoint(a.PlaneRect.Value.center);
            return a.transform.position;
        }
    }
}
