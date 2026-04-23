using UnityEngine;

namespace LasGranjasDelHastur
{
    public static class RuntimeSpriteFactory
    {
        static Texture2D _opaqueWhiteTex;
        static Sprite _opaqueWhiteSprite;

        public static Sprite OpaqueWhiteSprite
        {
            get
            {
                if (_opaqueWhiteSprite != null)
                    return _opaqueWhiteSprite;

                _opaqueWhiteTex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var w = new Color32(255, 255, 255, 255);
                var px = new Color32[16];
                for (var i = 0; i < px.Length; i++)
                    px[i] = w;
                _opaqueWhiteTex.SetPixels32(px);
                _opaqueWhiteTex.Apply(false, true);

                _opaqueWhiteSprite = Sprite.Create(
                    _opaqueWhiteTex,
                    new Rect(0, 0, 4, 4),
                    new Vector2(0.5f, 0.5f),
                    100f);
                return _opaqueWhiteSprite;
            }
        }
    }
}

