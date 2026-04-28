using LasGranjasDelHastur.Zone1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Core
{
    /// <summary>
    /// HUD y marcos de panel compartidos para prototipos Z2/Z3: reutiliza sprites de Z1 solo donde
    /// el pack pixel no cubre (barra HUD, marcos de panel, botón base) sin sustituir el tema de fondo.
    /// </summary>
    public static class ZonePrototypeUiChrome
    {
        public const string Zone1HudBarPath = "Assets/02_Sprites/Lucas/Zone1/UI/zone1_ui_hud_bar.png";
        public const string Zone1PanelCellPath = "Assets/02_Sprites/Lucas/Zone1/UI/zone1_ui_panel_cell.png";
        public const string Zone1PanelSalesPath = "Assets/02_Sprites/Lucas/Zone1/UI/zone1_ui_panel_sales.png";
        public const string Zone1ButtonBasePath = "Assets/02_Sprites/Lucas/Zone1/UI/zone1_ui_button_base.png";

        public static void ApplyPanelOutline(Image img)
        {
            if (img == null)
                return;
            var outline = img.GetComponent<Outline>() ?? img.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.8f, 0.75f, 0.45f, 0.35f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        public static void ApplyHybridHudBackground(Image hudImage, string packPanelPath, Color packFallbackTint)
        {
            if (hudImage == null)
                return;

            var z1 = Zone1ArtProvider.LoadSprite(Zone1HudBarPath);
            if (z1 != null)
            {
                hudImage.sprite = z1;
                hudImage.type = Image.Type.Sliced;
                hudImage.color = Color.white;
                return;
            }

            TryApplyPackSprite(hudImage, packPanelPath, packFallbackTint);
        }

        public static void ApplyHybridSidePanel(Image img, string packPanelPath, string zone1PanelPath, Color packFallbackTint)
        {
            if (img == null)
                return;

            var z1 = Zone1ArtProvider.LoadSprite(zone1PanelPath);
            if (z1 != null)
            {
                img.sprite = z1;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                ApplyPanelOutline(img);
                return;
            }

            TryApplyPackSprite(img, packPanelPath, packFallbackTint);
            ApplyPanelOutline(img);
        }

        static void TryApplyPackSprite(Image img, string packPanelPath, Color fallbackTint)
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(packPanelPath))
            {
                var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(packPanelPath);
                if (sp != null)
                {
                    img.sprite = sp;
                    img.type = Image.Type.Simple;
                    img.color = Color.white;
                    img.preserveAspect = false;
                    return;
                }
            }
#endif
            img.sprite = null;
            img.color = fallbackTint;
        }

        /// <summary>Limpia hijos del HUD y monta título + botones en fila 1 y recursos en fila 2 dentro del panel.</summary>
        public static void MountTwoRowHud(
            Transform hudTransform,
            Image hudImage,
            string zoneTitle,
            string packHudPath,
            Color hudFallbackTint,
            float hudHeight,
            out TextMeshProUGUI header,
            out TextMeshProUGUI resources,
            out TextMeshProUGUI tax,
            out TextMeshProUGUI difficulty,
            out Button btnSales,
            out Button btnBack)
        {
            header = null;
            resources = null;
            tax = null;
            difficulty = null;
            btnSales = null;
            btnBack = null;

            if (hudTransform == null)
                return;

            ClearChildren(hudTransform);
            ApplyHybridHudBackground(hudImage, packHudPath, hudFallbackTint);

            var hudRt = hudTransform.GetComponent<RectTransform>();
            if (hudRt != null)
                hudRt.sizeDelta = new Vector2(hudRt.sizeDelta.x, hudHeight);

            var mountGo = new GameObject("HUDMount");
            mountGo.transform.SetParent(hudTransform, false);
            var mountRt = mountGo.AddComponent<RectTransform>();
            mountRt.anchorMin = Vector2.zero;
            mountRt.anchorMax = Vector2.one;
            mountRt.offsetMin = new Vector2(12f, 8f);
            mountRt.offsetMax = new Vector2(-12f, -10f);
            var mountV = mountGo.AddComponent<VerticalLayoutGroup>();
            mountV.spacing = 6f;
            mountV.padding = new RectOffset(4, 4, 2, 4);
            mountV.childAlignment = TextAnchor.UpperCenter;
            mountV.childControlHeight = true;
            mountV.childControlWidth = true;
            mountV.childForceExpandHeight = false;
            mountV.childForceExpandWidth = true;

            // --- Fila título + botones
            var titleRow = new GameObject("TitleRow");
            titleRow.transform.SetParent(mountGo.transform, false);
            var titleLe = titleRow.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 54f;
            titleLe.minHeight = 48f;
            titleLe.flexibleWidth = 1f;
            var titleHg = titleRow.AddComponent<HorizontalLayoutGroup>();
            titleHg.padding = new RectOffset(6, 6, 4, 4);
            titleHg.spacing = 12f;
            titleHg.childAlignment = TextAnchor.MiddleCenter;
            titleHg.childControlHeight = true;
            titleHg.childControlWidth = true;
            titleHg.childForceExpandHeight = true;
            titleHg.childForceExpandWidth = false;

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(titleRow.transform, false);
            var spLe = spacer.AddComponent<LayoutElement>();
            spLe.flexibleWidth = 0.15f;
            spLe.minWidth = 8f;

            header = CreateTmp(titleRow.transform, "Header", zoneTitle, 24, TextAlignmentOptions.Center);
            var hLe = header.gameObject.AddComponent<LayoutElement>();
            hLe.flexibleWidth = 1f;
            hLe.minWidth = 420f;
            hLe.preferredHeight = 44f;

            var btnCol = new GameObject("NavButtons");
            btnCol.transform.SetParent(titleRow.transform, false);
            var btnColLe = btnCol.AddComponent<LayoutElement>();
            btnColLe.preferredWidth = 236f;
            btnColLe.minWidth = 220f;
            var btnV = btnCol.AddComponent<VerticalLayoutGroup>();
            btnV.spacing = 6f;
            btnV.childAlignment = TextAnchor.MiddleRight;
            btnV.childControlHeight = true;
            btnV.childControlWidth = true;
            btnV.childForceExpandWidth = true;
            btnV.childForceExpandHeight = false;

            btnSales = CreateStackedButton(btnCol.transform, "Ventas", 40f);
            btnBack = CreateStackedButton(btnCol.transform, "Volver a Zonas", 40f);

            // --- Fila stats (fondo oscuro dentro del HUD)
            var statsRow = new GameObject("StatsRow");
            statsRow.transform.SetParent(mountGo.transform, false);
            var statsLe = statsRow.AddComponent<LayoutElement>();
            statsLe.preferredHeight = 48f;
            statsLe.minHeight = 44f;
            statsLe.flexibleWidth = 1f;
            var statsBg = statsRow.AddComponent<Image>();
            statsBg.color = new Color(0.02f, 0.02f, 0.04f, 0.78f);
            statsBg.raycastTarget = false;
            var statsHg = statsRow.AddComponent<HorizontalLayoutGroup>();
            statsHg.padding = new RectOffset(14, 14, 8, 8);
            statsHg.spacing = 10f;
            statsHg.childAlignment = TextAnchor.MiddleLeft;
            statsHg.childControlHeight = true;
            statsHg.childControlWidth = true;
            statsHg.childForceExpandHeight = false;
            statsHg.childForceExpandWidth = true;

            resources = CreateTmp(statsRow.transform, "Resources", "", 17, TextAlignmentOptions.Left);
            AddFlexible(resources.gameObject, 1.1f);
            tax = CreateTmp(statsRow.transform, "Tax", "", 17, TextAlignmentOptions.Center);
            AddFlexible(tax.gameObject, 1f);
            difficulty = CreateTmp(statsRow.transform, "Difficulty", "", 16, TextAlignmentOptions.Right);
            AddFlexible(difficulty.gameObject, 1.1f);
        }

        public static RectTransform EnsurePanelBody(Transform panelTransform)
        {
            var existing = panelTransform.Find("PanelBody");
            if (existing != null)
            {
                ClearChildren(existing);
                return existing.GetComponent<RectTransform>();
            }

            var go = new GameObject("PanelBody");
            go.transform.SetParent(panelTransform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(20f, 18f);
            rt.offsetMax = new Vector2(-20f, -18f);
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = 8f;
            v.padding = new RectOffset(4, 4, 2, 2);
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return rt;
        }

        public static TextMeshProUGUI AddBodyLabel(Transform body, string name, string text, int fontSize, float preferredHeight, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(body, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.minHeight = preferredHeight - 2f;
            le.flexibleWidth = 1f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = fontSize >= 18
                ? new Color(0.93f, 0.88f, 0.72f, 1f)
                : new Color(0.88f, 0.90f, 0.95f, 1f);
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button AddBodyButton(Transform body, string label, float height)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(body, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height - 2f;
            le.flexibleWidth = 1f;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, height);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.16f, 0.22f, 1f);
            var btn = go.AddComponent<Button>();

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(8f, 4f);
            txtRt.offsetMax = new Vector2(-8f, -4f);
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.95f, 0.94f, 0.90f, 1f);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;

            return btn;
        }

        public static void ApplyHybridPackOrZone1Button(Button btn, bool danger, string packPrimaryPath, string packDangerPath)
        {
            if (btn == null)
                return;
            var img = btn.GetComponent<Image>();
            if (img == null)
                return;

            Sprite sp = null;
#if UNITY_EDITOR
            var path = danger ? packDangerPath : packPrimaryPath;
            if (!string.IsNullOrEmpty(path))
                sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
#endif
            if (sp == null)
                sp = Zone1ArtProvider.LoadSprite(Zone1ButtonBasePath);

            if (sp == null)
                return;

            img.sprite = sp;
            img.type = Image.Type.Sliced;
            img.color = danger ? new Color(0.92f, 0.62f, 0.58f, 1f) : Color.white;
        }

        static TextMeshProUGUI CreateTmp(Transform parent, string name, string text, int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = new Color(0.94f, 0.89f, 0.74f, 1f);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return tmp;
        }

        static Button CreateStackedButton(Transform parent, string label, float height)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height - 2f;
            le.flexibleWidth = 1f;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.17f, 0.22f, 1f);
            var btn = go.AddComponent<Button>();

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(6f, 2f);
            txtRt.offsetMax = new Vector2(-6f, -2f);
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 17;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.96f, 0.95f, 0.90f, 1f);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;

            return btn;
        }

        static void AddFlexible(GameObject go, float weight)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleWidth = weight;
            le.minWidth = 120f;
        }

        static void ClearChildren(Transform t)
        {
            for (var i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Object.Destroy(c);
                else
                    Object.DestroyImmediate(c);
            }
        }
    }
}
