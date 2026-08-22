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
            const string spherePath = dir + "/sphere.png";
            Directory.CreateDirectory(dir);

            EnsureSprite(roundedPath, UIFactory.RoundedSpritePng, new Vector4(24, 24, 24, 24));
            EnsureSprite(circlePath, UIFactory.CircleSpritePng);
            EnsureSprite(starPath, UIFactory.StarSpritePng);
            EnsureSprite(pennantPath, UIFactory.PennantSpritePng);
            EnsureSprite(spherePath, UIFactory.SphereSpritePng);

            // The studio mark is drawn art, not generated — it is only ever
            // imported and handed over.
            const string logoPath = dir + "/efs-logo.png";
            if (File.Exists(logoPath))
            {
                // Written from outside the editor, so the database has never
                // heard of it: import first, or GetAtPath hands back null and
                // the whole thing silently does nothing.
                AssetDatabase.ImportAsset(logoPath);
                ApplySpriteImport(logoPath);
                var logo = AssetDatabase.LoadAssetAtPath<Sprite>(logoPath);
                if (logo != null)
                {
                    UIFactory.UseStudioLogo(logo);
                }
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
            var sphere = AssetDatabase.LoadAssetAtPath<Sprite>(spherePath);
            if (rounded != null && circle != null && star != null && pennant != null && sphere != null)
            {
                UIFactory.UseSpriteAssets(rounded, circle, star, pennant, sphere);
            }
        }

        /// <summary>
        /// Generates a sprite asset if it is missing, and enforces its import
        /// settings every time — settings on an asset that already exists are
        /// otherwise frozen at whatever they were the day it was created.
        ///
        /// Clamp is the one that matters. Unity imports textures wrapping by
        /// default, so sampling a hair past the right edge of a sprite comes
        /// back with the pixel from its LEFT edge. On the pennant that left
        /// edge is the full-height side tied to the pole, so the menu emblem
        /// grew a red line down the far side of the flag — a rendering
        /// artifact that looked exactly like a drawing mistake.
        /// </summary>
        private static void EnsureSprite(string path, System.Func<byte[]> generate, Vector4 border = default)
        {
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, generate());
                AssetDatabase.ImportAsset(path);
            }

            ApplySpriteImport(path, border);
        }

        /// <summary>
        /// Forces the import settings a UI sprite needs, whether the file was
        /// generated here or drawn somewhere else.
        /// </summary>
        private static void ApplySpriteImport(string path, Vector4 border = default)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            // Explicit, because the default is not what it looks like. A file
            // first imported as a plain texture and later switched to Sprite
            // keeps its old sprite mode, and in Multiple with no sheet defined
            // there is no Sprite sub-asset at all — LoadAssetAtPath returns
            // null and every caller quietly falls back, which is exactly what
            // happened to the studio logo.
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (importer.spriteBorder != border)
            {
                importer.spriteBorder = border;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
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
        /// Prints everything a store upload depends on, so it can be READ
        /// rather than remembered: package id, version, target API, minimum
        /// API, architectures, scripting backend and whether the Unity splash
        /// will appear. Run it before a release; a build that fails Play's
        /// checks fails days later, in an email.
        /// </summary>
        [MenuItem("PuttSeed/Report Release Settings")]
        public static void ReportReleaseSettings()
        {
            ConfigureSplash();
            ConfigureAndroidTarget();
            var target = NamedBuildTarget.Android;
            var lines = new[]
            {
                "PuttSeed release settings",
                $"  package        {PlayerSettings.GetApplicationIdentifier(target)}",
                $"  version        {PlayerSettings.bundleVersion} (code {PlayerSettings.Android.bundleVersionCode})",
                $"  target API     {(int)PlayerSettings.Android.targetSdkVersion}",
                $"  minimum API    {(int)PlayerSettings.Android.minSdkVersion}",
                $"  architectures  {PlayerSettings.Android.targetArchitectures}",
                $"  backend        {PlayerSettings.GetScriptingBackend(target)}",
                $"  unity splash   {(PlayerSettings.SplashScreen.show ? "SHOWN" : "off")}",
                $"  orientation    {PlayerSettings.defaultInterfaceOrientation}",
                $"  il2cpp codegen {PlayerSettings.GetIl2CppCodeGeneration(target)}",
                $"  il2cpp config  {PlayerSettings.GetIl2CppCompilerConfiguration(target)}",
                $"  stripping      {PlayerSettings.GetManagedStrippingLevel(target)}",
            };

            Debug.Log(string.Join(System.Environment.NewLine, lines));
        }

        /// <summary>
        /// Batch-mode release build: signed .aab when keystore.properties is
        /// present next to the project (never committed), warning-unsigned
        /// otherwise. Entry point of scripts/build-release.bat.
        /// </summary>
        public static void BuildAndroidRelease()
        {
            BumpVersionCode();
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
        /// Raises the Android version code by one and writes it back to
        /// ProjectSettings.
        ///
        /// Play refuses an upload whose version code it has already seen, and
        /// remembering to raise it by hand is the single most common way a
        /// release stalls. The human version (bundleVersion, "1.0") is left
        /// alone — that one is a decision, not bookkeeping.
        /// </summary>
        private static void BumpVersionCode()
        {
            PlayerSettings.Android.bundleVersionCode++;
            AssetDatabase.SaveAssets();
            Debug.Log($"PuttSeed: version {PlayerSettings.bundleVersion} " +
                $"(code {PlayerSettings.Android.bundleVersionCode}).");
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
            ConfigureAndroidTarget();
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
        /// Batch-mode WebGL build for the playable demo, into
        /// <c>artifacts/webgl</c>. The demo is not a port: a mouse drag and a
        /// finger drag both reach <c>InputQuantizer</c> as the same two
        /// integers, so the browser runs the shipping sim, bit for bit.
        /// </summary>
        public static void BuildWebGL()
        {
            // Switching the active target to WebGL makes the editor rewrite
            // AndroidMinSdkVersion — to 25, where an Android build rewrites it
            // to 23 (see the commit that let the file hold Unity's number).
            // Neither value can stay clean while both builds exist, so this
            // build puts back whatever it found: one setting, one writer, and
            // a working tree that a WebGL build no longer dirties.
            var androidMinSdk = PlayerSettings.Android.minSdkVersion;

            EnsureFeelConfig();
            EnsureAppIcon();
            ConfigureSplash();
            if (!File.Exists(MenuScenePath) || !File.Exists(GameScenePath))
            {
                CreateScenes();
            }

            PlayerSettings.companyName = "PuttSeed";
            PlayerSettings.productName = "PuttSeed";

            // A static host cannot add a Content-Encoding header, and GitHub
            // Pages is exactly that: a compressed build would arrive as bytes
            // the browser never inflates. Gzip plus Unity's own JS
            // decompressor keeps the download small and the host dumb.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.template = "PROJECT:PuttSeed";

            // The demo is the first thing a reviewer touches, so a thrown
            // exception should still say what it was. Full stack traces cost
            // size and speed; explicitly thrown ones cost neither much.
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            // Release, not Master: Master's extra inlining buys a few percent
            // of runtime on a game whose whole sim is already a rounding error
            // next to the browser's frame budget, and costs a great deal of
            // build time on a target that is rebuilt by hand.
            PlayerSettings.SetIl2CppCompilerConfiguration(
                NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Release);

            AddAlwaysIncludedShader("Sprites/Default");

            const string output = "artifacts/webgl";
            Directory.CreateDirectory("artifacts");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { MenuScenePath, GameScenePath },
                target = BuildTarget.WebGL,
                locationPathName = output,
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"PuttSeed: WebGL build {report.summary.result}, " +
                $"{report.summary.totalSize} bytes, {report.summary.totalErrors} errors -> {output}");

            PlayerSettings.Android.minSdkVersion = androidMinSdk;
            AssetDatabase.SaveAssets();

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
            // Off where the licence allows it — two seconds of somebody else's
            // logo is two seconds of a daily game's whole session. Unity puts
            // it back at build time when the licence requires it, so the felt
            // background and the light logo stay configured for that case: if
            // it must be shown, it will at least be shown on our green.
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.22f, 0.52f, 0.31f);
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            Debug.Log(PlayerSettings.SplashScreen.show
                ? "PuttSeed: the Unity splash stays (licence requires it) — themed green."
                : "PuttSeed: Unity splash disabled.");
        }

        /// <summary>
        /// Pins the Android target API level.
        ///
        /// The setting was "Automatic (highest installed)", and the highest
        /// installed here is a PREVIEW SDK — Play rejects an upload built
        /// against one, and the failure would arrive months from now on a
        /// machine that had simply picked up a new SDK. A pinned number also
        /// makes two machines build the same thing, which is the rest of this
        /// repo's whole argument.
        ///
        /// Raise it when Play raises its requirement, deliberately.
        /// </summary>
        private static void ConfigureAndroidTarget()
        {
            const int targetApi = 36; // Android 16
            if ((int)PlayerSettings.Android.targetSdkVersion != targetApi)
            {
                PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)targetApi;
                Debug.Log($"PuttSeed: pinned Android target API to {targetApi}.");
            }

            // The FLOOR is pinned for the same reason as the ceiling, and for
            // one more: the editor keeps serializing 23 into ProjectSettings
            // whatever is asked of it. Setting it here is what actually
            // reaches the package — read back out of the built APK with
            // aapt2, which reports minSdkVersion 25 while the file on disk
            // still says 23. 25 is Android 7.1, the oldest phone this game
            // has any business claiming to run on.
            const int minimumApi = 25;
            if ((int)PlayerSettings.Android.minSdkVersion != minimumApi)
            {
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)minimumApi;
                Debug.Log($"PuttSeed: pinned Android minimum API to {minimumApi}.");
            }
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
