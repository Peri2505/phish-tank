#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Grab;
using Oculus.Interaction.GrabAPI;

namespace PhishTank.EditorTools
{
    /// <summary>
    /// Menu: PhishTank > Build Game Scene
    /// Creates the whole game (minus the Meta camera rig / hands, which come from Building Blocks):
    /// levels, materials, fish prefab, bins, GameManager, menus, HUD, feedback panel, aquarium.
    /// Safe to run again: it rebuilds the scene and overwrites the generated assets.
    /// </summary>
    public static class PhishTankSceneBuilder
    {
        const string Root = "Assets/PhishTank";
        const string GenDir = Root + "/Generated";
        const string ScenePath = Root + "/Scenes/PhishTank.unity";

        static readonly string[] Generated =
        {
            "Directional Light", "GameManager", "SAFE Inbox", "SCAM Net", "Feedback Panel", "HUD",
            "Start Menu", "Level Complete Menu", "Game Over Menu", "Aquarium", "Play Area", "Inbox Portal", "Tutorial Coach", "MRUK"
        };

        // Seated player: eyes roughly 1.2 m up, looking toward +Z.
        static readonly Vector3 Eye = new Vector3(0f, 1.2f, 0f);

        [MenuItem("PhishTank/Build Game Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory(GenDir);
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Levels");

            // Reuse the PhishTank scene if it's open (keeps Meta Building Blocks like the Camera Rig),
            // and only replace the objects this builder made last time.
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = File.Exists(ScenePath)
                    ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                    : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            foreach (var go in scene.GetRootGameObjects())
                if (System.Array.IndexOf(Generated, go.name) >= 0) Object.DestroyImmediate(go);

            // ---------- Light ----------
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // ---------- Materials ----------
            var fishMat   = Mat("Fish",      new Color(1f, 0.62f, 0.25f));
            fishMat.SetFloat("_Smoothness", 0.7f);
            var safeMat   = Mat("SafeBin",   new Color(0.25f, 0.8f, 0.45f));
            var scamMat   = Mat("ScamBin",   new Color(0.9f, 0.25f, 0.25f));
            var coralMat  = Mat("Coral",     new Color(1f, 0.45f, 0.55f));
            var sandMat   = Mat("Sand",      new Color(0.93f, 0.85f, 0.65f));
            var shellMat  = Mat("Shell",     new Color(0.95f, 0.9f, 1f));
            var castleMat = Mat("Castle",    new Color(0.6f, 0.6f, 0.75f));

            // ---------- Levels ----------
            var levels = new[]
            {
                Level("1_TidePool",   "Tide Pool",   5, 1, 0.6f, 0.8f),
                Level("2_Reef",       "Reef",        6, 1, 0.5f, 1.0f),
                Level("3_KelpForest", "Kelp Forest", 7, 2, 0.5f, 1.1f),
                Level("4_OpenSea",    "Open Sea",    8, 2, 0.5f, 1.3f),
                Level("5_TheDeep",    "The Deep",    8, 3, 0.5f, 1.4f),
            };

            // ---------- Fish prefab ----------
            var fishPrefab = BuildFishPrefab(fishMat);

            // ---------- GameManager ----------
            var gmGo = new GameObject("GameManager");
            var db = gmGo.AddComponent<EmailDatabase>();
            var spawnArea = gmGo.AddComponent<SpawnArea>();
            var spawner = gmGo.AddComponent<FishSpawner>();
            var gm = gmGo.AddComponent<GameManager>();
            gmGo.AddComponent<PauseHandler>();
            gmGo.AddComponent<DevShortcuts>();
            gmGo.AddComponent<SoundBank>();
            var sfx = gmGo.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            Set(db, "emailsJson", AssetDatabase.LoadAssetAtPath<TextAsset>(Root + "/Data/emails.json"));
            Set(spawner, "fishPrefab", fishPrefab.GetComponent<EmailFish>());
            Set(spawner, "spawnArea", spawnArea);

            // ---------- Bins (left = SAFE, right = SCAM) ----------
            BuildBin("SAFE Inbox", false, Eye + new Vector3(-0.45f, -0.5f, 0.5f), safeMat);
            BuildBin("SCAM Net",   true,  Eye + new Vector3( 0.45f, -0.5f, 0.5f), scamMat);

            // ---------- UI ----------
            var feedback = BuildFeedbackPanel();
            BuildHud();
            var start = BuildMenu("Start Menu", "PHISH TANK",
                "Scam emails are swimming around you.\nGrab a fish, read it, then throw it:\nSAFE (left)  or  SCAM (right).",
                "Start", gm, nameof(GameManager.StartGame));
            var levelDone = BuildMenu("Level Complete Menu", "Level clear!",
                "Nice catching. Ready for deeper water?", "Next level", gm, nameof(GameManager.NextLevel));
            var gameOver = BuildMenu("Game Over Menu", "Game over",
                "Every mistake was a lesson.\nPlay again and beat your score!", "Play again", gm, nameof(GameManager.StartGame));

            Set(gm, "database", db);
            Set(gm, "spawner", spawner);
            Set(gm, "feedback", feedback);
            Set(gm, "startMenu", start);
            Set(gm, "levelCompleteMenu", levelDone);
            Set(gm, "gameOverMenu", gameOver);
            Set(gm, "sfx", sfx);
            SetArray(gm, "levels", levels);

            // ---------- Aquarium (grows with progress) ----------
            BuildAquarium(sandMat, coralMat, shellMat, castleMat);

            // ---------- Inbox Portal: where the scam emails swim out from ----------
            var portal = BuildPortal();

            // ---------- Tutorial coach (first five minutes) ----------
            BuildCoach();

            // ---------- Mixed reality: Meta MR Utility Kit + our room logic ----------
            var mrukPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.mrutilitykit/Core/Tools/MRUK.prefab");
            if (mrukPrefab != null && Object.FindFirstObjectByType<Meta.XR.MRUtilityKit.MRUK>() == null)
            {
                var mruk = (GameObject)PrefabUtility.InstantiatePrefab(mrukPrefab);
                mruk.name = "MRUK";
            }
            var roomSetup = gmGo.AddComponent<RoomSetup>();
            Set(roomSetup, "portal", portal);

            // ---------- Play Area: everything follows the player's head (seated or standing) ----------
            var playArea = new GameObject("Play Area");
            playArea.transform.position = Eye;
            playArea.AddComponent<PlayAreaAnchor>();
            foreach (var root in scene.GetRootGameObjects())
                if (root != playArea && System.Array.IndexOf(Generated, root.name) >= 0 &&
                    root.name != "Directional Light" && root.name != "GameManager" && root.name != "MRUK")
                    root.transform.SetParent(playArea.transform, true);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var aqT = GameObject.Find("Aquarium");
            if (aqT != null) Set(roomSetup, "aquarium", aqT.transform);

            Debug.Log("[PhishTank] Scene built at " + ScenePath +
                      ". Next: add Meta Building Blocks (Camera Rig, Passthrough, Hand Tracking) and the grab interaction on the Fish prefab.");
        }

        // ================= builders =================

        static GameObject BuildFishPrefab(Material mat)
        {
            var fish = new GameObject("Fish");
            var rb = fish.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            var col = fish.AddComponent<SphereCollider>();
            col.radius = 0.07f;

            // Cartoon fish: procedural teardrop body, swishing forked tail, fins, big eyes.
            var model = new GameObject("Model");
            model.transform.SetParent(fish.transform, false);
            model.transform.localScale = Vector3.one * 0.15f;
            var swim = model.AddComponent<Wiggle>(); swim.axis = Vector3.up; swim.angle = 5f; swim.speed = 8f; swim.followVelocity = true;

            var body = new GameObject("Body", typeof(MeshFilter), typeof(MeshRenderer));
            body.transform.SetParent(model.transform, false);
            body.GetComponent<MeshFilter>().sharedMesh = PhishTankMeshes.FishBody();
            body.GetComponent<Renderer>().sharedMaterial = mat;

            var tailPivot = new GameObject("TailPivot");
            tailPivot.transform.SetParent(model.transform, false);
            tailPivot.transform.localPosition = new Vector3(0, 0, -0.47f);
            var tw = tailPivot.AddComponent<Wiggle>(); tw.axis = Vector3.up; tw.angle = 28f; tw.speed = 9f; tw.followVelocity = true;
            MeshPart(tailPivot.transform, "Tail", PhishTankMeshes.TailFin(), Vector3.zero, Quaternion.identity, Vector3.one, mat);

            MeshPart(model.transform, "FinDorsal", PhishTankMeshes.Fin(), new Vector3(0, 0.27f, -0.02f), Quaternion.identity, Vector3.one, mat);
            var finL = MeshPart(model.transform, "FinL", PhishTankMeshes.Fin(), new Vector3(-0.2f, -0.08f, 0.12f), Quaternion.Euler(0, 0, 110f), Vector3.one * 0.8f, mat);
            var finR = MeshPart(model.transform, "FinR", PhishTankMeshes.Fin(), new Vector3(0.2f, -0.08f, 0.12f), Quaternion.Euler(0, 0, -110f), Vector3.one * 0.8f, mat);
            var fwL = finL.AddComponent<Wiggle>(); fwL.axis = Vector3.forward; fwL.angle = 20f; fwL.speed = 7f;
            var fwR = finR.AddComponent<Wiggle>(); fwR.axis = Vector3.forward; fwR.angle = 20f; fwR.speed = 7f;

            var eyeWhite = UnlitMat("EyeWhite", Color.white);
            var eyeBlack = UnlitMat("EyePupil", new Color(0.05f, 0.05f, 0.08f));
            foreach (float x in new[] { -1f, 1f })
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "Eye";
                Object.DestroyImmediate(eye.GetComponent<Collider>());
                eye.transform.SetParent(model.transform, false);
                eye.transform.localPosition = new Vector3(0.17f * x, 0.08f, 0.28f);
                eye.transform.localScale = Vector3.one * 0.14f;
                eye.GetComponent<Renderer>().sharedMaterial = eyeWhite;
                var pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pupil.name = "Pupil";
                Object.DestroyImmediate(pupil.GetComponent<Collider>());
                pupil.transform.SetParent(eye.transform, false);
                pupil.transform.localPosition = new Vector3(0.28f * x, 0.05f, 0.25f);
                pupil.transform.localScale = Vector3.one * 0.55f;
                pupil.GetComponent<Renderer>().sharedMaterial = eyeBlack;
            }

            // Email card above the fish
            var canvas = WorldCanvas("EmailCard", fish.transform, new Vector2(440, 330), 0.0006f, new Color(0.08f, 0.1f, 0.14f, 1f));
            var card = canvas.gameObject.AddComponent<EmailCard>();
            var content = Text(canvas.transform, "Content", "", 19, Vector2.zero, new Vector2(400, 300), TextAlignmentOptions.TopLeft);
            content.overflowMode = TextOverflowModes.Ellipsis;
            content.enableAutoSizing = true;
            content.fontSizeMin = 120f; content.fontSizeMax = 190f;   // 3D TMP units (x10)
            Set(card, "contentText", content);

            AddHandGrab(fish, rb, throwOnRelease: true);

            var ef = fish.AddComponent<EmailFish>();
            Set(ef, "card", card);
            Set(ef, "grabbable", fish.GetComponent<Grabbable>());
            Set(ef, "bodyRenderer", body.GetComponent<Renderer>());

            var prefab = PrefabUtility.SaveAsPrefabAsset(fish, Root + "/Prefabs/Fish.prefab");
            Object.DestroyImmediate(fish);
            return prefab;
        }

        static void BuildBin(string name, bool isScam, Vector3 pos, Material mat)
        {
            var bin = new GameObject(name);
            bin.transform.position = pos;
            var trigger = bin.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(0.36f, 0.3f, 0.36f);
            trigger.center = new Vector3(0f, 0.05f, 0f);
            var sb = bin.AddComponent<SortBin>();
            Set(sb, "isScamBin", isScam);

            // simple open box: bottom + 4 walls
            Part(bin.transform, "Bottom", new Vector3(0, -0.1f, 0), new Vector3(0.36f, 0.02f, 0.36f), mat);
            Part(bin.transform, "WallL",  new Vector3(-0.18f, 0, 0), new Vector3(0.02f, 0.2f, 0.36f), mat);
            Part(bin.transform, "WallR",  new Vector3( 0.18f, 0, 0), new Vector3(0.02f, 0.2f, 0.36f), mat);
            Part(bin.transform, "WallF",  new Vector3(0, 0, -0.18f), new Vector3(0.36f, 0.2f, 0.02f), mat);
            Part(bin.transform, "WallB",  new Vector3(0, 0,  0.18f), new Vector3(0.36f, 0.2f, 0.02f), mat);

            var label = WorldCanvas("Label", bin.transform, new Vector2(300, 90), 0.001f, new Color(0, 0, 0, 0));
            label.transform.localPosition = new Vector3(0, 0.2f, 0.1f);
            label.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            var t = Text(label.transform, "Text", isScam ? "SCAM" : "SAFE", 72, Vector2.zero, new Vector2(300, 90), TextAlignmentOptions.Center);
            t.fontStyle = FontStyles.Bold;
            t.color = mat.color;
        }

        static FeedbackPanel BuildFeedbackPanel()
        {
            var holder = new GameObject("Feedback Panel");
            holder.transform.position = Eye + new Vector3(0.52f, 0.2f, 0.55f);
            holder.transform.rotation = Quaternion.Euler(0f, 40f, 0f);
            var c = WorldCanvas("Root", holder.transform, new Vector2(600, 240), 0.001f, new Color(0.05f, 0.07f, 0.1f, 1f));
            var title = Text(c.transform, "Title", "Caught it!", 48, new Vector2(0, 75), new Vector2(660, 70), TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            var clues = Text(c.transform, "Clues", "Clues", 26, new Vector2(0, 10), new Vector2(660, 50), TextAlignmentOptions.Center);
            clues.color = new Color(1f, 0.85f, 0.4f);
            var expl = Text(c.transform, "Explanation", "Explanation", 26, new Vector2(0, -70), new Vector2(660, 100), TextAlignmentOptions.Center);
            var fp = holder.AddComponent<FeedbackPanel>();
            Set(fp, "root", c.gameObject);
            Set(fp, "titleText", title);
            Set(fp, "cluesText", clues);
            Set(fp, "explanationText", expl);
            return fp;
        }

        static void BuildHud()
        {
            var c = WorldCanvas("HUD", null, new Vector2(320, 170), 0.001f, new Color(0.05f, 0.07f, 0.1f, 0.7f));
            c.transform.position = Eye + new Vector3(-0.52f, 0.22f, 0.55f);
            c.transform.rotation = Quaternion.Euler(0f, -40f, 0f);
            var root = new GameObject("Root");
            root.transform.SetParent(c.transform, false);
            var hudBg = c.transform.Find("Background");
            if (hudBg != null) hudBg.SetParent(root.transform, false);   // hide the panel too outside of play
            var level = Text(root.transform, "Level", "Tide Pool", 26, new Vector2(0, 55), new Vector2(300, 40), TextAlignmentOptions.Center);
            var score = Text(root.transform, "Score", "0", 48, new Vector2(0, 5), new Vector2(300, 60), TextAlignmentOptions.Center);
            score.fontStyle = FontStyles.Bold;
            var lives = Text(root.transform, "Lives", "♥♥♥", 32, new Vector2(-70, -55), new Vector2(150, 40), TextAlignmentOptions.Center);
            lives.color = new Color(1f, 0.4f, 0.45f);
            var combo = Text(root.transform, "Combo", "", 32, new Vector2(80, -55), new Vector2(130, 40), TextAlignmentOptions.Center);
            combo.color = new Color(1f, 0.85f, 0.3f);
            var hud = c.gameObject.AddComponent<HUD>();
            Set(hud, "scoreText", score);
            Set(hud, "livesText", lives);
            Set(hud, "comboText", combo);
            Set(hud, "levelText", level);
            Set(hud, "root", root);
        }

        static GameObject BuildMenu(string name, string title, string body, string buttonLabel, GameManager gm, string method)
        {
            var c = WorldCanvas(name, null, new Vector2(620, 470), 0.001f, new Color(0.05f, 0.08f, 0.13f, 0.92f));
            c.transform.position = Eye + new Vector3(0, -0.02f, 0.6f);
            var t = Text(c.transform, "Title", title, 60, new Vector2(0, 170), new Vector2(580, 80), TextAlignmentOptions.Center);
            t.fontStyle = FontStyles.Bold;
            t.color = new Color(0.45f, 0.8f, 1f);
            Text(c.transform, "Body", body, 28, new Vector2(0, 60), new Vector2(580, 130), TextAlignmentOptions.Center);

            // Hands-first "button": a glowing orb you pinch or grab.
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "Orb (" + buttonLabel + ")";
            orb.transform.SetParent(c.transform, false);
            orb.transform.localScale = Vector3.one * 90f;            // canvas units -> ~9 cm
            orb.transform.localPosition = new Vector3(0, -95f, -60f);
            orb.GetComponent<Renderer>().sharedMaterial = Mat("Orb", new Color(0.25f, 0.65f, 1f));
            var orbRb = orb.AddComponent<Rigidbody>();
            orbRb.useGravity = false;
            orbRb.isKinematic = true;
            AddHandGrab(orb, orbRb, throwOnRelease: false);
            var orbLabel = Text(c.transform, "OrbLabel", "Pinch the orb:  " + buttonLabel, 30, new Vector2(0, -190), new Vector2(560, 50), TextAlignmentOptions.Center);
            orbLabel.color = new Color(0.7f, 0.85f, 1f);
            var ao = orb.AddComponent<ActionOrb>();
            Set(ao, "grabbable", orb.GetComponent<Grabbable>());
            Set(ao, "label", orbLabel);
            var action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), gm, method);
            var field = typeof(ActionOrb).GetField("onActivated", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var ue = (UnityEvent)field.GetValue(ao);
            UnityEventTools.AddPersistentListener(ue, action);
            EditorUtility.SetDirty(ao);
            return c.gameObject;
        }

        static InboxPortal BuildPortal()
        {
            var root = new GameObject("Inbox Portal");
            root.transform.position = Eye + new Vector3(0f, -0.06f, 1.1f);

            var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glow.name = "Glow";
            Object.DestroyImmediate(glow.GetComponent<Collider>());
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = new Vector3(0, 0, 0.005f);
            glow.transform.localScale = new Vector3(0.42f, 0.29f, 1f);
            glow.GetComponent<Renderer>().sharedMaterial = UnlitMat("PortalGlow", new Color(0.25f, 0.6f, 1f));

            var laptop = new GameObject("Virtual Laptop");
            laptop.transform.SetParent(root.transform, false);
            var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.name = "Screen";
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            screen.transform.SetParent(laptop.transform, false);
            screen.transform.localScale = new Vector3(0.38f, 0.25f, 1f);
            screen.GetComponent<Renderer>().sharedMaterial = UnlitMat("LaptopScreen", new Color(0.04f, 0.08f, 0.16f));
            var bezel = Part(laptop.transform, "Bezel", new Vector3(0, 0, 0.012f), new Vector3(0.40f, 0.27f, 0.012f), Mat("Bezel", new Color(0.15f, 0.16f, 0.18f)));
            var keyboard = Part(laptop.transform, "Keyboard", new Vector3(0, -0.135f, -0.09f), new Vector3(0.40f, 0.012f, 0.2f), Mat("Bezel", new Color(0.15f, 0.16f, 0.18f)));

            var labelHolder = WorldCanvas("Label", root.transform, new Vector2(380, 60), 0.001f, new Color(0, 0, 0, 0));
            labelHolder.localPosition = new Vector3(0, 0.08f, -0.006f);
            var label = Text(labelHolder, "Text", "INBOX", 40, Vector2.zero, new Vector2(380, 60), TextAlignmentOptions.Center);
            label.color = new Color(0.55f, 0.85f, 1f);
            label.fontStyle = FontStyles.Bold;

            var sp = new GameObject("Spawn Point");
            sp.transform.SetParent(root.transform, false);
            sp.transform.localPosition = new Vector3(0, 0, -0.08f);

            var portal = root.AddComponent<InboxPortal>();
            Set(portal, "spawnPoint", sp.transform);
            Set(portal, "virtualLaptop", laptop);
            Set(portal, "glow", glow.transform);
            Set(portal, "inboxLabel", label);
            return portal;
        }

        static void BuildCoach()
        {
            var holder = new GameObject("Tutorial Coach");
            holder.transform.position = Eye + new Vector3(0f, 0.3f, 0.62f);
            var panel = WorldCanvas("Panel", holder.transform, new Vector2(560, 130), 0.001f, new Color(0.1f, 0.25f, 0.45f, 1f));
            var t = Text(panel, "Hint", "Pinch a fish to catch it", 34, Vector2.zero, new Vector2(530, 120), TextAlignmentOptions.Center);
            t.fontStyle = FontStyles.Bold;
            var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.name = "Arrow";
            Object.DestroyImmediate(arrow.GetComponent<Collider>());
            arrow.transform.SetParent(panel, false);
            arrow.transform.localPosition = new Vector3(0, -95f, 0);
            arrow.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            arrow.transform.localScale = new Vector3(30f, 30f, 4f);
            arrow.GetComponent<Renderer>().sharedMaterial = UnlitMat("CoachArrow", new Color(1f, 0.8f, 0.3f));
            var coach = holder.AddComponent<TutorialCoach>();
            Set(coach, "root", panel.gameObject);
            Set(coach, "text", t);
            Set(coach, "arrow", arrow.transform);
        }

        static void BuildAquarium(Material sand, Material coral, Material shell, Material castle)
        {
            var aq = new GameObject("Aquarium");
            aq.transform.position = Eye + new Vector3(0f, -0.52f, 0.72f);

            // glass tank
            var glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "Glass";
            Object.DestroyImmediate(glass.GetComponent<Collider>());
            glass.transform.SetParent(aq.transform, false);
            glass.transform.localPosition = new Vector3(0, 0.11f, 0);
            glass.transform.localScale = new Vector3(0.5f, 0.22f, 0.22f);
            glass.GetComponent<Renderer>().sharedMaterial = GlassMat();
            Part(aq.transform, "Base", new Vector3(0, -0.005f, 0), new Vector3(0.52f, 0.012f, 0.24f), Mat("TankBase", new Color(0.12f, 0.13f, 0.16f)));
            Part(aq.transform, "Sand", new Vector3(0, 0.012f, 0), new Vector3(0.49f, 0.022f, 0.21f), sand);

            // rocks
            var rockMat = Mat("Rock", new Color(0.45f, 0.47f, 0.5f));
            Blob(aq.transform, "Rock", new Vector3(-0.17f, 0.03f, 0.04f), new Vector3(0.07f, 0.035f, 0.05f), rockMat);
            Blob(aq.transform, "Rock", new Vector3(0.12f, 0.028f, -0.05f), new Vector3(0.05f, 0.03f, 0.04f), rockMat);
            Blob(aq.transform, "Rock", new Vector3(0.2f, 0.025f, 0.05f), new Vector3(0.035f, 0.02f, 0.03f), rockMat);

            // seaweed that sways
            var weed = Mat("Seaweed", new Color(0.2f, 0.7f, 0.35f));
            float[] wx = { -0.21f, -0.05f, 0.08f, 0.21f };
            for (int i = 0; i < wx.Length; i++)
            {
                Transform parent = aq.transform;
                Vector3 pos = new Vector3(wx[i], 0.022f, -0.07f + 0.04f * (i % 2));
                for (int k = 0; k < 4; k++)
                {
                    var seg = new GameObject("Seaweed");
                    seg.transform.SetParent(parent, false);
                    seg.transform.localPosition = k == 0 ? pos : new Vector3(0, 0.035f, 0);
                    var w = seg.AddComponent<Wiggle>(); w.axis = Vector3.forward; w.angle = 9f; w.speed = 1.6f; w.phase = i * 1.3f + k * 0.4f;
                    var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    cap.name = "Leaf";
                    Object.DestroyImmediate(cap.GetComponent<Collider>());
                    cap.transform.SetParent(seg.transform, false);
                    cap.transform.localPosition = new Vector3(0, 0.0175f, 0);
                    cap.transform.localScale = new Vector3(0.012f, 0.02f, 0.006f);
                    cap.GetComponent<Renderer>().sharedMaterial = weed;
                    parent = seg.transform;
                }
            }

            // two tiny fish living in the tank
            var mini = PhishTankMeshes.FishBody();
            Color[] mc = { new Color(1f, 0.55f, 0.2f), new Color(0.35f, 0.75f, 1f) };
            for (int i = 0; i < 2; i++)
            {
                var m = new GameObject("Mini Fish", typeof(MeshFilter), typeof(MeshRenderer));
                m.transform.SetParent(aq.transform, false);
                m.transform.localScale = Vector3.one * 0.05f;
                m.GetComponent<MeshFilter>().sharedMesh = mini;
                m.GetComponent<Renderer>().sharedMaterial = Mat("MiniFish" + i, mc[i]);
                var o = m.AddComponent<Orbit>(); o.radius = 0.15f - i * 0.04f; o.height = 0.09f + i * 0.04f; o.speed = i == 0 ? 0.7f : -0.9f; o.phase = i * 2f;
            }

            // rising bubbles
            var bub = new GameObject("Bubbles");
            bub.transform.SetParent(aq.transform, false);
            bub.transform.localPosition = new Vector3(0.18f, 0.03f, 0f);
            var ps = bub.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.startLifetime = 1.4f; main.startSpeed = 0.11f; main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.012f);
            main.startColor = new Color(0.8f, 0.95f, 1f, 0.8f); main.maxParticles = 30; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission; em.rateOverTime = 5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6f; sh.radius = 0.005f;
            sh.rotation = new Vector3(-90f, 0f, 0f);
            bub.GetComponent<ParticleSystemRenderer>().sharedMaterial = UnlitMat("Bubble", new Color(0.85f, 0.95f, 1f));

            // unlockable decorations (grow with progress)
            var coral1 = new GameObject("Coral 1"); coral1.transform.SetParent(aq.transform, false); coral1.transform.localPosition = new Vector3(-0.1f, 0.022f, 0.05f);
            CoralBranch(coral1.transform, coral);
            var shellGo = Blob(aq.transform, "Shell", new Vector3(0.02f, 0.03f, 0.06f), new Vector3(0.04f, 0.015f, 0.03f), shell);
            var coral2 = new GameObject("Coral 2"); coral2.transform.SetParent(aq.transform, false); coral2.transform.localPosition = new Vector3(0.15f, 0.022f, -0.02f);
            CoralBranch(coral2.transform, Mat("CoralOrange", new Color(1f, 0.6f, 0.25f)));
            var castleGo = new GameObject("Castle"); castleGo.transform.SetParent(aq.transform, false); castleGo.transform.localPosition = new Vector3(-0.03f, 0.022f, -0.05f);
            Part(castleGo.transform, "Keep", new Vector3(0, 0.035f, 0), new Vector3(0.06f, 0.07f, 0.05f), castle);
            foreach (float x in new[] { -0.035f, 0.035f })
            {
                var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.DestroyImmediate(tower.GetComponent<Collider>());
                tower.name = "Tower";
                tower.transform.SetParent(castleGo.transform, false);
                tower.transform.localPosition = new Vector3(x, 0.045f, 0);
                tower.transform.localScale = new Vector3(0.025f, 0.045f, 0.025f);
                tower.GetComponent<Renderer>().sharedMaterial = castle;
            }

            var unlockGos = new[] { coral1, shellGo, coral2, castleGo };
            int[] needed = { 5, 15, 30, 60 };
            string[] labels = { "Pink coral", "Shell", "Orange coral", "Castle" };
            foreach (var g in unlockGos) g.SetActive(false);

            var stats = WorldCanvas("Stats", aq.transform, new Vector2(520, 80), 0.0008f, new Color(0, 0, 0, 0));
            stats.transform.localPosition = new Vector3(0, 0.27f, 0);
            var statsText = Text(stats.transform, "Text", "0 correct sorts", 24, Vector2.zero, new Vector2(520, 80), TextAlignmentOptions.Center);
            statsText.color = new Color(0.8f, 0.92f, 1f);

            var growth = aq.AddComponent<AquariumGrowth>();
            var so = new SerializedObject(growth);
            var arr = so.FindProperty("unlocks");
            arr.arraySize = unlockGos.Length;
            for (int i = 0; i < unlockGos.Length; i++)
            {
                var el = arr.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("decoration").objectReferenceValue = unlockGos[i];
                el.FindPropertyRelative("correctSortsNeeded").intValue = needed[i];
                el.FindPropertyRelative("label").stringValue = labels[i];
            }
            so.FindProperty("statsText").objectReferenceValue = statsText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CoralBranch(Transform parent, Material m)
        {
            Vector3[] p = { new Vector3(0, 0.03f, 0), new Vector3(-0.015f, 0.06f, 0), new Vector3(0.017f, 0.055f, 0.005f), new Vector3(0.002f, 0.08f, -0.004f) };
            Vector3[] r = { Vector3.zero, new Vector3(0, 0, 25f), new Vector3(0, 0, -30f), new Vector3(8f, 0, 5f) };
            for (int i = 0; i < p.Length; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.DestroyImmediate(c.GetComponent<Collider>());
                c.name = "Branch";
                c.transform.SetParent(parent, false);
                c.transform.localPosition = p[i];
                c.transform.localRotation = Quaternion.Euler(r[i]);
                c.transform.localScale = new Vector3(0.016f, 0.03f, 0.016f);
                c.GetComponent<Renderer>().sharedMaterial = m;
            }
        }

        static GameObject Blob(Transform parent, string name, Vector3 pos, Vector3 scale, Material m)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = name;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }

        static GameObject MeshPart(Transform parent, string name, Mesh mesh, Vector3 pos, Quaternion rot, Vector3 scale, Material m)
        {
            var g = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = rot;
            g.transform.localScale = scale;
            g.GetComponent<MeshFilter>().sharedMesh = mesh;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }

        static Material GlassMat()
        {
            string path = $"{GenDir}/Glass.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            m.SetFloat("_Smoothness", 0.95f);
            m.color = new Color(0.55f, 0.85f, 1f, 0.16f);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ================= helpers =================

        /// <summary>Meta Interaction SDK: grab with a hand up close (pinch or palm) and pinch from a distance.</summary>
        static void AddHandGrab(GameObject go, Rigidbody rb, bool throwOnRelease)
        {
            var grabbable = go.AddComponent<Grabbable>();
            grabbable.InjectOptionalRigidbody(rb);
            grabbable.InjectOptionalThrowWhenUnselected(throwOnRelease);
            grabbable.InjectOptionalKinematicWhileSelected(true);

            var near = new GameObject("HandGrabInteractable");
            near.transform.SetParent(go.transform, false);
            var hgi = near.AddComponent<HandGrabInteractable>();
            hgi.InjectAllHandGrabInteractable(GrabTypeFlags.All, rb, GrabbingRule.DefaultPinchRule, GrabbingRule.DefaultPalmRule);
            hgi.InjectOptionalPointableElement(grabbable);

            var far = new GameObject("DistanceHandGrabInteractable");
            far.transform.SetParent(go.transform, false);
            var dgi = far.AddComponent<DistanceHandGrabInteractable>();
            dgi.InjectAllDistanceHandGrabInteractable(GrabTypeFlags.Pinch, rb, GrabbingRule.DefaultPinchRule, GrabbingRule.DefaultPalmRule);
            dgi.InjectOptionalPointableElement(grabbable);

            EditorUtility.SetDirty(grabbable);
            EditorUtility.SetDirty(hgi);
            EditorUtility.SetDirty(dgi);
        }

        static Material Mat(string name, Color color)
        {
            string path = $"{GenDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.color = color;
            EditorUtility.SetDirty(m);
            return m;
        }

        static LevelConfig Level(string file, string name, int fish, int diff, float scams, float speed)
        {
            string path = $"{Root}/Levels/{file}.asset";
            var l = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
            if (l == null) { l = ScriptableObject.CreateInstance<LevelConfig>(); AssetDatabase.CreateAsset(l, path); }
            l.levelName = name; l.fishCount = fish; l.maxDifficulty = diff; l.scamRatio = scams; l.speedMultiplier = speed;
            EditorUtility.SetDirty(l);
            return l;
        }

        static GameObject Part(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            return g;
        }

        /// <summary>
        /// A floating panel: a solid quad + room for 3D TextMeshPro, laid out in "pixel" units.
        /// (Uses normal 3D renderers instead of a UI Canvas so it shows reliably over passthrough.)
        /// </summary>
        static Transform WorldCanvas(string name, Transform parent, Vector2 size, float scale, Color bg)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * scale;
            if (bg.a > 0f)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Background";
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.transform.SetParent(go.transform, false);
                quad.transform.localPosition = new Vector3(0, 0, 2f);       // just behind the text
                quad.transform.localScale = new Vector3(size.x, size.y, 1f);
                quad.GetComponent<Renderer>().sharedMaterial = UnlitMat("Panel_" + ColorUtility.ToHtmlStringRGB(bg), bg);
            }
            return go.transform;
        }

        static Material UnlitMat(string name, Color color)
        {
            string path = $"{GenDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.color = new Color(color.r, color.g, color.b, 1f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static TMP_Text Text(Transform parent, string name, string text, float size, Vector2 pos, Vector2 box, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.localPosition = new Vector3(pos.x, pos.y, 0f);
            rt.sizeDelta = box;
            var t = go.GetComponent<TextMeshPro>();
            t.text = text;
            t.fontSize = size * 10f;   // 3D TextMeshPro units -> matches the old canvas pixel sizes
            t.alignment = align;
            t.color = Color.white;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[PhishTank] Field '{field}' not found on {target.GetType().Name}"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddSceneToBuild(string path)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(path, true) };
            foreach (var s in EditorBuildSettings.scenes) if (s.path != path) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
