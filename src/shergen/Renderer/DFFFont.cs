using Shergen.DistanceFieldFont;

namespace Shergen.Renderer
{
    /// <summary>
    /// IFont wrapper around a loaded DistanceFieldFont.DistanceFieldFont.
    /// Handles the SDF padding correction and optional fallback character.
    /// </summary>
    public class DFFFont : IFont
    {
        private readonly DistanceFieldFont.DistanceFieldFont _dff;

        public string Name        { get; }
        public float  MaxHeight   => _dff.MaxHeight;
        public float  SpaceWidth  => _dff.SpaceWidth;
        public float  SdfPadding  => _dff.Padding;

        public int ArrayTextureID => _dff.ArrayTextureID;
        public bool   IsSDF       => true;
        public char   FallbackChar { get; set; } = '\0';

        public DFFFont(string name, DistanceFieldFont.DistanceFieldFont dff)
        {
            Name = name;
            _dff = dff;
        }

        public bool TryGetCharInfo(char c, out CharInfo info)
        {
            if (_dff.TryGetCharUV(c, out var uv))
            {
                info = BuildInfo(uv);
                return true;
            }

            // Non-breaking space (U+00A0): pokud font nemá tento znak, nahradíme
            // ho mezerou — advance-only záznam (TextureID = -1, žádný glyf).
            if (c == ' ')
            {
                info = new CharInfo { AdvanceWidth = _dff.SpaceWidth, TextureID = -1 };
                return true;
            }

            // Try general fallback glyph
            if (FallbackChar != '\0' && _dff.TryGetCharUV(FallbackChar, out var fallbackUV))
            {
                info = BuildInfo(fallbackUV);
                return true;
            }

            info = default;
            return false;
        }

        private static CharInfo BuildInfo(DistanceFieldFont.DistanceFieldFont.CharUV uv)
        {
            return new CharInfo
            {
                TextureID   = uv.TextureID,
                U0          = uv.U0,
                V0          = uv.V0,
                U1          = uv.U1,
                V1          = uv.V1,
                AdvanceWidth = uv.AdvanceWidth,
                DrawWidth    = (uv.U1 - uv.U0) * 512f,   // atlas is always 512×512
                DrawHeight   = (uv.V1 - uv.V0) * 512f,
            };
        }
    }
}
