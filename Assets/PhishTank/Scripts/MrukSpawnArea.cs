// Optional: makes the fish swim above your REAL table using Meta's MR Utility Kit (MRUK).
// It only compiles when you add the scripting define symbol PHISHTANK_MRUK
// (Project Settings > Player > Android > Scripting Define Symbols) after installing MRUK.
// Without it, the game simply uses the head-relative box from SpawnArea.
#if PHISHTANK_MRUK
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace PhishTank
{
    public class MrukSpawnArea : MonoBehaviour
    {
        [SerializeField] private SpawnArea spawnArea;
        [SerializeField] private float heightAboveTable = 0.25f;
        [SerializeField] private float boxHeight = 0.35f;

        private void Start()
        {
            if (MRUK.Instance == null)
            {
                Debug.LogWarning("[PhishTank] No MRUK in scene — using head-relative spawn area.");
                return;
            }
            MRUK.Instance.RegisterSceneLoadedCallback(OnSceneLoaded);
        }

        private void OnSceneLoaded()
        {
            var room = MRUK.Instance.GetCurrentRoom();
            if (room == null) return;

            MRUKAnchor bestTable = null;
            float bestDistance = float.MaxValue;
            Vector3 head = Camera.main.transform.position;

            foreach (var anchor in room.Anchors)
            {
                if (!anchor.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE)) continue;
                float d = Vector3.Distance(head, anchor.transform.position);
                if (d < bestDistance) { bestDistance = d; bestTable = anchor; }
            }

            if (bestTable == null || !bestTable.PlaneRect.HasValue)
            {
                Debug.Log("[PhishTank] No table found — using head-relative spawn area.");
                return;
            }

            Rect r = bestTable.PlaneRect.Value;
            Vector3 center = bestTable.transform.position + Vector3.up * (heightAboveTable + boxHeight * 0.5f);
            Vector3 size = new Vector3(Mathf.Min(r.width, 1.2f), boxHeight, Mathf.Min(r.height, 0.8f));
            spawnArea.SetOverride(new Bounds(center, size));
            Debug.Log($"[PhishTank] Fish will swim above your table ({size.x:F2} x {size.z:F2} m).");
        }
    }
}
#endif
