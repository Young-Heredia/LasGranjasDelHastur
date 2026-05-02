using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LasGranjasDelHastur;
using UnityEngine;
using UnityEngine.UI;

namespace LasGranjasDelHastur.UI.Edwin
{
    /// <summary>
    /// Cursor temático "La Mano del Rey Amarillo".
    /// Se auto-instancia al cargar cualquier escena (DontDestroyOnLoad).
    /// Reemplaza el cursor de sistema con un Image de Unity que sigue al ratón.
    ///
    /// Uso externo:  GameCursorController.SetMode(CursorMode.Collect);
    ///
    /// En builds de producción las texturas se cargan desde Resources/Edwin/Cursors/.
    /// En el Editor se leen directamente con AssetDatabase.
    /// </summary>
    public enum CursorMode { Normal, Selection, Collect, Alert }

    [DisallowMultipleComponent]
    public class GameCursorController : MonoBehaviour
    {
        // Display height in screen pixels for all cursor sprites.
        const float CursorDisplayHeight = 56f;
        // Seconds between click-animation frames.
        const float ClickFrameInterval = 0.048f;

        // ── Editor: Edwin (copias con convención Cursor_*_YellowKing), fallback pack Lucas ──
        const string PathNormalEdwin    = "Assets/02_Sprites/Edwin/Cursor/Cursor_Normal_YellowKing.png";
        const string PathNormalLucas    = "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/Characters/Hastur/Cursor_HandOfYellowKing.png";
        const string PathSelectionEdwin = "Assets/02_Sprites/Edwin/Cursor/Cursor_Selection_YellowKing.png";
        const string PathSelectionLucas = "Assets/02_Sprites/Lucas/Zone1/Cursor/zone1_cursor_hover.png";
        const string PathCollectEdwin   = "Assets/02_Sprites/Edwin/Cursor/Cursor_Collect_YellowKing.png";
        const string PathCollectLucas   = "Assets/02_Sprites/Lucas/Zone1/Cursor/zone1_cursor_collect.png";
        const string PathAlert     = "Assets/02_Sprites/Edwin/Cursor/Cursor_Alert_YellowKing.png";
        const string PathAlertFallback = "Assets/02_Sprites/Lucas/Zone1/Cursor/zone1_cursor_blocked.png";
        const string PathClickSheet = "Assets/02_Sprites/Lucas/LasGranjasHastur_AssetPack_PixelArt/hastur_pixel_art_pack/Characters/Hastur/Cursor_HandOfYellowKing_Click_Sheet.png";

        // ── Runtime Resources (mismos nombres que en Edwin/Cursor) ───────────────────────────
        const string ResNormal     = "Edwin/Cursors/Cursor_Normal_YellowKing";
        const string ResSelection  = "Edwin/Cursors/Cursor_Selection_YellowKing";
        const string ResCollect    = "Edwin/Cursors/Cursor_Collect_YellowKing";
        const string ResAlert      = "Edwin/Cursors/Cursor_Alert_YellowKing";
        const string ResClickSheet = "Edwin/Cursors/Cursor_HandOfYellowKing_Click_Sheet";

        static GameCursorController _instance;
        public static GameCursorController Instance => _instance;

        Sprite   _sprNormal;
        Sprite   _sprSelection;
        Sprite   _sprCollect;
        Sprite   _sprAlert;
        Sprite[] _sprClick;

        CursorMode    _currentMode    = CursorMode.Normal;
        bool          _animatingClick;
        RectTransform _canvasRect;
        RectTransform _cursorRect;
        Image         _cursorImage;

        // ── Bootstrap ───────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (_instance != null)
                return;
            if (FindFirstObjectByType<GameCursorController>() != null)
                return;
            var go = new GameObject("GameCursorController");
            go.AddComponent<GameCursorController>();
        }

        // ── Lifecycle ───────────────────────────────────────────────────────

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSprites();
            BuildCursorOverlay();
            ApplyMode(_currentMode);
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                Cursor.visible = true;
                _instance = null;
            }
        }

        // ── Every frame ─────────────────────────────────────────────────────

        void Update()
        {
            MoveCursorToMouse();

            if (!_animatingClick
                && InputAdapter.LeftMouseDownThisFrame()
                && _sprClick != null && _sprClick.Length > 0)
            {
                StartCoroutine(PlayClickAnimation());
            }
        }

        void MoveCursorToMouse()
        {
            if (_canvasRect == null || _cursorRect == null)
                return;

            var mousePos = InputAdapter.MousePosition();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, mousePos, null, out var localPos))
                _cursorRect.anchoredPosition = localPos;
        }

        // ── Click animation ─────────────────────────────────────────────────

        IEnumerator PlayClickAnimation()
        {
            _animatingClick = true;

            foreach (var frame in _sprClick)
            {
                SetCursorSprite(frame);
                yield return new WaitForSeconds(ClickFrameInterval);
            }

            _animatingClick = false;
            ApplyMode(_currentMode);
        }

        // ── Mode switching ──────────────────────────────────────────────────

        /// <summary>Call from any script to change the cursor appearance.</summary>
        public static void SetMode(CursorMode mode)
        {
            if (_instance != null)
                _instance.ApplyMode(mode);
        }

        void ApplyMode(CursorMode mode)
        {
            _currentMode = mode;
            if (_animatingClick)
                return;

            var spr = mode switch
            {
                CursorMode.Selection => _sprSelection ?? _sprNormal,
                CursorMode.Collect   => _sprCollect   ?? _sprNormal,
                CursorMode.Alert     => _sprAlert     ?? _sprNormal,
                _                    => _sprNormal,
            };
            SetCursorSprite(spr);
        }

        void SetCursorSprite(Sprite spr)
        {
            if (_cursorImage == null || spr == null)
                return;

            _cursorImage.sprite = spr;
            _cursorImage.SetNativeSize();

            // Scale to a consistent display height while preserving aspect ratio.
            var h = _cursorRect.sizeDelta.y;
            if (h > 0.001f)
            {
                var scale = CursorDisplayHeight / h;
                _cursorRect.sizeDelta = new Vector2(
                    _cursorRect.sizeDelta.x * scale,
                    CursorDisplayHeight);
            }
        }

        // ── Canvas / Image construction ─────────────────────────────────────

        void BuildCursorOverlay()
        {
            var canvasGo = new GameObject("CursorCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            _canvasRect = canvasGo.GetComponent<RectTransform>();

            var cursorGo = new GameObject("CursorImage");
            cursorGo.transform.SetParent(canvasGo.transform, false);
            _cursorRect = cursorGo.AddComponent<RectTransform>();

            // Anchor to canvas center; anchoredPosition = local offset from center.
            _cursorRect.anchorMin = new Vector2(0.5f, 0.5f);
            _cursorRect.anchorMax = new Vector2(0.5f, 0.5f);
            // Pivot at top-left = hotspot of the cursor (tip of pointer finger).
            _cursorRect.pivot           = new Vector2(0f, 1f);
            _cursorRect.anchoredPosition = Vector2.zero;
            _cursorRect.sizeDelta        = new Vector2(CursorDisplayHeight, CursorDisplayHeight);

            _cursorImage = cursorGo.AddComponent<Image>();
            _cursorImage.raycastTarget = false;

            Cursor.visible = false;
        }

        // ── Sprite loading ──────────────────────────────────────────────────

        void LoadSprites()
        {
#if UNITY_EDITOR
            _sprNormal    = EditorLoadSprite(PathNormalEdwin,    "Cursor_Normal_YellowKing_0")
                            ?? EditorLoadSprite(PathNormalLucas,    "Cursor_HandOfYellowKing_0");
            _sprSelection = EditorLoadSprite(PathSelectionEdwin, "Cursor_Selection_YellowKing_0")
                            ?? EditorLoadSprite(PathSelectionLucas, "zone1_cursor_hover_0");
            _sprCollect   = EditorLoadSprite(PathCollectEdwin,   "Cursor_Collect_YellowKing_0")
                            ?? EditorLoadSprite(PathCollectLucas,   "zone1_cursor_collect_0");
            _sprAlert     = EditorLoadSprite(PathAlert,      "Cursor_Alert_YellowKing_0")
                            ?? EditorLoadSprite(PathAlertFallback, "zone1_cursor_blocked_0");

            var clickList = new List<Sprite>();
            for (int i = 0; i < 6; i++)
            {
                var s = EditorLoadSprite(PathClickSheet, $"Cursor_HandOfYellowKing_Click_Sheet_{i}");
                if (s != null)
                    clickList.Add(s);
            }
            _sprClick = NormalizeClickFrames(clickList.Count > 0 ? clickList.ToArray() : null);
#else
            _sprNormal    = ResourcesLoadSprite(ResNormal,    "Cursor_Normal_YellowKing_0");
            _sprSelection = ResourcesLoadSprite(ResSelection, "Cursor_Selection_YellowKing_0");
            _sprCollect   = ResourcesLoadSprite(ResCollect,   "Cursor_Collect_YellowKing_0");
            _sprAlert     = ResourcesLoadSprite(ResAlert,     "Cursor_Alert_YellowKing_0");

            var clickSheet = Resources.LoadAll<Sprite>(ResClickSheet);
            _sprClick = NormalizeClickFrames(clickSheet.Length > 0 ? clickSheet : null);
#endif
        }

        /// <summary>
        /// Si la textura está en modo Single por error, Unity expone un solo <see cref="Sprite"/>
        /// con el atlas completo: se verían las 6 manos a la vez. Nos quedamos solo con recortes plausibles.
        /// </summary>
        static Sprite[] NormalizeClickFrames(Sprite[] raw)
        {
            if (raw == null || raw.Length == 0)
                return null;

            var tex = raw[0].texture;
            if (tex == null)
                return raw;

            var maxSlice = Mathf.Max(32f, tex.width * 0.28f);
            var slices = new List<Sprite>();
            foreach (var s in raw)
            {
                if (s == null)
                    continue;
                if (s.rect.width <= maxSlice && s.rect.height <= tex.height)
                    slices.Add(s);
            }

            if (slices.Count == 0)
                return null;

            return slices.OrderBy(s => s.name, System.StringComparer.Ordinal).ToArray();
        }

#if UNITY_EDITOR
        static Sprite EditorLoadSprite(string assetPath, string spriteName)
        {
            var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (all != null)
                foreach (var a in all)
                    if (a is Sprite s && s.name == spriteName)
                        return s;
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
#endif

        static Sprite ResourcesLoadSprite(string resPath, string spriteName)
        {
            var all = Resources.LoadAll<Sprite>(resPath);
            foreach (var s in all)
                if (s.name == spriteName) return s;
            return all.Length > 0 ? all[0] : null;
        }
    }
}
