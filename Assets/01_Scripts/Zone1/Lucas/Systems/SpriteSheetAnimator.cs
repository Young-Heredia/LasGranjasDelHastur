using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteSheetAnimator : MonoBehaviour
    {
        [SerializeField] private string relativeSheetPath;
        [SerializeField] private int frameWidth = 32;
        [SerializeField] private int frameHeight = 32;
        [SerializeField] private int gridColumns = 1;
        [SerializeField] private int gridRows = 1;
        [SerializeField] private int gridMaxFrames;
        [SerializeField] private int[] gridFrameSequence;
        [SerializeField] private bool useUniformGrid;
        [SerializeField] private float fps = 8f;
        [SerializeField] private bool loop = true;

        SpriteRenderer _renderer;
        Sprite[] _frames;
        float _timer;
        int _index;

        public void Configure(string sheetPath, int width, int height, float newFps, bool shouldLoop = true)
        {
            relativeSheetPath = sheetPath;
            frameWidth = Mathf.Max(1, width);
            frameHeight = Mathf.Max(1, height);
            fps = Mathf.Max(1f, newFps);
            loop = shouldLoop;
            useUniformGrid = false;
            LoadFrames();
        }

        public void ConfigureGrid(string sheetPath, int columns, int rows, float newFps, bool shouldLoop = true, float pixelsPerUnit = 32f, int maxFrames = 0)
        {
            relativeSheetPath = sheetPath;
            gridColumns = Mathf.Max(1, columns);
            gridRows = Mathf.Max(1, rows);
            gridMaxFrames = Mathf.Max(0, maxFrames);
            gridFrameSequence = null;
            fps = Mathf.Max(1f, newFps);
            loop = shouldLoop;
            frameWidth = Mathf.RoundToInt(Mathf.Max(1f, pixelsPerUnit));
            useUniformGrid = true;
            LoadFrames();
        }

        public void ConfigureGridSequence(string sheetPath, int columns, int rows, int[] frameSequence, float newFps, bool shouldLoop = true, float pixelsPerUnit = 32f)
        {
            relativeSheetPath = sheetPath;
            gridColumns = Mathf.Max(1, columns);
            gridRows = Mathf.Max(1, rows);
            gridMaxFrames = 0;
            gridFrameSequence = frameSequence;
            fps = Mathf.Max(1f, newFps);
            loop = shouldLoop;
            frameWidth = Mathf.RoundToInt(Mathf.Max(1f, pixelsPerUnit));
            useUniformGrid = true;
            LoadFrames();
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            LoadFrames();
        }

        void LoadFrames()
        {
            if (string.IsNullOrEmpty(relativeSheetPath))
                return;

            _frames = useUniformGrid
                ? Zone1ArtProvider.LoadSheetUniformGrid(relativeSheetPath, gridColumns, gridRows, Mathf.Max(1f, frameWidth))
                : Zone1ArtProvider.LoadSheet(relativeSheetPath, frameWidth, frameHeight);
            if (_frames != null && _frames.Length > 0 && useUniformGrid && gridMaxFrames > 0 && gridMaxFrames < _frames.Length)
            {
                var trimmed = new Sprite[gridMaxFrames];
                for (var i = 0; i < trimmed.Length; i++)
                    trimmed[i] = _frames[i];
                _frames = trimmed;
            }
            if (_frames != null && _frames.Length > 0 && useUniformGrid && gridFrameSequence != null && gridFrameSequence.Length > 0)
            {
                var list = new System.Collections.Generic.List<Sprite>(gridFrameSequence.Length);
                for (var i = 0; i < gridFrameSequence.Length; i++)
                {
                    var idx = gridFrameSequence[i];
                    if (idx < 0 || idx >= _frames.Length)
                        continue;
                    list.Add(_frames[idx]);
                }
                if (list.Count > 0)
                    _frames = list.ToArray();
            }
            _index = 0;
            _timer = 0f;
            if (_frames != null && _frames.Length > 0 && _renderer != null)
                _renderer.sprite = _frames[0];
        }

        void Update()
        {
            if (_frames == null || _frames.Length <= 1 || _renderer == null)
                return;

            _timer += Time.deltaTime;
            var frameTime = 1f / Mathf.Max(1f, fps);
            if (_timer < frameTime)
                return;

            _timer -= frameTime;
            _index += 1;
            if (_index >= _frames.Length)
                _index = loop ? 0 : _frames.Length - 1;

            _renderer.sprite = _frames[_index];
        }
    }
}

