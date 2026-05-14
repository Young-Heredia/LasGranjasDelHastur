using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    /// <summary>Cycles <see cref="Sprite"/> frames loaded from Resources (sheet sliced as MusicP_0…).</summary>
    public sealed class MusicPLoopSpritePlayer : MonoBehaviour
    {
        [SerializeField] string resourcesPath = "Edwin/CosmicHarvestRhythm/MusicP";
        [SerializeField] float framesPerSecond = 10f;

        SpriteRenderer _spriteRenderer;
        Sprite[] _frames;

        void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _frames = Resources.LoadAll<Sprite>(resourcesPath);
            System.Array.Sort(_frames, (a, b) => string.CompareOrdinal(a.name, b.name));
            if (_frames.Length > 0 && _spriteRenderer != null)
                _spriteRenderer.sprite = _frames[0];
        }

        void Update()
        {
            if (_frames == null || _frames.Length == 0 || _spriteRenderer == null)
                return;
            var idx = Mathf.FloorToInt(Time.time * framesPerSecond) % _frames.Length;
            var s = _frames[idx];
            if (_spriteRenderer.sprite != s)
                _spriteRenderer.sprite = s;
        }
    }
}
