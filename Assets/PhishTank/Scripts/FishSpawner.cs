using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhishTank
{
    /// <summary>Spawns one fish per email, a little apart in time, inside the SpawnArea.</summary>
    public class FishSpawner : MonoBehaviour
    {
        [SerializeField] private EmailFish fishPrefab;
        [SerializeField] private SpawnArea spawnArea;
        [Tooltip("Fish colours. All fish use the same palette — colour must NOT give away the answer.")]
        [SerializeField] private Color[] palette =
        {
            new Color(1f, 0.62f, 0.25f), new Color(0.35f, 0.78f, 1f),
            new Color(0.98f, 0.85f, 0.3f), new Color(0.7f, 0.5f, 1f),
            new Color(0.4f, 0.95f, 0.7f)
        };

        private readonly List<EmailFish> _alive = new List<EmailFish>();
        public int AliveCount { get { _alive.RemoveAll(f => f == null); return _alive.Count; } }

        public Coroutine SpawnBatch(List<EmailData> emails, LevelConfig level)
            => StartCoroutine(SpawnRoutine(emails, level));

        private IEnumerator SpawnRoutine(List<EmailData> emails, LevelConfig level)
        {
            foreach (var email in emails)
            {
                Bounds area = spawnArea.GetBounds();
                Vector3 pos;
                var portal = InboxPortal.Instance;
                if (portal != null)
                {
                    pos = portal.SpawnPosition + Random.insideUnitSphere * 0.05f;   // burst out of the (real or virtual) screen
                    portal.Emit();
                }
                else
                {
                    pos = area.ClosestPoint(new Vector3(Random.Range(area.min.x, area.max.x), Random.Range(area.min.y, area.max.y), area.max.z));
                }

                var fish = Instantiate(fishPrefab, pos, Quaternion.identity);
                fish.Init(email, area, level.speedMultiplier, palette[Random.Range(0, palette.Length)]);
                if (portal != null) fish.Launch((area.center - pos).normalized * 0.9f + Random.insideUnitSphere * 0.15f);
                _alive.Add(fish);
                if (portal != null) portal.SetUnread(emails.Count - _alive.Count);

                yield return new WaitForSeconds(level.spawnInterval);
            }
        }

        public void ClearAll()
        {
            StopAllCoroutines();
            foreach (var f in _alive) if (f != null) Destroy(f.gameObject);
            _alive.Clear();
        }
    }
}
