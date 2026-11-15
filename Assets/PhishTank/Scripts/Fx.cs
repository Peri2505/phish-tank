using UnityEngine;

namespace PhishTank
{
    /// <summary>Lightweight particle bursts made in code (confetti, splash, bubbles).</summary>
    public static class Fx
    {
        static Material _mat;

        static Material Mat
        {
            get
            {
                if (_mat == null)
                {
                    var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
                    _mat = new Material(sh);
                }
                return _mat;
            }
        }

        /// <summary>A quick burst of colored particles. Self-destroys.</summary>
        public static void Burst(Vector3 pos, Color a, Color b, int count = 30, float speed = 1.2f, float size = 0.02f, float gravity = 0.6f)
        {
            var go = new GameObject("FX Burst");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count;
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.03f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = Mat;
            ps.Play();
            Object.Destroy(go, 2f);
        }

        public static void Correct(Vector3 pos) => Burst(pos, new Color(0.3f, 1f, 0.5f), new Color(1f, 0.85f, 0.2f), 40, 1.4f, 0.022f, 0.8f);
        public static void Wrong(Vector3 pos)   => Burst(pos, new Color(1f, 0.25f, 0.25f), new Color(0.5f, 0.1f, 0.1f), 25, 0.8f, 0.03f, 0.2f);
        public static void Bubbles(Vector3 pos) => Burst(pos, new Color(0.6f, 0.9f, 1f), new Color(1f, 1f, 1f, 0.7f), 18, 0.5f, 0.018f, -0.15f);
        public static void Combo(Vector3 pos)   => Burst(pos, new Color(1f, 0.85f, 0.2f), new Color(1f, 0.4f, 0.9f), 60, 2f, 0.02f, 0.5f);
    }
}
