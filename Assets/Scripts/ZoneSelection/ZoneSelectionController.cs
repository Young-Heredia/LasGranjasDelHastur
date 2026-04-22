using System.Collections;
using LasGranjasDelHastur;
using LasGranjasDelHastur.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

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
    [SerializeField] private MiniGameManager miniGameManager;
    [SerializeField] private ZoneManager zoneManager;

    [Header("Debug QA")]
    [SerializeField] private bool enableDebugUnlockUi = true;
    [SerializeField] private bool debugPanelStartsHidden = true;

    GameObject _debugPanel;

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
        if (zoneManager == null)
            zoneManager = ZoneManager.Instance != null ? ZoneManager.Instance : FindFirstObjectByType<ZoneManager>();
        if (miniGameManager == null)
            miniGameManager = GetComponent<MiniGameManager>() ?? gameObject.AddComponent<MiniGameManager>();

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

    void Update()
    {
        if (!IsDebugUiEnabled())
            return;
        if (InputAdapter.KeyDown(KeyCode.F9))
            ToggleDebugPanel();
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
        EnsureDebugUnlockUi();
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
        if (zoneNumber == 2 && (zoneManager == null || !zoneManager.IsZoneUnlocked(2)))
        {
            TryUnlockAndEnterZone2();
            return;
        }
        if (zoneNumber == 3 && (zoneManager == null || !zoneManager.IsZoneUnlocked(3)))
        {
            TryUnlockAndEnterZone3();
            return;
        }
        if (!ZoneProgressState.IsZoneUnlocked(zoneNumber))
            return;

        switch (zoneNumber)
        {
            case 1:
                SaveManager.Instance?.RequestRestoreOnNextGameplayScene();
                LoadSceneIfAvailable(zone1SceneName, mainMenuSceneName);
                break;
            case 2:
                SaveManager.Instance?.RequestRestoreOnNextGameplayScene();
                LoadSceneIfAvailable(zone2SceneName, zone1SceneName);
                break;
            case 3:
                SaveManager.Instance?.RequestRestoreOnNextGameplayScene();
                LoadSceneIfAvailable(zone3SceneName, zone2SceneName);
                break;
        }
    }

    void EnsureDebugUnlockUi()
    {
        if (!IsDebugUiEnabled())
            return;
        if (_debugPanel != null)
            return;

        var rootRt = transform as RectTransform;
        if (rootRt == null)
            return;

        var toggle = CreateDebugButton(rootRt, "Debug", new Vector2(1f, 1f), new Vector2(-84f, -36f), new Vector2(128f, 36f));
        toggle.onClick.AddListener(ToggleDebugPanel);

        _debugPanel = new GameObject("DebugPanel");
        _debugPanel.transform.SetParent(rootRt, false);
        var panelRt = _debugPanel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(1f, 1f);
        panelRt.anchorMax = new Vector2(1f, 1f);
        panelRt.pivot = new Vector2(1f, 1f);
        panelRt.anchoredPosition = new Vector2(-16f, -78f);
        panelRt.sizeDelta = new Vector2(260f, 300f);
        var panelImage = _debugPanel.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.08f, 0.13f, 0.9f);

        var layout = _debugPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var title = CreateDebugLabel(_debugPanel.transform, "QA Debug Unlock");
        title.alignment = TextAlignmentOptions.Center;

        var btnUnlock2 = CreateDebugButton(_debugPanel.transform, "Desbloquear Z2", Vector2.zero, Vector2.zero, new Vector2(220f, 36f));
        btnUnlock2.onClick.AddListener(() =>
        {
            zoneManager?.CompleteZone2Unlock();
            RefreshAllCards();
            Debug.Log("[ZoneSelection] Debug: Zona 2 desbloqueada.");
        });

        var btnForceMiniGame = CreateDebugButton(_debugPanel.transform, "Forzar Minijuego Z2", Vector2.zero, Vector2.zero, new Vector2(220f, 36f));
        btnForceMiniGame.onClick.AddListener(() =>
        {
            if (miniGameManager == null)
            {
                Debug.LogWarning("[ZoneSelection] Debug: MiniGameManager no disponible.");
                return;
            }

            if (!miniGameManager.StartMiniGame(ZoneManager.Zone2UnlockMiniGameId, success =>
                {
                    if (!success)
                    {
                        Debug.Log("[ZoneSelection] Debug: Minijuego Z2 fallado/cancelado.");
                        return;
                    }

                    zoneManager?.CompleteZone2Unlock();
                    RefreshAllCards();
                    Debug.Log("[ZoneSelection] Debug: Minijuego Z2 completado (desbloqueo aplicado).");
                }))
            {
                Debug.Log("[ZoneSelection] Debug: Minijuego Z2 ya en ejecución o no disponible.");
            }
        });

        var btnUnlock3 = CreateDebugButton(_debugPanel.transform, "Desbloquear Z3", Vector2.zero, Vector2.zero, new Vector2(220f, 36f));
        btnUnlock3.onClick.AddListener(() =>
        {
            zoneManager?.CompleteZone3Unlock();
            RefreshAllCards();
            Debug.Log("[ZoneSelection] Debug: Zona 3 desbloqueada.");
        });

        var btnForceMiniGameZ3 = CreateDebugButton(_debugPanel.transform, "Forzar Minijuego Z3", Vector2.zero, Vector2.zero, new Vector2(220f, 36f));
        btnForceMiniGameZ3.onClick.AddListener(() =>
        {
            if (miniGameManager == null)
            {
                Debug.LogWarning("[ZoneSelection] Debug: MiniGameManager no disponible.");
                return;
            }

            if (!miniGameManager.StartMiniGame(ZoneManager.Zone3UnlockMiniGameId, success =>
                {
                    if (!success)
                    {
                        Debug.Log("[ZoneSelection] Debug: Minijuego Z3 fallado/cancelado.");
                        return;
                    }

                    zoneManager?.CompleteZone3Unlock();
                    RefreshAllCards();
                    Debug.Log("[ZoneSelection] Debug: Minijuego Z3 completado (desbloqueo aplicado).");
                }))
            {
                Debug.Log("[ZoneSelection] Debug: Minijuego Z3 ya en ejecución o no disponible.");
            }
        });

        var btnReset = CreateDebugButton(_debugPanel.transform, "Reset Bloqueos", Vector2.zero, Vector2.zero, new Vector2(220f, 36f));
        btnReset.onClick.AddListener(() =>
        {
            zoneManager?.ResetAllUnlocksForDebug();
            RefreshAllCards();
            Debug.Log("[ZoneSelection] Debug: bloqueos reiniciados.");
        });

        _debugPanel.SetActive(!debugPanelStartsHidden);
    }

    void ToggleDebugPanel()
    {
        if (_debugPanel == null)
            return;
        _debugPanel.SetActive(!_debugPanel.activeSelf);
    }

    static Button CreateDebugButton(Transform parent, string text, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject($"DebugBtn_{text}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        if (anchor != Vector2.zero)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
        }
        rt.sizeDelta = size;
        if (anchor == Vector2.zero)
        {
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = size.x;
            le.preferredHeight = size.y;
        }

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.2f, 0.28f, 0.95f);
        var btn = go.AddComponent<Button>();

        var label = new GameObject("Text");
        label.transform.SetParent(go.transform, false);
        var lrt = label.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return btn;
    }

    static TextMeshProUGUI CreateDebugLabel(Transform parent, string text)
    {
        var go = new GameObject("DebugLabel");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220f, 30f);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 220f;
        le.preferredHeight = 30f;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 16;
        tmp.color = new Color(0.92f, 0.92f, 0.92f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    void TryUnlockAndEnterZone2()
    {
        if (zoneManager == null)
            return;

        if (!zoneManager.CanAttemptZone2Unlock(out var reason))
        {
            if (!string.IsNullOrEmpty(reason))
                Debug.Log($"[ZoneSelection] Zona 2 bloqueada: {reason}");
            return;
        }

        if (miniGameManager == null)
        {
            Debug.LogWarning("[ZoneSelection] MiniGameManager no disponible para desbloqueo de Zona 2.");
            return;
        }

        miniGameManager.StartMiniGame(ZoneManager.Zone2UnlockMiniGameId, success =>
        {
            if (!success)
                return;

            zoneManager.CompleteZone2Unlock();
            RefreshAllCards();
            SaveManager.Instance?.RequestRestoreOnNextGameplayScene();
            LoadSceneIfAvailable(zone2SceneName, zone1SceneName);
        });
    }

    void TryUnlockAndEnterZone3()
    {
        if (zoneManager == null)
            return;

        if (!zoneManager.CanAttemptZone3Unlock(out var reason))
        {
            if (!string.IsNullOrEmpty(reason))
                Debug.Log($"[ZoneSelection] Zona 3 bloqueada: {reason}");
            return;
        }

        if (miniGameManager == null)
        {
            Debug.LogWarning("[ZoneSelection] MiniGameManager no disponible para desbloqueo de Zona 3.");
            return;
        }

        miniGameManager.StartMiniGame(ZoneManager.Zone3UnlockMiniGameId, success =>
        {
            if (!success)
                return;

            zoneManager.CompleteZone3Unlock();
            RefreshAllCards();
            SaveManager.Instance?.RequestRestoreOnNextGameplayScene();
            LoadSceneIfAvailable(zone3SceneName, zone2SceneName);
        });
    }

    static void LoadSceneIfAvailable(string preferredSceneName, string fallbackSceneName)
    {
        if (TryLoadScene(preferredSceneName))
            return;

        if (TryLoadScene(fallbackSceneName))
        {
            Debug.LogWarning($"[ZoneSelection] Escena objetivo no disponible, usando fallback: {fallbackSceneName}");
            return;
        }

        Debug.LogError($"[ZoneSelection] No se pudo cargar escena objetivo '{preferredSceneName}' ni fallback '{fallbackSceneName}'.");
    }

    static bool TryLoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return true;
        }

#if UNITY_EDITOR
        var scenePath = $"Assets/Scenes/{sceneName}.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
        {
            EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
            return true;
        }
#endif
        return false;
    }

    bool IsDebugUiEnabled()
    {
        if (!enableDebugUnlockUi)
            return false;

#if UNITY_EDITOR
        return true;
#else
        return Debug.isDebugBuild;
#endif
    }
}
