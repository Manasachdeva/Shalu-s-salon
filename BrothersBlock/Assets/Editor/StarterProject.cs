using System.IO;
using BrothersBlock;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BrothersBlockEditor
{
    public static class StarterProject
    {
        public const string ScenePath = "Assets/Scenes/Neighbourhood.unity";
        private const string PrefabPath = "Assets/Prefabs/Explorer.prefab";

        [InitializeOnLoadMethod]
        private static void ScheduleFirstImport() { EditorApplication.delayCall += EnsureStarter; }

        private static void EnsureStarter()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += EnsureStarter;
                return;
            }
            if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode) CreateStarter();
            else if (!EditorApplication.isPlayingOrWillChangePlaymode) UpgradePrefab();
        }

        [MenuItem("Brothers Block/Create starter scene")]
        public static void CreateStarter()
        {
            if (File.Exists(ScenePath)) { UpgradePrefab(); Configure(); Debug.Log("Elemental Journey is ready."); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Materials");
            AssetDatabase.Refresh();
            Configure();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject player = CreatePlayerPrefab();
            var network = new GameObject("LAN session");
            var transport = network.AddComponent<UnityTransport>();
            var manager = network.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                PlayerPrefab = player,
                ConnectionApproval = true,
                EnableSceneManagement = false,
                TickRate = 30,
                ForceSamePrefabs = true
            };
            network.AddComponent<LanSession>();
            new GameObject("Neighbourhood").AddComponent<Neighbourhood>();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.74f, .86f, .85f);
            camera.fieldOfView = 62;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 230;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<FollowCamera>();
            cameraObject.transform.position = new Vector3(55, 55, -60);
            cameraObject.transform.LookAt(Vector3.zero);
            var sun = new GameObject("Afternoon sun");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, .90f, .75f);
            light.intensity = .85f;
            light.shadows = LightShadows.Hard;
            sun.transform.rotation = Quaternion.Euler(48, -30, 0);
            QualitySettings.shadowDistance = 45;
            QualitySettings.antiAliasing = 2;
            new GameObject("Game UI", typeof(RectTransform)).AddComponent<GameHud>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Brothers Block is ready. Press Play, then Host Game. Build the same APK for both phones using the Brothers Block menu.");
        }

        [MenuItem("Brothers Block/Open neighbourhood")]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath)) { CreateStarter(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Brothers Block/Configure Android settings")]
        public static void Configure()
        {
            PlayerSettings.companyName = "Brothers Block";
            PlayerSettings.productName = "Elemental Journey";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.Android.bundleVersionCode = 2;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.brothersblock.lan");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.runInBackground = true;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.forceInternetPermission = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            // Touch input and StandaloneInputModule use the built-in Input Manager.
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty input = settings.FindProperty("activeInputHandler");
            if (input != null) { input.intValue = 0; settings.ApplyModifiedPropertiesWithoutUndo(); }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Brothers Block/Build Android APK")]
        public static void BuildAndroid()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Install Android Build Support, Android SDK & NDK Tools, and OpenJDK for this Editor through Unity Hub.");
            if (!File.Exists(ScenePath)) CreateStarter();
            UpgradePrefab();
            Configure();
            Directory.CreateDirectory("Builds/Android");
            string path = "Builds/Android/ElementalJourney.apk";
            EditorUserBuildSettings.buildAppBundle = false;
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Android build failed. Read the first Console error for the cause.");
            Debug.Log("APK built: " + Path.GetFullPath(path));
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(path);
        }

        public static void BuildDesktopTest()
        {
            if (!File.Exists(ScenePath)) CreateStarter();
            UpgradePrefab(); Configure();
            Directory.CreateDirectory("Builds/Windows");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/ElementalJourney.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Desktop test build failed.");
        }

        private static GameObject CreatePlayerPrefab()
        {
            var root = new GameObject("Explorer");
            root.layer = 2; // IgnoreRaycast: the follow camera ignores both players.
            root.AddComponent<NetworkObject>();
            var networkTransform = root.AddComponent<OwnerNetworkTransform>();
            networkTransform.SyncRotAngleX = false;
            networkTransform.SyncRotAngleZ = false;
            networkTransform.SyncScaleX = networkTransform.SyncScaleY = networkTransform.SyncScaleZ = false;
            networkTransform.Interpolate = true;
            var controller = root.AddComponent<CharacterController>();
            controller.height = 1.9f;
            controller.radius = .34f;
            controller.center = new Vector3(0, .95f, 0);
            controller.stepOffset = .32f;
            controller.skinWidth = .035f;
            var motor = root.AddComponent<PlayerMotor>();
            root.AddComponent<BenderCombat>();
            Material shirt = Material("Explorer shirt", new Color(.15f, .69f, .68f));
            Material skin = Material("Explorer skin", new Color(.71f, .46f, .31f));
            Material trousers = Material("Explorer trousers", new Color(.13f, .20f, .25f));
            Material shoes = Material("Explorer shoes", new Color(.95f, .94f, .85f));
            motor.Shirt = Part("Shirt", root.transform, new Vector3(0, 1.12f, 0), new Vector3(.65f, .67f, .36f), shirt).GetComponent<Renderer>();
            Part("Head", root.transform, new Vector3(0, 1.72f, 0), new Vector3(.48f, .48f, .44f), skin);
            Part("Hair", root.transform, new Vector3(0, 1.98f, -.025f), new Vector3(.5f, .14f, .47f), trousers);
            Part("Nose", root.transform, new Vector3(0, 1.72f, .25f), new Vector3(.10f, .10f, .10f), skin);
            for (int eye = -1; eye <= 1; eye += 2)
                Part("Eye", root.transform, new Vector3(eye * .12f, 1.80f, .226f), new Vector3(.055f, .055f, .018f), trousers);
            motor.LeftArm = Limb("Left arm", root.transform, new Vector3(-.43f, 1.38f, 0), new Vector3(.21f, .62f, .25f), skin);
            motor.RightArm = Limb("Right arm", root.transform, new Vector3(.43f, 1.38f, 0), new Vector3(.21f, .62f, .25f), skin);
            motor.LeftLeg = Limb("Left leg", root.transform, new Vector3(-.18f, .78f, 0), new Vector3(.25f, .62f, .29f), trousers);
            motor.RightLeg = Limb("Right leg", root.transform, new Vector3(.18f, .78f, 0), new Vector3(.25f, .62f, .29f), trousers);
            Part("Shoe", motor.LeftLeg, new Vector3(0, -.66f, .07f), new Vector3(.29f, .17f, .43f), shoes);
            Part("Shoe", motor.RightLeg, new Vector3(0, -.66f, .07f), new Vector3(.29f, .17f, .43f), shoes);
            var name = new GameObject("Player name");
            name.layer = 2;
            name.transform.SetParent(root.transform, false);
            name.transform.localPosition = new Vector3(0, 2.5f, 0);
            motor.NameLabel = name.AddComponent<TextMesh>();
            motor.NameLabel.text = "EXPLORER";
            motor.NameLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            name.GetComponent<Renderer>().sharedMaterial = motor.NameLabel.font.material;
            motor.NameLabel.fontSize = 48;
            motor.NameLabel.characterSize = .045f;
            motor.NameLabel.anchor = TextAnchor.MiddleCenter;
            motor.NameLabel.color = Color.white;
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static void UpgradePrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing == null || existing.GetComponent<BenderCombat>() != null) return;
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try { contents.AddComponent<BenderCombat>(); PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        private static Material Material(string name, Color colour)
        {
            string path = "Assets/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Standard"));
            material.color = colour;
            material.SetFloat("_Glossiness", .1f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static GameObject Part(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.layer = 2;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }
        private static Transform Limb(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var pivot = new GameObject(name);
            pivot.layer = 2;
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = position;
            Part("Limb", pivot.transform, new Vector3(0, -scale.y / 2, 0), scale, material);
            return pivot.transform;
        }
    }
}
