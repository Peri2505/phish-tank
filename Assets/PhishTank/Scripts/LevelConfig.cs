using UnityEngine;

namespace PhishTank
{
    /// <summary>
    /// One level. Create via Assets > Create > PhishTank > Level Config.
    /// Suggested 5 levels (short = passes the "one bus stop" test):
    ///   1 "Tide Pool"   : 5 fish,  diff 1, scams 0.6, speed 0.8
    ///   2 "Reef"        : 6 fish,  diff 1, scams 0.5, speed 1.0
    ///   3 "Kelp Forest" : 7 fish,  diff 2, scams 0.5, speed 1.1
    ///   4 "Open Sea"    : 8 fish,  diff 2, scams 0.5, speed 1.3
    ///   5 "The Deep"    : 8 fish,  diff 3, scams 0.5, speed 1.4
    /// </summary>
    [CreateAssetMenu(menuName = "PhishTank/Level Config", fileName = "Level")]
    public class LevelConfig : ScriptableObject
    {
        public string levelName = "Tide Pool";
        [Range(1, 12)] public int fishCount = 5;
        [Range(1, 3)] public int maxDifficulty = 1;
        [Range(0f, 1f)] public float scamRatio = 0.5f;
        [Range(0.5f, 2f)] public float speedMultiplier = 1f;
        [Tooltip("Seconds between fish appearing.")]
        public float spawnInterval = 1.2f;
    }
}
