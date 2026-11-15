using UnityEngine;

namespace PhishTank
{
    /// <summary>
    /// All game sounds, synthesized at startup (no audio files needed, nothing to license).
    /// Call SoundBank.Play(SoundBank.Sfx.Correct, position).
    /// </summary>
    public class SoundBank : MonoBehaviour
    {
        public enum Sfx { Grab, Correct, Wrong, Combo, Splash, Bubble, LevelUp, Flip, Bonus }

        public static SoundBank Instance { get; private set; }

        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.12f;

        private AudioClip[] _clips;
        private AudioSource _music;
        const int Rate = 44100;

        private void Awake()
        {
            Instance = this;
            _clips = new AudioClip[System.Enum.GetValues(typeof(Sfx)).Length];
            _clips[(int)Sfx.Grab]    = Tone("grab",   new[] { 660f, 880f }, 0.07f, 0.9f, Wave.Sine);
            _clips[(int)Sfx.Correct] = Tone("correct",new[] { 784f, 1046f, 1318f }, 0.09f, 0.8f, Wave.Sine);
            _clips[(int)Sfx.Wrong]   = Tone("wrong",  new[] { 220f, 175f }, 0.16f, 0.7f, Wave.Square);
            _clips[(int)Sfx.Combo]   = Tone("combo",  new[] { 523f, 659f, 784f, 1046f, 1318f }, 0.06f, 0.8f, Wave.Triangle);
            _clips[(int)Sfx.Bubble]  = Bubble("bubble");
            _clips[(int)Sfx.Splash]  = Noise("splash", 0.35f);
            _clips[(int)Sfx.LevelUp] = Tone("level",  new[] { 523f, 659f, 784f, 1046f, 784f, 1046f }, 0.11f, 0.8f, Wave.Triangle);
            _clips[(int)Sfx.Flip]    = Tone("flip",   new[] { 900f, 1200f, 1500f }, 0.04f, 0.6f, Wave.Sine);
            _clips[(int)Sfx.Bonus]   = Tone("bonus",  new[] { 1318f, 1568f, 2093f }, 0.06f, 0.7f, Wave.Sine);

            _music = gameObject.AddComponent<AudioSource>();
            _music.clip = Pad("ocean-pad");
            _music.loop = true;
            _music.volume = musicVolume;
            _music.spatialBlend = 0f;
            _music.Play();
        }

        public static void Play(Sfx s, Vector3 at, float volume = 1f)
        {
            if (Instance == null) return;
            var clip = Instance._clips[(int)s];
            if (clip != null) AudioSource.PlayClipAtPoint(clip, at, Instance.sfxVolume * volume);
        }

        // ---------------- tiny synth ----------------
        enum Wave { Sine, Square, Triangle }

        static float Osc(Wave w, float phase)
        {
            switch (w)
            {
                case Wave.Square: return Mathf.Sin(phase) >= 0 ? 0.5f : -0.5f;
                case Wave.Triangle: return 2f * Mathf.Abs(2f * (phase / (2f * Mathf.PI) - Mathf.Floor(phase / (2f * Mathf.PI) + 0.5f))) - 1f;
                default: return Mathf.Sin(phase);
            }
        }

        static AudioClip Tone(string name, float[] notes, float noteLen, float gain, Wave w)
        {
            int perNote = Mathf.CeilToInt(noteLen * Rate);
            int tail = Mathf.CeilToInt(0.15f * Rate);
            var data = new float[perNote * notes.Length + tail];
            for (int n = 0; n < notes.Length; n++)
            {
                int start = n * perNote;
                int len = perNote + tail;
                for (int i = 0; i < len && start + i < data.Length; i++)
                {
                    float t = i / (float)Rate;
                    float env = Mathf.Min(1f, t * 200f) * Mathf.Exp(-t * 9f);
                    data[start + i] += Osc(w, 2f * Mathf.PI * notes[n] * t) * env * gain * 0.5f;
                }
            }
            return Make(name, data);
        }

        static AudioClip Bubble(string name)
        {
            int len = Mathf.CeilToInt(0.18f * Rate);
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float f = 300f + 1400f * t / 0.18f;          // rising "bloop"
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 18f) * 0.6f;
            }
            return Make(name, data);
        }

        static AudioClip Noise(string name, float seconds)
        {
            int len = Mathf.CeilToInt(seconds * Rate);
            var data = new float[len];
            var rng = new System.Random(7);
            float lp = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float n = (float)(rng.NextDouble() * 2 - 1);
                lp += (n - lp) * 0.25f;                      // soften = water-ish
                data[i] = lp * Mathf.Exp(-t * 9f) * 0.9f;
            }
            return Make(name, data);
        }

        static AudioClip Pad(string name)
        {
            float seconds = 16f;
            int len = Mathf.CeilToInt(seconds * Rate);
            var data = new float[len];
            float[][] chords = { new[] { 220f, 277.2f, 329.6f }, new[] { 196f, 246.9f, 293.7f },
                                 new[] { 174.6f, 220f, 261.6f }, new[] { 196f, 246.9f, 329.6f } };
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                int c = (int)(t / 4f) % chords.Length;
                float local = t % 4f;
                float env = Mathf.Clamp01(local / 1.2f) * Mathf.Clamp01((4f - local) / 1.2f);
                float s = 0f;
                foreach (var f in chords[c]) s += Mathf.Sin(2f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * f * 2f * t);
                data[i] = s * env * 0.08f * (0.85f + 0.15f * Mathf.Sin(t * 0.7f));
            }
            return Make(name, data);
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
