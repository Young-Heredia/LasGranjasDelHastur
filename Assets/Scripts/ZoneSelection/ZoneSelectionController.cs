using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pantalla de selección de zona: carga escenas y sincroniza tarjetas con <see cref="ZoneProgressState"/>.
/// </summary>
[DefaultExecutionOrder(-200)]
public class ZoneSelectionController : MonoBehaviour
{
    static Texture2D _runtimeOpaqueWhiteTex;
    static Sprite _runtimeOpaqueWhiteSprite;

    [Header("Escenas")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string zone1SceneName = "Zone1_Dungeons";
    [Tooltip("Reservado para cuando exista la escena.")]
    [SerializeField] private string zone2SceneName = "Zone2_Cities";
    [Tooltip("Reservado para cuando exista la escena.")]
    [SerializeField] private string zone3SceneName = "Zone3_Celestial";

    [Header("UI")]
    [SerializeField] private ZoneCardUI[] zoneCards;

#if UNITY_EDITOR
    void OnValidate()
    {
        // No tocar RectTransform dentro de OnValidate/Awake: provoca SendMessage sobre hijos (Background, etc.).
        UnityEditor.EditorApplication.delayCall += FixCanvasRootIfBrokenEditorDeferred;
    }

    void FixCanvasRootIfBrokenEditorDeferred()
    {
        if (this == null)
            return;
        if (Application.isPlaying)
            return;
        ForceCanvasRootLayout();
        EnsureCanvasOverlayMode();
        EnsureBackgroundLayersActive();
        EnsureBackdropImagesDoNotBlockRaycasts();
    }
#endif

    void Awake()
    {
        ForceCanvasRootLayout();
        EnsureCanvasOverlayMode();
        EnsureBackgroundLayersActive();
        EnsureBackdropImagesDoNotBlockRaycasts();
        EnsureBackgroundSolidOpaqueRuntimeSprite();
        EnsureAmbientOverlayBelowPanelFrame();
    }

    /// <summary>
    /// Orden: BackgroundSolid → Background → AmbientOverlay → PanelFrame → … (cosmic encima del arte de fondo, debajo del marco).
    /// Tinte sin transparencia (Color blanco opaco) y sin bloquear clics.
    /// </summary>
    void EnsureAmbientOverlayBelowPanelFrame()
    {
        var ambient = transform.Find("AmbientOverlay");
        var bg = transform.Find("Background");
        if (ambient == null || bg == null)
            return;

        var targetIndex = bg.GetSiblingIndex() + 1;
        if (ambient.GetSiblingIndex() != targetIndex)
            ambient.SetSiblingIndex(targetIndex);

        var img = ambient.GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(1f, 1f, 1f, 0.4f);
            img.raycastTarget = false;
        }
    }

    /// <summary>
    /// Imágenes de fondo a pantalla completa no deben interceptar clics (Footer, tarjetas, etc.).
    /// </summary>
    void EnsureBackdropImagesDoNotBlockRaycasts()
    {
        foreach (var name in new[] { "BackgroundSolid", "Background", "AmbientOverlay", "PanelFrame" })
        {
            var t = transform.Find(name);
            if (t == null)
                continue;
            var img = t.GetComponent<Image>();
            if (img != null)
                img.raycastTarget = false;
        }
    }

    /// <summary>
    /// Sprite 4×4 blanco opaco creado en runtime (no depende del PNG white_1x1 ni de la importación).
    /// </summary>
    static Sprite GetOrCreateRuntimeOpaqueWhiteSprite()
    {
        if (_runtimeOpaqueWhiteSprite != null)
            return _runtimeOpaqueWhiteSprite;

        _runtimeOpaqueWhiteTex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var w = new Color32(255, 255, 255, 255);
        var px = new Color32[16];
        for (var i = 0; i < 16; i++)
            px[i] = w;
        _runtimeOpaqueWhiteTex.SetPixels32(px);
        _runtimeOpaqueWhiteTex.Apply(false, false);
        _runtimeOpaqueWhiteSprite = Sprite.Create(
            _runtimeOpaqueWhiteTex,
            new Rect(0, 0, 4, 4),
            new Vector2(0.5f, 0.5f),
            100f);
        return _runtimeOpaqueWhiteSprite;
    }

    /// <summary>
    /// Fuerza un quad opaco detrás de todo; evita huecos si el sprite del proyecto falla o el reference está roto.
    /// </summary>
    void EnsureBackgroundSolidOpaqueRuntimeSprite()
    {
        var t = transform.Find("BackgroundSolid");
        if (t == null)
            return;
        var img = t.GetComponent<Image>();
        if (img == null)
            return;

        img.sprite = GetOrCreateRuntimeOpaqueWhiteSprite();
        img.type = Image.Type.Simple;
        img.color = new Color(0.06f, 0.04f, 0.1f, 1f);
        img.enabled = true;
        img.raycastTarget = false;
    }

    /// <summary>
    /// UI solo: Screen Space - Overlay evita depender de la cámara URP y reduce el tablero en huecos transparentes.
    /// </summary>
    void EnsureCanvasOverlayMode()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null)
            return;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
    }

    /// <summary>
    /// Si BackgroundSolid/Background/AmbientOverlay están desactivados, no hay nada opaco detrás del marco
    /// (huecos transparentes → tablero en vista de juego con Overlay, o rejilla en Scene view).
    /// </summary>
    void EnsureBackgroundLayersActive()
    {
        foreach (var name in new[] { "BackgroundSolid", "Background", "AmbientOverlay" })
        {
            var t = transform.Find(name);
            if (t == null)
                continue;
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            var img = t.GetComponent<Image>();
            if (img != null && !img.enabled)
                img.enabled = true;
        }
    }

    /// <summary>
    /// Fuerza siempre el layout del Canvas raíz (escala 0 / anclas colapsadas rompen toda la escena; no usar return temprano).
    /// </summary>
    void ForceCanvasRootLayout()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null)
            return;

        rt.localScale = Vector3.one;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    void Start()
    {
        StartCoroutine(StartZoneSelectionRoutine());
    }

    IEnumerator StartZoneSelectionRoutine()
    {
        EnsureCanvasOverlayMode();
        EnsureBackgroundLayersActive();
        ForceCanvasRootLayout();
        EnsureBackdropImagesDoNotBlockRaycasts();
        EnsureBackgroundSolidOpaqueRuntimeSprite();
        EnsureAmbientOverlayBelowPanelFrame();
        yield return null;
        ForceCanvasRootLayout();
        EnsureBackdropImagesDoNotBlockRaycasts();
        EnsureBackgroundSolidOpaqueRuntimeSprite();
        EnsureAmbientOverlayBelowPanelFrame();
        EnsureZoneSelectionUiEffects();
        RefreshAllCards();
        WireBackButtonAudio();
    }

    void EnsureZoneSelectionUiEffects()
    {
        var header = GameObject.Find("Header");
        if (header != null && header.GetComponent<TitleLogoPulse>() == null)
        {
            var p = header.AddComponent<TitleLogoPulse>();
            p.SetPulse(0.97f, 1.03f, 1.2f);
        }

        var row = GameObject.Find("CardsRow");
        if (row != null && row.GetComponent<TitleLogoPulse>() == null)
        {
            var p = row.AddComponent<TitleLogoPulse>();
            p.SetPulse(0.98f, 1.02f, 1f);
        }

        var back = GameObject.Find("BackButton");
        if (back != null && back.GetComponent<UIButtonHoverScale>() == null)
            back.AddComponent<UIButtonHoverScale>();
    }

    void WireBackButtonAudio()
    {
        var back = GameObject.Find("BackButton");
        if (back == null)
            return;
        var ui = back.GetComponent<BasicUIAudio>();
        if (ui == null || AudioManager.Instance == null)
            return;
        ui.hoverClip = AudioManager.Instance.uiHover;
        ui.clickClip = AudioManager.Instance.uiBack;
        ui.useAudioManagerFirst = true;
    }

    public void RefreshAllCards()
    {
        if (zoneCards == null)
            return;

        foreach (var card in zoneCards)
        {
            if (card != null)
                card.ApplyState();
        }
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void EnterZone1() => TryEnterZone(1);
    public void EnterZone2() => TryEnterZone(2);
    public void EnterZone3() => TryEnterZone(3);

    /// <summary>Llamado desde el botón de cada tarjeta (EnterZone1/2/3).</summary>
    public void TryEnterZone(int zoneNumber)
    {
        if (!ZoneProgressState.IsZoneUnlocked(zoneNumber))
            return;

        switch (zoneNumber)
        {
            case 1:
                if (!string.IsNullOrEmpty(zone1SceneName))
                    SceneManager.LoadScene(zone1SceneName);
                break;
            case 2:
                if (!string.IsNullOrEmpty(zone2SceneName))
                    SceneManager.LoadScene(zone2SceneName);
                break;
            case 3:
                if (!string.IsNullOrEmpty(zone3SceneName))
                    SceneManager.LoadScene(zone3SceneName);
                break;
        }
    }
}
