#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1.Editor
{
    /// <summary>
    /// Build safety net:
    /// 1) Ensures all game scenes under Assets/00_Scenes are included in build settings.
    /// 2) Copies runtime media used by path-based loaders into StreamingAssets/RuntimeArtCache.
    /// </summary>
    public sealed class ZoneBuildArtStreamingSync : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        const string SourceSpritesRoot = "Assets/02_Sprites";
        const string SourceAudioRoot = "Assets/03_Audio";
        const string GameScenesRoot = "Assets/00_Scenes";
        const string StreamingCacheRoot = "Assets/StreamingAssets/RuntimeArtCache";
        const string DefaultWindowsBuildOutput = "Builds/Windows/LasGranjasDelHastur.exe";
        const bool FailBuildOnMissingRuntimeAssets = false;
        const string SceneMainMenu = "Assets/00_Scenes/Edwin/MainMenu.unity";
        const string SceneIntroComic = "Assets/00_Scenes/Lucas/IntroComic.unity";
        const string SceneZoneSelection = "Assets/00_Scenes/Lucas/ZoneSelection.unity";
        const string SceneZone1 = "Assets/00_Scenes/Lucas/Zone1_Dungeons.unity";
        const string SceneZone2 = "Assets/00_Scenes/Lucas/Zone2_Cities.unity";
        const string SceneZone3 = "Assets/00_Scenes/Lucas/Zone3_Celestial.unity";
        const string SceneCosmicRhythm = "Assets/00_Scenes/Edwin/CosmicHarvestRhythm.unity";

        static readonly string[] IgnoredCopyExtensions =
        {
            ".meta",
        };

        static readonly Regex AssetPathRegex =
            new("\"(Assets/[^\"]+)\"", RegexOptions.Compiled);

        static readonly string[] RuntimeValidatedExtensions =
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".webp",
            ".bmp",
            ".tga",
            ".gif",
            ".mp3",
            ".wav",
            ".ogg",
            ".aiff",
            ".flac",
            ".prefab",
            ".unity",
            ".asset",
        };

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            EnsureGameScenesInBuildSettings();
            ValidateAssetPathsReferencedInCode();
            SyncStreamingCache();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            CleanupStreamingCache();
        }

        [MenuItem("Las Granjas/Build/Preparar cache de arte para build")]
        static void SyncStreamingCache()
        {
            var targetRootAbs = ToAbsolutePath(StreamingCacheRoot);
            if (Directory.Exists(targetRootAbs))
                Directory.Delete(targetRootAbs, true);

            Directory.CreateDirectory(targetRootAbs);

            var copied = 0;
            copied += CopyRuntimeMediaRoot(SourceSpritesRoot, targetRootAbs);
            copied += CopyRuntimeMediaRoot(SourceAudioRoot, targetRootAbs);

            AssetDatabase.Refresh();
            Debug.Log($"[ZoneBuildArtStreamingSync] Copiados {copied} archivos a {StreamingCacheRoot}.");
        }

        static int CopyRuntimeMediaRoot(string sourceAssetRoot, string targetRootAbs)
        {
            var sourceRootAbs = ToAbsolutePath(sourceAssetRoot);
            if (!Directory.Exists(sourceRootAbs))
            {
                Debug.LogWarning($"[ZoneBuildArtStreamingSync] No existe {sourceAssetRoot}; se omite.");
                return 0;
            }

            var files = Directory.GetFiles(sourceRootAbs, "*.*", SearchOption.AllDirectories);
            var copied = 0;
            var sourceAssetRootNoPrefix = sourceAssetRoot.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase)
                ? sourceAssetRoot.Substring("Assets/".Length)
                : sourceAssetRoot;

            foreach (var src in files)
            {
                if (ShouldSkipCopy(src))
                    continue;
                var relativeFromSource = src.Substring(sourceRootAbs.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var dst = Path.Combine(targetRootAbs, sourceAssetRootNoPrefix, relativeFromSource);
                var dstDir = Path.GetDirectoryName(dst);
                if (!string.IsNullOrEmpty(dstDir))
                    Directory.CreateDirectory(dstDir);
                File.Copy(src, dst, true);
                copied++;
            }

            return copied;
        }

        static bool ShouldSkipCopy(string fullPath)
        {
            var ext = Path.GetExtension(fullPath);
            if (string.IsNullOrEmpty(ext))
                return false; // keep extensionless files if any
            return IgnoredCopyExtensions.Any(ignored => ext.Equals(ignored, System.StringComparison.OrdinalIgnoreCase));
        }

        [MenuItem("Las Granjas/Build/Sincronizar escenas de juego (00_Scenes)")]
        static void EnsureGameScenesInBuildSettings()
        {
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { GameScenesRoot });
            var allScenePaths = sceneGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .OrderBy(p => p, System.StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (allScenePaths.Count == 0)
                return;

            var preferredOrder = new[]
            {
                SceneMainMenu,
                SceneIntroComic,
                SceneZoneSelection,
                SceneZone1,
                SceneZone2,
                SceneZone3,
                SceneCosmicRhythm, // debug/prototype scene, kept last on purpose
            };

            var updated = new List<EditorBuildSettingsScene>(allScenePaths.Count);
            foreach (var scenePath in preferredOrder)
            {
                if (!allScenePaths.Contains(scenePath))
                    continue;
                // Keep prototype scene out of shipping flow unless explicitly enabled by team.
                var enabled = !string.Equals(scenePath, SceneCosmicRhythm, System.StringComparison.OrdinalIgnoreCase);
                updated.Add(new EditorBuildSettingsScene(scenePath, enabled));
            }

            // Append any additional scenes not listed above, disabled by default.
            foreach (var extra in allScenePaths)
            {
                if (preferredOrder.Any(p => string.Equals(p, extra, System.StringComparison.OrdinalIgnoreCase)))
                    continue;
                updated.Add(new EditorBuildSettingsScene(extra, false));
            }

            var current = EditorBuildSettings.scenes;
            var same = current.Length == updated.Count;
            if (same)
            {
                for (var i = 0; i < current.Length; i++)
                {
                    if (current[i].enabled != updated[i].enabled ||
                        !string.Equals(current[i].path, updated[i].path, System.StringComparison.OrdinalIgnoreCase))
                    {
                        same = false;
                        break;
                    }
                }
            }

            if (same)
                return;

            EditorBuildSettings.scenes = updated.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"[ZoneBuildArtStreamingSync] Build settings sincronizado con {updated.Count} escenas de {GameScenesRoot}.");
        }

        [MenuItem("Las Granjas/Build/Validar rutas Assets/* en scripts")]
        static void ValidateAssetPathsReferencedInCode()
        {
            var scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { "Assets/01_Scripts" });
            var missing = new List<string>();

            foreach (var guid in scriptGuids)
            {
                var scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(scriptPath))
                    continue;
                if (scriptPath.IndexOf("/Editor/", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                var scriptAbs = ToAbsolutePath(scriptPath);
                if (!File.Exists(scriptAbs))
                    continue;

                var text = File.ReadAllText(scriptAbs);
                foreach (Match match in AssetPathRegex.Matches(text))
                {
                    var assetPath = match.Groups[1].Value;
                    if (string.IsNullOrWhiteSpace(assetPath))
                        continue;
                    var normalized = assetPath.Replace('\\', '/');
                    if (!normalized.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (normalized.Contains("*"))
                        continue;
                    if (normalized.Contains("{") || normalized.Contains("}"))
                        continue;
                    if (normalized.EndsWith("/", System.StringComparison.Ordinal))
                        continue;

                    var ext = Path.GetExtension(normalized);
                    if (string.IsNullOrEmpty(ext))
                        continue;
                    var shouldValidate = RuntimeValidatedExtensions.Any(
                        allowed => ext.Equals(allowed, System.StringComparison.OrdinalIgnoreCase));
                    if (!shouldValidate)
                        continue;

                    // Buyer portrait/icon legacy fallbacks are intentionally optional:
                    // current art source is Resources/Edwin/Buyers previews.
                    if (scriptPath.EndsWith("BuyerPortraitResolver.cs", System.StringComparison.OrdinalIgnoreCase) &&
                        (normalized.StartsWith("Assets/02_Sprites/Lucas/Zone1/Portraits/zone1_buyer_", System.StringComparison.OrdinalIgnoreCase) ||
                         normalized.StartsWith("Assets/02_Sprites/Lucas/Zone1/Icons/zone1_buyer_", System.StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var type = AssetDatabase.GetMainAssetTypeAtPath(normalized);
                    if (type == null && !File.Exists(ToAbsolutePath(normalized)))
                        missing.Add($"{scriptPath} -> {normalized}");
                }
            }

            if (missing.Count == 0)
            {
                Debug.Log("[ZoneBuildArtStreamingSync] Validación OK: no se detectaron rutas Assets/* faltantes en scripts.");
                return;
            }

            var preview = string.Join("\n", missing.Take(20));
            if (missing.Count > 20)
                preview += $"\n... y {missing.Count - 20} más.";
            var message =
                "[ZoneBuildArtStreamingSync] Se detectaron rutas de assets posiblemente faltantes en scripts " +
                "(revisa para evitar placeholders):\n" + preview;
            if (FailBuildOnMissingRuntimeAssets)
                throw new BuildFailedException(message);
            Debug.LogWarning(message);
        }

        [MenuItem("Las Granjas/Build/Build Windows Debug (Completa)")]
        static void BuildWindowsDebugComplete()
        {
            EnsureGameScenesInBuildSettings();
            ValidateAssetPathsReferencedInCode();
            SyncStreamingCache();

            var scenes = EditorBuildSettings.scenes
                .Where(s => s != null && s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new BuildFailedException("No hay escenas activas en Build Settings.");

            var output = ToAbsolutePath(DefaultWindowsBuildOutput);
            var outputDir = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development |
                          BuildOptions.AllowDebugging |
                          BuildOptions.ConnectWithProfiler |
                          BuildOptions.DetailedBuildReport,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Build falló: {report.summary.result}");

            Debug.Log($"[ZoneBuildArtStreamingSync] Build DEBUG completa OK: {output}");
        }

        [MenuItem("Las Granjas/Build/Limpiar cache de arte de StreamingAssets")]
        static void CleanupStreamingCache()
        {
            var targetRootAbs = ToAbsolutePath(StreamingCacheRoot);
            if (Directory.Exists(targetRootAbs))
            {
                Directory.Delete(targetRootAbs, true);
                var metaPath = targetRootAbs + ".meta";
                if (File.Exists(metaPath))
                    File.Delete(metaPath);
                AssetDatabase.Refresh();
            }
        }

        static string ToAbsolutePath(string assetPath)
        {
            var root = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            return Path.Combine(root, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
#endif
