using System;
using LasGranjasDelHastur;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Framework mínimo para lanzar minijuegos de desbloqueo desde ZoneSelection.
/// </summary>
[DisallowMultipleComponent]
public class MiniGameManager : MonoBehaviour
{
    [Header("Zone2 Unlock Trial")]
    [SerializeField, Min(1)] private int zone2TargetClicks = 10;
    [SerializeField, Min(1f)] private float zone2DurationSeconds = 8f;

    [Header("Zone3 Alignment Trial")]
    [SerializeField, Min(1)] private int zone3TargetAlignments = 3;
    [SerializeField, Min(1f)] private float zone3DurationSeconds = 14f;
    [SerializeField, Min(0.1f)] private float zone3CursorSpeed = 1.5f;
    [SerializeField, Range(0.05f, 0.4f)] private float zone3HitWindow = 0.16f;

    GameObject _overlayRoot;
    TextMeshProUGUI _title;
    TextMeshProUGUI _body;
    TextMeshProUGUI _status;
    Button _actionButton;
    Button _cancelButton;

    bool _isRunning;
    string _activeMiniGameId;
    int _clicks;
    float _remaining;
    Action<bool> _onFinish;

    float _zone3Cursor01;
    float _zone3Direction = 1f;
    float _zone3Target01;
    int _zone3Successes;
    bool _timerWarningPlayed;

    public bool IsRunning => _isRunning;

    void Awake()
    {
        BuildOverlayIfNeeded();
        SetOverlayVisible(false);
    }

    void Update()
    {
        if (!_isRunning)
            return;

        _remaining -= Time.unscaledDeltaTime;
        if (!_timerWarningPlayed && _remaining <= 3f)
        {
            _timerWarningPlayed = true;
            AudioManager.Instance?.PlayMiniGameTimerWarning();
        }
        if (InputAdapter.KeyDown(KeyCode.Space))
            RegisterAction();

        if (_activeMiniGameId == ZoneManager.Zone3UnlockMiniGameId)
        {
            _zone3Cursor01 += _zone3Direction * zone3CursorSpeed * Time.unscaledDeltaTime;
            if (_zone3Cursor01 >= 1f)
            {
                _zone3Cursor01 = 1f;
                _zone3Direction = -1f;
            }
            else if (_zone3Cursor01 <= 0f)
            {
                _zone3Cursor01 = 0f;
                _zone3Direction = 1f;
            }
        }

        UpdateStatusText();

        if (_activeMiniGameId == ZoneManager.Zone2UnlockMiniGameId && _clicks >= zone2TargetClicks)
        {
            Finish(true);
            return;
        }

        if (_activeMiniGameId == ZoneManager.Zone3UnlockMiniGameId && _zone3Successes >= zone3TargetAlignments)
        {
            Finish(true);
            return;
        }

        if (_remaining <= 0f)
            Finish(false);
    }

    public bool StartMiniGame(string miniGameId, Action<bool> onFinish)
    {
        if (_isRunning)
            return false;

        if (!string.Equals(miniGameId, ZoneManager.Zone2UnlockMiniGameId, StringComparison.Ordinal) &&
            !string.Equals(miniGameId, ZoneManager.Zone3UnlockMiniGameId, StringComparison.Ordinal))
            return false;

        BuildOverlayIfNeeded();
        _activeMiniGameId = miniGameId;
        _onFinish = onFinish;
        ResetGameState();
        _isRunning = true;
        SetOverlayVisible(true);
        _actionButton.onClick.RemoveAllListeners();
        _actionButton.onClick.AddListener(RegisterAction);
        _cancelButton.onClick.RemoveAllListeners();
        _cancelButton.onClick.AddListener(() => Finish(false));
        ConfigureUiForCurrentGame();
        UpdateStatusText();
        AudioManager.Instance?.PlayMiniGameStart();
        return true;
    }

    void RegisterAction()
    {
        if (!_isRunning)
            return;

        if (_activeMiniGameId == ZoneManager.Zone2UnlockMiniGameId)
        {
            _clicks++;
            AudioManager.Instance?.PlayMiniGameHit();
        }
        else if (_activeMiniGameId == ZoneManager.Zone3UnlockMiniGameId)
        {
            var aligned = Mathf.Abs(_zone3Cursor01 - _zone3Target01) <= zone3HitWindow;
            if (aligned)
            {
                _zone3Successes++;
                PickNewZone3Target();
                AudioManager.Instance?.PlayMiniGameHit();
            }
            else
            {
                _zone3Successes = Mathf.Max(0, _zone3Successes - 1);
                AudioManager.Instance?.PlayMiniGameMiss();
            }
        }

        UpdateStatusText();
    }

    void UpdateStatusText()
    {
        if (_status == null)
            return;

        if (_activeMiniGameId == ZoneManager.Zone2UnlockMiniGameId)
        {
            _status.text = $"Sellos: {_clicks}/{zone2TargetClicks}   Tiempo: {Mathf.Max(0f, _remaining):0.0}s";
            return;
        }

        if (_activeMiniGameId == ZoneManager.Zone3UnlockMiniGameId)
        {
            var marker = BuildZone3MarkerBar();
            _status.text = $"Alineaciones: {_zone3Successes}/{zone3TargetAlignments}   Tiempo: {Mathf.Max(0f, _remaining):0.0}s\n{marker}";
        }
    }

    void Finish(bool success)
    {
        if (!_isRunning)
            return;
        _isRunning = false;
        SetOverlayVisible(false);
        if (success)
            AudioManager.Instance?.PlayMiniGameComplete();
        else
            AudioManager.Instance?.PlayMiniGameFail();
        var callback = _onFinish;
        _onFinish = null;
        callback?.Invoke(success);
    }

    void ResetGameState()
    {
        _clicks = 0;
        _zone3Successes = 0;
        _zone3Cursor01 = 0f;
        _zone3Direction = 1f;
        _timerWarningPlayed = false;
        PickNewZone3Target();

        _remaining = _activeMiniGameId == ZoneManager.Zone2UnlockMiniGameId
            ? zone2DurationSeconds
            : zone3DurationSeconds;
    }

    void ConfigureUiForCurrentGame()
    {
        if (_activeMiniGameId == ZoneManager.Zone2UnlockMiniGameId)
        {
            _title.text = "Minijuego: Alineación de Sellos";
            _body.text = "Canaliza el sello pulsando el botón (o barra espaciadora)\nantes de que termine el tiempo.";
            SetActionLabel("Canalizar (+1)");
            return;
        }

        if (_activeMiniGameId == ZoneManager.Zone3UnlockMiniGameId)
        {
            _title.text = "Minijuego: Alineación Celestial";
            _body.text = "Deten el cursor cuando quede alineado con el objetivo.\nPulsa botón o barra espaciadora para intentar alinear.";
            SetActionLabel("Alinear");
        }
    }

    void SetActionLabel(string text)
    {
        if (_actionButton == null)
            return;
        var label = _actionButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = text;
    }

    void PickNewZone3Target()
    {
        _zone3Target01 = UnityEngine.Random.Range(0.12f, 0.88f);
    }

    string BuildZone3MarkerBar()
    {
        const int width = 24;
        var targetIndex = Mathf.Clamp(Mathf.RoundToInt(_zone3Target01 * (width - 1)), 0, width - 1);
        var cursorIndex = Mathf.Clamp(Mathf.RoundToInt(_zone3Cursor01 * (width - 1)), 0, width - 1);
        var chars = new char[width];
        for (var i = 0; i < width; i++)
            chars[i] = '-';
        chars[targetIndex] = 'X';
        chars[cursorIndex] = cursorIndex == targetIndex ? 'O' : '|';
        return new string(chars);
    }

    void BuildOverlayIfNeeded()
    {
        if (_overlayRoot != null)
            return;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        _overlayRoot = new GameObject("MiniGameOverlay");
        _overlayRoot.transform.SetParent(canvas.transform, false);
        var rootRt = _overlayRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var blocker = _overlayRoot.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.7f);

        var panel = new GameObject("Panel");
        panel.transform.SetParent(_overlayRoot.transform, false);
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(760f, 300f);
        panelRt.anchoredPosition = Vector2.zero;
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);

        _title = CreateText(panel.transform, "Title", 24, TextAlignmentOptions.Center, new Vector2(0f, 108f), new Vector2(700f, 42f));
        _body = CreateText(panel.transform, "Body", 18, TextAlignmentOptions.Center, new Vector2(0f, 42f), new Vector2(700f, 80f));
        _status = CreateText(panel.transform, "Status", 20, TextAlignmentOptions.Center, new Vector2(0f, -24f), new Vector2(700f, 40f));

        _actionButton = CreateButton(panel.transform, "Canalizar (+1)", new Vector2(-110f, -110f), new Vector2(220f, 44f));
        _cancelButton = CreateButton(panel.transform, "Cancelar", new Vector2(110f, -110f), new Vector2(220f, 44f));
    }

    void SetOverlayVisible(bool visible)
    {
        if (_overlayRoot != null)
            _overlayRoot.SetActive(visible);
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, int fontSize, TextAlignmentOptions alignment, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.fontSize = fontSize;
        txt.alignment = alignment;
        txt.color = Color.white;
        txt.textWrappingMode = TextWrappingModes.Normal;
        return txt;
    }

    static Button CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject($"Button_{label}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.18f, 0.24f, 1f);
        var btn = go.AddComponent<Button>();

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        var txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 18;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        return btn;
    }
}
