using UnityEngine;

namespace PhishTank
{
    /// <summary>Little fish circling inside the aquarium.</summary>
    public class Orbit : MonoBehaviour
    {
        public float radius = 0.12f;
        public float height = 0.08f;
        public float speed = 0.6f;
        public float phase;

        private void Update()
        {
            float a = Time.time * speed + phase;
            transform.localPosition = new Vector3(Mathf.Cos(a) * radius, height + Mathf.Sin(a * 2f) * 0.015f, Mathf.Sin(a) * radius * 0.45f);
            Vector3 dir = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a) * 0.45f);
            transform.localRotation = Quaternion.LookRotation(dir * Mathf.Sign(speed), Vector3.up);
        }
    }
}
