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
        private const string MenuScenePath = "Assets/Scenes/Menu.unity";
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string LegacyScenePath = "Assets/Scenes/Main.unity";
        private const string FeelConfigPath = "Assets/PuttSeed/Resources/FeelConfig.asset";

        /// <summary>
        /// Rebuilds both scenes WITH the full UI hierarchies baked in (Menu is
        /// entry, index 0). The UI lives in the scenes as ordinary objects —
        /// editable in the Inspector, reskinnable with art assets — and is not
        /// reconstructed on Play. Sprites used by the UI are saved as real
        /// assets under Assets/PuttSeed/UI first, so scene references survive.
        /// </summary>
        [MenuItem("PuttSeed/Rebuild Scenes")]
        public static void CreateScenes()
        {
            EnsureFeelConfig();
            EnsureUiSprites();
            Directory.CreateDirectory("Assets/Scenes");

            var menuScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var menu = new GameObject("Menu").AddComponent<MenuBootstrap>();
            UiConstruction.BuildMenu(menu);
            var menuCam = Camera.main;
            if (menuCam != null)
            {
                menuCam.clearFlags = CameraClearFlags.SolidColor;
                menuCam.backgroundColor = PaletteMaterials.Felt;
                menuCam.orthographic = true;
            }

            EditorSceneManager.SaveScene(menuScene, MenuScenePath);

            var gameScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var bootstrap = new GameObject("Bootstrap").AddComponent<GameBootstrap>();
            bootstrap.feel = AssetDatabase.LoadAssetAtPath<FeelConfig>(FeelConfigPath);

            var gameUi = new GameObject("UI").AddComponent<GameUI>();
            UiConstruction.BuildGameHud(gameUi);
            var overlay = new GameObject("LoadingOverlay").AddComponent<LoadingOverlay>();
            UiConstruction.BuildLoadingOverlay(overlay);
            bootstrap.gameUi = gameUi;
            bootstrap.loadingOverlay = overlay;
            EditorSceneManager.SaveScene(gameScene, GameScenePath);

            if (File.Exists(LegacyScenePath))
            {
                AssetDatabase.DeleteAsset(LegacyScenePath);
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };
            AssetDatabase.SaveAssets();
            Debug.Log($"PuttSeed: rebuilt {MenuScenePath} and {GameScenePath} with baked UI");
        }

        /// <summary>
        /// Saves the generated UI sprites as importable assets and points
        /// UIFactory at them, so scene-baked Images reference real assets.
        /// </summary>
        private static void EnsureUiSprites()
        {
            const string dir = "Assets/PuttSeed/UI";
            const string roundedPath = dir + "/rounded.png";
            const string circlePath = dir + "/circle.png";
            const string starPath = dir + "/star.png";
            const string pennantPath = dir + "/pennant.png";
            Directory.CreateDirectory(dir);

            if (!File.Exists(roundedPath))
            {
                File.WriteAllBytes(roundedPath, UIFactory.RoundedSpritePng());
                AssetDatabase.ImportAsset(roundedPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(roundedPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteBorder = new Vector4(24, 24, 24, 24);
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            if (!File.Exists(circlePath))
            {
                File.WriteAllBytes(circlePath, UIFactory.CircleSpritePng());
                AssetDatabase.ImportAsset(circlePath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(circlePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            if (!File.Exists(starPath))
            {
                File.WriteAllBytes(starPath, UIFactory.StarSpritePng());
                AssetDatabase.ImportAsset(starPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(starPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            if (!File.Exists(pennantPath))
            {
                File.WriteAllBytes(pennantPath, UIFactory.PennantSpritePng());
                AssetDatabase.ImportAsset(pennantPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(pennantPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            var font = ActiveUiFont(dir + "/Fonts");
            if (font != null)
            {
                UIFactory.UseFontAsset(font);
                Debug.Log($"PuttSeed: UI font is {font.name}.");
            }

            var rounded = AssetDatabase.LoadAssetAtPath<Sprite>(roundedPath);
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(circlePath);
            var star = AssetDatabase.LoadAssetAtPath<Sprite>(starPath);
            var pennant = AssetDatabase.LoadAssetAtPath<Sprite>(pennantPath);
            if (rounded != null && circle != null && star != null && pennant != null)
            {
                UIFactory.UseSpriteAssets(rounded, circle, star, pennant);
            }
        }

        /// <summary>
        /// The typeface the game is set in, or null for Unity's built-in face.
        /// Everything else the bake needs it GENERATES; a font cannot be
        /// generated, so this only looks — in two places, because there are two
        /// ways to mean it. Fonts/active.txt names one file in Fonts/Library,
        /// which is how you audition six faces without moving files around; a
        /// font dropped loose into Fonts/ wins when there is no such line, which
        /// is how you use one you already decided on.
        ///
        /// The tests resolve the face through here too — a font that cannot
        /// print Turkish is a bug the bake should not be the first to notice.
        /// </summary>
        public static Font? ActiveUiFont(string fontsDir = "Assets/PuttSeed/UI/Fonts")
        {
            string activePath = fontsDir + "/active.txt";
            if (File.Exists(activePath))
            {
                foreach (string line in File.ReadAllLines(activePath))
                {
                    string name = line.Trim();
                    if (name.Length == 0 || name.StartsWith("#"))
                    {
                        continue;
                    }

                    var chosen = AssetDatabase.LoadAssetAtPath<Font>(fontsDir + "/Library/" + name);
                    if (chosen == null)
                    {
                        Debug.LogWarning($"PuttSeed: active.txt names {name}, which is not in Fonts/Library.");
                    }

                    return chosen;
                }
            }

            foreach (string path in Directory.Exists(fontsDir)
                ? Directory.GetFiles(fontsDir)
                : System.Array.Empty<string>())
            {
                if (path.EndsWith(".ttf", System.StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".otf", System.StringComparison.OrdinalIgnoreCase))
                {
                    var loose = AssetDatabase.LoadAssetAtPath<Font>(path.Replace(Path.DirectorySeparatorChar, '/'));
                    if (loose != null)
                    {
                        return loose;
                    }
                }
            }

            return null;
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
            if (!File.Exists(MenuScenePath) || !File.Exists(GameScenePath))
            {
                CreateScenes();
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
                scenes = new[] { MenuScenePath, GameScenePath },
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

        /// <summary>Batch/menu entry: (re)generate and assign all app icons.</summary>
        [MenuItem("PuttSeed/Configure Icons")]
        public static void ConfigureIcons()
        {
            EnsureAppIcon();
            AssetDatabase.SaveAssets();
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

            EnsureAdaptiveIcons(icon);
        }

        /// <summary>
        /// Generates and assigns Android adaptive icon layers (felt background,
        /// transparent ball+hole foreground kept inside the 66% safe zone);
        /// round and legacy kinds reuse the flat icon.
        /// </summary>
        private static void EnsureAdaptiveIcons(Texture2D? legacy)
        {
            const string bgPath = "Assets/PuttSeed/Icon/adaptive-bg.png";
            const string fgPath = "Assets/PuttSeed/Icon/adaptive-fg.png";
            if (!File.Exists(bgPath) || !File.Exists(fgPath))
            {
                const int size = 432;
                var felt = new Color(0.22f, 0.52f, 0.31f);
                var ball = new Color(0.97f, 0.97f, 0.95f);
                var hole = new Color(0.07f, 0.07f, 0.09f);
                var ballCenter = new Vector2(size * 0.40f, size * 0.40f);
                var holeCenter = new Vector2(size * 0.61f, size * 0.59f);

                var bg = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var fg = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bg.SetPixel(x, y, felt);
                        var p = new Vector2(x, y);
                        Color c = Color.clear;
                        if (Vector2.Distance(p, holeCenter) < size * 0.075f)
                        {
                            c = hole;
                        }

                        if (Vector2.Distance(p, ballCenter) < size * 0.11f)
                        {
                            c = ball;
                        }

                        fg.SetPixel(x, y, c);
                    }
                }

                bg.Apply();
                fg.Apply();
                File.WriteAllBytes(bgPath, bg.EncodeToPNG());
                File.WriteAllBytes(fgPath, fg.EncodeToPNG());
                Object.DestroyImmediate(bg);
                Object.DestroyImmediate(fg);
                AssetDatabase.ImportAsset(bgPath);
                AssetDatabase.ImportAsset(fgPath);
            }

            var bgTex = AssetDatabase.LoadAssetAtPath<Texture2D>(bgPath);
            var fgTex = AssetDatabase.LoadAssetAtPath<Texture2D>(fgPath);
            if (bgTex == null || fgTex == null)
            {
                return;
            }

            var adaptive = PlayerSettings.GetPlatformIcons(
                NamedBuildTarget.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive);
            foreach (var slot in adaptive)
            {
                slot.SetTextures(bgTex, fgTex); // layer 0 background, layer 1 foreground
            }

            PlayerSettings.SetPlatformIcons(
                NamedBuildTarget.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive, adaptive);

            if (legacy != null)
            {
                foreach (var kind in new[]
                {
                    UnityEditor.Android.AndroidPlatformIconKind.Round,
                    UnityEditor.Android.AndroidPlatformIconKind.Legacy,
                })
                {
                    var slots = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                    foreach (var slot in slots)
                    {
                        slot.SetTextures(legacy);
                    }

                    PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, slots);
                }
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
