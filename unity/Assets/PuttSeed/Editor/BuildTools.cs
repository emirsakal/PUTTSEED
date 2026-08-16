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
        /// Batch-mode Android build. Pass <c>-buildApk</c> on the command line
        /// for an installable APK instead of the default .aab.
        /// </summary>
        public static void BuildAndroid()
        {
            EnsureFeelConfig();
            if (!File.Exists(ScenePath))
            {
                CreateMainScene();
            }

            bool apk = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-buildApk") >= 0;

            PlayerSettings.companyName = "PuttSeed";
            PlayerSettings.productName = "PuttSeed";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.puttseed.daily");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorUserBuildSettings.buildAppBundle = !apk;

            AddAlwaysIncludedShader("Sprites/Default");

            Directory.CreateDirectory("../artifacts");
            string output = apk ? "../artifacts/PuttSeed.apk" : "../artifacts/PuttSeed.aab";

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
