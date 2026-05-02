using System;
using LasGranjasDelHastur.Core;
using LasGranjasDelHastur.Zone1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LasGranjasDelHastur.UI.Edwin
{
    /// <summary>Ventana modal de contrato de ventas (comprador, recurso, lote, precio, XP).</summary>
    public sealed class EdwinSalesOfferPanel : MonoBehaviour
    {
        const string HeaderStripPath = "Assets/Resources/Edwin/Sales/edwin-sales-offer-header.png";

        BuyerManager _buyers;
        ResourceManager _resources;

        GameObject _modalRoot;
        Image _portrait;
        TextMeshProUGUI _txtBuyer;
        TextMeshProUGUI _txtResource;
        TextMeshProUGUI _txtQty;
        TextMeshProUGUI _txtPrice;
        TextMeshProUGUI _txtXp;
        TextMeshProUGUI _txtStockHint;
        Button _btnSell;
        Button _btnReject;
        TextMeshProUGUI _btnSellLabel;

        BuyerDefinition _current;
        Action _onAfterClose;

        public void Configure(BuyerManager buyers, ResourceManager resources)
        {
            _buyers = buyers;
            _resources = resources;
        }

        public static EdwinSalesOfferPanel Ensure(Transform uiRoot)
        {
            var go = uiRoot.gameObject;
            var p = go.GetComponent<EdwinSalesOfferPanel>();
            if (p == null)
                p = go.AddComponent<EdwinSalesOfferPanel>();
            var rt = uiRoot as RectTransform ?? uiRoot.GetComponent<RectTransform>();
            if (rt != null)
                p.EnsureHierarchy(rt);
            return p;
        }

        public void Show(BuyerDefinition buyer, Action onClosed = null)
        {
            if (buyer == null || _buyers == null || _resources == null || _modalRoot == null)
                return;

            _current = buyer;
            _onAfterClose = onClosed;
            RefreshLabels();
            _modalRoot.transform.SetAsLastSibling();
            _modalRoot.SetActive(true);
        }

        public void Hide()
        {
            if (_modalRoot != null)
                _modalRoot.SetActive(false);
            _current = null;
        }

        void EnsureHierarchy(RectTransform uiRoot)
        {
            if (_modalRoot != null)
                return;

            _modalRoot = new GameObject("EdwinSalesOfferModal");
            _modalRoot.transform.SetParent(uiRoot, false);
            var rootRt = _modalRoot.AddComponent<RectTransform>();
            StretchFull(rootRt);

            var dim = new GameObject("Dim");
            dim.transform.SetParent(_modalRoot.transform, false);
            var dimRt = dim.AddComponent<RectTransform>();
            StretchFull(dimRt);
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0.02f, 0.03f, 0.06f, 0.72f);
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(OnRejectClicked);

            var panelGo = new GameObject("OfferPanel");
            panelGo.transform.SetParent(_modalRoot.transform, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(540f, 468f);

            var panelBg = panelGo.AddComponent<Image>();
            panelBg.color = new Color(0.07f, 0.08f, 0.10f, 0.98f);
            ZonePrototypeUiChrome.ApplyHybridSidePanel(
                panelBg,
                null,
                ZonePrototypeUiChrome.Zone1PanelSalesPath,
                new Color(0.10f, 0.11f, 0.12f, 0.95f));

            var strip = new GameObject("HeaderStrip");
            strip.transform.SetParent(panelGo.transform, false);
            var stripRt = strip.AddComponent<RectTransform>();
            stripRt.anchorMin = new Vector2(0f, 1f);
            stripRt.anchorMax = new Vector2(1f, 1f);
            stripRt.pivot = new Vector2(0.5f, 1f);
            stripRt.sizeDelta = new Vector2(-24f, 36f);
            stripRt.anchoredPosition = new Vector2(0f, -12f);
            var stripImg = strip.AddComponent<Image>();
            stripImg.raycastTarget = false;
            var stripSprite = Zone1ArtProvider.LoadSprite(HeaderStripPath);
            if (stripSprite != null)
            {
                stripImg.sprite = stripSprite;
                stripImg.type = Image.Type.Simple;
                stripImg.color = Color.white;
            }
            else
                stripImg.color = new Color(0.55f, 0.48f, 0.18f, 0.55f);

            var body = new GameObject("Body");
            body.transform.SetParent(panelGo.transform, false);
            var bodyRt = body.AddComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(18f, 56f);
            bodyRt.offsetMax = new Vector2(-18f, -52f);

            var v = body.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10f;
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            v.padding = new RectOffset(4, 4, 8, 8);

            var title = CreateTmp(body.transform, "Contrato de venta", 22, TextAlignmentOptions.Center);
            title.color = new Color(0.95f, 0.88f, 0.55f, 1f);

            var headerRow = new GameObject("BuyerHeader");
            headerRow.transform.SetParent(body.transform, false);
            var h = headerRow.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlHeight = true;
            h.childControlWidth = false;
            h.childForceExpandHeight = false;
            h.childForceExpandWidth = false;
            headerRow.AddComponent<LayoutElement>().preferredHeight = 52f;

            var portraitGo = new GameObject("Portrait");
            portraitGo.transform.SetParent(headerRow.transform, false);
            _portrait = portraitGo.AddComponent<Image>();
            _portrait.color = Color.white;
            var pLe = portraitGo.AddComponent<LayoutElement>();
            pLe.preferredWidth = 48f;
            pLe.preferredHeight = 48f;

            _txtBuyer = CreateTmp(headerRow.transform, "-", 18, TextAlignmentOptions.Left);
            _txtBuyer.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            _txtResource = CreateTmp(body.transform, "-", 17, TextAlignmentOptions.Left);
            _txtQty = CreateTmp(body.transform, "-", 17, TextAlignmentOptions.Left);
            _txtPrice = CreateTmp(body.transform, "-", 17, TextAlignmentOptions.Left);
            _txtXp = CreateTmp(body.transform, "-", 17, TextAlignmentOptions.Left);
            _txtStockHint = CreateTmp(body.transform, "", 14, TextAlignmentOptions.Left);
            _txtStockHint.color = new Color(0.72f, 0.76f, 0.82f, 0.92f);
            _txtStockHint.textWrappingMode = TextWrappingModes.Normal;

            var btnRow = new GameObject("Actions");
            btnRow.transform.SetParent(panelGo.transform, false);
            var btnRowRt = btnRow.AddComponent<RectTransform>();
            btnRowRt.anchorMin = new Vector2(0f, 0f);
            btnRowRt.anchorMax = new Vector2(1f, 0f);
            btnRowRt.pivot = new Vector2(0.5f, 0f);
            btnRowRt.sizeDelta = new Vector2(-36f, 44f);
            btnRowRt.anchoredPosition = new Vector2(0f, 14f);
            var btnH = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnH.spacing = 14f;
            btnH.childAlignment = TextAnchor.MiddleCenter;
            btnH.childControlHeight = true;
            btnH.childControlWidth = true;
            btnH.childForceExpandHeight = false;
            btnH.childForceExpandWidth = true;

            _btnReject = CreateModalButton(btnRow.transform, "Rechazar", 200f, preferDanger: true);
            _btnReject.onClick.AddListener(OnRejectClicked);

            _btnSell = CreateModalButton(btnRow.transform, "Vender", 200f, preferDanger: false);
            _btnSell.onClick.AddListener(OnSellClicked);
            _btnSellLabel = _btnSell.GetComponentInChildren<TextMeshProUGUI>();

            _modalRoot.SetActive(false);
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void RefreshLabels()
        {
            if (_current == null)
                return;

            var batch = _current.ContractBatchSize;
            var unitPrice = _buyers.GetCurrentPrice(_current);
            var totalCoins = batch * unitPrice;
            var xp = BuyerManager.GetXpRewardForUnits(batch);
            var stock = _resources.Get(_current.buysResource);

            ApplyPortrait(_current);

            _txtBuyer.text = $"Comprador activo: <b>{_current.buyerName}</b>";
            _txtResource.text = $"Recurso solicitado: <b>{ResourceLabel(_current.buysResource)}</b>";
            _txtQty.text = $"Cantidad requerida (lote): <b>{batch}</b> u.";
            _txtPrice.text =
                $"Precio ofrecido: <b>{totalCoins}</b> monedas oscuras " +
                $"(<color=#c9c9c9>{unitPrice}/u</color>)";
            _txtXp.text = $"Experiencia por cerrar el contrato: <b>{xp}</b> XP";

            var ok = stock >= batch;
            _txtStockHint.text = ok
                ? $"En almacén: {stock} u. · Cumples el pedido."
                : $"En almacén: {stock} u. · <color=#ffaaaa>Falta recurso para este lote ({batch} u.).</color>";

            _btnSell.interactable = ok;
            if (_btnSellLabel != null)
            {
                var c = _btnSellLabel.color;
                c.a = ok ? 1f : 0.45f;
                _btnSellLabel.color = c;
            }
        }

        void ApplyPortrait(BuyerDefinition buyer)
        {
            if (_portrait == null)
                return;
            var portraitPath = buyer.buyerName switch
            {
                "Los Profundos" => "Assets/02_Sprites/Lucas/Zone1/Portraits/zone1_buyer_deepone_portrait.png",
                "Yekuvian" => "Assets/02_Sprites/Lucas/Zone1/Portraits/zone1_buyer_yekuvian_portrait.png",
                "Ángeles Caídos" => "Assets/02_Sprites/Lucas/Zone1/Portraits/zone1_buyer_fallenangel_portrait.png",
                _ => null
            };
            _portrait.sprite = !string.IsNullOrEmpty(portraitPath)
                ? Zone1ArtProvider.LoadSprite(portraitPath)
                : null;
            _portrait.color = _portrait.sprite != null ? Color.white : new Color(0.25f, 0.25f, 0.28f, 0.9f);
        }

        void OnSellClicked()
        {
            if (_current == null || _buyers == null)
                return;
            var batch = _current.ContractBatchSize;
            if (!_buyers.TrySell(_current, batch))
                return;

            if (AudioManager.Instance != null && AudioManager.Instance.zone1Sell != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.zone1Sell);

            Hide();
            _onAfterClose?.Invoke();
        }

        void OnRejectClicked()
        {
            Hide();
            _onAfterClose?.Invoke();
        }

        static TextMeshProUGUI CreateTmp(Transform parent, string text, int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.color = new Color(0.93f, 0.93f, 0.93f, 1f);
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        static Button CreateModalButton(Transform parent, string caption, float width, bool preferDanger)
        {
            var go = new GameObject($"Btn_{caption}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.14f, 0.14f, 0.16f, 1f);
            var spritePath = preferDanger
                ? "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/UI/Buttons/UI_Button_Danger.png"
                : "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/UI/Buttons/UI_Button_Primary.png";
            ApplyPackOrBase(img, spritePath);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.78f, 1f);
            btn.colors = colors;

            var txt = CreateTmp(go.transform, caption, 17, TextAlignmentOptions.Center);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = Vector2.zero;
            txt.rectTransform.offsetMax = Vector2.zero;
            if (preferDanger)
                txt.color = new Color(1f, 0.88f, 0.88f, 1f);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 42f;
            le.preferredWidth = width;
            le.flexibleWidth = 1f;

            if (go.GetComponent<BasicUIAudio>() == null)
            {
                var uiAudio = go.AddComponent<BasicUIAudio>();
                if (AudioManager.Instance != null)
                {
                    uiAudio.hoverClip = AudioManager.Instance.uiHover;
                    uiAudio.clickClip = AudioManager.Instance.uiClick;
                    uiAudio.useAudioManagerFirst = true;
                }
            }

            return btn;
        }

        static void ApplyPackOrBase(Image img, string packPath)
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(packPath))
            {
                var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(packPath);
                if (sp != null)
                {
                    img.sprite = sp;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                    return;
                }
            }
#endif
            var z1 = Zone1ArtProvider.LoadSprite(ZonePrototypeUiChrome.Zone1ButtonBasePath);
            if (z1 != null)
            {
                img.sprite = z1;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
        }

        static string ResourceLabel(ResourceType type)
        {
            return type switch
            {
                ResourceType.WeakSouls => "Almas débiles",
                ResourceType.PureEnergy => "Energía pura",
                ResourceType.MemoryShards => "Frag. recuerdo",
                ResourceType.UnstableSouls => "Almas inestables",
                ResourceType.DarkCoins => "Monedas oscuras",
                _ => type.ToString()
            };
        }
    }
}
