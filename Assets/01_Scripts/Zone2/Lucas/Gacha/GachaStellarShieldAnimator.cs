using LasGranjasDelHastur.Zone1;
using UnityEngine;

namespace LasGranjasDelHastur.Zone2.Lucas
{
    /// <summary>Reproduce un spritesheet ya cortado en frames (orden filas como <see cref="Zone1ArtProvider.LoadSheetUniformGrid"/>).</summary>
    [DisallowMultipleComponent]
    public sealed class GachaStellarShieldAnimator : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] float framesPerSecond = 10f;

        SpriteRenderer _sr;
        Sprite[] _frames;
        float _accum;
        int _idx;

        void Awake() => _sr = GetComponent<SpriteRenderer>();

        public void SetFrames(Sprite[] frames)
        {
            _frames = frames;
            _idx = 0;
            _accum = 0f;
            if (_sr == null)
                _sr = GetComponent<SpriteRenderer>();
            if (_sr != null && _frames != null && _frames.Length > 0)
                _sr.sprite = _frames[0];
        }

        void Update()
        {
            if (_frames == null || _frames.Length <= 1 || _sr == null)
                return;

            _accum += Time.deltaTime;
            var step = 1f / framesPerSecond;
            while (_accum >= step)
            {
                _accum -= step;
                _idx = (_idx + 1) % _frames.Length;
                _sr.sprite = _frames[_idx];
            }
        }
    }
}
