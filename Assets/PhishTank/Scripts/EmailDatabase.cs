using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PhishTank
{
    /// <summary>Loads all emails from JSON and hands out a shuffled batch for each level.</summary>
    public class EmailDatabase : MonoBehaviour
    {
        [SerializeField] private TextAsset emailsJson;

        private List<EmailData> _all = new List<EmailData>();

        private void Awake()
        {
            if (emailsJson == null)
            {
                Debug.LogError("[PhishTank] EmailDatabase: assign Data/emails.json in the Inspector.");
                return;
            }
            var list = JsonUtility.FromJson<EmailList>(emailsJson.text);
            _all = list?.emails ?? new List<EmailData>();
            Debug.Log($"[PhishTank] Loaded {_all.Count} emails.");
        }

        /// <summary>Returns `count` emails up to `maxDifficulty`, with roughly `scamRatio` of them scams.</summary>
        public List<EmailData> GetBatch(int maxDifficulty, int count, float scamRatio)
        {
            var pool = _all.Where(e => e.difficulty <= maxDifficulty)
                           .OrderBy(_ => Random.value).ToList();

            int scamCount = Mathf.RoundToInt(count * scamRatio);
            var scams = pool.Where(e => e.isScam).Take(scamCount);
            var legit = pool.Where(e => !e.isScam).Take(count - scamCount);

            var batch = scams.Concat(legit).OrderBy(_ => Random.value).ToList();

            // If the pool was too small, top up with anything left.
            if (batch.Count < count)
                batch.AddRange(pool.Except(batch).Take(count - batch.Count));

            return batch;
        }
    }
}
