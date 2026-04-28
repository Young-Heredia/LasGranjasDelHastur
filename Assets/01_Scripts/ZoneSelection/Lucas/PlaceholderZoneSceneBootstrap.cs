using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Construye placeholders jugables para Zone2/Zone3 mientras esas escenas se implementan.
/// </summary>
public static class PlaceholderZoneSceneBootstrap
{
    static bool _hookInstalled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InstallSceneHook()
    {
        if (_hookInstalled)
            return;
        SceneManager.sceneLoaded += OnSceneLoaded;
        _hookInstalled = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureCurrentScenePlaceholderUi()
    {
        EnsurePlaceholderUiForScene(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsurePlaceholderUiForScene(scene);
    }

    static void EnsurePlaceholderUiForScene(Scene scene)
    {
        if (scene.name != "Zone2_Cities" && scene.name != "Zone3_Celestial")
            return;

        // If zone gameplay root already exists (manually placed or editor scaffolded), do not spawn duplicates.
        if (scene.name == "Zone2_Cities" && Object.FindFirstObjectByType<LasGranjasDelHastur.Zone2.Zone2PrototypeGame>() != null)
            return;
        if (scene.name == "Zone3_Celestial" && Object.FindFirstObjectByType<LasGranjasDelHastur.Zone3.Zone3PrototypeGame>() != null)
            return;

        EnsureCamera();
        EnsureEventSystem();

        if (scene.name == "Zone2_Cities")
        {
            LasGranjasDelHastur.Zone2.Zone2RuntimeScaffold.EnsureSceneScaffold();
            return;
        }

        if (scene.name == "Zone3_Celestial")
        {
            LasGranjasDelHastur.Zone3.Zone3RuntimeScaffold.EnsureSceneScaffold();
            return;
        }

        var root = new GameObject("PlaceholderZoneUI");
        root.AddComponent<PlaceholderZoneSceneMarker>();
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.AddComponent<GraphicRaycaster>();

        var backdrop = CreateImage(root.transform, "Backdrop", new Color(0.04f, 0.06f, 0.12f, 1f));
        StretchFull(backdrop.rectTransform);

        var panel = CreateImage(root.transform, "Panel", new Color(0.08f, 0.10f, 0.16f, 0.95f));
        var panelRt = panel.rectTransform;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(980f, 460f);
        panelRt.anchoredPosition = Vector2.zero;

        CreateLabel(panel.transform, "Title", scene.name, 42, new Vector2(0f, 130f), new Vector2(860f, 70f), TextAlignmentOptions.Center);
        CreateLabel(panel.transform, "Subtitle", "Placeholder de Fase 5 para pruebas de flujo.", 24, new Vector2(0f, 66f), new Vector2(860f, 46f), TextAlignmentOptions.Center);
        CreateLabel(panel.transform, "Body",
            "Aqui iremos montando la jugabilidad real de esta zona.\nPor ahora solo valida entrada/salida y pipeline de desbloqueo.",
            20, new Vector2(0f, -8f), new Vector2(860f, 120f), TextAlignmentOptions.Center);

        var backButton = CreateButton(panel.transform, "Volver a Zonas", new Vector2(0f, -152f), new Vector2(320f, 56f));
        backButton.onClick.AddListener(() => SceneManager.LoadScene("ZoneSelection"));
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var go = new GameObject("EventSystem");
        var es = go.AddComponent<EventSystem>();
        var bridge = go.GetComponent<LasGranjasDelHastur.EventSystemInputModuleBridge>();
        if (bridge == null)
            bridge = go.AddComponent<LasGranjasDelHastur.EventSystemInputModuleBridge>();
        bridge.EnsureCorrectInputModule();
    }

    static void EnsureCamera()
    {
        if (Camera.main != null)
            return;

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.03f, 0.06f, 1f);
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camGo.AddComponent<AudioListener>();
    }

    static Image CreateImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, int fontSize, Vector2 anchoredPos, Vector2 size, TextAlignmentOptions align)
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
        txt.text = text;
        txt.fontSize = fontSize;
        txt.alignment = align;
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

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.17f, 0.25f, 1f);

        var btn = go.AddComponent<Button>();
        var text = CreateLabel(go.transform, "Text", label, 24, Vector2.zero, size, TextAlignmentOptions.Center);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        return btn;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}

[DisallowMultipleComponent]
public class PlaceholderZoneSceneMarker : MonoBehaviour
{
}
