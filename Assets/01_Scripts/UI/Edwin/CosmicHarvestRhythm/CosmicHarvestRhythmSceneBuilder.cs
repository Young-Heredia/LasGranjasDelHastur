#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System;
using System.Collections.Generic;
using LasGranjasDelHastur.Zone1;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LasGranjasDelHastur.UI.Edwin
{
    /// <summary>
    /// Dev layout mockup for the Cosmic Harvest Rhythm minigame.
    /// All player-visible copy is Spanish; code, identifiers, and comments remain in English.
    /// In the editor, markup is reconciled (<see cref="EnsureEditorHierarchy"/>): full rebuild if <c>RootLayout</c> is missing or the saved hierarchy is stale (missing expected nodes such as Estado del Ritual card), plus a scene-load hook under <c>Editor/</c>. Save the scene after it self-heals so the Hierarchy persists without entering Play Mode.
    /// Use context menu Rebuild to regenerate from scratch after changing strings.
    /// Future: bridge hits to ResourceManager / TaxManager / corruption (not wired here).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class CosmicHarvestRhythmSceneBuilder : MonoBehaviour
    {
        [SerializeField] bool _layoutBuilt;

        [Tooltip("If set, used as fullscreen background (persisted with the scene / prefab). Falls back to file load.")]
        [SerializeField] Sprite _backgroundSprite;

        [Tooltip("If set, How to Play card sprite (persisted with the scene / prefab). Falls back to file load.")]
        [SerializeField] Sprite _howToPlayCardSprite;

        [Tooltip("If set, Estado del Ritual card sprite (persisted with the scene / prefab). Falls back to file load.")]
        [SerializeField] Sprite _stateOfRitualCardSprite;

#if UNITY_EDITOR
        bool _pendingEditorEnsure;
#endif

        bool HasBuiltUi()
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).name == "RootLayout")
                    return true;
            }
            return false;
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EnsureEditorHierarchy();
                return;
            }
#endif
            if (HasBuiltUi())
                return;
            BuildNow();
            _layoutBuilt = true;
            MarkDirty();
        }

        void Start()
        {
            if (!Application.isPlaying)
                return;
            var canvasRt = GetComponent<RectTransform>();
            EnsureGameplayHost(canvasRt);
        }

        static void EnsureGameplayHost(RectTransform canvasRt)
        {
            if (canvasRt == null || canvasRt.Find("CosmicHarvestRhythmGameplay") != null)
                return;
            var host = new GameObject("CosmicHarvestRhythmGameplay");
            host.transform.SetParent(canvasRt, false);
            host.AddComponent<CosmicHarvestRhythmGameplayController>().Initialize(canvasRt);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (Application.isPlaying)
                return;
            if (_pendingEditorEnsure)
                return;
            _pendingEditorEnsure = true;
            EditorApplication.delayCall += FlushEditorEnsure;
        }

        void FlushEditorEnsure()
        {
            _pendingEditorEnsure = false;
            if (this == null)
                return;
            EnsureEditorHierarchy();
        }

        void Awake()
        {
            if (Application.isPlaying)
                return;
            EditorApplication.delayCall += DelayedEnsureAfterAwake;
        }

        void DelayedEnsureAfterAwake()
        {
            if (this == null || Application.isPlaying)
                return;
            EnsureEditorHierarchy();
        }

        /// <summary>Rebuilds markup in Edit Mode when <c>RootLayout</c> is missing or the persisted UI no longer matches the current builder blueprint.</summary>
        public void EnsureEditorHierarchy()
        {
            if (Application.isPlaying)
                return;
            if (!enabled)
                return;
            if (HasBuiltUi())
            {
                if (!EditorHierarchyHasBlueprintStateRitualCard())
                {
                    ClearBuiltChildren();
                    BuildNow();
                    _layoutBuilt = true;
                    MarkDirty();
                    return;
                }

                EditorRefreshBackgroundSprite();
                if (!_layoutBuilt)
                {
                    _layoutBuilt = true;
                    EditorUtility.SetDirty(this);
                }
                return;
            }

            ClearBuiltChildren();
            BuildNow();
            _layoutBuilt = true;
            MarkDirty();
        }

        /// <remarks>Stale scenes that already had <c>RootLayout</c> skip rebuild unless we probe for blueprint nodes (<see cref="RhythmStateRitualCardRowHierarchyName"/> under <c>RightPanels</c>).</remarks>
        static Transform EditorTryGetDirectChild(Transform parent, string childName)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var ch = parent.GetChild(i);
                if (ch.name == childName)
                    return ch;
            }

            return null;
        }

        bool EditorHierarchyHasBlueprintStateRitualCard()
        {
            var rootLayout = EditorTryGetDirectChild(transform, "RootLayout");
            if (rootLayout == null)
                return false;
            var midRow = EditorTryGetDirectChild(rootLayout, "MidRow");
            if (midRow == null)
                return false;
            var rightPanels = EditorTryGetDirectChild(midRow, "RightPanels");
            if (rightPanels == null)
                return false;
            for (var i = 0; i < rightPanels.childCount; i++)
            {
                if (rightPanels.GetChild(i).name == RhythmStateRitualCardRowHierarchyName)
                    return true;
            }

            return false;
        }

        void EditorRefreshBackgroundSprite()
        {
            if (Application.isPlaying)
                return;
            Transform bgTf = null;
            for (var i = 0; i < transform.childCount; i++)
            {
                var c = transform.GetChild(i);
                if (c.name == "Background")
                {
                    bgTf = c;
                    break;
                }
            }

            ApplyRhythmBackground(bgTf != null ? bgTf.GetComponent<Image>() : null);
        }
#endif

        const string RhythmBackgroundAssetPath =
            "Assets/Resources/Edwin/CosmicHarvestRhythm/cosmic-harvest-rhythm-bg-preview.png";

        const string RhythmResourceCardAssetPath =
            "Assets/Resources/Edwin/CosmicHarvestRhythm/Resource/resource-card.png";
        const string RhythmIconPureEnergy =
            "Assets/Resources/Edwin/CosmicHarvestRhythm/Resource/resource-pure-energy-icon.png";
        const string RhythmIconWeakSouls =
            "Assets/Resources/Edwin/CosmicHarvestRhythm/Resource/resource-weak-souls-icon.png";
        const string RhythmIconDarkCoins =
            "Assets/Resources/Edwin/CosmicHarvestRhythm/Resource/resource-dark-coins-icon.png";
        const string RhythmIconMemoryFragments =
            "Assets/Resources/Edwin/CosmicHarvestRhythm/Resource/resource-fragments-memory-icon.png";
        const string RhythmIconUnstableSouls =
            "Assets/Resources/Edwin/CosmicHarvestRhythm/Resource/resource-unstable-souls-icon.png";

        const string RhythmHowToPlayCardAssetPath =
            "Assets/Resources/Edwin/CosmicHarvestRhythm/HowToPlay/how-play-card.png";
        const string RhythmHowToPlayIconPureEnergy =
            "Assets/Resources/Edwin/CosmicHarvestRhythm/HowToPlay/how-play-generate-pure-energy-icon.png";
        const string RhythmHowToPlayIconWeakSouls =
            "Assets/Resources/Edwin/CosmicHarvestRhythm/HowToPlay/how-play-generate-weak-souls-icon.png";
        const string RhythmHowToPlayIconDarkCoinsMemory =
            "Assets/Resources/Edwin/CosmicHarvestRhythm/HowToPlay/how-play-generate-dark-coins-memory-fragments-icon.png";
        const string RhythmHowToPlayIconUnstableSoulsCombo =
            "Assets/Resources/Edwin/CosmicHarvestRhythm/HowToPlay/how-play-perfect-combo-can-grant-unstable-souls-icon.png";

        const string RhythmStateRitualCardAssetPath =
            "Assets/Resources/Edwin/CosmicHarvestRhythm/StateOfTheRitual/state-ritual-card.png";
        const string RhythmStateRitualIconNormalStable =
            "Assets/Resources/Edwin/CosmicHarvestRhythm/StateOfTheRitual/state-ritual-normal-stable-icon.png";

        /// <summary>Row object under RightPanels wrapping <c>StateOfTheRitualCard</c>; must match persisted hierarchy probes.</summary>
        const string RhythmStateRitualCardRowHierarchyName = "StateOfTheRitualCard_Row";

        static Color BgVoid => new(0.04f, 0.03f, 0.08f, 1f);
        static Color PanelFrame => new(0.12f, 0.09f, 0.18f, 0.94f);
        static Color AccentCyan => new(0.45f, 0.92f, 0.98f, 1f);
        static Color AccentViolet => new(0.72f, 0.55f, 0.98f, 1f);
        static Color AccentGold => new(0.95f, 0.82f, 0.45f, 1f);
        /// <summary>In-card highlight for unstable / risk line (paired with unstable-souls icon).</summary>
        static Color HowPlayRiskRed => new(0.92f, 0.36f, 0.41f, 1f);
        static Color RitualStateStableGreen => new(0.38f, 0.94f, 0.55f, 1f);
        static Color RitualProgressGreen => new(0.38f, 0.74f, 0.42f, 1f);

        /// <summary>TMP rich-text color span; keep inner text free of markup.</summary>
        static string TmpRichColorSpan(Color tint, string inner) =>
            $"<color=#{ColorUtility.ToHtmlStringRGBA(tint)}>{inner}</color>";

        [ContextMenu("Rebuild rhythm mockup UI")]
        void RebuildMockupUi()
        {
            ClearBuiltChildren();
            _layoutBuilt = false;
            BuildNow();
            _layoutBuilt = true;
            MarkDirty();
        }

        void MarkDirty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                if (gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }

        void ClearBuiltChildren()
        {
            var t = transform;
            for (var i = t.childCount - 1; i >= 0; i--)
            {
                var ch = t.GetChild(i).gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(ch);
                else
#endif
                    Destroy(ch);
            }
        }

        Sprite ResolveRhythmBackgroundSprite()
        {
            if (_backgroundSprite != null)
                return _backgroundSprite;

            var fromDisk = Zone1ArtProvider.LoadSprite(RhythmBackgroundAssetPath);
            if (fromDisk != null)
                return fromDisk;

            var fromResources =
                Resources.LoadAll<Sprite>("Edwin/CosmicHarvestRhythm/cosmic-harvest-rhythm-bg-preview");
            return fromResources != null && fromResources.Length > 0 ? fromResources[0] : null;
        }

        void ApplyRhythmBackground(Image img)
        {
            if (img == null)
                return;
            var sprite = ResolveRhythmBackgroundSprite();
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Simple;
                img.color = Color.white;
                img.preserveAspect = false;
            }
            else
                img.color = BgVoid;
        }

        void BuildNow()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
                canvas.vertexColorAlwaysGammaSpace = true;

            var bgImg = AddImage(transform, "Background", BgVoid);
            StretchFull(bgImg.rectTransform);
            ApplyRhythmBackground(bgImg);

            var root = AddRect(transform, "RootLayout");
            StretchFull(root);
            var rootV = root.gameObject.AddComponent<VerticalLayoutGroup>();
            rootV.padding = new RectOffset(20, 20, 16, 14);
            rootV.spacing = 10f;
            rootV.childAlignment = TextAnchor.UpperCenter;
            rootV.childControlHeight = true;
            rootV.childControlWidth = true;
            rootV.childForceExpandHeight = false;
            rootV.childForceExpandWidth = true;

            BuildHeader(root);
            BuildMidRow(root);
            BuildBottomHud(root);
        }

        void BuildHeader(Transform parent)
        {
            var row = AddRect(parent, "Header");
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 72f;
            le.flexibleHeight = 0f;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.padding = new RectOffset(8, 8, 4, 4);

            var titleBlock = AddRect(row, "TitleBlock");
            var tbV = titleBlock.gameObject.AddComponent<VerticalLayoutGroup>();
            tbV.spacing = 4f;
            tbV.childAlignment = TextAnchor.MiddleCenter;
            tbV.childControlHeight = true;
            tbV.childControlWidth = true;
            titleBlock.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            AddTmp(titleBlock, "Ritmo de la Cosecha Cósmica", 30, FontStyles.Bold, AccentGold, TextAlignmentOptions.Center);
            AddTmp(
                titleBlock,
                "Sincroniza el ritual. Extrae. Cosecha. Obedece.",
                16,
                FontStyles.Italic,
                new Color(0.78f, 0.76f, 0.88f),
                TextAlignmentOptions.Center);
        }

        void BuildMidRow(Transform parent)
        {
            var row = AddRect(parent, "MidRow");
            var rowLe = row.gameObject.AddComponent<LayoutElement>();
            rowLe.flexibleHeight = 1f;
            rowLe.minHeight = 420f;
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12f;
            h.childAlignment = TextAnchor.UpperCenter;
            h.childControlHeight = true;
            h.childControlWidth = true;
            h.childForceExpandHeight = true;
            h.childForceExpandWidth = false;

            BuildLeftPanels(row);
            BuildLaneBlock(row);
            BuildRightPanels(row);
        }

        static void ApplySpritePanelStyle(Image img, Sprite spr)
        {
            if (img == null || spr == null)
                return;
            img.sprite = spr;
            img.color = Color.white;
            img.preserveAspect = false;
            var b = spr.border;
            img.type = b.sqrMagnitude > 0.001f ? Image.Type.Sliced : Image.Type.Simple;
        }

        /// <returns>
        /// Transform for vertical content (titles + rows). Uses <paramref name="cardBgAssetPath"/> /
        /// <paramref name="serializedCardBgOverride"/> for Background; row reserves layout slot; card RectTransform stays manually resizable.
        /// </returns>
        /// <param name="serializedCardBgOverride">Optional scene-assigned sprite; when null, loads from <paramref name="cardBgAssetPath"/>.</param>
        /// <param name="cardRootAnchoredPosition">Anchored placement of the bare card RectTransform inside the layout row (matches scene-tuned offsets).</param>
        Transform BuildPortraitStackedCard(
            Transform parent,
            string rowHierarchyName,
            string cardHierarchyName,
            string cardBgAssetPath,
            float layoutRowMinHeight,
            float layoutRowPreferredHeight,
            Vector2 initialCardRootSizeDelta,
            RectOffset contentPadding,
            float contentVerticalSpacing,
            Sprite serializedCardBgOverride,
            Vector2? cardRootAnchoredPosition)
        {
            var row = AddRect(parent, rowHierarchyName);
            var rowLe = row.gameObject.AddComponent<LayoutElement>();
            rowLe.flexibleWidth = 1f;
            rowLe.flexibleHeight = 0f;
            rowLe.minHeight = layoutRowMinHeight;
            rowLe.preferredHeight = layoutRowPreferredHeight;

            var cardRoot = AddRect(row.transform, cardHierarchyName);
            var rt = cardRoot;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = cardRootAnchoredPosition ?? Vector2.zero;
            rt.sizeDelta = initialCardRootSizeDelta;

            var cardBg = AddImage(cardRoot.transform, "Background", Color.white);
            StretchFull(cardBg.rectTransform);
            var cardSpr = serializedCardBgOverride != null ? serializedCardBgOverride : Zone1ArtProvider.LoadSprite(cardBgAssetPath);
            if (cardSpr != null)
                ApplySpritePanelStyle(cardBg, cardSpr);
            else
                cardBg.color = new Color(0.08f, 0.06f, 0.12f, 0.94f);

            var content = AddRect(cardRoot.transform, "Content");
            StretchFull(content);
            var v = EnsureVertical(content.gameObject);
            v.padding = contentPadding;
            v.spacing = contentVerticalSpacing;

            return content.transform;
        }

        void BuildResourcesObtainedCard(Transform parent)
        {
            var content = BuildPortraitStackedCard(
                parent,
                "ResourcesObtainedCard_Row",
                "ResourcesObtainedCard",
                RhythmResourceCardAssetPath,
                layoutRowMinHeight: 260f,
                layoutRowPreferredHeight: 280f,
                initialCardRootSizeDelta: new Vector2(300f, 260f),
                contentPadding: new RectOffset(14, 14, 12, 14),
                contentVerticalSpacing: 4f,
                serializedCardBgOverride: null,
                cardRootAnchoredPosition: new Vector2(-155f, 94f));

            AddSectionTitle(content, "Recursos obtenidos");

            AddResourceStatRow(content, RhythmIconPureEnergy, "PureEnergy", "Energía pura", "0");
            AddResourceStatRow(content, RhythmIconWeakSouls, "WeakSouls", "Almas débiles", "0");
            AddResourceStatRow(content, RhythmIconDarkCoins, "DarkCoins", "Monedas oscuras", "0");
            AddResourceStatRow(content, RhythmIconMemoryFragments, "MemoryFragments", "Fragmentos de recuerdo", "0");
            AddResourceStatRow(content, RhythmIconUnstableSouls, "UnstableSouls", "Almas inestables", "0");
        }

        void BuildHowToPlayCard(Transform parent)
        {
            var cyan = AccentCyan;
            var violet = AccentViolet;
            var gold = AccentGold;
            var risk = HowPlayRiskRed;

            var content = BuildPortraitStackedCard(
                parent,
                "HowToPlayCard_Row",
                "HowToPlayCard",
                RhythmHowToPlayCardAssetPath,
                layoutRowMinHeight: 300f,
                layoutRowPreferredHeight: 360f,
                initialCardRootSizeDelta: new Vector2(296f, 420f),
                contentPadding: new RectOffset(14, 14, 12, 14),
                contentVerticalSpacing: 6f,
                serializedCardBgOverride: _howToPlayCardSprite,
                cardRootAnchoredPosition: new Vector2(-155.9f, 61f));

            AddSectionTitle(content, "CÓMO JUGAR");

            AddHowToPlayIconRow(
                content,
                RhythmHowToPlayIconPureEnergy,
                "GeneratePureEnergy",
                $"Click izquierdo genera {TmpRichColorSpan(cyan, "Energía pura")}.");
            AddHowToPlayIconRow(
                content,
                RhythmHowToPlayIconWeakSouls,
                "GenerateWeakSouls",
                $"Click derecho genera {TmpRichColorSpan(violet, "Almas débiles")}.");
            AddHowToPlayIconRow(
                content,
                RhythmHowToPlayIconDarkCoinsMemory,
                "DarkCoinsMemoryFragments",
                $"Ambos clicks generan {TmpRichColorSpan(gold, "Monedas oscuras")} o {TmpRichColorSpan(gold, "Fragmentos de recuerdo")}.");
            AddHowToPlayIconRow(
                content,
                RhythmHowToPlayIconUnstableSoulsCombo,
                "PerfectComboUnstableSouls",
                $"{TmpRichColorSpan(risk, "Perfect Combo")} puede otorgar {TmpRichColorSpan(risk, "Almas inestables")}.");
        }

        void AddResourceStatRow(Transform parent, string iconAssetPath, string hierarchyId, string labelEs, string amount)
        {
            var row = AddRect(parent, $"ResourceRow_{hierarchyId}");
            var rowH = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowH.spacing = 10f;
            rowH.padding = new RectOffset(0, 0, 2, 2);
            rowH.childAlignment = TextAnchor.MiddleLeft;
            rowH.childControlHeight = true;
            rowH.childControlWidth = true;
            rowH.childForceExpandHeight = false;
            rowH.childForceExpandWidth = false;

            var iconImg = AddImage(row.transform, "Icon", new Color(0.4f, 0.4f, 0.45f, 0.35f));
            var iconSpr = Zone1ArtProvider.LoadSprite(iconAssetPath);
            if (iconSpr != null)
            {
                iconImg.sprite = iconSpr;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
            }

            var iconLe = iconImg.gameObject.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 36f;
            iconLe.preferredHeight = 36f;
            iconLe.minWidth = 36f;
            iconLe.minHeight = 36f;
            iconLe.flexibleWidth = 0f;
            iconLe.flexibleHeight = 0f;

            var line = $"{labelEs} · {amount}";
            var tmp = AddTmp(row.transform, line, 15, FontStyles.Normal, new Color(0.92f, 0.92f, 0.96f), TextAlignmentOptions.Left);
            tmp.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        void AddHowToPlayIconRow(Transform parent, string iconAssetPath, string hierarchyId, string captionTmpRichText)
        {
            var row = AddRect(parent, $"HowToPlayRow_{hierarchyId}");
            var rowH = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowH.spacing = 10f;
            rowH.padding = new RectOffset(0, 0, 2, 2);
            rowH.childAlignment = TextAnchor.MiddleLeft;
            rowH.childControlHeight = true;
            rowH.childControlWidth = true;
            rowH.childForceExpandHeight = false;
            rowH.childForceExpandWidth = false;

            var iconImg = AddImage(row.transform, "Icon", new Color(0.4f, 0.4f, 0.45f, 0.35f));
            var iconSpr = Zone1ArtProvider.LoadSprite(iconAssetPath);
            if (iconSpr != null)
            {
                iconImg.sprite = iconSpr;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
            }

            var iconLe = iconImg.gameObject.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 36f;
            iconLe.preferredHeight = 36f;
            iconLe.minWidth = 36f;
            iconLe.minHeight = 36f;
            iconLe.flexibleWidth = 0f;
            iconLe.flexibleHeight = 0f;

            var tmp = AddTmpForRichMarkup(
                row.transform,
                TmpSafeGlyphs(captionTmpRichText),
                14,
                FontStyles.Normal,
                new Color(0.92f, 0.92f, 0.96f),
                TextAlignmentOptions.Left);
            tmp.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        void BuildLeftPanels(Transform midRow)
        {
            var col = PanelColumn(midRow, "LeftPanels", minWidth: 300f, flex: true);
            var v = EnsureVertical(col.gameObject);

            BuildResourcesObtainedCard(v.transform);
            BuildHowToPlayCard(v.transform);

            AddSectionTitle(v.transform, "Bucle de partida (lore)");
            AddBody(
                v.transform,
                "Cada compás: extracción dimensional — drena almas, estabiliza portales, cosecha energía."
            );

            Spacer(v.transform, 8f);

            AddSectionTitle(v.transform, "Estado ritual -> botín (concepto)");
            AddBody(
                v.transform,
                "Normal · sesgo a energía pura\n" +
                "Ritual corrupto · sesgo a almas inestables\n" +
                "Ventana de combo perfecto · pico de fragmentos\n" +
                "Evento cósmico · pico monedas x2"
            );
        }

        void BuildLaneBlock(Transform midRow)
        {
            var wrap = PanelColumn(midRow, "LaneBlock", minWidth: 520f, flex: true);
            var v = EnsureVertical(wrap.gameObject);

            AddSectionTitle(v.transform, "Carriles (runas / glifos)");
            AddBody(v.transform, "Marcador: tres carriles que bajan hacia un ojo cósmico / portal.");

            var lanesRow = AddRect(v.transform, "LaneColumns");
            var lanesLe = lanesRow.gameObject.AddComponent<LayoutElement>();
            lanesLe.preferredHeight = 220f;
            lanesLe.flexibleHeight = 0f;
            var laneH = lanesRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            laneH.spacing = 10f;
            laneH.childAlignment = TextAnchor.MiddleCenter;
            laneH.childControlHeight = true;
            laneH.childControlWidth = true;
            laneH.childForceExpandWidth = true;

            LaneColumn(laneH.transform, "LaneLeft", "Izquierda · Energía pura", AccentCyan, "Clic izquierdo");
            LaneColumn(laneH.transform, "LaneBoth", "Ambos · Monedas / fragmento", AccentViolet, "Ambos clics");
            LaneColumn(laneH.transform, "LaneRight", "Derecha · Almas débiles", AccentGold, "Clic derecho");

            AddBody(
                v.transform,
                "Notas visuales: runas cayendo, pulso del tentáculo al compás, vacío respirante (arte + música pendiente)."
            );
        }

        void BuildStateOfTheRitualCard(Transform parent)
        {
            var lblGrey = new Color(0.86f, 0.87f, 0.93f);
            var content = BuildPortraitStackedCard(
                parent,
                RhythmStateRitualCardRowHierarchyName,
                "StateOfTheRitualCard",
                RhythmStateRitualCardAssetPath,
                layoutRowMinHeight: 360f,
                layoutRowPreferredHeight: 420f,
                initialCardRootSizeDelta: new Vector2(306f, 420f),
                contentPadding: new RectOffset(14, 14, 14, 14),
                contentVerticalSpacing: 8f,
                serializedCardBgOverride: _stateOfRitualCardSprite,
                cardRootAnchoredPosition: Vector2.zero);

            var header = AddTmp(
                content,
                TmpSafeGlyphs("ESTADO DEL RITUAL"),
                16,
                FontStyles.Bold,
                AccentGold,
                TextAlignmentOptions.Center);
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

            AddStateRitualStatusRow(content, RhythmStateRitualIconNormalStable);

            var nextCap = AddTmp(
                content,
                TmpSafeGlyphs("SIGUIENTE UMBRAL"),
                13,
                FontStyles.Bold,
                AccentGold,
                TextAlignmentOptions.Center);
            nextCap.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            AddTmp(
                    content,
                    TmpSafeGlyphs("Combo perfecto x 30 para Fragmento de recuerdo"),
                    13,
                    FontStyles.Normal,
                    lblGrey,
                    TextAlignmentOptions.Center)
                .gameObject.AddComponent<LayoutElement>().preferredWidth = -1;

            AddProgressBarWithCenterLabel(content, 18f / 30f, RitualProgressGreen, "18 / 30");

            var multCap = AddTmp(
                content,
                TmpSafeGlyphs("MULTIPLICADOR"),
                13,
                FontStyles.Bold,
                AccentGold,
                TextAlignmentOptions.Center);
            multCap.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            var multVal = AddTmp(content, TmpSafeGlyphs("x2.4"), 28, FontStyles.Bold, AccentGold, TextAlignmentOptions.Center);
            multVal.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        }

        static void AddStateRitualStatusRow(Transform parent, string iconAssetPath)
        {
            var row = AddRect(parent, "RitualStatusRow");
            var rowLe = row.gameObject.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 72f;
            rowLe.flexibleHeight = 0f;
            var rowH = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowH.spacing = 12f;
            rowH.padding = new RectOffset(4, 4, 4, 4);
            rowH.childAlignment = TextAnchor.MiddleLeft;
            rowH.childControlHeight = true;
            rowH.childControlWidth = true;
            rowH.childForceExpandHeight = false;
            rowH.childForceExpandWidth = false;

            var iconImg = AddImage(row.transform, "StateIcon", new Color(0.35f, 0.37f, 0.42f, 0.5f));
            var spr = Zone1ArtProvider.LoadSprite(iconAssetPath);
            if (spr != null)
            {
                iconImg.sprite = spr;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
            }

            var iconLe = iconImg.gameObject.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 56f;
            iconLe.preferredHeight = 56f;
            iconLe.flexibleWidth = 0f;
            iconLe.flexibleHeight = 0f;

            var textCol = AddRect(row.transform, "StatusLabels");
            textCol.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var colV = EnsureVertical(textCol.gameObject);
            colV.spacing = 2f;
            colV.childAlignment = TextAnchor.MiddleLeft;

            AddTmp(textCol.transform, TmpSafeGlyphs("Normal"), 18, FontStyles.Bold, RitualStateStableGreen, TextAlignmentOptions.Left)
                .gameObject.AddComponent<LayoutElement>().flexibleHeight = 0f;
            var statusGrey = new Color(0.86f, 0.87f, 0.93f);
            AddTmp(textCol.transform, TmpSafeGlyphs("Estable"), 13, FontStyles.Normal, statusGrey, TextAlignmentOptions.Left)
                .gameObject.AddComponent<LayoutElement>().flexibleHeight = 0f;
        }

        static void AddProgressBarWithCenterLabel(
            Transform parent,
            float normalized01,
            Color fillTint,
            string centerLabel)
        {
            var row = AddRect(parent, "ProgressBarLabeled");
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

            var bg = AddImage(row.transform, "BarBg", new Color(0.08f, 0.06f, 0.12f, 1f));
            StretchFull(bg.rectTransform);

            var fill = AddImage(row.transform, "BarFill", fillTint);
            StretchFull(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = Mathf.Clamp01(normalized01);

            bg.transform.SetSiblingIndex(0);
            fill.transform.SetSiblingIndex(1);

            var cap = AddTmp(row.transform, centerLabel, 12, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            StretchFull(cap.rectTransform);
            cap.outlineWidth = 0.12f;
            cap.outlineColor = new Color32(0, 0, 0, 140);
            cap.rectTransform.SetAsLastSibling();
        }

        void LaneColumn(Transform parent, string objectName, string title, Color laneColor, string inputLine)
        {
            var col = AddRect(parent, objectName);
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var v = EnsureVertical(col.gameObject);
            v.padding = new RectOffset(6, 6, 6, 6);
            v.spacing = 6f;

            var header = AddTmp(v.transform, title, 14, FontStyles.Bold, laneColor, TextAlignmentOptions.Center);
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

            var laneBg = AddImage(v.transform, "LaneStrip", new Color(laneColor.r, laneColor.g, laneColor.b, 0.18f));
            laneBg.gameObject.AddComponent<LayoutElement>().preferredHeight = 140f;

            var hit = AddImage(v.transform, "HitZone", new Color(laneColor.r, laneColor.g, laneColor.b, 0.55f));
            var hitRt = hit.GetComponent<RectTransform>();
            hitRt.sizeDelta = new Vector2(56f, 56f);
            hit.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;

            AddTmp(v.transform, inputLine, 13, FontStyles.Normal, Color.white, TextAlignmentOptions.Center);
        }

        void BuildRightPanels(Transform midRow)
        {
            var col = PanelColumn(midRow, "RightPanels", minWidth: 340f, flex: false);
            var v = EnsureVertical(col.gameObject);

            BuildStateOfTheRitualCard(v.transform);

            AddSectionTitle(v.transform, "Errores y consecuencias");
            AddBody(
                v.transform,
                "Fallo · pérdida de cordura\n" +
                "Muchos fallos · sube la corrupción\n" +
                "Romper combo · sube el impuesto\n" +
                "Fracasar ritual · nace una entidad"
            );

            AddSectionTitle(v.transform, "Impacto meta (diseño)");
            AddBody(
                v.transform,
                "Debe afectar: producción, economía, impuestos, corrupción y desbloqueos — no solo puntos."
            );
        }

        void BuildBottomHud(Transform parent)
        {
            var bar = PanelColumn(parent, "BottomHud", minWidth: 100f, flex: true);
            var barLe = bar.gameObject.GetComponent<LayoutElement>();
            if (barLe != null)
            {
                barLe.preferredHeight = 132f;
                barLe.flexibleHeight = 0f;
            }

            var hudRow = AddRect(bar, "HudRowInner");
            hudRow.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var h = hudRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(10, 10, 8, 8);
            h.spacing = 14f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlHeight = true;
            h.childControlWidth = true;
            h.childForceExpandHeight = false;
            h.childForceExpandWidth = false;

            var combo = AddRect(hudRow.transform, "Combo");
            combo.gameObject.AddComponent<LayoutElement>().preferredWidth = 240f;
            AddTmp(combo, "128 ¡PERFECTO!", 22, FontStyles.Bold, AccentGold, TextAlignmentOptions.Left);

            var ritual = AddRect(hudRow.transform, "RitualBar");
            ritual.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var rv = EnsureVertical(ritual.gameObject);
            AddTmp(rv.transform, "Progreso del ritual", 13, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            AddProgressBar(rv.transform, 0.62f);

            var sanity = AddRect(hudRow.transform, "Sanity");
            sanity.gameObject.AddComponent<LayoutElement>().preferredWidth = 260f;
            var sv = EnsureVertical(sanity.gameObject);
            AddTmp(sv.transform, "CORDURA 72 / 100", 14, FontStyles.Bold, AccentCyan, TextAlignmentOptions.Left);
            AddProgressBar(sv.transform, 0.72f, AccentCyan);

            var tax = AddRect(hudRow.transform, "Tax");
            tax.gameObject.AddComponent<LayoutElement>().preferredWidth = 220f;
            AddTmp(tax, "IMPUESTO ACTUAL\n15%", 16, FontStyles.Bold, AccentViolet, TextAlignmentOptions.Right);
        }

        static VerticalLayoutGroup EnsureVertical(GameObject go)
        {
            if (!go.TryGetComponent<VerticalLayoutGroup>(out var v))
                v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = 6f;
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            return v;
        }

        RectTransform PanelColumn(Transform parent, string name, float minWidth, bool flex)
        {
            var rt = AddRect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = PanelFrame;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(AccentGold.r, AccentGold.g, AccentGold.b, 0.35f);
            outline.effectDistance = new Vector2(1f, -1f);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minWidth = minWidth;
            le.preferredWidth = minWidth;
            le.flexibleWidth = flex ? 1f : 0f;
            le.flexibleHeight = 1f;
            EnsureVertical(rt.gameObject).padding = new RectOffset(12, 12, 12, 12);
            return rt;
        }

        static void Spacer(Transform parent, float h)
        {
            var rt = AddRect(parent, "Spacer");
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = h;
            le.flexibleHeight = 0f;
        }

        void AddSectionTitle(Transform parent, string text)
        {
            AddTmp(parent, text, 17, FontStyles.Bold, AccentGold, TextAlignmentOptions.Left);
        }

        void AddBody(Transform parent, string text)
        {
            var tmp = AddTmp(parent, text, 14, FontStyles.Normal, new Color(0.9f, 0.9f, 0.94f), TextAlignmentOptions.TopLeft);
            tmp.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static void AddProgressBar(Transform parent, float normalized01, Color? fillTint = null)
        {
            var row = AddRect(parent, "ProgressBar");
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
            var tint = fillTint ?? AccentViolet;
            var bg = AddImage(row.transform, "BarBg", new Color(0.08f, 0.06f, 0.12f, 1f));
            StretchFull(bg.rectTransform);
            var fill = AddImage(row.transform, "BarFill", tint);
            StretchFull(fill.rectTransform);
            var img = fill;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = Mathf.Clamp01(normalized01);
            bg.transform.SetSiblingIndex(0);
        }

        /// <summary>NewRocker-Regular SDF often lacks arrow glyphs; avoids TMPro warnings at runtime.</summary>
        static string TmpSafeGlyphs(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return s
                .Replace("\u2192", "->")
                .Replace("\u2190", "<-")
                .Replace("\u2194", "<->")
                .Replace("\u25bc", ">")
                .Replace("\u25BC", ">");
        }

        /// <summary>TMP line with markup (e.g. color spans). Applies <see cref="TmpSafeGlyphs"/> to the supplied string.</summary>
        static TextMeshProUGUI AddTmpForRichMarkup(
            Transform parent,
            string textRich,
            float size,
            FontStyles style,
            Color baseColor,
            TextAlignmentOptions align)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = TmpSafeGlyphs(textRich);
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = baseColor;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.richText = true;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            return tmp;
        }

        static TextMeshProUGUI AddTmp(
            Transform parent,
            string text,
            float size,
            FontStyles style,
            Color color,
            TextAlignmentOptions align)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
                tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = TmpSafeGlyphs(text);
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            return tmp;
        }

        static Image AddImage(Transform parent, string name, Color c)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        static RectTransform AddRect(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
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

    /// <summary>
    /// Play-mode rhythm prototype for CosmicHarvestRhythm.unity (scene hierarchy is not modified).
    /// Kept in this file with <see cref="CosmicHarvestRhythmSceneBuilder"/> so the type is always in the same compilation unit (fixes CS0246 in Unity).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CosmicHarvestRhythmGameplayController : MonoBehaviour
    {
        public enum LaneKind
        {
            PureEnergy,
            DarkCoins,
            WeakSouls
        }

        const string ResourcesAmountSeparator = " · ";
        const string EditorMusicAssetPath = "Assets/03_Audio/Music/Edwin/cosmic-harvest-rhythm-music.mp3";

        [Header("Note feel")]
        [SerializeField] float _noteTravelSeconds = 2.4f;
        [SerializeField] float _noteIconSize = 56f;
        [SerializeField] float _holdZoneStart = 0.72f;
        [SerializeField] float _spawnStagger = 0.16f;
        [SerializeField] float _waveGapMin = 0.4f;
        [SerializeField] float _waveGapMax = 1.25f;

        [Header("Reactive audio")]
        [SerializeField] float _duckedVolume = 0.22f;
        [SerializeField] float _fullVolume = 1f;
        [SerializeField] float _volumeLerpSpeed = 3.5f;

        RectTransform _canvasRoot;
        AudioSource _music;
        readonly Dictionary<LaneKind, LaneAnchors> _lanes = new Dictionary<LaneKind, LaneAnchors>();
        readonly Dictionary<LaneKind, Sprite> _sprites = new Dictionary<LaneKind, Sprite>();
        readonly List<ActiveNote> _active = new List<ActiveNote>();
        readonly List<PendingSpawn> _pending = new List<PendingSpawn>();
        readonly Dictionary<LaneKind, int> _scores = new Dictionary<LaneKind, int>
        {
            { LaneKind.PureEnergy, 0 },
            { LaneKind.DarkCoins, 0 },
            { LaneKind.WeakSouls, 0 }
        };
        readonly Dictionary<LaneKind, TextMeshProUGUI> _resourceLabels = new Dictionary<LaneKind, TextMeshProUGUI>();

        float _musicVolumeTarget = 1f;
        float _nextWaveTime;
        int _noteIdCounter;

        struct LaneAnchors
        {
            public RectTransform Top;
            public RectTransform Bottom;
            public RectTransform NoteParent;
            public string ResourcePath;
        }

        struct PendingSpawn
        {
            public float Time;
            public LaneKind Lane;
        }

        sealed class ActiveNote
        {
            public LaneKind Kind;
            public RectTransform IconRt;
            public Vector2 TopPos;
            public Vector2 BottomPos;
            public float Progress;
            public bool HoldBroken;
        }

        public void Initialize(RectTransform canvasRoot)
        {
            _canvasRoot = canvasRoot;
        }

        void Awake()
        {
            if (_canvasRoot == null)
                _canvasRoot = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        }

        void Start()
        {
            if (_canvasRoot == null)
            {
                Debug.LogWarning($"[{nameof(CosmicHarvestRhythmGameplayController)}] Canvas root was null; disabled.");
                enabled = false;
                return;
            }

            if (!CacheLaneAnchors())
            {
                Debug.LogWarning(
                    $"[{nameof(CosmicHarvestRhythmGameplayController)}] FirePoint* Top/Bottom not found under canvas; disabled.");
                enabled = false;
                return;
            }

            if (!CacheSprites())
            {
                Debug.LogWarning(
                    $"[{nameof(CosmicHarvestRhythmGameplayController)}] Lane sprites missing from Resources; disabled.");
                enabled = false;
                return;
            }

            if (!BindResourceRows())
            {
                Debug.LogWarning(
                    $"[{nameof(CosmicHarvestRhythmGameplayController)}] ResourceRow_PureEnergy / DarkCoins / WeakSouls TMP not found; disabled.");
                enabled = false;
                return;
            }

            SetupMusic();
            PushResourceUi();
            _nextWaveTime = Time.time + 0.35f;
        }

        void OnDestroy()
        {
            if (_music != null)
                Destroy(_music.gameObject);
        }

        bool CacheLaneAnchors()
        {
            _lanes.Clear();
            if (!TryGetPair("FirePointPureEnergyTop", "FirePointPureEnergyBottom", "Edwin/CosmicHarvestRhythm/audio-tracks-pure-energy-icon", out var pe))
                return false;
            if (!TryGetPair("FirePointDarkCoinsTop", "FirePointDarkCoinsBottom", "Edwin/CosmicHarvestRhythm/audio-tracks-dark-coins-icon", out var dc))
                return false;
            if (!TryGetPair("FirePointWeakSoulsTop", "FirePointWeakSoulsBottom", "Edwin/CosmicHarvestRhythm/audio-tracks-weak-souls-icon", out var ws))
                return false;
            _lanes[LaneKind.PureEnergy] = pe;
            _lanes[LaneKind.DarkCoins] = dc;
            _lanes[LaneKind.WeakSouls] = ws;
            return true;
        }

        bool TryGetPair(string topName, string bottomName, string resourcePath, out LaneAnchors anchors)
        {
            anchors = default;
            RectTransform top = null, bottom = null;
            foreach (var candidateTop in _canvasRoot.GetComponentsInChildren<RectTransform>(true))
            {
                if (candidateTop.name != topName)
                    continue;
                var parent = candidateTop.parent;
                if (parent == null)
                    continue;
                for (var c = 0; c < parent.childCount; c++)
                {
                    var child = parent.GetChild(c);
                    if (child.name != bottomName)
                        continue;
                    bottom = child as RectTransform;
                    if (bottom != null)
                    {
                        top = candidateTop;
                        goto Paired;
                    }
                }
            }

            Paired:
            if (top == null || bottom == null)
            {
                Debug.LogWarning($"[{nameof(CosmicHarvestRhythmGameplayController)}] Missing {topName}/{bottomName}.");
                return false;
            }

            anchors.Top = top;
            anchors.Bottom = bottom;
            anchors.NoteParent = top.parent as RectTransform;
            anchors.ResourcePath = resourcePath;
            return anchors.NoteParent != null;
        }

        bool CacheSprites()
        {
            _sprites.Clear();
            foreach (var kv in _lanes)
            {
                var s = LoadFirstSprite(kv.Value.ResourcePath);
                if (s == null)
                {
                    Debug.LogWarning($"[{nameof(CosmicHarvestRhythmGameplayController)}] Sprite not found: {kv.Value.ResourcePath}");
                    return false;
                }
                _sprites[kv.Key] = s;
            }
            return true;
        }

        static Sprite LoadFirstSprite(string path)
        {
            var all = Resources.LoadAll<Sprite>(path);
            if (all != null && all.Length > 0)
                return all[0];
            return Resources.Load<Sprite>(path);
        }

        bool BindResourceRows()
        {
            _resourceLabels.Clear();
            foreach (var kind in new[] { LaneKind.PureEnergy, LaneKind.DarkCoins, LaneKind.WeakSouls })
            {
                var rowName = RowObjectName(kind);
                Transform row = null;
                foreach (var t in _canvasRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name != rowName)
                        continue;
                    row = t;
                    break;
                }

                if (row == null)
                {
                    Debug.LogWarning($"[{nameof(CosmicHarvestRhythmGameplayController)}] Missing {rowName}.");
                    return false;
                }

                var tmp = row.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp == null)
                {
                    Debug.LogWarning($"[{nameof(CosmicHarvestRhythmGameplayController)}] No TMP under {rowName}.");
                    return false;
                }

                _resourceLabels[kind] = tmp;
            }

            return true;
        }

        static string RowObjectName(LaneKind k) =>
            k switch
            {
                LaneKind.PureEnergy => "ResourceRow_PureEnergy",
                LaneKind.DarkCoins => "ResourceRow_DarkCoins",
                LaneKind.WeakSouls => "ResourceRow_WeakSouls",
                _ => throw new ArgumentOutOfRangeException(nameof(k), k, null)
            };

        void SetupMusic()
        {
            var clip = LoadMusicClip();
            if (clip == null)
            {
                Debug.LogWarning(
                    $"[{nameof(CosmicHarvestRhythmGameplayController)}] No music clip (editor path {EditorMusicAssetPath} or Resources).");
                return;
            }

            var go = new GameObject("CosmicHarvestRhythmMusic");
            go.transform.SetParent(transform, false);
            _music = go.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.clip = clip;
            _music.volume = _fullVolume;
            _music.Play();
        }

        static AudioClip LoadMusicClip()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<AudioClip>(EditorMusicAssetPath);
#else
            return Resources.Load<AudioClip>("Music/Edwin/cosmic-harvest-rhythm-music");
#endif
        }

        void PushResourceUi()
        {
            foreach (var kv in _scores)
                ApplyScoreToLabel(kv.Value, _resourceLabels[kv.Key]);
        }

        static void ApplyScoreToLabel(int value, TextMeshProUGUI tmp)
        {
            if (tmp == null)
                return;
            var s = tmp.text;
            var i = s.LastIndexOf(ResourcesAmountSeparator, StringComparison.Ordinal);
            if (i < 0)
                return;
            tmp.text = s.Substring(0, i + ResourcesAmountSeparator.Length) + value;
        }

        void ApplyScoreToLabel(LaneKind kind, int value)
        {
            if (_resourceLabels.TryGetValue(kind, out var tmp))
                ApplyScoreToLabel(value, tmp);
        }

        void Update()
        {
            TickMusicReactiveVolume();
            TickPendingSpawns();
            TickActiveNotes();
            if (Time.time >= _nextWaveTime)
                QueueRandomWave();
        }

        void TickMusicReactiveVolume()
        {
            if (_music == null)
                return;
            _music.volume = Mathf.MoveTowards(_music.volume, _musicVolumeTarget, Time.deltaTime * _volumeLerpSpeed);
        }

        void TickPendingSpawns()
        {
            for (var i = _pending.Count - 1; i >= 0; i--)
            {
                if (_pending[i].Time > Time.time)
                    continue;
                SpawnNote(_pending[i].Lane);
                _pending.RemoveAt(i);
            }
        }

        void QueueRandomWave()
        {
            var t = Time.time;
            var pattern = UnityEngine.Random.Range(0, 3);
            var stagger = Mathf.Max(0.05f, _spawnStagger);
            float waveEnd = t;

            if (pattern == 0)
            {
                var lane = RandomLane();
                var n = UnityEngine.Random.Range(1, 5);
                for (var i = 0; i < n; i++)
                {
                    _pending.Add(new PendingSpawn { Time = t + i * stagger, Lane = lane });
                    waveEnd = Mathf.Max(waveEnd, t + i * stagger);
                }
            }
            else if (pattern == 1)
            {
                var n = UnityEngine.Random.Range(1, 4);
                for (var i = 0; i < n; i++)
                {
                    _pending.Add(new PendingSpawn { Time = t + i * stagger * 0.55f, Lane = RandomLane() });
                    waveEnd = Mathf.Max(waveEnd, t + i * stagger * 0.55f);
                }
            }
            else
            {
                var n = UnityEngine.Random.Range(2, 5);
                for (var i = 0; i < n; i++)
                    _pending.Add(new PendingSpawn { Time = t + UnityEngine.Random.Range(0f, 0.09f), Lane = RandomLane() });
                waveEnd = t + 0.09f;
            }

            _nextWaveTime = waveEnd + UnityEngine.Random.Range(_waveGapMin, _waveGapMax);
        }

        static LaneKind RandomLane() => (LaneKind)UnityEngine.Random.Range(0, 3);

        void SpawnNote(LaneKind kind)
        {
            if (!_lanes.TryGetValue(kind, out var lane) || !_sprites.TryGetValue(kind, out var sprite))
                return;
            _noteIdCounter++;
            var go = new GameObject($"Note_{kind}_{_noteIdCounter:000}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(lane.NoteParent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(_noteIconSize, _noteIconSize);
            rt.anchoredPosition = lane.Top.anchoredPosition;
            rt.SetAsLastSibling();
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;

            _active.Add(new ActiveNote
            {
                Kind = kind,
                IconRt = rt,
                TopPos = lane.Top.anchoredPosition,
                BottomPos = lane.Bottom.anchoredPosition,
                Progress = 0f,
                HoldBroken = false
            });
        }

        void TickActiveNotes()
        {
            var travelInv = 1f / Mathf.Max(0.01f, _noteTravelSeconds);
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var n = _active[i];
                n.Progress += Time.deltaTime * travelInv;

                if (n.Progress >= _holdZoneStart && n.Progress < 1f && !InputMatches(n.Kind))
                    n.HoldBroken = true;

                n.IconRt.anchoredPosition = Vector2.Lerp(n.TopPos, n.BottomPos, Mathf.Clamp01(n.Progress));

                if (n.Progress < 1f)
                {
                    _active[i] = n;
                    continue;
                }

                if (!n.HoldBroken && InputMatches(n.Kind))
                    RegisterHit(n.Kind);
                else
                    RegisterMiss();

                Destroy(n.IconRt.gameObject);
                _active.RemoveAt(i);
            }
        }

        /// <summary>
        /// Chord during hold + hit. Aggregates every <see cref="Mouse"/> device so laptop touchpads that are not <see cref="Mouse.current"/> still work like a mouse.
        /// Touchscreen: one contact → left; two+ contacts → left+right (“ambos”). Pen barrel buttons count as right. Keyboard: ←/A, →/D.
        /// </summary>
        static void ReadRhythmChord(out bool left, out bool right)
        {
            left = false;
            right = false;

            foreach (var device in InputSystem.devices)
            {
                if (device is not Mouse mouse)
                    continue;
                left |= mouse.leftButton.isPressed || mouse.leftButton.ReadValue() > 0.5f;
                right |= mouse.rightButton.isPressed || mouse.rightButton.ReadValue() > 0.5f;
            }

            var pen = Pen.current;
            if (pen != null)
            {
                left |= pen.tip.isPressed || pen.tip.ReadValue() > 0.5f;
                right |= pen.firstBarrelButton.isPressed || pen.firstBarrelButton.ReadValue() > 0.5f
                    || pen.secondBarrelButton.isPressed || pen.secondBarrelButton.ReadValue() > 0.5f
                    || pen.eraser.isPressed || pen.eraser.ReadValue() > 0.5f;
            }

            var ts = Touchscreen.current;
            if (ts != null)
            {
                var pressedTouches = 0;
                for (var i = 0; i < ts.touches.Count; i++)
                {
                    var touch = ts.touches[i];
                    if (touch.press.isPressed || touch.press.ReadValue() > 0.5f)
                        pressedTouches++;
                }

                if (pressedTouches >= 2)
                {
                    left |= true;
                    right |= true;
                }
                else if (pressedTouches == 1)
                {
                    left |= true;
                }
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                left |= kb.leftArrowKey.isPressed || kb.aKey.isPressed;
                right |= kb.rightArrowKey.isPressed || kb.dKey.isPressed;
            }
        }

        static bool InputMatches(LaneKind kind)
        {
            ReadRhythmChord(out var l, out var r);
            return kind switch
            {
                LaneKind.PureEnergy => l && !r,
                LaneKind.WeakSouls => r && !l,
                LaneKind.DarkCoins => l && r,
                _ => false
            };
        }

        void RegisterHit(LaneKind kind)
        {
            _scores[kind]++;
            ApplyScoreToLabel(kind, _scores[kind]);
            _musicVolumeTarget = _fullVolume;
        }

        void RegisterMiss()
        {
            _musicVolumeTarget = _duckedVolume;
        }
    }
}
