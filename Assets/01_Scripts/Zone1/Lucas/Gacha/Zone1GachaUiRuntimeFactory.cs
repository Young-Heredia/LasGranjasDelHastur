using LasGranjasDelHastur.Zone1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Zone1.Gacha
{
    /// <summary>
    /// Construye la jerarquía UI del gacha (misma estructura que el prefab generado por el menú de editor).
    /// </summary>
    public static class Zone1GachaUiRuntimeFactory
    {
        public static GameObject CreateOverlayRoot(int fivePullCount, out Zone1GachaPanelView view)
        {
            var go = new GameObject("Zone1GachaOverlay");
            go.transform.SetParent(null, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 240;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            var dim = new GameObject("Dim");
            dim.transform.SetParent(go.transform, false);
            var dimRt = dim.AddComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0.02f, 0.02f, 0.05f, 0.72f);
            dimImg.raycastTarget = true;

            var panel = new GameObject("Panel");
            panel.transform.SetParent(go.transform, false);
            var panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(1080f, 860f);
            panelRt.anchoredPosition = Vector2.zero;

            var bg = panel.AddComponent<Image>();
            var bgSp = Zone1ArtProvider.LoadSprite(Zone1GachaArtPaths.PanelBg);
            if (bgSp != null)
            {
                bg.sprite = bgSp;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
                bg.color = new Color(0.08f, 0.08f, 0.1f, 0.96f);

            var frameGo = new GameObject("Frame");
            frameGo.transform.SetParent(panel.transform, false);
            var frameRt = frameGo.AddComponent<RectTransform>();
            frameRt.anchorMin = Vector2.zero;
            frameRt.anchorMax = Vector2.one;
            frameRt.offsetMin = new Vector2(-18f, -18f);
            frameRt.offsetMax = new Vector2(18f, 18f);
            var frameImg = frameGo.AddComponent<Image>();
            var frSp = Zone1ArtProvider.LoadSprite(Zone1GachaArtPaths.PanelFrame);
            if (frSp != null)
            {
                frameImg.sprite = frSp;
                frameImg.type = Image.Type.Sliced;
                frameImg.color = Color.white;
            }
            else
                frameImg.color = new Color(0.5f, 0.45f, 0.3f, 0.35f);
            frameImg.raycastTarget = false;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 22, 22);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var txtTitle = CreateTmp(panel.transform, "Gacha del abismo", 26, TextAlignmentOptions.Center);
            var titleLe = txtTitle.gameObject.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 40f;

            var txtHint = CreateTmp(panel.transform, "", 16, TextAlignmentOptions.Center);
            var hintLe = txtHint.gameObject.AddComponent<LayoutElement>();
            hintLe.preferredHeight = 28f;

            var mid = new GameObject("MidRow");
            mid.transform.SetParent(panel.transform, false);
            var midH = mid.AddComponent<HorizontalLayoutGroup>();
            midH.spacing = 18f;
            midH.childAlignment = TextAnchor.MiddleCenter;
            midH.childControlHeight = true;
            midH.childControlWidth = false;
            midH.childForceExpandHeight = true;
            midH.childForceExpandWidth = false;
            var midLe = mid.AddComponent<LayoutElement>();
            midLe.preferredHeight = 320f;
            midLe.flexibleHeight = 0f;

            var imgMachine = CreateImage(mid.transform, 280f, 280f);
            var imgCapsule = CreateImage(mid.transform, 160f, 200f);
            var vfxWrap = new GameObject("VfxWrap");
            vfxWrap.transform.SetParent(mid.transform, false);
            var vfxLe = vfxWrap.AddComponent<LayoutElement>();
            vfxLe.preferredWidth = 200f;
            vfxLe.preferredHeight = 200f;
            var imgVfx = CreateImage(vfxWrap.transform, 180f, 180f);
            imgVfx.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            imgVfx.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            imgVfx.rectTransform.anchoredPosition = Vector2.zero;

            var resultRow = new GameObject("ResultRow");
            resultRow.transform.SetParent(panel.transform, false);
            var rrH = resultRow.AddComponent<HorizontalLayoutGroup>();
            rrH.spacing = 14f;
            rrH.childAlignment = TextAnchor.MiddleCenter;
            rrH.childControlHeight = true;
            rrH.childControlWidth = true;
            rrH.childForceExpandWidth = true;
            rrH.childForceExpandHeight = false;
            var rrLe = resultRow.AddComponent<LayoutElement>();
            rrLe.preferredHeight = 200f;

            var resPanelGo = new GameObject("ResultPanel");
            resPanelGo.transform.SetParent(resultRow.transform, false);
            resPanelGo.AddComponent<RectTransform>();
            var resPanelImg = resPanelGo.AddComponent<Image>();
            var rpSp = Zone1ArtProvider.LoadSprite(Zone1GachaArtPaths.ResultPanel);
            if (rpSp != null)
            {
                resPanelImg.sprite = rpSp;
                resPanelImg.type = Image.Type.Sliced;
                resPanelImg.color = Color.white;
            }
            else
                resPanelImg.color = new Color(0.1f, 0.1f, 0.12f, 0.9f);
            var resPanelLe = resPanelGo.AddComponent<LayoutElement>();
            resPanelLe.flexibleWidth = 1f;
            resPanelLe.preferredHeight = 190f;
            var resPanelV = resPanelGo.AddComponent<VerticalLayoutGroup>();
            resPanelV.padding = new RectOffset(14, 14, 10, 10);
            resPanelV.spacing = 8f;
            resPanelV.childAlignment = TextAnchor.UpperLeft;
            resPanelV.childControlWidth = true;
            resPanelV.childControlHeight = true;
            resPanelV.childForceExpandWidth = true;
            resPanelV.childForceExpandHeight = false;

            var txtResult = CreateTmp(resPanelGo.transform, "Resultado", 18, TextAlignmentOptions.Left);
            var txtResultLe = txtResult.gameObject.AddComponent<LayoutElement>();
            txtResultLe.preferredHeight = 28f;
            txtResultLe.flexibleWidth = 1f;

            var imgResultIcon = CreateImage(resPanelGo.transform, 128f, 128f);
            var imgResultLe = imgResultIcon.GetComponent<LayoutElement>();
            if (imgResultLe != null)
            {
                imgResultLe.minWidth = 108f;
                imgResultLe.minHeight = 108f;
                imgResultLe.preferredWidth = 128f;
                imgResultLe.preferredHeight = 128f;
                imgResultLe.flexibleHeight = 0f;
            }

            imgResultIcon.gameObject.SetActive(false);

            var txtSummary = CreateTmp(resPanelGo.transform, "", 15, TextAlignmentOptions.Left);
            txtSummary.textWrappingMode = TextWrappingModes.Normal;
            var txtSummaryLe = txtSummary.gameObject.AddComponent<LayoutElement>();
            txtSummaryLe.flexibleWidth = 1f;
            txtSummaryLe.flexibleHeight = 1f;

            var btnRow = new GameObject("Buttons");
            btnRow.transform.SetParent(panel.transform, false);
            var btnH = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnH.spacing = 16f;
            btnH.childAlignment = TextAnchor.MiddleCenter;
            btnH.childControlHeight = true;
            btnH.childControlWidth = false;
            btnH.childForceExpandHeight = false;
            btnH.childForceExpandWidth = false;
            var btnRowLe = btnRow.AddComponent<LayoutElement>();
            btnRowLe.preferredHeight = 64f;

            var btnPull1 = CreateGachaButton(btnRow.transform, "Tirar x1", Zone1GachaArtPaths.ButtonPull, 220f, 56f, usePullStyle: true);
            var btnPull5 = CreateGachaButton(btnRow.transform, $"Tirar x{fivePullCount}", Zone1GachaArtPaths.ButtonPull, 260f, 56f, usePullStyle: true);

            var btnClose = CreateGachaButton(panel.transform, "", Zone1GachaArtPaths.ButtonClose, 52f, 52f, usePullStyle: false);
            var closeRt = btnClose.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-18f, -14f);
            closeRt.sizeDelta = new Vector2(52f, 52f);
            var closeLe = btnClose.gameObject.GetComponent<LayoutElement>();
            if (closeLe != null)
                closeLe.ignoreLayout = true;
            btnClose.transform.SetAsLastSibling();

            view = go.AddComponent<Zone1GachaPanelView>();
            view.rootCanvas = canvas;
            view.txtTitle = txtTitle;
            view.txtHint = txtHint;
            view.txtResult = txtResult;
            view.txtSummary = txtSummary;
            view.imgMachine = imgMachine;
            view.imgCapsule = imgCapsule;
            view.imgVfx = imgVfx;
            view.imgResultIcon = imgResultIcon;
            view.btnClose = btnClose;
            view.btnPull1 = btnPull1;
            view.btnPull5 = btnPull5;

            go.SetActive(false);
            return go;
        }

        static TextMeshProUGUI CreateTmp(Transform parent, string text, int size, TextAlignmentOptions align)
        {
            var go = new GameObject("TMP");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = Safe(text);
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.color = new Color(0.9f, 0.86f, 0.78f, 1f);
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            var rt = tmp.rectTransform;
            rt.sizeDelta = new Vector2(0f, 0f);
            return tmp;
        }

        static string Safe(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return UIManager.SafeGlyphs(s);
        }

        static Image CreateImage(Transform parent, float w, float h)
        {
            var go = new GameObject("Image");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            img.preserveAspect = true;
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = w;
            le.preferredHeight = h;
            return img;
        }

        static Button CreateGachaButton(Transform parent, string label, string iconPath, float w, float h, bool usePullStyle)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            if (usePullStyle)
            {
                var baseSp = Zone1ArtProvider.LoadSprite("Assets/02_Sprites/Lucas/Zone1/UI/zone1_ui_button_base.png");
                if (baseSp != null)
                {
                    img.sprite = baseSp;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                }
                else
                    img.color = new Color(0.15f, 0.15f, 0.18f, 1f);
            }
            else
            {
                var sp = Zone1ArtProvider.LoadSprite(iconPath);
                if (sp != null)
                {
                    img.sprite = sp;
                    img.type = Image.Type.Simple;
                    img.color = Color.white;
                }
                else
                    img.color = new Color(0.15f, 0.15f, 0.18f, 1f);
            }

            var btn = go.AddComponent<Button>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = w;
            le.preferredHeight = h;
            if (!string.IsNullOrEmpty(label))
            {
                var txtGo = new GameObject("Label");
                txtGo.transform.SetParent(go.transform, false);
                var txtRt = txtGo.AddComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = new Vector2(6, 4);
                txtRt.offsetMax = new Vector2(-6, -4);
                var tmp = txtGo.AddComponent<TextMeshProUGUI>();
                tmp.text = Safe(label);
                tmp.fontSize = 17;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(0.95f, 0.9f, 0.82f, 1f);
                tmp.raycastTarget = false;
            }

            return btn;
        }
    }
}
