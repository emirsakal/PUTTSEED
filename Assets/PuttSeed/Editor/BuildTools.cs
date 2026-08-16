#nullable enable
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuttSeed.Unity.Editor
{
    /// <summary>
    /// Batch-mode entry points: scene creation and the Android build. Also
    /// callable from the editor menu for manual use.
    /// </summary>
    public static class BuildTools
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string FeelConfigPath = "Assets/PuttSeed/Resources/FeelConfig.asset";

        /// <summary>Creates the FeelConfig asset and the single main scene.</summary>
        [MenuItem("PuttSeed/Create Main Scene")]
        public static void CreateMainScene()
        {
            EnsureFeelConfig();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var bootstrapGo = new GameObject("Bootstrap");
            bootstrapGo.AddComponent<GameBootstrap>().feel =
                AssetDatabase.LoadAssetAtPath<FeelConfig>(FeelConfigPath);

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"PuttSeed: created {ScenePath}");
        }

        /// <summary>
        /// Batch-mode release build: signed .aab when keystore.properties is
        /// present next to the project (never committed), warning-unsigned
        /// otherwise. Entry point of scripts/build-release.bat.
        /// </summary>
        public static void BuildAndroidRelease()
        {
            bool signed = ApplySigningFromProperties();
            if (!signed)
            {
                Debug.LogWarning("PuttSeed: keystore.properties not found or incomplete — " +
                    "building with the DEBUG key. Create keystore.properties (see README) for a store upload.");
            }

            EditorUserBuildSettings.development = false;
            BuildAndroidInternal(apk: false, output: "artifacts/PuttSeed-release.aab");
        }

        /// <summary>
        /// Batch-mode Android build (debug-keyed). Pass <c>-buildApk</c> on the
        /// command line for an installable APK instead of the default .aab.
        /// </summary>
        public static void BuildAndroid()
        {
            bool apk = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-buildApk") >= 0;
            BuildAndroidInternal(apk, apk ? "artifacts/PuttSeed.apk" : "artifacts/PuttSeed.aab");
        }

        private static void BuildAndroidInternal(bool apk, string output)
        {
            EnsureFeelConfig();
            EnsureAppIcon();
            ConfigureSplash();
            if (!File.Exists(ScenePath))
            {
                CreateMainScene();
            }

            PlayerSettings.companyName = "PuttSeed";
            PlayerSettings.productName = "PuttSeed";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.puttseed.daily");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorUserBuildSettings.buildAppBundle = !apk;

            AddAlwaysIncludedShader("Sprites/Default");

            Directory.CreateDirectory("artifacts");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                target = BuildTarget.Android,
                locationPathName = output,
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"PuttSeed: Android build {report.summary.result}, " +
                $"{report.summary.totalSize} bytes, {report.summary.totalErrors} errors -> {output}");
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>
        /// Reads Android signing from keystore.properties beside the project
        /// (gitignored — secrets never enter the repo). Expected keys:
        /// storeFile, storePassword, keyAlias, keyPassword.
        /// </summary>
        private static bool ApplySigningFromProperties()
        {
            const string propsPath = "keystore.properties";
            if (!File.Exists(propsPath))
            {
                PlayerSettings.Android.useCustomKeystore = false;
                return false;
            }

            string? storeFile = null, storePassword = null, keyAlias = null, keyPassword = null;
            foreach (var rawLine in File.ReadAllLines(propsPath))
            {
                var line = rawLine.Trim();
                int eq = line.IndexOf('=');
                if (line.StartsWith("#") || eq <= 0)
                {
                    continue;
                }

                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();
                switch (key)
                {
                    case "storeFile": storeFile = value; break;
                    case "storePassword": storePassword = value; break;
                    case "keyAlias": keyAlias = value; break;
                    case "keyPassword": keyPassword = value; break;
                }
            }

            if (string.IsNullOrEmpty(storeFile) || string.IsNullOrEmpty(storePassword)
                || string.IsNullOrEmpty(keyAlias) || string.IsNullOrEmpty(keyPassword)
                || !File.Exists(storeFile))
            {
                PlayerSettings.Android.useCustomKeystore = false;
                return false;
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = storeFile;
            PlayerSettings.Android.keystorePass = storePassword;
            PlayerSettings.Android.keyaliasName = keyAlias;
            PlayerSettings.Android.keyaliasPass = keyPassword;
            Debug.Log($"PuttSeed: signing with {storeFile} (alias {keyAlias}).");
            return true;
        }

        /// <summary>
        /// Generates the flat app icon once (felt green, ball, hole) and
        /// assigns it. Committed after first generation so builds are stable.
        /// </summary>
        private static void EnsureAppIcon()
        {
            const string iconPath = "Assets/PuttSeed/Icon/app-icon.png";
            if (!File.Exists(iconPath))
            {
                const int size = 512;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var felt = new Color(0.22f, 0.52f, 0.31f);
                var ball = new Color(0.97f, 0.97f, 0.95f);
                var hole = new Color(0.07f, 0.07f, 0.09f);
                var ballCenter = new Vector2(size * 0.36f, size * 0.36f);
                var holeCenter = new Vector2(size * 0.70f, size * 0.68f);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        var p = new Vector2(x, y);
                        Color c = felt;
                        if (Vector2.Distance(p, holeCenter) < size * 0.13f)
                        {
                            c = hole;
                        }

                        if (Vector2.Distance(p, ballCenter) < size * 0.19f)
                        {
                            c = ball;
                        }

                        tex.SetPixel(x, y, c);
                    }
                }

                tex.Apply();
                Directory.CreateDirectory("Assets/PuttSeed/Icon");
                File.WriteAllBytes(iconPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(iconPath);
            }

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (icon != null)
            {
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            }
        }

        private static void ConfigureSplash()
        {
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.22f, 0.52f, 0.31f);
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
        }

        private static void EnsureFeelConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<FeelConfig>(FeelConfigPath) != null)
            {
                return;
            }

            Directory.CreateDirectory("Assets/PuttSeed/Resources");
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<FeelConfig>(), FeelConfigPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"PuttSeed: created {FeelConfigPath}");
        }

        private static void AddAlwaysIncludedShader(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                return;
            }

            var graphicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0];
            var serialized = new SerializedObject(graphicsSettings);
            var list = serialized.FindProperty("m_AlwaysIncludedShaders");
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                {
                    return;
                }
            }

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            serialized.ApplyModifiedProperties();
        }
    }
}
