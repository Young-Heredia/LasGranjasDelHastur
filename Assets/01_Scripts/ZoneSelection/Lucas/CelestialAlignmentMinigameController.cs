using System.Collections.Generic;
using LasGranjasDelHastur;
using LasGranjasDelHastur.Zone1;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Alineación celestial: arrastra un planeta y dos lunas a los puntos de luz. Victoria al mantener la alineación.
/// </summary>
public class CelestialAlignmentMinigameController : MonoBehaviour
{
    [SerializeField] float snapDistancePx = 44f;
    [SerializeField] float victoryHoldSeconds = 0.45f;
    [SerializeField] float guideFlowSpeed = 0.45f;
    [SerializeField] float guideLineThickness = 8f;
    [SerializeField] float timingClickWindowPx = 28f;
    [SerializeField] string planetSpritePath = "Assets/02_Sprites/Lucas/Zone3/NewCells/z3_celestial_energy_sun_idle.png";
    [SerializeField] string moonASpritePath = "Assets/02_Sprites/Lucas/Zone3/NewCells/z3_celestial_soul_moon_idle.png";
    [SerializeField] string moonBSpritePath = "Assets/02_Sprites/Lucas/Zone3/NewCells/z3_celestial_coin_asteroid_idle.png";

    RectTransform _playArea;
    readonly List<RectTransform> _targets = new();
    readonly List<Image> _targetImages = new();
    readonly List<CelestialBodyDrag> _bodies = new();
    readonly List<Vector2> _guideRoute = new();
    float _holdTimer;
    bool _victory;
    Sprite _white;
    TextMeshProUGUI _guideText;
    RectTransform _flowMarkerRt;
    TextMeshProUGUI _clickNowText;
    int _flowTargetIndex = -1;

    public bool IsVictory => _victory;
    public int BodiesPlaced { get; private set; }
    public int BodiesRequired => _bodies.Count;

    public void Build()
    {
        if (_playArea != null)
            return;

        _white = RuntimeSpriteFactory.OpaqueWhiteSprite;

        var go = new GameObject("CelestialPlayfield");
        go.transform.SetParent(transform, false);
        var root = go.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(720f, 360f);
        root.anchoredPosition = new Vector2(0f, 8f);
        _playArea = root;
        _playArea.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.04f, 0.12f, 0.9f);
        BuildGuideLabel();

        // Puntos de alineación (anillos)
        var targetPositions = new[]
        {
            new Vector2(-200f, 50f),
            new Vector2(0f, -40f),
            new Vector2(200f, 50f),
        };
        var extendedGuideRoute = new[]
        {
            new Vector2(-300f, 112f),
            targetPositions[0],
            targetPositions[1],
            targetPositions[2],
            new Vector2(300f, 112f),
        };
        for (var i = 0; i < 3; i++)
        {
            var t = new GameObject($"Alineador_{i + 1}");
            t.transform.SetParent(_playArea, false);
            var rt = t.AddComponent<RectTransform>();
            rt.anchoredPosition = targetPositions[i];
            rt.sizeDelta = new Vector2(64f, 64f);
            var img = t.AddComponent<Image>();
            img.sprite = _white;
            img.color = new Color(0.95f, 0.85f, 0.35f, 0.45f);
            _targetImages.Add(img);
            _targets.Add(rt);
            var ring = new GameObject("Ring");
            ring.transform.SetParent(t.transform, false);
            var rrt = ring.AddComponent<RectTransform>();
            rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 0.5f);
            rrt.sizeDelta = new Vector2(80f, 80f);
            var ringImg = ring.AddComponent<Image>();
            ringImg.sprite = _white;
            ringImg.color = new Color(0.4f, 0.75f, 1f, 0.5f);
        }
        BuildTargetGuideLines(extendedGuideRoute);

        // Cuerpos: planeta (0), luna 1, luna 2
        var starts = new[]
        {
            (new Vector2(-220f, -130f), new Color(0.3f, 0.5f, 1f, 0.95f), 52f, "Planeta"),
            (new Vector2(0f, -140f), new Color(0.75f, 0.78f, 0.85f, 0.9f), 32f, "Luna A"),
            (new Vector2(220f, -130f), new Color(0.65f, 0.7f, 0.8f, 0.88f), 28f, "Luna B"),
        };
        for (var i = 0; i < 3; i++)
        {
            var b = new GameObject(starts[i].Item4);
            b.transform.SetParent(_playArea, false);
            var rt = b.AddComponent<RectTransform>();
            rt.anchoredPosition = starts[i].Item1;
            rt.sizeDelta = new Vector2(starts[i].Item3, starts[i].Item3);
            var img = b.AddComponent<Image>();
            var bodySprite = ResolveBodySprite(i);
            img.sprite = bodySprite ?? _white;
            img.color = bodySprite != null ? Color.white : starts[i].Item2;
            img.preserveAspect = true;
            img.raycastTarget = true;
            var d = b.AddComponent<CelestialBodyDrag>();
            d.TargetIndex = i;
            d.Controller = this;
            d.PlaneRect = _playArea;
            d.RootCanvas = b.GetComponentInParent<Canvas>();
            _bodies.Add(d);
        }
    }

    public void ResetGame()
    {
        _victory = false;
        _holdTimer = 0f;
        if (_bodies.Count == 0)
            return;
        var starts = new[] { new Vector2(-220f, -130f), new Vector2(0f, -140f), new Vector2(220f, -130f) };
        for (var i = 0; i < _bodies.Count; i++)
        {
            _bodies[i].GetComponent<RectTransform>().anchoredPosition = starts[i];
            _bodies[i].Placed = false;
        }
        UpdatePlacedCount();
        UpdateGuidanceVisuals();
    }

    public void Tick()
    {
        if (_victory || _targets.Count < 3 || _bodies.Count < 3)
            return;
        if (AllAligned())
        {
            _holdTimer += Time.unscaledDeltaTime;
            if (_holdTimer >= victoryHoldSeconds)
                _victory = true;
        }
        else
        {
            _holdTimer = 0f;
        }
        UpdateGuidanceVisuals();
    }

    public string BuildStatusLine(float timeLeft)
    {
        UpdatePlacedCount();
        return $"Alineación: {BodiesPlaced}/{BodiesRequired}   Sigue la línea brillante y suelta/click cerca del anillo   Tiempo: {Mathf.Max(0f, timeLeft):0.0}s";
    }

    void UpdatePlacedCount()
    {
        var n = 0;
        foreach (var b in _bodies)
        {
            if (b.Placed)
                n++;
        }
        BodiesPlaced = n;
    }

    bool AllAligned()
    {
        for (var i = 0; i < 3; i++)
        {
            if (!IsBodyInTarget(i))
                return false;
        }
        return true;
    }

    public bool IsBodyInTarget(int index)
    {
        if (index < 0 || index >= _bodies.Count || index >= _targets.Count)
            return false;
        var a = _bodies[index].GetComponent<RectTransform>().anchoredPosition;
        var b = _targets[index].anchoredPosition;
        return (a - b).sqrMagnitude <= snapDistancePx * snapDistancePx;
    }

    public void NotifyDragEnd()
    {
        for (var i = 0; i < _bodies.Count; i++)
        {
            var body = _bodies[i].GetComponent<RectTransform>();
            var tgt = _targets[i].anchoredPosition;
            if ((body.anchoredPosition - tgt).sqrMagnitude <= snapDistancePx * snapDistancePx)
            {
                body.anchoredPosition = tgt;
                _bodies[i].Placed = true;
            }
            else
            {
                _bodies[i].Placed = false;
            }
        }
        _holdTimer = 0f;
        RefreshGuideText();
    }

    public void TrySnapIfClose()
    {
        // Modo timing: si el marcador pasa por un objetivo y el jugador pulsa click/espacio, encaja esa pieza.
        if (TryTimingSnapAtCurrentFlowTarget())
        {
            NotifyDragEnd();
            return;
        }

        foreach (var b in _bodies)
        {
            var body = b.GetComponent<RectTransform>();
            var i = b.TargetIndex;
            var tgt = _targets[i].anchoredPosition;
            if ((body.anchoredPosition - tgt).sqrMagnitude <= snapDistancePx * snapDistancePx * 2.25f)
                body.anchoredPosition = tgt;
        }
        NotifyDragEnd();
    }

    void BuildGuideLabel()
    {
        var go = new GameObject("GuideText");
        go.transform.SetParent(_playArea, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -8f);
        rt.sizeDelta = new Vector2(690f, 28f);
        _guideText = go.AddComponent<TextMeshProUGUI>();
        _guideText.fontSize = 15;
        _guideText.alignment = TextAlignmentOptions.Center;
        _guideText.color = new Color(0.78f, 0.9f, 1f, 1f);
        _guideText.raycastTarget = false;

        var clickGo = new GameObject("ClickNowText");
        clickGo.transform.SetParent(_playArea, false);
        var clickRt = clickGo.AddComponent<RectTransform>();
        clickRt.anchorMin = new Vector2(0.5f, 0f);
        clickRt.anchorMax = new Vector2(0.5f, 0f);
        clickRt.pivot = new Vector2(0.5f, 0f);
        clickRt.anchoredPosition = new Vector2(0f, 10f);
        clickRt.sizeDelta = new Vector2(690f, 26f);
        _clickNowText = clickGo.AddComponent<TextMeshProUGUI>();
        _clickNowText.fontSize = 15;
        _clickNowText.alignment = TextAlignmentOptions.Center;
        _clickNowText.color = new Color(1f, 0.9f, 0.4f, 0f);
        _clickNowText.text = "CLICK AHORA / ESPACIO: pieza en rango de encaje";
        _clickNowText.raycastTarget = false;
        RefreshGuideText();
    }

    void BuildTargetGuideLines(IReadOnlyList<Vector2> points)
    {
        if (points == null || points.Count < 2)
            return;
        for (var i = 0; i < points.Count - 1; i++)
            CreateGuideLine(points[i], points[i + 1], i);
    }

    void CreateGuideLine(Vector2 from, Vector2 to, int idx)
    {
        var go = new GameObject($"GuideLine_{idx + 1}");
        go.transform.SetParent(_playArea, false);
        var rt = go.AddComponent<RectTransform>();
        var delta = to - from;
        var len = delta.magnitude;
        rt.sizeDelta = new Vector2(len, Mathf.Max(2f, guideLineThickness));
        rt.anchoredPosition = from + delta * 0.5f;
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        var img = go.AddComponent<Image>();
        img.sprite = _white;
        img.color = new Color(0.45f, 0.75f, 1f, 0.6f);
        img.raycastTarget = false;
        if (idx == 0)
            _guideRoute.Add(from);
        _guideRoute.Add(to);
        if (_flowMarkerRt == null)
            CreateFlowMarker();
    }

    void CreateFlowMarker()
    {
        var go = new GameObject("FlowMarker");
        go.transform.SetParent(_playArea, false);
        _flowMarkerRt = go.AddComponent<RectTransform>();
        _flowMarkerRt.sizeDelta = new Vector2(14f, 14f);
        var img = go.AddComponent<Image>();
        img.sprite = _white;
        img.color = new Color(1f, 0.96f, 0.5f, 0.95f);
        img.raycastTarget = false;
    }

    Sprite ResolveBodySprite(int index)
    {
        var path = index switch
        {
            0 => planetSpritePath,
            1 => moonASpritePath,
            _ => moonBSpritePath
        };
        return string.IsNullOrWhiteSpace(path) ? null : Zone1ArtProvider.LoadSprite(path);
    }

    void RefreshGuideText()
    {
        if (_guideText == null)
            return;
        _guideText.text = BodiesPlaced >= BodiesRequired
            ? "Perfecto: mantén esta alineación un instante para completar."
            : "Guía: sigue la línea brillante y encaja cada pieza en su anillo.";
    }

    void UpdateGuidanceVisuals()
    {
        UpdateFlowMarker();
        UpdateTargetHighlights();
        UpdateClickNowHint();
    }

    void UpdateFlowMarker()
    {
        if (_flowMarkerRt == null || _guideRoute.Count < 2)
            return;

        var segmentCount = _guideRoute.Count - 1;
        var t = Mathf.Repeat(Time.unscaledTime * Mathf.Max(0.05f, guideFlowSpeed), segmentCount);
        var segmentIndex = Mathf.Clamp(Mathf.FloorToInt(t), 0, segmentCount - 1);
        var localT = t - segmentIndex;
        _flowMarkerRt.anchoredPosition = Vector2.Lerp(_guideRoute[segmentIndex], _guideRoute[segmentIndex + 1], localT);
        _flowTargetIndex = ResolveActiveFlowTarget();
    }

    void UpdateTargetHighlights()
    {
        for (var i = 0; i < _targetImages.Count; i++)
        {
            var img = _targetImages[i];
            if (img == null)
                continue;

            if (IsBodyInTarget(i))
            {
                img.color = new Color(0.55f, 1f, 0.65f, 0.72f);
                continue;
            }

            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f + i * 0.8f);
            img.color = new Color(0.95f, 0.85f, 0.35f, 0.3f + pulse * 0.28f);
        }
    }

    void UpdateClickNowHint()
    {
        if (_clickNowText == null)
            return;
        var shouldClickNow = HasAnyBodyInSnapAssistRange();
        var alpha = shouldClickNow ? (0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f))) : 0f;
        _clickNowText.color = new Color(1f, 0.9f, 0.4f, alpha);
    }

    bool HasAnyBodyInSnapAssistRange()
    {
        if (_flowTargetIndex >= 0 && _flowTargetIndex < _bodies.Count && !_bodies[_flowTargetIndex].Placed)
            return true;

        for (var i = 0; i < _bodies.Count; i++)
        {
            var body = _bodies[i].GetComponent<RectTransform>();
            var target = _targets[i];
            if (body == null || target == null)
                continue;
            var inRange = (body.anchoredPosition - target.anchoredPosition).sqrMagnitude <= snapDistancePx * snapDistancePx * 2.25f;
            if (inRange && !_bodies[i].Placed)
                return true;
        }
        return false;
    }

    bool TryTimingSnapAtCurrentFlowTarget()
    {
        if (_flowMarkerRt == null || _flowTargetIndex < 0 || _flowTargetIndex >= _targets.Count || _flowTargetIndex >= _bodies.Count)
            return false;
        if (_bodies[_flowTargetIndex].Placed)
            return false;

        var markerPos = _flowMarkerRt.anchoredPosition;
        var targetPos = _targets[_flowTargetIndex].anchoredPosition;
        var inTimingWindow = (markerPos - targetPos).sqrMagnitude <= timingClickWindowPx * timingClickWindowPx;
        if (!inTimingWindow)
            return false;

        _bodies[_flowTargetIndex].GetComponent<RectTransform>().anchoredPosition = targetPos;
        _bodies[_flowTargetIndex].Placed = true;
        return true;
    }

    int ResolveActiveFlowTarget()
    {
        if (_flowMarkerRt == null || _targets.Count == 0)
            return -1;

        var markerPos = _flowMarkerRt.anchoredPosition;
        var bestIndex = -1;
        var bestDist = float.MaxValue;
        for (var i = 0; i < _targets.Count; i++)
        {
            var d = (markerPos - _targets[i].anchoredPosition).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                bestIndex = i;
            }
        }

        return bestDist <= timingClickWindowPx * timingClickWindowPx * 1.3f ? bestIndex : -1;
    }
}

public sealed class CelestialBodyDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    public int TargetIndex;
    public CelestialAlignmentMinigameController Controller;
    public RectTransform PlaneRect;
    public Canvas RootCanvas;
    public bool Placed { get; set; }

    RectTransform _rt;
    Vector2 _pointerOffset;

    void Awake() => _rt = GetComponent<RectTransform>();

    public void OnPointerDown(PointerEventData e) { }

    public void OnBeginDrag(PointerEventData e)
    {
        Placed = false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(PlaneRect, e.position, GetCam(), out var local))
            return;
        _pointerOffset = _rt.anchoredPosition - local;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(PlaneRect, e.position, GetCam(), out var local))
            return;
        var p = local + _pointerOffset;
        p.x = Mathf.Clamp(p.x, -300f, 300f);
        p.y = Mathf.Clamp(p.y, -150f, 150f);
        _rt.anchoredPosition = p;
    }

    public void OnEndDrag(PointerEventData e)
    {
        Controller?.NotifyDragEnd();
    }

    UnityEngine.Camera GetCam() =>
        RootCanvas != null && RootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? RootCanvas.worldCamera
            : null;
}
