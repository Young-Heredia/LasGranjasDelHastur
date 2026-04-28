using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Core
{
    /// <summary>
    /// Panel global de Game Over: solo permite volver al menú y borra todo el progreso.
    /// </summary>
    [DisallowMultipleComponent]
    public class GlobalGameOverPresenter : MonoBehaviour
    {
        public static GlobalGameOverPresenter Instance { get; private set; }

        static GameOverOrigin _pendingOrigin = GameOverOrigin.Dungeons;
        static bool _requested;

        Canvas _canvas;
        bool _visible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureExists()
        {
            if (Instance != null)
                return;
            var go = new GameObject("GlobalGameOverPresenter");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<GlobalGameOverPresenter>();
        }

        public static void Request(GameOverOrigin origin)
        {
            EnsureExists();
            _pendingOrigin = origin;
            _requested = true;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            if (_requested)
                TryPresent();
        }

        void TryPresent()
        {
            if (!_requested || _visible)
                return;
            _requested = false;
            _visible = true;
            Time.timeScale = 0f;
            BuildUi();
        }

        void BuildUi()
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(true);
                return;
            }

            var root = new GameObject("GameOverCanvas");
            root.transform.SetParent(transform, false);
            _canvas = root.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32000;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(root.transform, false);
            var bg = backdrop.AddComponent<Image>();
            bg.color = ThemeBackdrop(_pendingOrigin);
            var rt = backdrop.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.06f, 0.06f, 0.08f, 0.94f);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(720f, 420f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text = "GAME OVER";
            title.fontSize = 46;
            title.alignment = TextAlignmentOptions.Center;
            title.color = ThemeAccent(_pendingOrigin);
            var trt = titleGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 1f);
            trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -36f);
            trt.sizeDelta = new Vector2(640f, 70f);

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(panel.transform, false);
            var body = bodyGo.AddComponent<TextMeshProUGUI>();
            body.text = ThemeBody(_pendingOrigin);
            body.fontSize = 24;
            body.alignment = TextAlignmentOptions.TopJustified;
            body.color = new Color(0.88f, 0.86f, 0.82f, 1f);
            var brt = bodyGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(0f, 8f);
            brt.sizeDelta = new Vector2(640f, 200f);

            var btnGo = new GameObject("BtnMenu");
            btnGo.transform.SetParent(panel.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.anchoredPosition = new Vector2(0f, 36f);
            btnRt.sizeDelta = new Vector2(360f, 52f);
            var btnBg = btnGo.AddComponent<Image>();
            btnBg.color = new Color(0.18f, 0.16f, 0.22f, 1f);
            var btn = btnGo.AddComponent<Button>();
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(btnGo.transform, false);
            var lbl = lblGo.AddComponent<TextMeshProUGUI>();
            lbl.text = "Volver al menú";
            lbl.fontSize = 26;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color = Color.white;
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            btn.onClick.AddListener(OnReturnToMenuClicked);
        }

        static Color ThemeBackdrop(GameOverOrigin o) => o switch
        {
            GameOverOrigin.CondensedCities => new Color(0.05f, 0.07f, 0.12f, 0.92f),
            GameOverOrigin.Celestial => new Color(0.04f, 0.03f, 0.10f, 0.94f),
            _ => new Color(0.04f, 0.05f, 0.06f, 0.93f)
        };

        static Color ThemeAccent(GameOverOrigin o) => o switch
        {
            GameOverOrigin.CondensedCities => new Color(0.82f, 0.72f, 0.38f, 1f),
            GameOverOrigin.Celestial => new Color(0.55f, 0.78f, 0.95f, 1f),
            _ => new Color(0.78f, 0.72f, 0.42f, 1f)
        };

        static string ThemeBody(GameOverOrigin o)
        {
            var collector = o switch
            {
                GameOverOrigin.CondensedCities => "Kthanid",
                GameOverOrigin.Celestial => "Azathoth",
                _ => "Cthulhu"
            };
            return
                $"Tres multas fiscales acumuladas. El recaudador ({collector}) cierra la contabilidad.\n\n" +
                "Todo el progreso se anulará: monedas, niveles, zonas y granjas.";
        }

        void OnReturnToMenuClicked()
        {
            Time.timeScale = 1f;
            SaveManager.Instance?.ResetAllProgress(resetIntroSeen: true);
            SceneManager.LoadScene("MainMenu");
            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
                _canvas = null;
            }
            _visible = false;
        }
    }
}
