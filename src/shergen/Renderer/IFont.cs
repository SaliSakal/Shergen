namespace Shergen.Renderer
{
    /// <summary>
    /// Per-character rendering info returned by IFont.TryGetCharInfo.
    /// DrawWidth / DrawHeight are the actual quad dimensions at scale = 1.
    /// AdvanceWidth is the logical cursor advance (may be smaller than DrawWidth for SDF fonts).
    /// </summary>
    public struct CharInfo
    {
        public int   TextureID;
        public float U0, V0, U1, V1;
        public float AdvanceWidth;   // logical advance (cursor movement), in font pixels
        public float DrawWidth;      // actual quad width  at scale = 1, in font pixels
        public float DrawHeight;     // actual quad height at scale = 1, in font pixels
    }

    /// <summary>
    /// Unified font interface.  Both DFF (SDF) and future TTF fonts implement this.
    /// </summary>
    public interface IFont
    {
        /// <summary>Display / file name used as cache key.</summary>
        string Name { get; }

        /// <summary>Full slot height in font pixels – used to derive scale from pt size.</summary>
        float MaxHeight { get; }

        /// <summary>Width of a space character in font pixels.</summary>
        float SpaceWidth { get; }

        /// <summary>
        /// Extra pixels of SDF gradient around each glyph (both sides).
        /// Used to correct right-alignment: the visual right edge of the last character
        /// extends SdfPadding * scaleX beyond its AdvanceWidth.
        /// Zero for non-SDF fonts.
        /// </summary>
        float SdfPadding { get; }

        /// <summary>True for SDF fonts that need rangeMin/rangeMax shader uniforms.</summary>
        bool IsSDF { get; }

        /// <summary>
        /// Character to render when the requested glyph is missing.
        /// '\0' means "skip the character silently".
        /// </summary>
        char FallbackChar { get; set; }

        /// <summary>
        /// Look up rendering info for a character.
        /// If the character is not found and FallbackChar != '\0', returns the fallback glyph.
        /// Returns false only when neither the char nor the fallback exist.
        /// </summary>
        bool TryGetCharInfo(char c, out CharInfo info);
        /// <summary>
        /// Returns the OpenGL texture ID for the font's array texture (or 0 if not applicable).
        /// </summary>
        int ArrayTextureID { get; }
    }
}
