#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace PhishTank.EditorTools
{
    /// <summary>
    /// Menu: PhishTank > Record Demo Video
    /// Plays one and a half levels on autopilot (catch, read, flip, throw, one deliberate mistake)
    /// and records the Game view to <project>/Recordings/PhishTank_demo.mp4 with Unity Recorder.
    /// No desktop, cursor or overlays end up in the video.
    /// </summary>
    [InitializeOnLoad]
    public static class PhishTankDemoRecorder
    {
        const string FlagKey = "PhishTank.RecordDemo";
        static RecorderController _controller;
        static IEnumerator _routine;
        static double _waitUntil;
        const string DemoTag = "DemoCam";
        static Camera _demoCam;
        static RenderTexture _demoRT;
        static readonly System.Collections.Generic.List<Object> _tileOnlyRenderers = new System.Collections.Generic.List<Object>();

        /// <summary>Tile-Only Mode (a Quest speed setting) forbids the extra video camera; switch it off only while recording.</summary>
        static void SetTileOnly(bool recording)
        {
            if (recording)
            {
                _tileOnlyRenderers.Clear();
                foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
                {
                    var r = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
                    var so = new SerializedObject(r);
                    var p = so.FindProperty("m_TileOnlyMode");
                    if (p != null && p.boolValue) { p.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); _tileOnlyRenderers.Add(r); }
                }
            }
            else
            {
                foreach (var r in _tileOnlyRenderers)
                {
                    if (r == null) continue;
                    var so = new SerializedObject(r);
                    so.FindProperty("m_TileOnlyMode").boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(r);
                }
                _tileOnlyRenderers.Clear();
                AssetDatabase.SaveAssets();
            }
        }

        static void EnsureTag(string tag)
        {
            if (UnityEditorInternal.InternalEditorUtility.tags.Contains(tag)) return;
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tags = tm.FindProperty("tags");
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            tm.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A wider "over the shoulder" camera that follows the head, so bins, HUD and feedback are all in frame.</summary>
        static void CreateDemoCamera()
        {
            var go = new GameObject("Demo Recording Camera");
            go.tag = DemoTag;
            _demoCam = go.AddComponent<Camera>();
            _demoCam.stereoTargetEye = StereoTargetEyeMask.None;
            _demoRT = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { name = "DemoRT", antiAliasing = 4 };
            _demoRT.Create();
            _demoCam.targetTexture = _demoRT;          // renders only into the video, never the headset
            _demoCam.fieldOfView = 72f;
            _demoCam.nearClipPlane = 0.02f;
            _demoCam.clearFlags = CameraClearFlags.SolidColor;
            _demoCam.backgroundColor = new Color(0.035f, 0.05f, 0.08f);
            FollowHead(true);
        }

        /// <summary>The flat video shows a narrower view than the headset, so pull the nets and result panel a little toward the centre.</summary>
        static void FrameForVideo()
        {
            void Move(string name, Vector3 localPos, float yaw)
            {
                var go = GameObject.Find(name);
                if (go == null) { Debug.LogWarning($"[PhishTank] Demo framing: '{name}' not found"); return; }
                go.transform.localPosition = localPos;
                go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            }
            Move("SAFE Inbox", new Vector3(-0.32f, -0.48f, 0.52f), 0f);
            Move("SCAM Net", new Vector3(0.32f, -0.48f, 0.52f), 0f);
            Move("Feedback Panel", new Vector3(0.34f, 0.12f, 0.64f), 25f);
        }

        static void FollowHead(bool snap)
        {
            var head = Camera.main;
            if (_demoCam == null || head == null) return;
            Vector3 fwd = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 pos = head.transform.position - fwd * 0.38f + Vector3.up * 0.12f;
            Quaternion rot = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(16f, 0, 0);
            var t = _demoCam.transform;
            if (snap) t.SetPositionAndRotation(pos, rot);
            else t.SetPositionAndRotation(Vector3.Lerp(t.position, pos, 0.1f), Quaternion.Slerp(t.rotation, rot, 0.1f));
        }

        static PhishTankDemoRecorder()
        {
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(FlagKey, false))
                {
                    SessionState.SetBool(FlagKey, false);
                    Begin();
                }
                if (s == PlayModeStateChange.ExitingPlayMode) Stop("play mode ended");
            };
        }

        static bool _manual;

        /// <summary>Record while YOU play with your hands (no autopilot). Stop Play mode to save.</summary>
        [MenuItem("PhishTank/Record My Own Play")]
        public static void RecordMine()
        {
            _manual = true;
            SessionState.SetBool(FlagKey + ".manual", true);
            if (Application.isPlaying) { Begin(); return; }
            SessionState.SetBool(FlagKey, true);
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("PhishTank/Record Demo Video")]
        public static void Record()
        {
            SessionState.SetBool(FlagKey + ".manual", false);
            if (Application.isPlaying) { Begin(); return; }
            SessionState.SetBool(FlagKey, true);
            EditorApplication.EnterPlaymode();
        }

        static void Begin()
        {
            _manual = SessionState.GetBool(FlagKey + ".manual", false);
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recordings"));
            Directory.CreateDirectory(dir);

            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "PhishTank Demo";
            movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
            };
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = 1920, OutputHeight = 1080 };
            FrameForVideo();
            movie.AudioInputSettings.PreserveAudio = true;
            movie.OutputFile = Path.Combine(dir, _manual ? "PhishTank_my_hands" : "PhishTank_demo");
            controllerSettings.AddRecorderSettings(movie);
            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRate = 30;

            _controller = new RecorderController(controllerSettings);
            _controller.PrepareRecording();
            _controller.StartRecording();
            Debug.Log($"[PhishTank] DEMO RECORDING STARTED -> {movie.OutputFile}.mp4");

            _routine = _manual ? WaitForever() : Script();
            _waitUntil = 0;
            EditorApplication.update += Tick;
        }

        static void Stop(string why)
        {
            EditorApplication.update -= Tick;
            _routine = null;
            if (_controller != null && _controller.IsRecording())
            {
                _controller.StopRecording();
                Debug.Log($"[PhishTank] DEMO RECORDING SAVED ({why}): Recordings/PhishTank_demo.mp4");
            }
            _controller = null;
            if (_demoCam != null) Object.Destroy(_demoCam.gameObject);
            _demoCam = null;
            if (_demoRT != null) { _demoRT.Release(); Object.Destroy(_demoRT); _demoRT = null; }
        }

        static void Tick()
        {
            if (_routine == null || !Application.isPlaying) return;

            if (EditorApplication.timeSinceStartup < _waitUntil) return;
            try
            {
                if (!_routine.MoveNext()) { Stop("finished"); return; }
                if (_routine.Current is float secs) _waitUntil = EditorApplication.timeSinceStartup + secs;
            }
            catch (System.Exception e) { Debug.LogException(e); Stop("error"); }
        }

        static IEnumerator WaitForever()
        {
            while (true) yield return 1f;     // you play; leaving Play mode saves the video
        }

        // ---------- the choreography ----------

        static IEnumerator Script()
        {
            yield return 3f;                                  // title screen
            var gm = GameManager.Instance;
            gm.StartGame();
            yield return 4.5f;                                // fish swim out of the inbox

            int sorted = 0;
            while (gm.State == GameState.Playing)
            {
                bool mistake = sorted == 3;                   // show one wrong sort, so judges see the lesson
                var e = PlayOneFish(mistake);
                while (e.MoveNext()) yield return e.Current;
                sorted++;
                if (sorted > 12) break;
            }

            yield return 5f;                                  // level clear screen
            if (gm.State == GameState.LevelComplete)
            {
                gm.NextLevel();
                yield return 4f;
                for (int i = 0; i < 2 && gm.State == GameState.Playing; i++)
                {
                    var e = PlayOneFish(false);
                    while (e.MoveNext()) yield return e.Current;
                }
                yield return 2f;
            }
        }

        static IEnumerator PlayOneFish(bool deliberateMistake)
        {
            var cam = Camera.main;
            EmailFish fish = null;
            for (int tries = 0; tries < 40 && fish == null; tries++)
            {
                fish = Object.FindObjectsByType<EmailFish>(FindObjectsSortMode.None)
                    .Where(f => !f.Judged && f.Data != null)
                    .OrderBy(f => Vector3.Distance(cam.transform.position, f.transform.position))
                    .FirstOrDefault();
                if (fish == null) yield return 0.25f;
            }
            if (fish == null) yield break;

            var rb = fish.GetComponent<Rigidbody>();

            // 1) Catch: the fish glides into the hand in front of you.
            fish.OnGrabbed();
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
            Vector3 holdPos = cam.transform.position + cam.transform.forward * 0.42f - cam.transform.up * 0.13f;
            Quaternion upright = Quaternion.LookRotation(-cam.transform.right, Vector3.up);
            Vector3 p0 = fish.transform.position; Quaternion r0 = fish.transform.rotation;
            for (float t = 0; t < 1f; t += 0.05f)
            {
                if (!fish) yield break;
                float k = Mathf.SmoothStep(0, 1, t);
                fish.transform.SetPositionAndRotation(Vector3.Lerp(p0, holdPos, k), Quaternion.Slerp(r0, upright, k));
                yield return 0.035f;
            }
            yield return 2.4f;                                // read the email

            // 2) Flip: turn the hand over, belly up -> TRUE COLORS.
            Quaternion bellyUp = upright * Quaternion.AngleAxis(180f, Vector3.forward);
            for (float t = 0; t <= 1f; t += 0.06f)
            {
                if (!fish) yield break;
                fish.transform.rotation = Quaternion.Slerp(upright, bellyUp, Mathf.SmoothStep(0, 1, t));
                yield return 0.03f;
            }
            yield return 2.6f;                                // read the real address

            // 3) Throw into the net.
            bool scam = fish.Data.isScam ^ deliberateMistake;
            var bin = Object.FindObjectsByType<SortBin>(FindObjectsSortMode.None).FirstOrDefault(b => b.IsScamBin == scam);
            if (bin == null) yield break;
            Vector3 target = bin.GetComponent<Collider>().bounds.center;
            fish.OnReleased();
            Vector3 start = fish.transform.position;
            Quaternion startRot = fish.transform.rotation;
            Quaternion dive = Quaternion.LookRotation((target - start).normalized, Vector3.up);
            for (float t = 0; t <= 1f; t += 0.035f)
            {
                if (!fish || fish.Judged) break;
                float k = Mathf.SmoothStep(0, 1, t);
                Vector3 p = Vector3.Lerp(start, target, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.12f;   // little arc
                fish.transform.SetPositionAndRotation(p, Quaternion.Slerp(startRot, dive, k));
                yield return 0.03f;
            }
            if (fish && !fish.Judged) GameManager.Instance.Judge(fish, scam);
            yield return 3.6f;                                // feedback panel
        }
    }
}
#endif
