#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PhishTank.EditorTools
{
    /// <summary>Menu: PhishTank > Move Editor To Laptop Screen (for screen recording).</summary>
    public static class PhishTankWindowMover
    {
        [MenuItem("PhishTank/Move Editor To Laptop Screen")]
        public static void Move()
        {
            EditorGUIUtility.SetMainWindowPosition(new Rect(0, 33, 1512, 920));
            var tester = Resources.FindObjectsOfTypeAll<EditorWindow>()
                .FirstOrDefault(w => w.titleContent.text == "Phish Tank Tester");
            if (tester != null) tester.position = new Rect(1180, 120, 320, 560);
            Debug.Log("[PhishTank] Editor moved to the laptop screen.");
        }
    }
}
#endif
