using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace LasGranjas.Editor
{
    /// <summary>
    /// Construye la escena ZoneSelection (UI + referencias). Ejecutar una vez desde el menú Tools.
    /// </summary>
    public static class ZoneSelectionSceneBuilder
    {
        const string ZoneScenePath = "Assets/00_Scenes/ZoneSelection.unity";
        const string MainMenuPath = "Assets/00_Scenes/MainMenu.unity";
        const string PlaceholderSpritePath = "Assets/02_Sprites/Placeholders/white_1x1.png";
        const string TempEventPrefabPath = "Assets/Editor/Temp/_LasGranjas_EventSystemFromMainMenu.prefab";

        [MenuItem("Tools/Las Granjas del Hastur/Reparar layout textos ZoneSelection")]
        public static void FixZoneSelectionTextLayout()
        {
            EnsureFolders();
            EditorSceneManager.OpenScene(ZoneScenePath, OpenSceneMode.Single);

            var row = GameObject.Find("CardsRow");
            if (row != null)
            {
                var h = row.GetComponent<HorizontalLayoutGroup>();
                if (h != null)
                {
                    h.childControlWidth = true;
                    h.childControlHeight = true;
                }
            }

            for (var i = 1; i <= 3; i++)
            {
                var card = GameObject.Find("ZoneCard_" + i);
                if (card == null)
                    continue;

                var crt = card.GetComponent<RectTransform>();
                crt.sizeDelta = new Vector2(520, 480);

                var content = card.transform.Find("Content");
                if (content == null)
                    continue;

                var vlg = content.GetComponent<VerticalLayoutGroup>();
                if (vlg == null)
                    vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();

                vlg.padding = new RectOffset(8, 8, 4, 8);
                vlg.spacing = 10;
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = true;

                FixTmpLayoutChild(content, "TitleText", 72, 72, 0);
                FixTmpLayoutChild(content, "BodyText", 160, 220, 1);
                FixTmpLayoutChild(content, "LockedHint", 52, 72, 0);

                LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LasGranjas] Layout de textos en ZoneSelection reparado (VLG + anchos de tarjeta).");
        }

        static void FixTmpLayoutChild(Transform content, string childName, float minH, float prefH, float flex)
        {
            var t = content.Find(childName);
            if (t == null)
                return;

            var rt = t.GetComponent<RectTransform>();
            StretchLayoutChild(rt);

            var le = t.GetComponent<LayoutElement>();
            if (le == null)
                le = t.gameObject.AddComponent<LayoutElement>();

            le.minHeight = minH;
            le.preferredHeight = prefH;
            le.flexibleHeight = flex;
        }

        [MenuItem("Tools/Las Granjas del Hastur/Guardar prefab ZoneCard desde escena")]
        public static void SaveZoneCardPrefab()
        {
            var go = GameObject.Find("ZoneCard_1");
            if (go == null)
            {
                Debug.LogError("[LasGranjas] No se encontró ZoneCard_1. Abre ZoneSelection y ejecuta Construir escena primero.");
                return;
            }

            EnsureFolders();
            var path = "Assets/04_Prefabs/ZoneSelection/ZoneCard.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Debug.Log("[LasGranjas] Prefab guardado en " + path);
        }

        [MenuItem("Tools/Las Granjas del Hastur/Construir escena ZoneSelection")]
        public static void BuildZoneSelectionScene()
        {
            BuildZoneSelectionSceneCore();
        }

        /// <summary>
        /// Reconstruye la UI desde cero y aplica sprites, BackgroundSolid, marco, overlay y referencias de tarjetas.
        /// Usar cuando la escena esté corrupta (escala 0, referencias rotas).
        /// </summary>
        [MenuItem("Tools/Las Granjas del Hastur/RECONSTRUIR ZoneSelection (limpio + visuales)")]
        public static void RebuildZoneSelectionFromScratch()
        {
            EnsureFolders();
            BuildZoneSelectionSceneCore();
            ZoneSelectionVisualSetup.WireSceneVisuals();
            Debug.Log("[LasGranjas] RECONSTRUCCIÓN completa: Canvas nuevo + visuales enlazados. Guarda y prueba Play.");
        }

        static void BuildZoneSelectionSceneCore()
        {
            EnsureFolders();

            EditorSceneManager.OpenScene(ZoneScenePath, OpenSceneMode.Single);

            var zoneScene = EditorSceneManager.GetActiveScene();
            DestroyAllRootObjectsNamed(zoneScene, "Canvas");
            DestroyAllEventSystemsInScene(zoneScene);

            CopyEventSystemFromMainMenu(zoneScene);
            StyleMainCamera();

            var sprite = LoadPlaceholderSprite();
            var canvas = CreateCanvasRoot();
            var controller = canvas.gameObject.AddComponent<ZoneSelectionController>();

            CreateBackground(canvas.transform, sprite);
            CreateHeader(canvas.transform);
            var cards = CreateCardsRow(canvas.transform, sprite, controller);
            CreateFooter(canvas.transform, sprite, controller);

            var so = new SerializedObject(controller);
            so.FindProperty("zoneCards").arraySize = 3;
            so.FindProperty("zoneCards").GetArrayElementAtIndex(0).objectReferenceValue = cards[0];
            so.FindProperty("zoneCards").GetArrayElementAtIndex(1).objectReferenceValue = cards[1];
            so.FindProperty("zoneCards").GetArrayElementAtIndex(2).objectReferenceValue = cards[2];
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[LasGranjas] ZoneSelection construida (base). Si quieres arte final: ejecuta RECONSTRUIR (limpio + visuales).");
        }

        static void EnsureFolders()
        {
            foreach (var p in new[]
                     {
                         "Assets/01_Scripts/UI", "Assets/01_Scripts/ZoneSelection", "Assets/01_Scripts/SceneManagement",
                         "Assets/04_Prefabs/UI", "Assets/04_Prefabs/ZoneSelection", "Assets/02_Sprites/Placeholders", "Assets/03_AUDIO/SFX"
                     })
            {
                if (!AssetDatabase.IsValidFolder(p))
                {
                    var parts = p.Split('/');
                    var cur = "Assets";
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var next = cur + "/" + parts[i];
                        if (!AssetDatabase.IsValidFolder(next))
                            AssetDatabase.CreateFolder(cur, parts[i]);
                        cur = next;
                    }
                }
            }
        }

        static void DestroyAllRootObjectsNamed(Scene scene, string objectName)
        {
            if (!scene.IsValid())
                return;
            var toDestroy = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root != null && root.name == objectName)
                    toDestroy.Add(root);
            }

            foreach (var go in toDestroy)
                UnityEngine.Object.DestroyImmediate(go);
        }

        static void DestroyAllEventSystemsInScene(Scene scene)
        {
            if (!scene.IsValid())
                return;
            var seen = new HashSet<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var es in root.GetComponentsInChildren<EventSystem>(true))
                {
                    if (es == null)
                        continue;
                    var go = es.gameObject;
                    if (seen.Add(go))
                        UnityEngine.Object.DestroyImmediate(go);
                }
            }
        }

        /// <summary>
        /// Copia el EventSystem de MainMenu sin cargar dos escenas a la vez (evita el error de 2 Global Light 2D).
        /// Guarda Zone antes de abrir MainMenu en Single; el clon se hace vía prefab temporal.
        /// </summary>
        static void CopyEventSystemFromMainMenu(Scene zoneScene)
        {
            if (!zoneScene.IsValid())
                return;

            DestroyAllEventSystemsInScene(zoneScene);
            EditorSceneManager.SaveScene(zoneScene, ZoneScenePath);

            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            var esGo = GameObject.Find("EventSystem");

            EnsureEditorTempFolder();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(TempEventPrefabPath) != null)
                AssetDatabase.DeleteAsset(TempEventPrefabPath);

            var savedPrefab = false;
            if (esGo != null)
            {
                try
                {
                    PrefabUtility.SaveAsPrefabAsset(esGo, TempEventPrefabPath);
                    AssetDatabase.Refresh();
                    savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TempEventPrefabPath) != null;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[LasGranjas] No se pudo guardar prefab temporal del EventSystem: " + e.Message);
                }
            }
            else
                Debug.LogWarning("[LasGranjas] MainMenu no tiene un GameObject llamado 'EventSystem'.");

            EditorSceneManager.OpenScene(ZoneScenePath, OpenSceneMode.Single);
            var zone = EditorSceneManager.GetSceneByPath(ZoneScenePath);
            DestroyAllEventSystemsInScene(zone);

            var ok = false;
            if (savedPrefab)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TempEventPrefabPath);
                if (prefab != null)
                {
                    var inst = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    if (inst != null)
                    {
                        SceneManager.MoveGameObjectToScene(inst, zone);
                        inst.name = "EventSystem";
                        ok = true;
                    }
                }
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(TempEventPrefabPath) != null)
                AssetDatabase.DeleteAsset(TempEventPrefabPath);

            if (!ok)
            {
                DestroyAllEventSystemsInScene(zone);
                var dest = new GameObject("EventSystem");
                dest.AddComponent<EventSystem>();
                dest.AddComponent<InputSystemUIInputModule>();
                Debug.LogWarning(
                    "[LasGranjas] EventSystem en ZoneSelection creado con valores por defecto (copia desde MainMenu falló o no disponible).");
            }
        }

        static void EnsureEditorTempFolder()
        {
            if (AssetDatabase.IsValidFolder("Assets/Editor/Temp"))
                return;
            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                AssetDatabase.CreateFolder("Assets", "Editor");
            AssetDatabase.CreateFolder("Assets/Editor", "Temp");
        }

        static void StyleMainCamera()
        {
            var cam = GameObject.Find("Main Camera");
            if (cam == null)
                return;
            var c = cam.GetComponent<Camera>();
            if (c == null)
                return;
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.06f, 0.04f, 0.1f, 1f);
        }

        static Sprite LoadPlaceholderSprite()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(PlaceholderSpritePath);
            var sp = assets.OfType<Sprite>().FirstOrDefault();
            if (sp == null)
                Debug.LogWarning("[LasGranjas] No se encontró sprite en " + PlaceholderSpritePath + ". Asigna uno en las imágenes UI.");
            return sp;
        }

        static RectTransform CreateCanvasRoot()
        {
            var go = new GameObject("Canvas", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            var rt = go.GetComponent<RectTransform>();
            SetFullStretch(rt);
            rt.localScale = Vector3.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            return rt;
        }

        static void CreateBackground(Transform parent, Sprite sprite)
        {
            var go = CreateUIObject("Background", parent, sprite, new Color(0.07f, 0.05f, 0.11f, 1f));
            var rt = go.GetComponent<RectTransform>();
            SetFullStretch(rt);
            var img = go.GetComponent<Image>();
            if (img != null)
                img.raycastTarget = false;
            rt.SetAsFirstSibling();
        }

        static void CreateHeader(Transform parent)
        {
            var root = CreateUIObject("Header", parent, null, new Color(0, 0, 0, 0));
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(1600, 220);
            rt.anchoredPosition = new Vector2(0, -40);

            var title = CreateTMP("Title", root.transform, "Selección de Zona", 52, FontStyles.Bold, TextAlignmentOptions.Center);
            StretchTop(title.rectTransform, 0, 0, 80);

            var sub = CreateTMP("Subtitle", root.transform,
                "Elige el territorio que administrarás para Hastur", 22, FontStyles.Normal, TextAlignmentOptions.Center);
            var srt = sub.rectTransform;
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = new Vector2(40, 0);
            srt.offsetMax = new Vector2(-40, -90);
        }

        static ZoneCardUI[] CreateCardsRow(Transform parent, Sprite sprite, ZoneSelectionController controller)
        {
            var row = CreateUIObject("CardsRow", parent, null, new Color(0, 0, 0, 0));
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1720, 520);
            rt.anchoredPosition = new Vector2(0, -20);

            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 28;
            h.padding = new RectOffset(24, 24, 16, 16);
            h.childAlignment = TextAnchor.MiddleCenter;
            // Si childControlWidth es false, las tarjetas pueden quedar con ancho 0.
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            var cards = new ZoneCardUI[3];
            cards[0] = CreateZoneCard(row.transform, sprite, controller, 1,
                "Zona 1 – Calabozos",
                "Produce almas y energía pura.\nPrimer territorio del jugador.",
                "",
                nameof(ZoneSelectionController.EnterZone1));

            cards[1] = CreateZoneCard(row.transform, sprite, controller, 2,
                "Zona 2 – Ciudades",
                "Próximo frente de culto y tributo.",
                "Bloqueada. Requiere progreso en Zona 1.",
                nameof(ZoneSelectionController.EnterZone2));

            cards[2] = CreateZoneCard(row.transform, sprite, controller, 3,
                "Zona 3 – Cuerpos Celestes",
                "El cielo también debe arder.",
                "Bloqueada. Requiere progreso en Zona 2.",
                nameof(ZoneSelectionController.EnterZone3));

            return cards;
        }

        static ZoneCardUI CreateZoneCard(Transform parent, Sprite sprite, ZoneSelectionController controller, int zoneNum,
            string title, string desc, string lockedHint, string enterMethod)
        {
            var card = CreateUIObject("ZoneCard_" + zoneNum, parent, sprite, new Color(0.42f, 0.36f, 0.52f, 0.95f));
            var cardRt = card.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(520, 480);

            var le = card.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 520;
            le.preferredHeight = 480;
            le.minWidth = 400;
            le.minHeight = 420;

            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = card.gameObject.GetComponent<Image>();
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.55f, 0.48f, 0.65f, 1f);
            colors.pressedColor = new Color(0.35f, 0.3f, 0.45f, 1f);
            btn.colors = colors;

            var uiAudio = card.gameObject.AddComponent<BasicUIAudio>();
            uiAudio.useAudioManagerFirst = true;

            var zc = card.gameObject.AddComponent<ZoneCardUI>();
            var zso = new SerializedObject(zc);
            zso.FindProperty("zoneNumber").intValue = zoneNum;
            zso.FindProperty("title").stringValue = title;
            zso.FindProperty("description").stringValue = desc;
            zso.FindProperty("lockedHint").stringValue = lockedHint;
            zso.FindProperty("zoneButton").objectReferenceValue = btn;
            zso.FindProperty("uiAudio").objectReferenceValue = uiAudio;
            zso.ApplyModifiedPropertiesWithoutUndo();

            var content = CreateUIObject("Content", card.transform, null, new Color(0, 0, 0, 0));
            var crt = content.GetComponent<RectTransform>();
            SetFullStretch(crt);
            crt.offsetMin = new Vector2(24, 24);
            crt.offsetMax = new Vector2(-24, -24);

            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 4, 8);
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            var titleTmp = CreateTMP("TitleText", content.transform, title, 30, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            StretchLayoutChild(titleTmp.rectTransform);
            var leTitle = titleTmp.gameObject.AddComponent<LayoutElement>();
            leTitle.minHeight = 72;
            leTitle.preferredHeight = 72;
            leTitle.flexibleHeight = 0;

            var body = CreateTMP("BodyText", content.transform, desc, 20, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            StretchLayoutChild(body.rectTransform);
            var leBody = body.gameObject.AddComponent<LayoutElement>();
            leBody.minHeight = 160;
            leBody.preferredHeight = 220;
            leBody.flexibleHeight = 1;

            var locked = CreateTMP("LockedHint", content.transform, lockedHint, 18, FontStyles.Italic, TextAlignmentOptions.BottomLeft);
            StretchLayoutChild(locked.rectTransform);
            var leLocked = locked.gameObject.AddComponent<LayoutElement>();
            leLocked.minHeight = 52;
            leLocked.preferredHeight = 72;
            leLocked.flexibleHeight = 0;

            var lockGo = CreateUIObject("LockOverlay", card.transform, sprite, new Color(0, 0, 0, 0.35f));
            var lockRt = lockGo.GetComponent<RectTransform>();
            SetFullStretch(lockRt);
            var lockImg = lockGo.GetComponent<Image>();
            if (lockImg != null)
                lockImg.raycastTarget = false;
            lockGo.SetActive(false);

            var cg = card.gameObject.AddComponent<CanvasGroup>();

            zso = new SerializedObject(zc);
            zso.FindProperty("titleText").objectReferenceValue = titleTmp;
            zso.FindProperty("descriptionText").objectReferenceValue = body;
            zso.FindProperty("lockedHintText").objectReferenceValue = locked;
            zso.FindProperty("lockOverlay").objectReferenceValue = lockGo;
            zso.FindProperty("cardCanvasGroup").objectReferenceValue = cg;
            zso.ApplyModifiedPropertiesWithoutUndo();

            SetButtonVoidListener(btn, controller, enterMethod);
            return zc;
        }

        static void CreateFooter(Transform parent, Sprite sprite, ZoneSelectionController controller)
        {
            var foot = CreateUIObject("Footer", parent, null, new Color(0, 0, 0, 0));
            var rt = foot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(1200, 140);
            rt.anchoredPosition = new Vector2(0, 24);

            var back = CreateUIObject("BackButton", foot.transform, sprite, new Color(0.32f, 0.26f, 0.4f, 1f));
            var brt = back.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(320, 64);
            brt.anchoredPosition = new Vector2(0, 28);

            var bbtn = back.gameObject.AddComponent<Button>();
            bbtn.targetGraphic = back.gameObject.GetComponent<Image>();

            var backAudio = back.gameObject.AddComponent<BasicUIAudio>();
            backAudio.useAudioManagerFirst = true;

            var label = CreateTMP("Label", back.transform, "Volver al menú", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            SetFullStretch(label.rectTransform);

            SetButtonVoidListener(bbtn, controller, nameof(ZoneSelectionController.GoToMainMenu));

            var flavor = CreateTMP("FlavorText", foot.transform,
                "Los pactos se firman con paciencia. El vacío observa.", 16, FontStyles.Italic, TextAlignmentOptions.Center);
            var frt = flavor.rectTransform;
            frt.anchorMin = new Vector2(0.5f, 0f);
            frt.anchorMax = new Vector2(0.5f, 0f);
            frt.pivot = new Vector2(0.5f, 0f);
            frt.sizeDelta = new Vector2(900, 36);
            frt.anchoredPosition = new Vector2(0, 6);
        }

        static GameObject CreateUIObject(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            if (sprite != null)
            {
                var img = go.AddComponent<Image>();
                img.sprite = sprite;
                img.color = color;
                img.type = Image.Type.Simple;
            }

            return go;
        }

        static TextMeshProUGUI CreateTMP(string name, Transform parent, string text, float size, FontStyles style,
            TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = new Color(0.93f, 0.9f, 0.85f, 1f);
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        static void StretchTop(RectTransform rt, float left, float right, float height)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -height);
            rt.offsetMax = new Vector2(-right, 0);
        }

        /// <summary>Hijo de VerticalLayoutGroup: ancho completo, altura la fija el layout.</summary>
        static void StretchLayoutChild(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        static void SetButtonVoidListener(Button button, UnityEngine.Object target, string methodName)
        {
            var so = new SerializedObject(button);
            var calls = so.FindProperty("m_OnClick").FindPropertyRelative("m_PersistentCalls.m_Calls");
            calls.ClearArray();
            calls.arraySize = 1;
            var e = calls.GetArrayElementAtIndex(0);
            e.FindPropertyRelative("m_Target").objectReferenceValue = target;
            e.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue =
                $"{target.GetType().Name}, Assembly-CSharp";
            e.FindPropertyRelative("m_MethodName").stringValue = methodName;
            e.FindPropertyRelative("m_Mode").intValue = 1;
            e.FindPropertyRelative("m_CallState").intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
