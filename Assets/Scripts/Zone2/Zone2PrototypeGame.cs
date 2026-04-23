using LasGranjasDelHastur.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Zone2
{
    [DisallowMultipleComponent]
    public class Zone2PrototypeGame : MonoBehaviour
    {
        [Header("Economy")]
        [SerializeField] private bool sharedEconomyWithZone1 = true;
        [SerializeField, Min(1f)] private float taxIntervalSeconds = 45f;

        int _darkCoins = 260;
        int _citySupplies;
        int _arcaneBlueprints;
        int _difficultyTier = 1;
        int _totalSold;
        float _taxTimer;
        float _runtime;

        TextMeshProUGUI _txtHeader;
        TextMeshProUGUI _txtResources;
        TextMeshProUGUI _txtTax;
        TextMeshProUGUI _txtDifficulty;
        TextMeshProUGUI _txtHint;

        void Awake()
        {
            _taxTimer = taxIntervalSeconds;
            if (sharedEconomyWithZone1)
                PullCoinsFromZone1Save();
            TryRestoreFromSaveIfRequested();
            BuildUi();
            RefreshUi();
        }

        void Update()
        {
            _runtime += Time.deltaTime;
            if (_runtime >= 70f * _difficultyTier && _difficultyTier < 5)
                _difficultyTier++;

            _taxTimer -= Time.deltaTime;
            if (_taxTimer <= 0f)
            {
                ApplyTax();
                _taxTimer = taxIntervalSeconds;
            }

            RefreshUi();
        }

        void BuildUi()
        {
            var root = new GameObject("Zone2PrototypeUI");
            root.transform.SetParent(transform, false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.65f;
            root.AddComponent<GraphicRaycaster>();

            var backdrop = CreateImage(root.transform, "Backdrop", new Color(0.04f, 0.08f, 0.10f, 1f));
            Stretch(backdrop.rectTransform);

            var panel = CreateImage(root.transform, "Panel", new Color(0.08f, 0.12f, 0.15f, 0.94f));
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(Mathf.Min(940f, Screen.width * 0.9f), Mathf.Min(530f, Screen.height * 0.86f));
            prt.anchoredPosition = Vector2.zero;

            _txtHeader = CreateLabel(panel.transform, "Header", "Zone2_Cities - Fase 6 Prototype", 34, new Vector2(0f, 210f), new Vector2(840f, 56f));
            _txtResources = CreateLabel(panel.transform, "Resources", "", 22, new Vector2(0f, 146f), new Vector2(840f, 78f));
            _txtTax = CreateLabel(panel.transform, "Tax", "", 20, new Vector2(0f, 98f), new Vector2(840f, 42f));
            _txtDifficulty = CreateLabel(panel.transform, "Difficulty", "", 20, new Vector2(0f, 58f), new Vector2(840f, 42f));
            _txtHint = CreateLabel(panel.transform, "Hint", "", 17, new Vector2(0f, 20f), new Vector2(840f, 42f));

            var btnProduceA = CreateButton(panel.transform, "Recolectar Suministros +7", new Vector2(-210f, -70f), new Vector2(300f, 50f));
            btnProduceA.onClick.AddListener(() => _citySupplies += 7 + _difficultyTier);

            var btnProduceB = CreateButton(panel.transform, "Trazar Planos +4", new Vector2(210f, -70f), new Vector2(300f, 50f));
            btnProduceB.onClick.AddListener(() => _arcaneBlueprints += 4 + Mathf.Max(0, _difficultyTier - 1));

            var btnSellA = CreateButton(panel.transform, "Vender Suministros", new Vector2(-210f, -138f), new Vector2(300f, 46f));
            btnSellA.onClick.AddListener(SellSupplies);

            var btnSellB = CreateButton(panel.transform, "Vender Planos", new Vector2(210f, -138f), new Vector2(300f, 46f));
            btnSellB.onClick.AddListener(SellBlueprints);

            var btnBack = CreateButton(panel.transform, "Volver a Zonas", new Vector2(0f, -215f), new Vector2(320f, 52f));
            btnBack.onClick.AddListener(() =>
            {
                PushCoinsToZone1Save();
                SaveManager.Instance?.SaveNow();
                SceneManager.LoadScene("ZoneSelection");
            });
        }

        void SellSupplies()
        {
            if (_citySupplies <= 0)
                return;
            var amount = _citySupplies;
            _citySupplies = 0;
            _darkCoins += amount * (5 + _difficultyTier);
            _totalSold += amount;
            PushCoinsToZone1Save();
        }

        void SellBlueprints()
        {
            if (_arcaneBlueprints <= 0)
                return;
            var amount = _arcaneBlueprints;
            _arcaneBlueprints = 0;
            _darkCoins += amount * (8 + _difficultyTier);
            _totalSold += amount;
            PushCoinsToZone1Save();
        }

        void ApplyTax()
        {
            var rate = 0.16f + _difficultyTier * 0.025f;
            var tax = Mathf.CeilToInt(_darkCoins * rate);
            _darkCoins = Mathf.Max(0, _darkCoins - tax);
            _txtHint.text = $"Impuesto urbano aplicado: -{tax} monedas.";
            PushCoinsToZone1Save();
        }

        void RefreshUi()
        {
            _txtResources.text = $"Monedas: {_darkCoins}  |  Suministros: {_citySupplies}  |  Planos: {_arcaneBlueprints}";
            _txtTax.text = $"Próximo impuesto: {Mathf.Max(0f, _taxTimer):0.0}s";
            _txtDifficulty.text = $"Dificultad: Tier {_difficultyTier}  |  Vendido total: {_totalSold}";
            if (string.IsNullOrEmpty(_txtHint.text))
                _txtHint.text = sharedEconomyWithZone1 ? "Economía compartida con Zona 1: activa." : "Economía aislada de Zona 1: activa.";
        }

        void PullCoinsFromZone1Save()
        {
            if (!sharedEconomyWithZone1 || SaveManager.Instance == null || SaveManager.Instance.CachedData == null ||
                SaveManager.Instance.CachedData.zone1 == null || !SaveManager.Instance.CachedData.zone1.valid)
                return;

            _darkCoins = Mathf.Max(0, SaveManager.Instance.CachedData.zone1.darkCoins);
        }

        void PushCoinsToZone1Save()
        {
            if (!sharedEconomyWithZone1 || SaveManager.Instance == null || SaveManager.Instance.CachedData == null ||
                SaveManager.Instance.CachedData.zone1 == null || !SaveManager.Instance.CachedData.zone1.valid)
                return;

            SaveManager.Instance.CachedData.zone1.darkCoins = Mathf.Max(0, _darkCoins);
        }

        void TryRestoreFromSaveIfRequested()
        {
            if (SaveManager.Instance == null || !SaveManager.Instance.ShouldRestoreFromSave)
                return;

            var data = SaveManager.Instance.CachedData;
            if (data != null && data.zone2 != null && data.zone2.valid)
                ApplySaveData(data.zone2);

            SaveManager.Instance.MarkRestoreConsumed();
        }

        public Zone2SaveData CaptureSaveData()
        {
            return new Zone2SaveData
            {
                valid = true,
                darkCoins = _darkCoins,
                citySupplies = _citySupplies,
                arcaneBlueprints = _arcaneBlueprints,
                difficultyTier = _difficultyTier,
                totalSold = _totalSold,
                taxTimer = _taxTimer,
                runtimeSeconds = _runtime,
            };
        }

        public void ApplySaveData(Zone2SaveData data)
        {
            if (data == null || !data.valid)
                return;

            _darkCoins = Mathf.Max(0, data.darkCoins);
            _citySupplies = Mathf.Max(0, data.citySupplies);
            _arcaneBlueprints = Mathf.Max(0, data.arcaneBlueprints);
            _difficultyTier = Mathf.Clamp(data.difficultyTier, 1, 9);
            _totalSold = Mathf.Max(0, data.totalSold);
            _taxTimer = Mathf.Max(0f, data.taxTimer);
            _runtime = Mathf.Max(0f, data.runtimeSeconds);
        }

        static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, int size, Vector2 pos, Vector2 rectSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = rectSize;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        static Button CreateButton(Transform parent, string text, Vector2 pos, Vector2 rectSize)
        {
            var go = new GameObject($"Button_{text}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = rectSize;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.16f, 0.21f, 0.26f, 1f);
            var btn = go.AddComponent<Button>();
            var lbl = CreateLabel(go.transform, "Text", text, 20, Vector2.zero, rectSize);
            lbl.rectTransform.anchorMin = Vector2.zero;
            lbl.rectTransform.anchorMax = Vector2.one;
            lbl.rectTransform.offsetMin = Vector2.zero;
            lbl.rectTransform.offsetMax = Vector2.zero;
            return btn;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
