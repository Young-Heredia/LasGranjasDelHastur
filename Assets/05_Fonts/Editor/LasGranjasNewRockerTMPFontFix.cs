using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace LasGranjas.Editor
{
    /// <summary>
    /// "NewRocker-Regular SDF.asset" creado como Unity TextCore Font Asset no es un TMP_FontAsset;
    /// TMP no puede usarlo y acaba buscando LiberationSans (warnings / NullReference).
    /// Este script lo sustituye por un TMP_FontAsset SDF válido vía la API pública CreateFontAsset.
    /// </summary>
    [InitializeOnLoad]
    public static class LasGranjasNewRockerTMPFontFix
    {
        const string TtfPath = "Assets/05_Fonts/Lucas/NewRocker-Regular.ttf";
        const string SdfPath = "Assets/05_Fonts/Lucas/NewRocker-Regular SDF.asset";

        static LasGranjasNewRockerTMPFontFix()
        {
            EditorApplication.delayCall += TryFixOnceAfterLoad;
        }

        static void TryFixOnceAfterLoad()
        {
            if (Application.isPlaying)
                return;

            try
            {
                var ok = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SdfPath);
                if (ok != null)
                    return;

                RunFix();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LasGranjas] Error al reparar la fuente TMP: {e.Message}\n{e.StackTrace}");
            }
        }

        [MenuItem("Tools/Las Granjas del Hastur/Reparar fuente New Rocker (TMP)")]
        public static void RunFixFromMenu()
        {
            RunFix();
        }

        static void RunFix()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[LasGranjas] Reparar fuente: solo en modo Editor (no en Play).");
                return;
            }

            try
            {
                RunFixCore();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LasGranjas] Reparar fuente New Rocker falló: {e.Message}\n{e.StackTrace}");
            }
        }

        static void RunFixCore()
        {
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (sourceFont == null)
            {
                Debug.LogError($"[LasGranjas] No se encuentra la fuente en {TtfPath}.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SdfPath) != null)
                AssetDatabase.DeleteAsset(SdfPath);

            // API pública: el código interno de TMP puede asignar campos internal.
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);

            if (fontAsset == null)
            {
                Debug.LogWarning(
                    $"[LasGranjas] CreateFontAsset falló. Activa \"Include Font Data\" en el importador del .ttf.",
                    sourceFont);
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(TtfPath);
            fontAsset.creationSettings = new FontAssetCreationSettings
            {
                sourceFontFileName = string.Empty,
                sourceFontFileGUID = guid,
                faceIndex = 0,
                pointSize = 90,
                pointSizeSamplingMode = 0,
                padding = 9,
                paddingMode = 2,
                packingMode = 0,
                atlasWidth = 1024,
                atlasHeight = 1024,
                characterSetSelectionMode = 7,
                characterSequence = string.Empty,
                referencedFontAssetGUID = string.Empty,
                referencedTextAssetGUID = string.Empty,
                fontStyle = 0,
                fontStyleModifier = 0,
                renderMode = (int)GlyphRenderMode.SDFAA,
                includeFontFeatures = false
            };

            AssetDatabase.CreateAsset(fontAsset, SdfPath);

            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            if (fontAsset.material != null)
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var loaded = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SdfPath);
            if (loaded == null)
            {
                Debug.LogError("[LasGranjas] No se pudo cargar el TMP Font Asset recién creado.");
                return;
            }

            AssignFontEverywhere(loaded);
            SetTmpDefaultFont(loaded);

            Debug.Log("[LasGranjas] Fuente New Rocker (TMP SDF) reparada y asignada.");
        }

        static void AssignFontEverywhere(TMP_FontAsset fontAsset)
        {
            string activePath = SceneManager.GetActiveScene().path;
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });

            foreach (var guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    continue;

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool dirty = false;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        if (tmp.font != fontAsset)
                        {
                            tmp.font = fontAsset;
                            dirty = true;
                        }
                    }
                }

                if (dirty)
                    EditorSceneManager.SaveScene(scene);
            }

            if (!string.IsNullOrEmpty(activePath))
                EditorSceneManager.OpenScene(activePath, OpenSceneMode.Single);
        }

        static void SetTmpDefaultFont(TMP_FontAsset fontAsset)
        {
            if (TMP_Settings.instance == null)
                return;

            var so = new SerializedObject(TMP_Settings.instance);
            var prop = so.FindProperty("m_defaultFontAsset");
            if (prop != null)
            {
                prop.objectReferenceValue = fontAsset;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(TMP_Settings.instance);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
