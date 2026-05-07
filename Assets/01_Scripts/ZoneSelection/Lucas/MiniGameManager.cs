using System;
using LasGranjasDelHastur;
using LasGranjasDelHastur.Zone1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minijuegos de desbloqueo: Z2 sellos; Z3 alineación celestial (arrastre) con temporizador y victoria.
/// </summary>
[DisallowMultipleComponent]
public class MiniGameManager : MonoBehaviour
{
    [Header("Zone2 Unlock Trial")]
    [SerializeField, Min(1)] private int zone2TargetClicks = 10;
    [SerializeField, Min(1f)] private float zone2DurationSeconds = 8f;
    [SerializeField] private string zone2PibleSpritePath = "Assets/02_Sprites/Lucas/Zone2/EasterEgg/zone1_easteregg_tindalos_pible_idle_v3.png";
    [SerializeField, Min(10f)] private float zone2PiblePixelsPerUnit = 96f;
    [SerializeField, Min(0.05f)] private float zone2JumpDuration = 0.18f;
    [SerializeField, Min(4f)] private float zone2JumpHeight = 34f;

    [Header("Zone3 Celestial Alignment")]
    [SerializeField, Min(1f)] private float zone3DurationSeconds = 50f;

    GameObject _overlayRoot;
    RectTransform _panelRt;
    TextMeshProUGUI _title;
    TextMeshProUGUI _body;
    TextMeshProUGUI _status;
    Button _actionButton;
    Button _cancelButton;
    CelestialAlignmentMinigameController _celestial;
    RectTransform _zone2PibleRt;
    Image _zone2PibleImg;
    Vector2 _zone2PibleBasePos;

    bool _isRunning;
    string _activeMiniGameId;
    int _clicks;
    float _remaining;
    Action<bool> _onFinish;
    bool _timerWarningPlayed;
    float _zone2JumpT = -1f;

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
        if (InputAdapter.KeyDown(KeyCode.Space) && _activeMiniGameId == ZoneManager.Zone2UnlockMiniGameId)
            RegisterAction();
        if (InputAdapter.KeyDown(KeyCode.Space) && _activeMiniGameId == ZoneManager.Zone3UnlockMiniGameId && _celestial != null)
            _celestial.TrySnapIfClose();
        TickZone2PibleJump();

        if (_activeMiniGameId == ZoneManager.Zone2UnlockMiniGameId && _clicks >= zone2TargetClicks)
        {
            Finish(true);
            return;
        }

        if (_activeMiniGameId == ZoneManager.Zone3UnlockMiniGameId)
        {
            _celestial?.Tick();
            if (_celestial != null && _celestial.IsVictory)
            {
                Finish(true);
                return;
            }
        }

        UpdateStatusText();

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
        ConfigureUiForCurrentGame();
        ResetGameState();
        _isRunning = true;
        SetOverlayVisible(true);
        _actionButton.onClick.RemoveAllListeners();
        _actionButton.onClick.AddListener(RegisterAction);
        _cancelButton.onClick.RemoveAllListeners();
        _cancelButton.onClick.AddListener(() => Finish(false));
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
            TriggerZone2PibleJump();
            AudioManager.Instance?.PlayMiniGameHit();
        }
        else if (_activeMiniGameId == ZoneManager.Zone3UnlockMiniGameId && _celestial != null)
        {
            _celestial.TrySnapIfClose();
            AudioManager.Instance?.PlayMiniGameHit();
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

        if (_activeMiniGameId == ZoneManager.Zone3UnlockMiniGameId && _celestial != null)
            _status.text = _celestial.BuildStatusLine(_remaining);
    }

    void Finish(bool success)
    {
        if (!_isRunning)
            return;
        _isRunning = false;
        SetOverlayVisible(false);
        if (_celestial != null)
            _celestial.gameObject.SetActive(false);
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
        _timerWarningPlayed = false;

        if (_activeMiniGameId == ZoneManager.Zone2UnlockMiniGameId)
        {
            _remaining = zone2DurationSeconds;
            return;
        }

        if (_activeMiniGameId == ZoneManager.Zone3UnlockMiniGameId)
        {
            _remaining = zone3DurationSeconds;
            if (_celestial != null)
            {
                _celestial.gameObject.SetActive(true);
                _celestial.ResetGame();
            }
        }
    }

    void ConfigureUiForCurrentGame()
    {
        if (_activeMiniGameId == ZoneManager.Zone2UnlockMiniGameId)
        {
            if (_celestial != null)
                _celestial.gameObject.SetActive(false);
            if (_panelRt != null)
                _panelRt.sizeDelta = new Vector2(760f, 300f);
            if (_title != null)
                _title.text = "Minijuego: Alineación de Sellos";
            if (_body != null)
                _body.text = "Haz que Pible complete 10 saltos: pulsa el botón (o barra espaciadora)\nantes de que termine el tiempo.";
            SetActionLabel("Canalizar (+1)");
            SetZone2PibleVisible(true);
            return;
        }

        if (_activeMiniGameId == ZoneManager.Zone3UnlockMiniGameId)
        {
            if (_panelRt != null)
                _panelRt.sizeDelta = new Vector2(920f, 580f);
            if (_title != null)
                _title.text = "Alineación celestial";
            if (_body != null)
                _body.text = "Alinea las 3 piezas sobre la guía luminosa y los anillos marcados.\nHaz click o Espacio para ajustar/snap cuando estén cerca del objetivo.";
            SetActionLabel("Acercar piezas");
            SetZone2PibleVisible(false);
            EnsureCelestial();
        }
    }

    void EnsureCelestial()
    {
        if (_celestial != null)
            return;
        if (_panelRt == null)
            return;
        var cgo = new GameObject("CelestialController");
        cgo.transform.SetParent(_panelRt, false);
        _celestial = cgo.AddComponent<CelestialAlignmentMinigameController>();
        _celestial.Build();
    }

    void SetActionLabel(string text)
    {
        if (_actionButton == null)
            return;
        var label = _actionButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = text;
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
        blocker.color = new Color(0.02f, 0.02f, 0.06f, 0.88f);

        var panel = new GameObject("Panel");
        panel.transform.SetParent(_overlayRoot.transform, false);
        _panelRt = panel.AddComponent<RectTransform>();
        _panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        _panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        _panelRt.pivot = new Vector2(0.5f, 0.5f);
        _panelRt.sizeDelta = new Vector2(760f, 300f);
        _panelRt.anchoredPosition = Vector2.zero;
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.06f, 0.07f, 0.14f, 0.98f);

        _title = CreateText(panel.transform, "Title", 24, TextAlignmentOptions.Center, new Vector2(0f, 248f), new Vector2(860f, 40f));
        _body = CreateText(panel.transform, "Body", 17, TextAlignmentOptions.Center, new Vector2(0f, 180f), new Vector2(860f, 64f));
        _status = CreateText(panel.transform, "Status", 18, TextAlignmentOptions.Center, new Vector2(0f, -240f), new Vector2(860f, 32f));
        EnsureZone2PibleWidget(panel.transform);

        _actionButton = CreateButton(panel.transform, "Action", new Vector2(-120f, -268f), new Vector2(200f, 40f));
        _cancelButton = CreateButton(panel.transform, "Cancelar", new Vector2(120f, -268f), new Vector2(200f, 40f));
    }

    void EnsureZone2PibleWidget(Transform parent)
    {
        if (_zone2PibleRt != null)
            return;

        var go = new GameObject("Zone2Pible");
        go.transform.SetParent(parent, false);
        _zone2PibleRt = go.AddComponent<RectTransform>();
        _zone2PibleRt.anchorMin = new Vector2(0.5f, 0.5f);
        _zone2PibleRt.anchorMax = new Vector2(0.5f, 0.5f);
        _zone2PibleRt.pivot = new Vector2(0.5f, 0.5f);
        _zone2PibleBasePos = new Vector2(0f, 20f);
        _zone2PibleRt.anchoredPosition = _zone2PibleBasePos;
        _zone2PibleRt.sizeDelta = new Vector2(110f, 110f);

        _zone2PibleImg = go.AddComponent<Image>();
        _zone2PibleImg.raycastTarget = false;
        var sprite = Zone1ArtProvider.LoadSprite(zone2PibleSpritePath);
        _zone2PibleImg.sprite = sprite;
        _zone2PibleImg.color = Color.white;
        if (sprite != null)
        {
            var ppuScale = Mathf.Clamp(zone2PiblePixelsPerUnit / 96f, 0.6f, 2f);
            _zone2PibleRt.sizeDelta = new Vector2(128f / ppuScale, 128f / ppuScale);
        }
        _zone2PibleRt.gameObject.SetActive(false);
    }

    void SetZone2PibleVisible(bool visible)
    {
        if (_zone2PibleRt == null)
            return;
        _zone2PibleRt.gameObject.SetActive(visible);
        _zone2PibleRt.anchoredPosition = _zone2PibleBasePos;
        _zone2JumpT = -1f;
    }

    void TriggerZone2PibleJump()
    {
        _zone2JumpT = 0f;
    }

    void TickZone2PibleJump()
    {
        if (_zone2PibleRt == null || !_zone2PibleRt.gameObject.activeSelf || _zone2JumpT < 0f)
            return;

        _zone2JumpT += Time.unscaledDeltaTime;
        var t = Mathf.Clamp01(_zone2JumpT / Mathf.Max(0.05f, zone2JumpDuration));
        var y = Mathf.Sin(t * Mathf.PI) * zone2JumpHeight;
        _zone2PibleRt.anchoredPosition = _zone2PibleBasePos + new Vector2(0f, y);
        if (t >= 1f)
        {
            _zone2PibleRt.anchoredPosition = _zone2PibleBasePos;
            _zone2JumpT = -1f;
        }
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
        txt.color = new Color(0.95f, 0.95f, 0.97f, 1f);
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
        img.color = new Color(0.2f, 0.25f, 0.4f, 1f);
        go.AddComponent<Button>();

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        var txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 17;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        return go.GetComponent<Button>();
    }
}
