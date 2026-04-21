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

            _frames = Zone1ArtProvider.LoadSheet(relativeSheetPath, frameWidth, frameHeight);
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

