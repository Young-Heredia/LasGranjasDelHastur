using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace LasGranjasDelHastur.Core
{
    [DisallowMultipleComponent]
    public class RuntimeDiagnosticsOverlay : MonoBehaviour
    {
        [SerializeField] private bool showOverlay = true;
        [SerializeField] private Vector2 anchoredPos = new(8f, -8f);
        [SerializeField] private bool includeSaveDiagnostics = true;

        TextMeshProUGUI _text;
        Button _resetProgressButton;
        float _timer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
#if UNITY_EDITOR
            EnsureExists();
#endif
        }

        static void EnsureExists()
        {
            if (FindFirstObjectByType<RuntimeDiagnosticsOverlay>() != null)
                return;
            var go = new GameObject("RuntimeDiagnosticsOverlay");
            go.AddComponent<RuntimeDiagnosticsOverlay>();
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            showOverlay = false; // keep hidden by default for normal gameplay tests
            BuildUi();
        }

        void BuildUi()
        {
            var root = new GameObject("DiagnosticsCanvas");
            root.transform.SetParent(transform, false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            root.AddComponent<UnityEngine.UI.CanvasScaler>();
            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(root.transform, false);
            _text = textGo.AddComponent<TextMeshProUGUI>();
            _text.fontSize = 15;
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.color = new Color(1f, 0.95f, 0.7f, 0.85f);
            _text.raycastTarget = false;

            var rt = _text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(980f, 110f);

            var btnGo = new GameObject("ResetProgressButton");
            btnGo.transform.SetParent(root.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0f, 1f);
            btnRt.anchorMax = new Vector2(0f, 1f);
            btnRt.pivot = new Vector2(0f, 1f);
            btnRt.anchoredPosition = anchoredPos + new Vector2(0f, -118f);
            btnRt.sizeDelta = new Vector2(260f, 34f);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.30f, 0.12f, 0.12f, 0.95f);
            _resetProgressButton = btnGo.AddComponent<Button>();
            _resetProgressButton.onClick.AddListener(ResetAllProgressFromDebug);

            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "DEBUG: Reset progreso total";
            label.fontSize = 16;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        void Update()
        {
            if (global::LasGranjasDelHastur.InputAdapter.KeyDown(KeyCode.F8))
                showOverlay = !showOverlay;

            if (_text == null)
                return;
            _text.enabled = showOverlay;
            if (_resetProgressButton != null)
                _resetProgressButton.gameObject.SetActive(showOverlay);
            if (!showOverlay)
                return;

            _timer += Time.unscaledDeltaTime;
            if (_timer < 0.5f)
                return;
            _timer = 0f;

            var uiRoots = 0;
            var roots = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in roots)
            {
                if (t != null && t.parent == null && t.name == "UI")
                    uiRoots++;
            }

            var eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (!includeSaveDiagnostics)
            {
                _text.text = $"[Diag] UI roots: {uiRoots} | EventSystems: {eventSystems.Length}";
                return;
            }

            var saveExists = SaveManager.Instance != null && SaveManager.Instance.HasSaveFile();
            var path = SaveManager.Instance != null ? SaveManager.Instance.GetSaveFilePath() : "(SaveManager missing)";
            _text.text =
                $"[Diag] UI roots: {uiRoots} | EventSystems: {eventSystems.Length}\n" +
                $"[Save] Exists: {saveExists} | Version: {SaveManager.CurrentSaveVersion}\n" +
                $"[Save] Path: {path}";
        }

        void ResetAllProgressFromDebug()
        {
            SaveManager.Instance?.ResetAllProgress(resetIntroSeen: true);
            var zoneManager = FindFirstObjectByType<ZoneManager>();
            if (zoneManager != null)
                zoneManager.ResetAllUnlocksForDebug();

            SceneManager.LoadScene("MainMenu");
        }
    }
}

