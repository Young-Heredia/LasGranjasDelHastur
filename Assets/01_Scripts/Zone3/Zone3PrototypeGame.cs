using LasGranjasDelHastur.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Zone3
{
    [DisallowMultipleComponent]
    public class Zone3PrototypeGame : MonoBehaviour
    {
        const string PrestigeKey = "LasGranjas_Zone3_PrestigePoints";

        int _darkCoins = 320;
        int _astralResidue;
        int _voidInk;
        int _difficultyTier = 1;
        int _totalSold;
        int _prestigePoints;

        float _taxTimer = 40f;
        float _taxInterval = 40f;
        float _runtime;
        bool _endNarrativeShown;

        TextMeshProUGUI _txtHeader;
        TextMeshProUGUI _txtResources;
        TextMeshProUGUI _txtTax;
        TextMeshProUGUI _txtDifficulty;
        TextMeshProUGUI _txtHint;
        GameObject _endPanel;

        void Awake()
        {
<<<<<<< HEAD:Assets/01_Scripts/Zone3/Zone3PrototypeGame.cs
=======
            AudioManager.EnsureInstance();
>>>>>>> origin/devlucas:Assets/Scripts/Zone3/Zone3PrototypeGame.cs
            _prestigePoints = PlayerPrefs.GetInt(PrestigeKey, 0);
            TryRestoreFromSaveIfRequested();
            BuildUi();
            RefreshUi();
        }

        void Update()
        {
            _runtime += Time.deltaTime;
            if (_runtime >= 60f * _difficultyTier && _difficultyTier < 5)
                _difficultyTier++;

            _taxTimer -= Time.deltaTime;
            if (_taxTimer <= 0f)
            {
                ApplyTax();
                _taxTimer = _taxInterval;
            }

            RefreshUi();

            if (!_endNarrativeShown && _difficultyTier >= 3 && _darkCoins >= 1200 && _totalSold >= 120)
                ShowEndNarrative();
        }

        void BuildUi()
        {
            var root = new GameObject("Zone3PrototypeUI");
            root.transform.SetParent(transform, false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.65f;
            root.AddComponent<GraphicRaycaster>();

            var backdrop = CreateImage(root.transform, "Backdrop", new Color(0.03f, 0.02f, 0.08f, 1f));
            Stretch(backdrop.rectTransform);

            var panel = CreateImage(root.transform, "Panel", new Color(0.07f, 0.08f, 0.14f, 0.93f));
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(Mathf.Min(960f, Screen.width * 0.9f), Mathf.Min(540f, Screen.height * 0.86f));
            prt.anchoredPosition = Vector2.zero;

            _txtHeader = CreateLabel(panel.transform, "Header", "Zone3_Celestial - Fase 8 Prototype", 34, new Vector2(0, 212), new Vector2(840, 56));
            _txtResources = CreateLabel(panel.transform, "Resources", "", 22, new Vector2(0, 146), new Vector2(840, 78));
            _txtTax = CreateLabel(panel.transform, "Tax", "", 20, new Vector2(0, 98), new Vector2(840, 42));
            _txtDifficulty = CreateLabel(panel.transform, "Difficulty", "", 20, new Vector2(0, 58), new Vector2(840, 42));
            _txtHint = CreateLabel(panel.transform, "Hint", "", 17, new Vector2(0, 20), new Vector2(840, 42));

            var btnExtract = CreateButton(panel.transform, "Extraer Residuo +8", new Vector2(-210, -70), new Vector2(300, 50));
<<<<<<< HEAD:Assets/01_Scripts/Zone3/Zone3PrototypeGame.cs
            btnExtract.onClick.AddListener(() => _astralResidue += 8 + _difficultyTier);

            var btnCondense = CreateButton(panel.transform, "Condensar Tinta +5", new Vector2(210, -70), new Vector2(300, 50));
            btnCondense.onClick.AddListener(() => _voidInk += 5 + Mathf.Max(0, _difficultyTier - 1));
=======
            btnExtract.onClick.AddListener(() =>
            {
                _astralResidue += 8 + _difficultyTier;
                AudioManager.Instance?.PlayZone3ExtractResidue();
            });

            var btnCondense = CreateButton(panel.transform, "Condensar Tinta +5", new Vector2(210, -70), new Vector2(300, 50));
            btnCondense.onClick.AddListener(() =>
            {
                _voidInk += 5 + Mathf.Max(0, _difficultyTier - 1);
                AudioManager.Instance?.PlayZone3CondenseInk();
            });
>>>>>>> origin/devlucas:Assets/Scripts/Zone3/Zone3PrototypeGame.cs

            var btnSellResidue = CreateButton(panel.transform, "Vender Residuo", new Vector2(-210, -138), new Vector2(300, 46));
            btnSellResidue.onClick.AddListener(() => SellResidue());

            var btnSellInk = CreateButton(panel.transform, "Vender Tinta", new Vector2(210, -138), new Vector2(300, 46));
            btnSellInk.onClick.AddListener(() => SellInk());

            var btnBack = CreateButton(panel.transform, "Volver a Zonas", new Vector2(0, -215), new Vector2(320, 52));
            btnBack.onClick.AddListener(() =>
            {
                SaveManager.Instance?.SaveNow();
<<<<<<< HEAD:Assets/01_Scripts/Zone3/Zone3PrototypeGame.cs
=======
                AudioManager.Instance?.PlayZone3BackToZones();
>>>>>>> origin/devlucas:Assets/Scripts/Zone3/Zone3PrototypeGame.cs
                SceneManager.LoadScene("ZoneSelection");
            });

            BuildEndPanel(root.transform);
        }

        void BuildEndPanel(Transform parent)
        {
            _endPanel = new GameObject("EndPanel");
            _endPanel.transform.SetParent(parent, false);
            var bg = _endPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.8f);
            var rt = _endPanel.GetComponent<RectTransform>();
            Stretch(rt);

            var panel = CreateImage(_endPanel.transform, "Narrative", new Color(0.12f, 0.10f, 0.16f, 0.95f));
            var prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(920f, 380f);

            CreateLabel(panel.transform, "Title", "Final Narrativo Alcanzado", 34, new Vector2(0f, 130f), new Vector2(820f, 60f));
            CreateLabel(panel.transform, "Body",
                "El portal celestial responde a tu granja ritual.\nPuedes continuar en modo infinito o reiniciar con prestigio.",
                22, new Vector2(0f, 52f), new Vector2(840f, 110f));

            var btnEndless = CreateButton(panel.transform, "Modo infinito", new Vector2(-170f, -120f), new Vector2(260f, 54f));
            btnEndless.onClick.AddListener(() =>
            {
                _endPanel.SetActive(false);
                _txtHint.text = "Modo infinito activo: la dificultad seguirá escalando.";
            });

            var btnPrestige = CreateButton(panel.transform, "Prestigio +1", new Vector2(170f, -120f), new Vector2(260f, 54f));
            btnPrestige.onClick.AddListener(ApplyPrestige);

            _endPanel.SetActive(false);
        }

        void SellResidue()
        {
            if (_astralResidue <= 0)
                return;
            var amount = _astralResidue;
            _astralResidue = 0;
            _darkCoins += amount * (4 + _difficultyTier);
            _totalSold += amount;
<<<<<<< HEAD:Assets/01_Scripts/Zone3/Zone3PrototypeGame.cs
=======
            AudioManager.Instance?.PlayZone3Sell();
>>>>>>> origin/devlucas:Assets/Scripts/Zone3/Zone3PrototypeGame.cs
        }

        void SellInk()
        {
            if (_voidInk <= 0)
                return;
            var amount = _voidInk;
            _voidInk = 0;
            _darkCoins += amount * (7 + _difficultyTier * 2);
            _totalSold += amount;
<<<<<<< HEAD:Assets/01_Scripts/Zone3/Zone3PrototypeGame.cs
=======
            AudioManager.Instance?.PlayZone3Sell();
>>>>>>> origin/devlucas:Assets/Scripts/Zone3/Zone3PrototypeGame.cs
        }

        void ApplyTax()
        {
            var rate = 0.20f + _difficultyTier * 0.03f;
            var taxAmount = Mathf.CeilToInt(_darkCoins * rate);
            _darkCoins = Mathf.Max(0, _darkCoins - taxAmount);
            _txtHint.text = $"Impuesto celestial aplicado: -{taxAmount} monedas.";
<<<<<<< HEAD:Assets/01_Scripts/Zone3/Zone3PrototypeGame.cs
=======
            AudioManager.Instance?.PlayZone3TaxAlert();
>>>>>>> origin/devlucas:Assets/Scripts/Zone3/Zone3PrototypeGame.cs
        }

        void ShowEndNarrative()
        {
            _endNarrativeShown = true;
            if (_endPanel != null)
                _endPanel.SetActive(true);
<<<<<<< HEAD:Assets/01_Scripts/Zone3/Zone3PrototypeGame.cs
=======
            AudioManager.Instance?.PlayZone3EndNarrative();
>>>>>>> origin/devlucas:Assets/Scripts/Zone3/Zone3PrototypeGame.cs
        }

        void ApplyPrestige()
        {
            _prestigePoints++;
            PlayerPrefs.SetInt(PrestigeKey, _prestigePoints);
            PlayerPrefs.Save();

            _darkCoins = 280 + _prestigePoints * 40;
            _astralResidue = 0;
            _voidInk = 0;
            _difficultyTier = 1;
            _taxTimer = _taxInterval;
            _runtime = 0f;
            _endNarrativeShown = false;
            _totalSold = 0;
            if (_endPanel != null)
                _endPanel.SetActive(false);
            _txtHint.text = $"Prestigio aplicado. Puntos totales: {_prestigePoints}.";
<<<<<<< HEAD:Assets/01_Scripts/Zone3/Zone3PrototypeGame.cs
=======
            AudioManager.Instance?.PlayZone3Prestige();
>>>>>>> origin/devlucas:Assets/Scripts/Zone3/Zone3PrototypeGame.cs
        }

        void TryRestoreFromSaveIfRequested()
        {
            if (SaveManager.Instance == null || !SaveManager.Instance.ShouldRestoreFromSave)
                return;

            var data = SaveManager.Instance.CachedData;
            if (data != null && data.zone3 != null && data.zone3.valid)
                ApplySaveData(data.zone3);

            SaveManager.Instance.MarkRestoreConsumed();
        }

        public Zone3SaveData CaptureSaveData()
        {
            return new Zone3SaveData
            {
                valid = true,
                darkCoins = _darkCoins,
                astralResidue = _astralResidue,
                voidInk = _voidInk,
                difficultyTier = _difficultyTier,
                totalSold = _totalSold,
                taxTimer = _taxTimer,
                runtimeSeconds = _runtime,
                prestigePoints = _prestigePoints,
                endNarrativeShown = _endNarrativeShown,
            };
        }

        public void ApplySaveData(Zone3SaveData data)
        {
            if (data == null || !data.valid)
                return;

            _darkCoins = Mathf.Max(0, data.darkCoins);
            _astralResidue = Mathf.Max(0, data.astralResidue);
            _voidInk = Mathf.Max(0, data.voidInk);
            _difficultyTier = Mathf.Clamp(data.difficultyTier, 1, 9);
            _totalSold = Mathf.Max(0, data.totalSold);
            _taxTimer = Mathf.Max(0f, data.taxTimer);
            _runtime = Mathf.Max(0f, data.runtimeSeconds);
            _prestigePoints = Mathf.Max(0, data.prestigePoints);
            _endNarrativeShown = data.endNarrativeShown;
            PlayerPrefs.SetInt(PrestigeKey, _prestigePoints);
            PlayerPrefs.Save();
        }

        void RefreshUi()
        {
            if (_txtResources != null)
                _txtResources.text = $"Monedas: {_darkCoins}  |  Residuo Astral: {_astralResidue}  |  Tinta del Vacío: {_voidInk}";
            if (_txtTax != null)
                _txtTax.text = $"Próximo impuesto: {Mathf.Max(0f, _taxTimer):0.0}s";
            if (_txtDifficulty != null)
                _txtDifficulty.text = $"Dificultad: Tier {_difficultyTier}  |  Prestigio: {_prestigePoints}  |  Vendido total: {_totalSold}";
            if (_txtHint != null && string.IsNullOrEmpty(_txtHint.text))
                _txtHint.text = "Objetivo final: Tier >= 3, Monedas >= 1200 y Vendido >= 120.";
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
            bg.color = new Color(0.17f, 0.20f, 0.28f, 1f);
            var btn = go.AddComponent<Button>();

            var lbl = CreateLabel(go.transform, "Text", text, 22, Vector2.zero, rectSize);
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
