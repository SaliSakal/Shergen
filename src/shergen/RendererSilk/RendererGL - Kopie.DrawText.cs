using Silk.NET.OpenGL;
using System.Numerics;


namespace Shergen.Renderer
{
    public partial class RendererGL : IRenderer
    {
        // ── Font cache ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the IFont for <paramref name="name"/>, loading it lazily on first use.
        /// Empty / null name resolves to DefaultDFFFont.
        /// </summary>
        /// 
        private IFont GetFontMetrics(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = DefaultDFFFont;

            if (_fonts.TryGetValue(name, out var cached)) return cached;

            if (_fontsMetrics.TryGetValue(name, out var mCached)) return mCached;
            if (name.EndsWith(".DFF", StringComparison.OrdinalIgnoreCase))
            {
                string path = "Fonts/" + name;
                if (!FileManager.FileExists(path)) return null;
                var raw = DistanceFieldFont.DistanceFieldFont.LoadFromFile(path, true);
                var dffFontMetrics = new DFFFont(name, raw);

                if (_pendingFallbacks.TryGetValue(name, out char fb))
                    dffFontMetrics.FallbackChar = fb;

                _fontsMetrics[name] = dffFontMetrics;
                return dffFontMetrics;
            }
            return null;
        }
        private IFont GetFont(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = DefaultDFFFont;

            if (_fonts.TryGetValue(name, out var cached))
                return cached;

            if (name.EndsWith(".DFF", StringComparison.OrdinalIgnoreCase))
            {
                string path = "Fonts/" + name;
                if (!FileManager.FileExists(path))
                {
                    Console.WriteLine($"❌ DFF font not found: {path}");
                    return null;
                }

                var raw = DistanceFieldFont.DistanceFieldFont.LoadFromFile(path);

                raw.UploadToGPU(gl);   // Silk.NET: gl instance required
                var dffFont = new DFFFont(name, raw);

                if (_pendingFallbacks.TryGetValue(name, out char fb))
                    dffFont.FallbackChar = fb;

                _fonts[name] = dffFont;
                Console.WriteLine($"✅ DFF loaded: {name} — {raw.Characters.Count} chars, {raw.Textures.Count} textures");

                if (_fontsMetrics.ContainsKey(name))
                    _fontsMetrics.Remove(name); ;
                return dffFont;
            }

            Console.WriteLine($"❌ Unknown font format: {name}");
            return null;
        }

        public void SetFontFallbackChar(string fontName, char c)
        {
            _pendingFallbacks[fontName] = c;
            if (_fonts.TryGetValue(fontName, out var font) && font is DFFFont dff)
                dff.FallbackChar = c;
        }

        // ── Text rendering ────────────────────────────────────────────────────────

        public void DrawText(UILabel elem)
        {
            if (string.IsNullOrEmpty(elem.Text) && elem is not UITextField) return;

            var font = GetFont(elem.Font);
            if (font == null) return;

            UITextField tf = elem as UITextField;
            float cursorX = -1, cursorY = -1, cursorH = 0;

            int charCount = 0;

            if (elem.ScrollText)
            {
                var   now = DateTime.Now;
                float dt  = elem._scrollLastTick == DateTime.MinValue ? 0f
                            : (float)(now - elem._scrollLastTick).TotalSeconds;
                elem._scrollLastTick = now;

                if (elem._scrollWaiting)
                {
                    elem._scrollDelayMs -= dt * 1000f;
                    if (elem._scrollDelayMs <= 0f)
                    {
                        elem._scrollWaiting  = false;
                        elem._scrollDir      = -elem._scrollDir;
                        elem._scrollOffset  += elem.ScrollTextSpeed * elem._scrollDir * dt;
                    }
                }
                else
                {
                    elem._scrollOffset += elem.ScrollTextSpeed * elem._scrollDir * dt;
                }
            }


            float fade = elem.GetEffectiveFade();

            // ── Scale ─────────────────────────────────────────────────────────────
            const float DFF_PT_SCALE = 1.8f;
            float scale  = (elem.FontSize / font.MaxHeight) * (font.IsSDF ? DFF_PT_SCALE : 1f);
            float scaleX = scale * (elem.CharScaleX / 100f);
            float scaleY = scale * (elem.CharScaleY / 100f);

            // ── SDF range ─────────────────────────────────────────────────────────
            float AutoRangeMin(float s)
            {
                float t = Math.Clamp((s - 0.28f) / 0.54f, 0f, 1f);
                return 0.21f + 0.25f * MathF.Sqrt(t);
            }
            float AutoRangeMax(float s) => s < 0.35f ? 0.65f : 0.55f;

            float startX = elem.GetAbsX() - (elem is UITextField ? tf._xOffset : 0f); ;
            float startY = elem.GetAbsY() - (elem is UITextLog tlog ? tlog._logScrollOffset : 0f);

            // ── Parse rich text (cached) ──────────────────────────────────────────
            bool richText = elem is UITextField ? tf.ShowRich : true;
            

            if (elem._rtCacheKey != elem.Text)
            {
                elem._rtRuns = (elem.Text.Contains('<') && richText)
                    ? RichTextParser.Parse(elem.Text)
                    : new List<TextRun> { new TextRun { Text = elem.Text, Format = FormatState.Default } };
                elem._rtCacheKey  = elem.Text;
                elem._layoutValid = false;
            }
            var runs = elem._rtRuns;

            // ── Measurement helpers ───────────────────────────────────────────────
            float MeasureAtomWidth(Atom atom)
            {
                if (atom.IsSpace)   return font.SpaceWidth * scaleX * atom.Fmt.SizeMult;
                if (atom.IsNewline) return 0f;
                float sx      = scaleX * atom.Fmt.SizeMult * (atom.Fmt.Bold ? 1.08f : 1f);
                float charGap = elem.CharSpacing * atom.Fmt.SizeMult;
                float w       = 0f;
                foreach (char c in atom.Text)


                   if (c == ' ')
                        w += font.SpaceWidth * sx;
                    else if (font.TryGetCharInfo(c, out var ci))
                        w += ci.AdvanceWidth * sx + charGap;
                return w;
            }

            float MeasureLine(List<Atom> line)
            {
                float w       = 0f;
                float lastSx  = scaleX;
                bool  hasChar = false;
                foreach (var atom in line)
                {
                    if (atom.IsNewline) continue;
                    if (atom.IsSpace)
                    {
                        w      += font.SpaceWidth * scaleX * atom.Fmt.SizeMult;
                        hasChar = false;
                        continue;
                    }
                    float sx      = scaleX * atom.Fmt.SizeMult * (atom.Fmt.Bold ? 1.08f : 1f);
                    float charGap = elem.CharSpacing * atom.Fmt.SizeMult;
                    foreach (char c in atom.Text)
                        if (c == ' ')
                            w += font.SpaceWidth * sx;
                        else if (font.TryGetCharInfo(c, out var ci))
                        { w += ci.AdvanceWidth * sx + charGap; lastSx = sx; hasChar = true; }
                }
                if (hasChar)
                {
                    if (elem.CharSpacing > 0f) w -= elem.CharSpacing;
                    w += 2f * font.SdfPadding * lastSx;
                }
                return w;
            }

            float LineHeight(List<Atom> line)
            {
                float maxMult = 1f;
                foreach (var a in line)
                    if (a.IsContent && a.Fmt.SizeMult > maxMult)
                        maxMult = a.Fmt.SizeMult;
                return font.MaxHeight * scaleY * maxMult + elem.LineSpacing * maxMult;
            }

            // ── Layout cache ──────────────────────────────────────────────────────
            var newLayoutKey = (elem.Text,
                                elem.AutoSizeWidth ? elem.AutoSizeWidth_Max : elem.Width,
                                elem.FontSize,
                                elem.CharScaleX, elem.CharScaleY,
                                elem.CharSpacing, elem.LineSpacing,
                                elem.WordWrap, elem.Font ?? "");

            List<List<Atom>> lines;
            float[]          lineWidths;
            float[]          lineHeights;

            if (!elem._layoutValid || elem._layoutKey != newLayoutKey)
            {
                var atoms = new List<Atom>();
                foreach (var run in runs)
                {
                    string t   = run.Text;
                    int    pos = 0;
                    for (int k = 0; k <= t.Length; k++)
                    {
                        char ch = k < t.Length ? t[k] : '\0';
                        if (ch == ' ' || ch == '\n' || k == t.Length)
                        {
                            if (k > pos)
                                atoms.Add(new Atom { Text = t.Substring(pos, k - pos), Fmt = run.Format });
                            if      (ch == ' ')  atoms.Add(new Atom { Fmt = run.Format, IsSpace   = true });
                            else if (ch == '\n') atoms.Add(new Atom { Fmt = run.Format, IsNewline = true });
                            pos = k + 1;
                        }
                    }
                }

                var   linesW       = new List<List<Atom>>();
                var   currentLine  = new List<Atom>();
                float currentLineW = 0f;
                var   pendingWord  = new List<Atom>();
                float pendingWordW = 0f;

                void CommitWord()
                {
                    if (pendingWord.Count == 0) return;

                    if (elem.WordWrap)
                    {
                        float wrapWidth = elem.AutoSizeWidth
                        ? (elem.AutoSizeWidth_Max > 0 ? elem.AutoSizeWidth_Max : float.MaxValue)
                        : elem.Width;

                        if (pendingWordW > wrapWidth)
                        {
                            if (currentLineW > 0)
                            {
                                while (currentLine.Count > 0 && currentLine[^1].IsSpace)
                                {
                                    currentLineW -= font.SpaceWidth * scaleX * currentLine[^1].Fmt.SizeMult;
                                    currentLine.RemoveAt(currentLine.Count - 1);
                                }
                                linesW.Add(new List<Atom>(currentLine));
                                currentLine.Clear();
                                currentLineW = 0f;
                            }

                            var   charBuf = new System.Text.StringBuilder();
                            float bufW    = 0f;

                            void FlushCharBuf(FormatState fmt)
                            {
                                if (charBuf.Length == 0) return;
                                currentLine.Add(new Atom { Text = charBuf.ToString(), Fmt = fmt });
                                currentLineW += bufW;
                                charBuf.Clear();
                                bufW = 0f;
                            }

                            foreach (var atom in pendingWord)
                            {
                                foreach (char c in atom.Text)
                                {
                                    float cw = 0f;
                                    if (c == ' ')
                                        cw = font.SpaceWidth * scaleX * atom.Fmt.SizeMult;
                                    else if (font.TryGetCharInfo(c, out var ci))
                                        cw = ci.AdvanceWidth * scaleX * atom.Fmt.SizeMult
                                             * (atom.Fmt.Bold ? 1.08f : 1f)
                                             + elem.CharSpacing * atom.Fmt.SizeMult;

                                    if (currentLineW + bufW + cw > wrapWidth && (currentLineW > 0 || bufW > 0))
                                    {
                                        FlushCharBuf(atom.Fmt);
                                        linesW.Add(new List<Atom>(currentLine));
                                        currentLine.Clear();
                                        currentLineW = 0f;
                                    }

                                    charBuf.Append(c);
                                    bufW += cw;
                                }
                                FlushCharBuf(atom.Fmt);
                            }

                            pendingWord.Clear();
                            pendingWordW = 0f;
                            return;
                        }

                        if (currentLineW > 0 && currentLineW + pendingWordW > wrapWidth)
                        {
                            while (currentLine.Count > 0 && currentLine[^1].IsSpace)
                            {
                                currentLineW -= font.SpaceWidth * scaleX * currentLine[^1].Fmt.SizeMult;
                                currentLine.RemoveAt(currentLine.Count - 1);
                            }
                            linesW.Add(new List<Atom>(currentLine));
                            currentLine.Clear();
                            currentLineW = 0f;
                        }
                    }

                    currentLine.AddRange(pendingWord);
                    currentLineW += pendingWordW;
                    pendingWord.Clear();
                    pendingWordW = 0f;
                }

                foreach (var atom in atoms)
                {
                    if (atom.IsNewline)
                    {
                        CommitWord();
                        linesW.Add(new List<Atom>(currentLine));
                        currentLine.Clear();
                        currentLineW = 0f;
                    }
                    else if (atom.IsSpace)
                    {
                        CommitWord();
                        currentLine.Add(atom);
                        currentLineW += font.SpaceWidth * scaleX * atom.Fmt.SizeMult;
                    }
                    else
                    {
                        pendingWord.Add(atom);
                        pendingWordW += MeasureAtomWidth(atom);
                    }
                }
                CommitWord();
                while (currentLine.Count > 0 && currentLine[^1].IsSpace)
                    currentLine.RemoveAt(currentLine.Count - 1);
                if (currentLine.Count > 0 || linesW.Count == 0)
                    linesW.Add(new List<Atom>(currentLine));

                lineWidths  = new float[linesW.Count];
                lineHeights = new float[linesW.Count];
                for (int li2 = 0; li2 < linesW.Count; li2++)
                {
                    lineWidths [li2] = MeasureLine (linesW[li2]);
                    lineHeights[li2] = LineHeight  (linesW[li2]);
                }

                lines = linesW;
                elem._layoutLines       = lines;
                elem._layoutLineWidths  = lineWidths;
                elem._layoutLineHeights = lineHeights;
                elem._layoutKey         = newLayoutKey;
                elem._layoutValid       = true;
            }
            else
            {
                lines       = elem._layoutLines;
                lineWidths  = elem._layoutLineWidths;
                lineHeights = elem._layoutLineHeights;
            }

            // ── Line positions ────────────────────────────────────────────────────
            float totalHeight = 0f;
            for (int li = 0; li < lines.Count; li++) totalHeight += lineHeights[li];

            if (elem is UITextLog textLog)
                textLog.OnContentHeightUpdated(totalHeight);

            // ── Auto size ──────────────────────────────────────────────────────────

            if (elem.AutoSizeHeight)
            {
                float newH = totalHeight;
                if (elem.AutoSizeHeight_Max > 0 && newH > elem.AutoSizeHeight_Max)
                    newH = elem.AutoSizeHeight_Max;
                elem.Height = newH;
            }

            if (elem.AutoSizeWidth)
            {
                float maxW = 0f;
                foreach (var lw in lineWidths) if (lw > maxW) maxW = lw;
                if (elem.AutoSizeWidth_Max > 0 && maxW > elem.AutoSizeWidth_Max)
                    maxW = elem.AutoSizeWidth_Max;
                elem.Width = maxW;
            }

            // ── Scroll offset ──────────────────────────────────────────────────────

            if (elem.ScrollText)
            {
                float maxOffset;
                if (!elem.WordWrap)
                {
                    float maxLineW = 0f;
                    foreach (var lw in lineWidths) if (lw > maxLineW) maxLineW = lw;
                    maxOffset = Math.Max(0f, maxLineW - elem.Width);
                }
                else
                {
                    maxOffset = Math.Max(0f, totalHeight - elem.Height);
                }

                if (maxOffset <= 0f)
                {
                    elem._scrollOffset  = 0f;
                    elem._scrollDir     = 1f;
                    elem._scrollWaiting = false;
                }
                else if (elem._scrollOffset >= maxOffset)
                {
                    elem._scrollOffset = maxOffset;
                    if (!elem._scrollWaiting)
                    {
                        elem._scrollWaiting = true;
                        elem._scrollDelayMs = elem.ScrollTextDelay;
                    }
                }
                else if (elem._scrollOffset <= 0f)
                {
                    elem._scrollOffset = 0f;
                    if (!elem._scrollWaiting && elem._scrollDir < 0f)
                    {
                        elem._scrollWaiting = true;
                        elem._scrollDelayMs = elem.ScrollTextDelay;
                    }
                }
            }

            float yAlignOffset = elem.VAling switch
            {
                UIAling.Middle =>  (elem.Height - totalHeight) / 2f,
                UIAling.Bottom =>   elem.Height - totalHeight,
                _              =>  0f
            };

            var   lineStartXs = new float[lines.Count];
            var   lineStartYs = new float[lines.Count];
            float scrollVApply = 0f;

            if (elem.ScrollText && elem.WordWrap && totalHeight > elem.Height)
            {
                // Text overflows the element — scroll controls position, alignment doesn't apply
                if (elem.VAling == UIAling.Bottom)
                    scrollVApply = elem._scrollOffset;
                else
                {
                    yAlignOffset = 0f;
                    scrollVApply = -elem._scrollOffset;
                }
            }

            float curLineY = startY + yAlignOffset + scrollVApply;
            for (int li = 0; li < lines.Count; li++)
            {
                float lw     = lineWidths[li];
                float offset = elem.HAling switch
                {
                    UIAling.Middle => (elem.Width - lw) / 2f,
                    UIAling.Right  =>  elem.Width - lw,
                    _              =>  0f
                };

                float scrollApply = 0f;
                if (elem.ScrollText && !elem.WordWrap)
                {
                    if (elem.HAling == UIAling.Right)
                        scrollApply = (offset < 0f ? offset : 0f) + elem._scrollOffset;
                    else
                        scrollApply = -elem._scrollOffset;
                }

                if (offset < 0f) offset = 0f;
                lineStartXs[li] = startX + offset + scrollApply;
                lineStartYs[li] = curLineY;
                curLineY       += lineHeights[li];
            }

            // ── Clip range for line culling (scissor → screen-space Y, 0=top) ─────
            float clipTop    = float.MinValue;
            float clipBottom = float.MaxValue;
            var   clip       = _scissorStack.Current;
            if (clip.HasValue)
            {
                // GL scissor stores Y=0-at-bottom; convert to screen Y (0=top)
                clipTop    = screenHeight - (clip.Value.Y + clip.Value.Height);
                clipBottom = screenHeight - clip.Value.Y;
            }

            // ── Bind text shader + VAO ────────────────────────────────────────────
            textShader.Use();
            gl.BindVertexArray(textVao);

            var decorations = new List<(float x, float y, float w, float h, RGBA col)>();

            // ── Per-atom shader setup ─────────────────────────────────────────────
            void PrepareAtom(Atom atom, int renderMode)
            {
                float effScale = scale * atom.Fmt.SizeMult;
                float aRMin    = elem.RangeMin >= 0 ? elem.RangeMin : AutoRangeMin(effScale);
                float aRMax    = elem.RangeMax >= 0 ? elem.RangeMax : AutoRangeMax(effScale);
                float bias     = atom.Fmt.Bold ? -0.07f : 0f;
                float effRMin  = aRMin + bias;

                textShader.SetFloat("rangeMin", effRMin);
                textShader.SetFloat("rangeMax", aRMax + bias);

                if (renderMode == 1)
                {
                    float outW = elem.OutlineEnabled
                        ? (elem.OutlineWidth >= 1f
                            ? elem.OutlineWidth / (font.MaxHeight * effScale)
                            : elem.OutlineWidth)
                        : 0f;
                    textShader.SetFloat("outlineMin", Math.Max(0f, effRMin - outW));
                    textShader.SetFloat("outlineMax", effRMin);
                }
            }

            // ── Batched character renderer ────────────────────────────────────────
            int _batchCount = 0;
            //int _batchTex   = -1;   old one

            void FlushBatch()
            {/*
                if (_batchCount == 0 || _batchTex < 0) return;
                gl.BindBuffer(GLEnum.ArrayBuffer, textVbo);
                gl.BufferSubData<float>(GLEnum.ArrayBuffer, 0,
                    _textVerts.AsSpan(0, _batchCount * 16));
                gl.ActiveTexture(TextureUnit.Texture0);
                gl.BindTexture(TextureTarget.Texture2D, (uint)_batchTex);
                textShader.SetInt("fontAtlas", 0);
                unsafe { gl.DrawElements(PrimitiveType.Triangles, (uint)(_batchCount * 6),
                                         DrawElementsType.UnsignedInt, null); }
                _batchCount = 0;*/

                if (_batchCount == 0) return;
                gl.BindBuffer(GLEnum.ArrayBuffer, textVbo);
                gl.BufferSubData<float>(GLEnum.ArrayBuffer, 0, _textVerts.AsSpan(0, _batchCount * 20));
                gl.ActiveTexture(TextureUnit.Texture0);
                gl.BindTexture(TextureTarget.Texture2DArray, (uint)font.ArrayTextureID);
                textShader.SetInt("fontAtlas", 0);
                unsafe { gl.DrawElements(PrimitiveType.Triangles, (uint)(_batchCount * 6), DrawElementsType.UnsignedInt, null); }
                _batchCount = 0;

                _frameTextCalls++; //debug
            }

            void DrawAtomChars(Atom atom, ref float rx, float ry, bool collectDecos)
            {
                if (atom.IsNewline) return;
                if (atom.IsSpace)
                {
                    float sw = font.SpaceWidth * scaleX * atom.Fmt.SizeMult;
                    if (collectDecos && (atom.Fmt.Underline || atom.Fmt.Strikethrough))
                    {
                        float sy2   = scaleY * atom.Fmt.SizeMult;
                        float ch2   = font.MaxHeight * sy2;
                        float padY2 = font.SdfPadding * sy2;
                        float visH2 = ch2 - 2f * padY2;
                        float thk2  = Math.Max(1f, visH2 * 0.07f);
                        RGBA col2  = atom.Fmt.Color ?? elem.FontColor;
                        if (atom.Fmt.Underline)
                            decorations.Add((rx, ry + padY2 + visH2 * 0.85f, sw, thk2, col2));
                        if (atom.Fmt.Strikethrough)
                            decorations.Add((rx, ry + padY2 + visH2 * 0.45f, sw, thk2, col2));
                    }

                    if (tf != null && charCount == tf.CursorPosition)
                    { cursorX = rx; cursorY = ry; cursorH = font.MaxHeight * scaleY; }

                    if (tf != null && tf._charXPositions != null && charCount < tf._charXPositions.Length)
                        tf._charXPositions[charCount] = rx - elem.GetAbsX() + tf._xOffset;

                    charCount++;

                    rx += sw;
                    return;
                }

                float sx         = scaleX * atom.Fmt.SizeMult * (atom.Fmt.Bold ? 1.08f : 1f);
                float sy         = scaleY * atom.Fmt.SizeMult;
                float charGap    = elem.CharSpacing * atom.Fmt.SizeMult;
                float slant      = atom.Fmt.Italic ? 0.25f : 0f;
                float atomStartX = rx;

                foreach (char c in atom.Text)
                {
                    if (tf != null && charCount == tf.CursorPosition)
                    { cursorX = rx; cursorY = ry; cursorH = font.MaxHeight * scaleY; }

                    if (tf != null && tf._charXPositions != null && charCount < tf._charXPositions.Length)
                        tf._charXPositions[charCount] = rx - elem.GetAbsX() + tf._xOffset;


                    charCount++;

                    if (c == ' ') { rx += font.SpaceWidth * sx + charGap; continue; }
                    if (!font.TryGetCharInfo(c, out var ci)) continue;

                    /* old one
                    if (ci.TextureID != _batchTex || _batchCount >= _maxTextChars)
                    {
                        FlushBatch();
                        _batchTex = ci.TextureID;
                    }

                    float x  = rx, y = ry;
                    float w  = ci.DrawWidth  * sx;
                    float h  = ci.DrawHeight * sy;
                    float so = h * slant;

                    int b = _batchCount * 16;
                    _textVerts[b+ 0] = x + so;     _textVerts[b+ 1] = y;   _textVerts[b+ 2] = ci.U0; _textVerts[b+ 3] = ci.V1;
                    _textVerts[b+ 4] = x + w + so; _textVerts[b+ 5] = y;   _textVerts[b+ 6] = ci.U1; _textVerts[b+ 7] = ci.V1;
                    _textVerts[b+ 8] = x + w;      _textVerts[b+ 9] = y+h; _textVerts[b+10] = ci.U1; _textVerts[b+11] = ci.V0;
                    _textVerts[b+12] = x;          _textVerts[b+13] = y+h; _textVerts[b+14] = ci.U0; _textVerts[b+15] = ci.V0;
                    _batchCount++;
                    */

                    if(_batchCount >= _maxTextChars) FlushBatch();

                    float x = rx, y = ry;
                    float w = ci.DrawWidth * sx;
                    float h = ci.DrawHeight * sy;
                    float so = h * slant;

                    float ti = (float)ci.TextureID;  // layer index
                    int b = _batchCount * 20;
                    _textVerts[b + 0] = x + so; _textVerts[b + 1] = y; _textVerts[b + 2] = ci.U0; _textVerts[b + 3] = ci.V1; _textVerts[b + 4] = ti;
                    _textVerts[b + 5] = x + w + so; _textVerts[b + 6] = y; _textVerts[b + 7] = ci.U1; _textVerts[b + 8] = ci.V1; _textVerts[b + 9] = ti;
                    _textVerts[b + 10] = x + w; _textVerts[b + 11] = y + h; _textVerts[b + 12] = ci.U1; _textVerts[b + 13] = ci.V0; _textVerts[b + 14] = ti;
                    _textVerts[b + 15] = x; _textVerts[b + 16] = y + h; _textVerts[b + 17] = ci.U0; _textVerts[b + 18] = ci.V0; _textVerts[b + 19] = ti;
                    _batchCount++;

                    rx += ci.AdvanceWidth * sx + charGap;
                }

                //FlushBatch();

                if (collectDecos)
                {
                    float charH  = font.MaxHeight * sy;
                    float padY   = font.SdfPadding * sy;
                    float padX   = font.SdfPadding * sx;
                    float visH   = charH - 2f * padY;
                    float atomW  = rx - atomStartX;
                    float thick  = Math.Max(1f, visH * 0.07f);
                    float decoX  = atomStartX + padX;
                    RGBA col    = atom.Fmt.Color ?? elem.FontColor;
                    if (atom.Fmt.Underline)
                        decorations.Add((decoX, ry + padY + visH * 0.85f, atomW, thick, col));
                    if (atom.Fmt.Strikethrough)
                        decorations.Add((decoX, ry + padY + visH * 0.45f, atomW, thick, col));
                }
            }

            // ── Shadow pass ───────────────────────────────────────────────────────
            if (elem.ShadowEnabled)
            {
                textShader.SetInt("renderMode", 2);
                textShader.SetVector4("color", new Vector4(elem.ShadowColor.R, elem.ShadowColor.G,
                                                           elem.ShadowColor.B, elem.ShadowColor.A * fade));
                for (int li = 0; li < lines.Count; li++)
                {
                    float lineTop = lineStartYs[li] + elem.ShadowOffsetY;
                    if (lineTop + lineHeights[li] <= clipTop || lineTop >= clipBottom) continue;
                    float curX = lineStartXs[li] + elem.ShadowOffsetX;
                    float curY = lineTop;
                    foreach (var atom in lines[li])
                    {
                        if (atom.IsContent) PrepareAtom(atom, 2);
                        DrawAtomChars(atom, ref curX, curY, false);
                    }
                }
                FlushBatch();
            }

            // ── Outline pass ──────────────────────────────────────────────────────
            if (elem.OutlineEnabled)
            {
                textShader.SetInt("renderMode", 1);
                textShader.SetVector4("outlineColor", new Vector4(elem.OutlineColor.R, elem.OutlineColor.G,
                                                                  elem.OutlineColor.B, elem.OutlineColor.A * fade));
                for (int li = 0; li < lines.Count; li++)
                {
                    float lineTop = lineStartYs[li];
                    if (lineTop + lineHeights[li] <= clipTop || lineTop >= clipBottom) continue;
                    float curX = lineStartXs[li];
                    float curY = lineTop;
                    foreach (var atom in lines[li])
                    {
                        if (atom.IsContent) PrepareAtom(atom, 1);
                        DrawAtomChars(atom, ref curX, curY, false);
                    }
                }
                FlushBatch();
            }


            // ── Fill pass ─────────────────────────────────────────────────────────
            textShader.SetInt("renderMode", 2);

            float lastCurX = lineStartXs[0];
            float lastCurY = lineStartYs[0];
            RGBA? lastFillColor = null;

            if (tf != null)
                tf._charXPositions = new float[elem.Text.Length + 1];

            for (int li = 0; li < lines.Count; li++)
            {
                float lineTop = lineStartYs[li];
                if (lineTop + lineHeights[li] <= clipTop || lineTop >= clipBottom) continue;
                float curX = lineStartXs[li];
                float curY = lineTop;
                foreach (var atom in lines[li])
                {
                    if (atom.IsContent)
                    {
                        PrepareAtom(atom, 2);
                        RGBA c = atom.Fmt.Color ?? elem.FontColor;
                        if (!lastFillColor.HasValue || !lastFillColor.Value.Equals(c))
                        {
                            FlushBatch();
                            textShader.SetVector4("color", new Vector4(c.R, c.G, c.B, c.A * fade));
                            lastFillColor = c;
                        }
                    }
                    DrawAtomChars(atom, ref curX, curY, true);

                    //charCount += atom.IsContent ? atom.Text.Length : (atom.IsSpace ? 1 : 0);
                    //if (tf != null && charCount == tf.CursorPosition) { cursorX = curX; cursorY = curY; cursorH = lineHeights[li]; };
                    lastCurX = curX;
                    lastCurY = curY;

                }
            }

            FlushBatch();


            if (tf != null && tf._charXPositions != null && charCount < tf._charXPositions.Length)
                tf._charXPositions[charCount] = lastCurX - elem.GetAbsX();

            if (tf != null && cursorX < 0)
            {
                float spaceW = font.SpaceWidth * scaleX;
                int extraSpaces = tf.CursorPosition - charCount;
                cursorX = lastCurX + spaceW * Math.Max(0, extraSpaces);
                cursorY = lastCurY;
                cursorH = font.MaxHeight * scaleY;
            }

            if (tf != null && tf.CursorPosition == 0) { cursorX = lineStartXs[0]; cursorY = lineStartYs[0]; cursorH = lineHeights[0]; };

            if (tf != null) tf._cursorRawX = cursorX - elem.GetAbsX() + tf._xOffset;



            if (tf != null && cursorX >= 0 && tf.cursorShow)
            {
                float cursorWidth = 2f;
                DrawQuad(cursorX+2, cursorY, cursorWidth, cursorH, tf.CursorColor);

            }



            // if (tf != null) Console.WriteLine($"cursor: tf={tf != null} cursorX={cursorX} cursorShow={tf?.cursorShow}");

            gl.BindVertexArray(0);

            // ── Decorations (underline / strikethrough) ───────────────────────────
            if (decorations.Count > 0)
            {
                shader.Use();
                shader.SetInt("useTexture", 0);
                foreach (var (x, y, w, h, col) in decorations)
                    DrawQuad(x, y, w, h, new RGBA(col.R, col.G, col.B, col.A * fade));
            }

            // DrawText binds the font atlas in Texture0 and does not restore the
            // previous binding. Reset the cache so the next DrawQuad always rebinds
            // its own texture instead of accidentally sampling the font atlas.
            _lastBoundTextureId = -1;
        }
    }
}