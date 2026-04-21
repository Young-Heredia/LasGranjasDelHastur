using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LasGranjas.Editor
{
    /// <summary>
    /// Importa texturas de ZoneSelection (pixel art, sprites) y enlaza la escena con los assets.
    /// </summary>
    public static class ZoneSelectionVisualSetup
    {
        const string ZoneScenePath = "Assets/Scenes/ZoneSelection.unity";
        const string Root = "Assets/Sprites/UI/ZoneSelection";

        const string Bg = Root + "/Backgrounds/zone_selection_bg_16x9.png";
        const string PanelFrame = Root + "/Panels/panel_frame_ritual.png";
        const string TitleBanner = Root + "/Panels/title_ornament_banner.png";
        const string CardNormal = Root + "/Cards/zone_card_panel_normal.png";
        const string CardLocked = Root + "/Cards/zone_card_panel_locked.png";
        const string Thumb1 = Root + "/Thumbnails/thumb_zone1_dungeons.png";
        const string Thumb2 = Root + "/Thumbnails/thumb_zone2_cities.png";
        const string Thumb3 = Root + "/Thumbnails/thumb_zone3_celestial.png";
        const string BadgeLocked = Root + "/Badges/badge_locked_seal.png";
        const string BtnPrimary = Root + "/Buttons/btn_primary_normal.png";
        const string BtnSecondary = Root + "/Buttons/btn_secondary_normal.png";
        const string OverlayAmbient = Root + "/Overlays/overlay_ambient_cosmic.png";
        const string IconStrip = Root + "/Icons/ui_icons_strip_6.png";
        const string DecorCorner = Root + "/Decorations/decor_corner_ornament.png";
        const string DecorSep = Root + "/Decorations/decor_separator_line.png";
        const string WhiteSpritePath = "Assets/Sprites/Placeholders/white_1x1.png";

        [MenuItem("Tools/Las Granjas del Hastur/ZoneSelection: importar texturas UI (pixel)")]
        public static void ConfigureImportSettings()
        {
            EnsureZoneSelectionFolders();
            var paths = new[]
            {
                Bg, PanelFrame, TitleBanner, CardNormal, CardLocked,
                Thumb1, Thumb2, Thumb3, BadgeLocked, BtnPrimary, BtnSecondary,
                OverlayAmbient, DecorCorner, DecorSep
            };

            foreach (var p in paths)
                if (AssetExists(p))
                    ConfigureSingleSpriteTexture(p);

            if (AssetExists(IconStrip))
                ConfigureIconStripTexture(IconStrip);

            AssetDatabase.Refresh();
            Debug.Log("[LasGranjas] Importación ZoneSelection configurada (revisa la tira de iconos en el Sprite Editor si hace falta).");
        }

        /// <summary>
        /// Convierte píxeles de mate gris casi neutro en transparencia (el PNG puede traer gris opaco en lugar de alfa).
        /// Ejecutar una vez; revisa el resultado en el Sprite Editor y haz backup si hace falta.
        /// </summary>
        [MenuItem("Tools/Las Granjas del Hastur/ZoneSelection: limpiar mate gris decor_corner_ornament.png")]
        public static void StripDecorCornerGrayMatte()
        {
            if (!AssetExists(DecorCorner))
            {
                Debug.LogError("[LasGranjas] No existe " + DecorCorner);
                return;
            }

            var imp = AssetImporter.GetAtPath(DecorCorner) as TextureImporter;
            if (imp == null)
                return;

            var prevReadable = imp.isReadable;
            var prevCompression = imp.textureCompression;
            imp.isReadable = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(DecorCorner);
            if (tex == null)
            {
                Debug.LogError("[LasGranjas] No se pudo cargar la textura.");
                RestoreImporter(imp, prevReadable, prevCompression);
                return;
            }

            var pixels = tex.GetPixels32();
            var changed = 0;
            for (var i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                if (c.a < 200)
                    continue;
                var r = c.r / 255f;
                var g = c.g / 255f;
                var b = c.b / 255f;
                var mx = Mathf.Max(r, Mathf.Max(g, b));
                var mn = Mathf.Min(r, Mathf.Min(g, b));
                if (mx < 0.52f && mn > 0.06f && mx - mn < 0.1f)
                {
                    pixels[i] = new Color32(0, 0, 0, 0);
                    changed++;
                }
            }

            if (changed == 0)
            {
                Debug.Log("[LasGranjas] No se detectaron píxeles de mate gris con el criterio actual; el PNG no se modificó.");
                RestoreImporter(imp, prevReadable, prevCompression);
                return;
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            var diskPath = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName,
                DecorCorner.Replace('/', Path.DirectorySeparatorChar));
            File.WriteAllBytes(diskPath, tex.EncodeToPNG());

            RestoreImporter(imp, prevReadable, prevCompression);
            ConfigureSingleSpriteTexture(DecorCorner);
            Debug.Log("[LasGranjas] decor_corner_ornament: " + changed +
                      " píxeles pasados a transparente. Revisa el sprite; si se comió detalle del ornamento, restaura desde backup y ajusta el criterio.");
        }

        static void RestoreImporter(TextureImporter imp, bool readable, TextureImporterCompression compression)
        {
            if (imp == null)
                return;
            imp.isReadable = readable;
            imp.textureCompression = compression;
            imp.SaveAndReimport();
        }

        static bool AssetExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                return false;
            var full = Path.Combine(Application.dataPath, assetPath.Substring(7));
            return File.Exists(full);
        }

        [MenuItem("Tools/Las Granjas del Hastur/ZoneSelection: enlazar visuales en la escena")]
        public static void WireSceneVisuals()
        {
            EnsureZoneSelectionFolders();
            ConfigureImportSettings();

            EditorSceneManager.OpenScene(ZoneScenePath, OpenSceneMode.Single);

            var canvas = GameObject.Find("Canvas")?.GetComponent<RectTransform>();
            if (canvas == null)
            {
                Debug.LogError("[LasGranjas] No hay Canvas en ZoneSelection.");
                return;
            }

            EnsureCanvasRootValid(canvas);
            EnsureBackgroundSolid(canvas);

            EnsurePanelFrame(canvas);
            EnsureAmbientOverlay(canvas);
            EnsureHeaderDecor(canvas);
            EnsureCardThumbnailsAndSeals();
            WireBackButton();

            var bg = GameObject.Find("Background")?.GetComponent<Image>();
            if (bg != null)
            {
                var sp = LoadFirstSprite(Bg);
                if (sp != null)
                {
                    bg.sprite = sp;
                    bg.color = Color.white;
                    bg.preserveAspect = false;
                }
            }

            WireZoneCards();

            EnsureBackdropRaycastsOff(canvas);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LasGranjas] Visuales ZoneSelection enlazados en la escena.");
        }

        [MenuItem("Tools/Las Granjas del Hastur/ZoneSelection: REPARAR Canvas y fondos (escala, overlay, raycasts)")]
        public static void RepairZoneSelectionCanvasAndBackdrops()
        {
            EditorSceneManager.OpenScene(ZoneScenePath, OpenSceneMode.Single);
            var canvas = GameObject.Find("Canvas")?.GetComponent<RectTransform>();
            if (canvas == null)
            {
                Debug.LogError("[LasGranjas] No hay Canvas en ZoneSelection.");
                return;
            }

            EnsureCanvasRootValid(canvas);
            EnsureBackgroundSolid(canvas);
            EnsureBackdropRaycastsOff(canvas);

            var header = canvas.Find("Header");
            if (header != null)
                RefreshDecorCornerTransforms(header);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LasGranjas] ZoneSelection: Canvas + BackgroundSolid + raycasts de fondo reparados. Revisa Play.");
        }

        static void EnsureZoneSelectionFolders()
        {
            var parts = Root.Split('/');
            var cur = "Assets";
            for (var i = 1; i < parts.Length; i++)
            {
                var next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        static void ConfigureSingleSpriteTexture(string assetPath)
        {
            var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null)
                return;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }

        static void ConfigureIconStripTexture(string assetPath)
        {
            var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null)
                return;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex == null)
            {
                Debug.LogWarning("[LasGranjas] No se pudo leer " + assetPath + " para cortar iconos.");
                return;
            }

            var w = tex.width;
            var h = tex.height;

            imp.spriteImportMode = SpriteImportMode.Multiple;
            imp.SaveAndReimport();

            imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null)
                return;

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(imp);
            if (provider == null)
            {
                Debug.LogWarning("[LasGranjas] No hay ISpriteEditorDataProvider para " + assetPath + ".");
                return;
            }

            provider.InitSpriteEditorDataProvider();

            var names = new[] { "icon_available", "icon_locked", "icon_progress", "icon_warning", "icon_portal", "icon_back" };
            var sliceW = w / 6;
            var rects = new SpriteRect[6];
            for (var i = 0; i < 6; i++)
            {
                var x = i * sliceW;
                var rw = i == 5 ? w - x : sliceW;
                var sr = new SpriteRect
                {
                    name = names[i],
                    rect = new Rect(x, 0, rw, h),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = SpriteAlignment.Center,
                    border = Vector4.zero
                };
                sr.spriteID = GUID.Generate();
                rects[i] = sr;
            }

            provider.SetSpriteRects(rects);
            provider.Apply();
            imp.SaveAndReimport();
        }

        static Sprite LoadFirstSprite(string assetPath)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (s != null)
                return s;
            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
        }

        /// <summary>
        /// Corrige escenas dañadas: Canvas raíz a escala 1, stretch a pantalla completa, Screen Space - Overlay.
        /// </summary>
        static void EnsureCanvasRootValid(RectTransform canvas)
        {
            canvas.localScale = Vector3.one;
            StretchFull(canvas);

            var cv = canvas.GetComponent<Canvas>();
            if (cv != null)
            {
                cv.renderMode = RenderMode.ScreenSpaceOverlay;
                cv.worldCamera = null;
            }
        }

        /// <summary>
        /// Relleno opaco bajo el arte y el marco (el marco PNG suele tener centro transparente).
        /// Evita el patrón de tablero cuando falta el sprite del fondo o hay huecos alfa.
        /// </summary>
        static void EnsureBackgroundSolid(RectTransform canvas)
        {
            var sp = LoadFirstSprite(WhiteSpritePath);
            if (sp == null)
            {
                Debug.LogWarning("[LasGranjas] No se encontró " + WhiteSpritePath + " para BackgroundSolid.");
                return;
            }

            var existing = canvas.Find("BackgroundSolid");
            if (existing != null)
            {
                var rt = existing.GetComponent<RectTransform>();
                StretchFull(rt);
                existing.SetAsFirstSibling();
                var img = existing.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = sp;
                    img.color = new Color(0.06f, 0.04f, 0.1f, 1f);
                    img.raycastTarget = false;
                }

                return;
            }

            var go = new GameObject("BackgroundSolid", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var nrt = go.GetComponent<RectTransform>();
            StretchFull(nrt);
            go.transform.SetAsFirstSibling();

            var nimg = go.AddComponent<Image>();
            nimg.sprite = sp;
            nimg.color = new Color(0.06f, 0.04f, 0.1f, 1f);
            nimg.raycastTarget = false;
            nimg.type = Image.Type.Simple;
        }

        static void EnsurePanelFrame(RectTransform canvas)
        {
            if (canvas.Find("PanelFrame") != null)
                return;

            var go = new GameObject("PanelFrame", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var rt = go.GetComponent<RectTransform>();
            StretchFull(rt);
            go.transform.SetSiblingIndex(1);

            var img = go.AddComponent<Image>();
            var sp = LoadFirstSprite(PanelFrame);
            if (sp != null)
                img.sprite = sp;
            img.color = Color.white;
            img.raycastTarget = false;
            img.preserveAspect = false;
        }

        static void EnsureAmbientOverlay(RectTransform canvas)
        {
            var t = canvas.Find("AmbientOverlay");
            GameObject go;
            if (t == null)
            {
                go = new GameObject("AmbientOverlay", typeof(RectTransform));
                go.transform.SetParent(canvas, false);
                var rt = go.GetComponent<RectTransform>();
                StretchFull(rt);
                go.AddComponent<Image>();
            }
            else
                go = t.gameObject;

            // Debajo del marco y del contenido: encima de Background, debajo de PanelFrame (no SetAsLastSibling).
            var bg = canvas.Find("Background");
            if (bg != null)
                go.transform.SetSiblingIndex(bg.GetSiblingIndex() + 1);
            else
                go.transform.SetSiblingIndex(1);

            var img = go.GetComponent<Image>();
            var sp = LoadFirstSprite(OverlayAmbient);
            if (sp != null)
                img.sprite = sp;
            img.color = new Color(1f, 1f, 1f, 0.4f);
            img.raycastTarget = false;
        }

        static void EnsureBackdropRaycastsOff(RectTransform canvas)
        {
            foreach (var name in new[] { "BackgroundSolid", "Background", "AmbientOverlay", "PanelFrame" })
            {
                var t = canvas.Find(name);
                if (t == null)
                    continue;
                var img = t.GetComponent<Image>();
                if (img != null)
                    img.raycastTarget = false;
            }
        }

        static void EnsureHeaderDecor(RectTransform canvas)
        {
            var header = canvas.Find("Header");
            if (header == null)
                return;

            if (header.Find("TitleOrnament") == null)
            {
                var go = new GameObject("TitleOrnament", typeof(RectTransform));
                go.transform.SetParent(header, false);
                go.transform.SetAsFirstSibling();
                var rt = go.GetComponent<RectTransform>();
                StretchFull(rt);
                var img = go.AddComponent<Image>();
                var sp = LoadFirstSprite(TitleBanner);
                if (sp != null)
                    img.sprite = sp;
                img.color = Color.white;
                img.raycastTarget = false;
            }

            if (header.Find("DecorCornerLeft") == null)
            {
                var go = new GameObject("DecorCornerLeft", typeof(RectTransform));
                go.transform.SetParent(header, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.sizeDelta = new Vector2(96, 96);
                rt.anchoredPosition = new Vector2(8, -8);
                var img = go.AddComponent<Image>();
                var sp = LoadFirstSprite(DecorCorner);
                if (sp != null)
                    img.sprite = sp;
                img.color = new Color(1f, 1f, 1f, 0.55f);
                img.raycastTarget = false;
                img.preserveAspect = true;
            }

            if (header.Find("DecorCornerRight") == null)
            {
                var go = new GameObject("DecorCornerRight", typeof(RectTransform));
                go.transform.SetParent(header, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                rt.sizeDelta = new Vector2(96, 96);
                rt.anchoredPosition = new Vector2(-8, -8);
                var img = go.AddComponent<Image>();
                var sp = LoadFirstSprite(DecorCorner);
                if (sp != null)
                    img.sprite = sp;
                img.color = new Color(1f, 1f, 1f, 0.55f);
                img.raycastTarget = false;
                img.preserveAspect = true;
            }

            RefreshDecorCornerTransforms(header);
        }

        /// <summary>
        /// El sprite suele estar dibujado para una sola esquina: espejo en la izquierda, natural en la derecha.
        /// </summary>
        static void RefreshDecorCornerTransforms(Transform header)
        {
            var left = header.Find("DecorCornerLeft");
            if (left != null)
            {
                left.localScale = new Vector3(-1f, 1f, 1f);
                var imgL = left.GetComponent<Image>();
                if (imgL != null)
                    imgL.preserveAspect = true;
            }

            var right = header.Find("DecorCornerRight");
            if (right != null)
            {
                right.localScale = Vector3.one;
                var imgR = right.GetComponent<Image>();
                if (imgR != null)
                    imgR.preserveAspect = true;
            }
        }

        static void WireBackButton()
        {
            var back = GameObject.Find("BackButton");
            if (back == null)
                return;
            var img = back.GetComponent<Image>();
            if (img == null)
                return;
            var sp = LoadFirstSprite(BtnSecondary);
            if (sp != null)
            {
                img.sprite = sp;
                img.color = Color.white;
            }
        }

        static void EnsureCardContentOrder(Transform content)
        {
            var title = content.Find("TitleText");
            var body = content.Find("BodyText");
            var thumb = content.Find("Thumbnail");
            var locked = content.Find("LockedHint");
            if (title == null || body == null)
                return;

            var i = 0;
            title.SetSiblingIndex(i++);
            body.SetSiblingIndex(i++);
            if (thumb != null)
                thumb.SetSiblingIndex(i++);
            if (locked != null)
                locked.SetSiblingIndex(i++);
        }

        /// <summary>
        /// Oscurece la parte inferior del arte generado para que no compita con el texto en español.
        /// </summary>
        static void EnsureThumbnailBottomFade(Transform thumbnailRoot)
        {
            if (thumbnailRoot == null || thumbnailRoot.Find("ThumbnailBottomFade") != null)
                return;

            var sp = LoadFirstSprite(WhiteSpritePath);
            if (sp == null)
                return;

            var go = new GameObject("ThumbnailBottomFade", typeof(RectTransform));
            go.transform.SetParent(thumbnailRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0.48f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f);

            var img = go.AddComponent<Image>();
            img.sprite = sp;
            img.color = new Color(0.02f, 0.01f, 0.07f, 0.88f);
            img.raycastTarget = false;
            img.type = Image.Type.Simple;
        }

        static void EnsureCardThumbnailsAndSeals()
        {
            for (var i = 1; i <= 3; i++)
            {
                var card = GameObject.Find("ZoneCard_" + i);
                if (card == null)
                    continue;

                var content = card.transform.Find("Content");
                if (content == null)
                    continue;

                EnsureCardContentOrder(content);

                if (content.Find("Thumbnail") == null)
                {
                    var thumbGo = new GameObject("Thumbnail", typeof(RectTransform));
                    thumbGo.transform.SetParent(content, false);
                    var body = content.Find("BodyText");
                    if (body != null)
                        thumbGo.transform.SetSiblingIndex(body.GetSiblingIndex() + 1);
                    else
                        thumbGo.transform.SetAsFirstSibling();

                    var trt = thumbGo.GetComponent<RectTransform>();
                    trt.anchorMin = new Vector2(0, 1);
                    trt.anchorMax = new Vector2(1, 1);
                    trt.pivot = new Vector2(0.5f, 1);
                    trt.sizeDelta = new Vector2(0, 0);
                    trt.offsetMin = Vector2.zero;
                    trt.offsetMax = Vector2.zero;
                    var le = thumbGo.AddComponent<LayoutElement>();
                    le.minHeight = 118;
                    le.preferredHeight = 122;
                    le.flexibleHeight = 0;
                    var tim = thumbGo.AddComponent<Image>();
                    tim.color = Color.white;
                    tim.raycastTarget = false;
                    tim.preserveAspect = true;
                }
                else
                {
                    var trt = content.Find("Thumbnail").GetComponent<RectTransform>();
                    trt.anchorMin = new Vector2(0, 1);
                    trt.anchorMax = new Vector2(1, 1);
                    trt.pivot = new Vector2(0.5f, 1);
                    trt.sizeDelta = new Vector2(0, 0);
                    trt.offsetMin = Vector2.zero;
                    trt.offsetMax = Vector2.zero;
                    var le = content.Find("Thumbnail").GetComponent<LayoutElement>();
                    if (le != null)
                    {
                        le.minHeight = 118;
                        le.preferredHeight = 122;
                    }
                }

                var thumbTf = content.Find("Thumbnail");
                EnsureThumbnailBottomFade(thumbTf);

                var lockOverlay = card.transform.Find("LockOverlay");
                if (lockOverlay != null && lockOverlay.Find("LockSeal") == null)
                {
                    var sealGo = new GameObject("LockSeal", typeof(RectTransform));
                    sealGo.transform.SetParent(lockOverlay, false);
                    var srt = sealGo.GetComponent<RectTransform>();
                    srt.anchorMin = new Vector2(0.5f, 0.5f);
                    srt.anchorMax = new Vector2(0.5f, 0.5f);
                    srt.sizeDelta = new Vector2(112, 112);
                    srt.anchoredPosition = Vector2.zero;
                    var sim = sealGo.AddComponent<Image>();
                    var sealSp = LoadFirstSprite(BadgeLocked);
                    if (sealSp != null)
                        sim.sprite = sealSp;
                    sim.color = Color.white;
                    sim.preserveAspect = true;
                    sim.raycastTarget = false;
                }
            }
        }

        static void WireZoneCards()
        {
            var thumbs = new[] { Thumb1, Thumb2, Thumb3 };
            for (var i = 1; i <= 3; i++)
            {
                var card = GameObject.Find("ZoneCard_" + i);
                if (card == null)
                    continue;
                var zc = card.GetComponent<ZoneCardUI>();
                if (zc == null)
                    continue;

                var so = new SerializedObject(zc);
                so.FindProperty("unlockedCardSprite").objectReferenceValue = LoadFirstSprite(CardNormal);
                so.FindProperty("lockedCardSprite").objectReferenceValue = LoadFirstSprite(CardLocked);
                so.FindProperty("zoneThumbnailSprite").objectReferenceValue = LoadFirstSprite(thumbs[i - 1]);

                var thumbImg = card.transform.Find("Content/Thumbnail")?.GetComponent<Image>();
                if (thumbImg != null)
                    so.FindProperty("thumbnailImage").objectReferenceValue = thumbImg;

                var sealImg = card.transform.Find("LockOverlay/LockSeal")?.GetComponent<Image>();
                if (sealImg != null)
                {
                    so.FindProperty("lockSealImage").objectReferenceValue = sealImg;
                    var sealSp = LoadFirstSprite(BadgeLocked);
                    if (sealSp != null)
                        sealImg.sprite = sealSp;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                zc.ApplyState();
            }
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }
    }
}
