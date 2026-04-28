using System.Collections.Generic;
using LasGranjasDelHastur;
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

    RectTransform _playArea;
    readonly List<RectTransform> _targets = new();
    readonly List<CelestialBodyDrag> _bodies = new();
    float _holdTimer;
    bool _victory;
    Sprite _white;

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

        // Puntos de alineación (anillos)
        var targetPositions = new[]
        {
            new Vector2(-200f, 50f),
            new Vector2(0f, -40f),
            new Vector2(200f, 50f),
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
            img.sprite = _white;
            img.color = starts[i].Item2;
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
    }

    public string BuildStatusLine(float timeLeft)
    {
        UpdatePlacedCount();
        return $"Alineación: {BodiesPlaced}/{BodiesRequired}   Mantén la formación   Tiempo: {Mathf.Max(0f, timeLeft):0.0}s";
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
    }

    public void TrySnapIfClose()
    {
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
