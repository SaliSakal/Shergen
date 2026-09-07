using Silk.NET.OpenGL;
using System.Numerics;


namespace Shergen.Renderer
{
    public partial class RendererGL : IRenderer
    {
        // ── Font cache ────────────────────────────────────────────────────────────

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
                raw.UploadToGPU(gl);
                var dffFont = new DFFFont(name, raw);

                if (_pendingFallbacks.TryGetValue(name, out char fb))
                    dffFont.FallbackChar = fb;

                _fonts[name] = dffFont;
                Console.WriteLine($"✅ DFF loaded: {name} — {raw.Characters.Count} chars, {raw.Textures.Count} textures");

                _fontsMetrics.Remove(name);
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

        // ── Layout (CPU only, safe to call from Lua thread) ───────────────────────

        public void MeasureEl(UILabel elem)
        {
            if (string.IsNullOrEmpty(elem.Text) && elem is not UITextField) return;

            var font = GetFontMetrics(elem.Font);
            if (font == null) return;

            UITextField tf = elem as UITextField;

            // ── Scale ─────────────────────────────────────────────────────────────
            const float DFF_PT_SCALE = 1.8f;
            float scale  = (elem.FontSize / font.MaxHeight) * (font.IsSDF ? DFF_PT_SCALE : 1f);
            float scaleX = scale * (elem.CharScaleX / 100f);
            float scaleY = scale * (elem.CharScaleY / 100f);

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
                if (atom.Fmt.IconTag != null && IconRegistry.TryGet(atom.Fmt.IconTag, out var iconM))
                {
                    float ih = font.MaxHeight * scaleY * atom.Fmt.SizeMult * iconM.Scale;
                    return ih * (iconM.TexH > 0 ? iconM.TexW / (float)iconM.TexH : 1f);
                }
                if (atom.IsSpace)   return font.SpaceWidth * scaleX * atom.Fmt.SizeMult;
                if (atom.IsNewline) return 0f;
                float sx      = scaleX * atom.Fmt.SizeMult * (atom.Fmt.Bold ? 1.08f : 1f);
                float charGap = elem.CharSpacing * atom.Fmt.SizeMult;
                float w       = 0f;
                foreach (char c in atom.Text)
                    if (c == ' ')
                        w += font.SpaceWidth * sx;
                    else if (font.TryGetCharInfo(c, out var ci))
                        w += ci.AdvanceWidth * sx + charGap;
                return w;
            }

            float MeasureLine(List<Atom> line)
            {
                float w      = 0f;
                float lastSx = scaleX;
                bool hasChar = false;
                foreach (var atom in line)
                {
                    if (atom.IsNewline) continue;
                    if (atom.IsStripped) continue;//return 0f;
                    if (atom.IsSpace)
                    {
                        w      += font.SpaceWidth * scaleX * atom.Fmt.SizeMult;
                        hasChar = false;
                        continue;
                    }
                    if (atom.Fmt.IconTag != null && IconRegistry.TryGet(atom.Fmt.IconTag, out var iconL))
                    {
                        float ih = font.MaxHeight * scaleY * atom.Fmt.SizeMult * iconL.Scale;
                        w += ih * (iconL.TexH > 0 ? iconL.TexW / (float)iconL.TexH : 1f);
                        hasChar = true;
                        lastSx = scaleX * atom.Fmt.SizeMult;
                        continue;
                    }

                    float sx      = scaleX * atom.Fmt.SizeMult * (atom.Fmt.Bold ? 1.08f : 1f);
                    float charGap = elem.CharSpacing * atom.Fmt.SizeMult;
                    foreach (char c in atom.Text)
                        if (c == ' ')
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
            /*
            float LineHeight(List<Atom> line)
            {
                float maxMult = 1f;
                float maxIconExtent = 0f;
                foreach (var a in line)
                {
                    if (!a.IsContent) continue;
                    if (a.Fmt.SizeMult > maxMult) maxMult = a.Fmt.SizeMult;

                    if (a.Fmt.IconTag != null && IconRegistry.TryGet(a.Fmt.IconTag, out var iconLH))
                    {
                        float iconMult = a.Fmt.SizeMult * iconLH.Scale;
                        float extent = iconMult + Math.Abs(a.Fmt.SizeMult * iconLH.OffsetY);
                        if (extent > maxIconExtent) maxIconExtent = extent;
                    }
                }
                float effMult = Math.Max(maxMult, maxIconExtent);
                return font.MaxHeight * scaleY * effMult + elem.LineSpacing * effMult;
            }
            */

            // ── Layout cache ──────────────────────────────────────────────────────
            var newLayoutKey = (elem.Text,
                                elem.AutoSizeWidth ? elem.AutoSizeWidth_Max : elem.Width,
                                elem.FontSize,
                                elem.CharScaleX, elem.CharScaleY,
                                elem.CharSpacing, elem.LineSpacing,
                                elem.WordWrap, elem.Font ?? "",
                                IconRegistry.Version);

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
                                /*while (currentLine.Count > 0 && currentLine[^1].IsSpace)
                                {
                                    currentLineW -= font.SpaceWidth * scaleX * currentLine[^1].Fmt.SizeMult;
                                    currentLine.RemoveAt(currentLine.Count - 1);
                                }*/

                                for (int si = currentLine.Count - 1; si >= 0 && currentLine[si].IsSpace; si--)
                                {
                                    currentLineW -= font.SpaceWidth * scaleX * currentLine[si].Fmt.SizeMult;
                                    var a = currentLine[si];
                                    currentLine[si] = new Atom { Fmt = a.Fmt, IsStripped = true, Text = string.Empty }; // invisible, ale charCount ho počítá
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
                                    if (c == ' ')
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
                            for (int si = currentLine.Count - 1; si >= 0 && currentLine[si].IsSpace; si--)
                            {
                                currentLineW -= font.SpaceWidth * scaleX * currentLine[si].Fmt.SizeMult;
                                var a = currentLine[si];
                                currentLine[si] = new Atom { Fmt = a.Fmt, IsStripped = true, Text = string.Empty };
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
                        currentLine.Add(atom);  // ← tohle přidej
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
                for (int si = currentLine.Count - 1; si >= 0 && currentLine[si].IsSpace; si--)
                    currentLine[si] = new Atom { Fmt = currentLine[si].Fmt, IsStripped = true, Text = string.Empty };

                bool endsWithNewline = atoms.Count > 0 && atoms[^1].IsNewline;
                if (currentLine.Count > 0 || linesW.Count == 0 || endsWithNewline)
                    linesW.Add(new List<Atom>(currentLine));

                var lineWidths  = new float[linesW.Count];
                var lineHeights = new float[linesW.Count];
                for (int li2 = 0; li2 < linesW.Count; li2++)
                {
                    lineWidths [li2] = MeasureLine (linesW[li2]);
                    lineHeights[li2] = LineHeight  (linesW[li2]);
                }

                elem._layoutLines       = linesW;
                elem._layoutLineWidths  = lineWidths;
                elem._layoutLineHeights = lineHeights;
                elem._layoutKey         = newLayoutKey;
                elem._layoutValid       = true;

                for (int dbgI = 0; dbgI < linesW.Count; dbgI++)
                {
                    string txt = string.Concat(linesW[dbgI].Select(a => a.Text));
                    //Console.WriteLine($"[LAYOUT] line {dbgI}: \"{txt}\"");
                }
            }

            // ── Total height ──────────────────────────────────────────────────────
            float totalHeight = 0f;
            for (int li = 0; li < elem._layoutLines.Count; li++)
                totalHeight += elem._layoutLineHeights[li];

            if (elem is UITextLog textLog)
                textLog.OnContentHeightUpdated(totalHeight);

            if (elem is UITextField textField)
                textField.OnContentHeightUpdated(totalHeight);

            // ── Auto size ─────────────────────────────────────────────────────────
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
                foreach (var lw in elem._layoutLineWidths) if (lw > maxW) maxW = lw;
                if (elem.AutoSizeWidth_Max > 0 && maxW > elem.AutoSizeWidth_Max)
                    maxW = elem.AutoSizeWidth_Max;
                elem.Width = maxW;
            }
        }

        // ── Text rendering (GL thread only) ───────────────────────────────────────

        public void DrawText(UILabel elem)
        {
            if (string.IsNullOrEmpty(elem.Text) && elem is not UITextField) return;

            var font = GetFont(elem.Font);
            if (font == null) return;

            MeasureEl(elem);  // fast no-op when key unchanged; fallback recompute otherwise
            if (elem._layoutLines == null) return;

            UITextField tf = elem as UITextField;

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

            float fade = elem.GetEffectiveFade();

            // ── Read cached layout ────────────────────────────────────────────────
            var lines       = elem._layoutLines;
            var lineWidths  = elem._layoutLineWidths;
            var lineHeights = elem._layoutLineHeights;

            float totalHeight = 0f;
            for (int li = 0; li < lines.Count; li++) totalHeight += lineHeights[li];

            // ── ScrollText timing ─────────────────────────────────────────────────
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
                    elem._scrollOffset += elem.ScrollTextSpeed * elem._scrollDir * dt;

                // ── ScrollText clamp ───────────────────────────────────────────────
                float maxOffset;
                if (!elem.WordWrap)
                {
                    float maxLineW = 0f;
                    foreach (var lw in lineWidths) if (lw > maxLineW) maxLineW = lw;
                    maxOffset = Math.Max(0f, maxLineW - elem.Width);
                }
                else
                    maxOffset = Math.Max(0f, totalHeight - elem.Height);

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

            // ── Absolute position ─────────────────────────────────────────────────
            float startX = elem.GetAbsX() - (elem is UITextField ? tf._xOffset : 0f);
            float startY = elem.GetAbsY()
                - (elem is UITextLog tlog ? tlog._logScrollOffset : 0f)
                - (elem is UITextField tfScroll && tfScroll.WordWrap ? tfScroll._yOffset : 0f);

            // ── Line positions ────────────────────────────────────────────────────
            float yAlignOffset = elem.VAling switch
            {
                UIAling.Middle => (elem.Height - totalHeight) / 2f,
                UIAling.Bottom =>  elem.Height - totalHeight,
                _              =>  0f
            };

            var   lineStartXs  = new float[lines.Count];
            var   lineStartYs  = new float[lines.Count];
            float scrollVApply = 0f;

            if (elem.ScrollText && elem.WordWrap && totalHeight > elem.Height)
            {
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
                float bias     = atom.Fmt.Bold ? -0.11f : 0f;
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
            float cursorX = -1, cursorY = -1, cursorH = 0;
            int   charCount   = 0;
            int   _batchCount = 0;
            float[] tfXs = null, tfYs = null;

            void FlushBatch()
            {
                if (_batchCount == 0) return;
                gl.BindBuffer(GLEnum.ArrayBuffer, textVbo);
                gl.BufferSubData<float>(GLEnum.ArrayBuffer, 0, _textVerts.AsSpan(0, _batchCount * 20));
                gl.ActiveTexture(TextureUnit.Texture0);
                gl.BindTexture(TextureTarget.Texture2DArray, (uint)font.ArrayTextureID);
                textShader.SetInt("fontAtlas", 0);
                unsafe { gl.DrawElements(PrimitiveType.Triangles, (uint)(_batchCount * 6), DrawElementsType.UnsignedInt, null); }
                _batchCount = 0;
                _frameTextCalls++;
            }

            void DrawAtomChars(Atom atom, ref float rx, float ry, bool collectDecos, bool emit = true)
            {
                if (atom.IsNewline || atom.IsStripped)
                {
                    if (tf != null)
                    {
                        if (charCount == tf._rendererCursorSlot)
                        { cursorX = rx; cursorY = ry; cursorH = font.MaxHeight * scaleY; tf._cursorLineHeight = cursorH; }
                        if (tfXs != null && charCount < tfXs.Length)
                            // \n → float.MaxValue jako sentinel (cursor zůstane na tomto řádku)
                            // IsStripped → skutečná X pozice (lze přejít na další řádek)
                            tfXs[charCount] = atom.IsNewline
                                ? float.MaxValue
                                : rx - elem.GetAbsX() + tf._xOffset;
                        if (tfYs != null && charCount < tfYs.Length)
                            tfYs[charCount] = ry - elem.GetAbsY() + tf._yOffset;
                        charCount++;
                    }
                    // IsStripped = wordwrap trailing space — dekorace musí pokračovat
                    if (collectDecos && atom.IsStripped && (atom.Fmt.Underline || atom.Fmt.Strikethrough))
                    {
                        float sw2   = font.SpaceWidth * scaleX * atom.Fmt.SizeMult;
                        float sy2   = scaleY * atom.Fmt.SizeMult;
                        float padY2 = font.SdfPadding * sy2;
                        float padX2 = font.SdfPadding * scaleX * atom.Fmt.SizeMult;
                        float visH2 = (font.MaxHeight * sy2) - 2f * padY2;
                        float thk2  = Math.Max(1f, visH2 * 0.07f);
                        RGBA  col2  = atom.Fmt.Color ?? elem.FontColor;
                        if (atom.Fmt.Underline)     decorations.Add((rx, ry + padY2 + visH2 * 0.85f, sw2 + padX2, thk2, col2));
                        if (atom.Fmt.Strikethrough) decorations.Add((rx, ry + padY2 + visH2 * 0.45f, sw2 + padX2, thk2, col2));
                    }

                    if (atom.IsStripped)
                        rx += font.SpaceWidth * scaleX * atom.Fmt.SizeMult;

                    return;
                }
                if (atom.IsSpace)
                {
                    float sw = font.SpaceWidth * scaleX * atom.Fmt.SizeMult;
                    if (collectDecos && (atom.Fmt.Underline || atom.Fmt.Strikethrough))
                    {
                        float sy2   = scaleY * atom.Fmt.SizeMult;
                        float padY2 = font.SdfPadding * sy2;
                        float padX2 = font.SdfPadding * scaleX * atom.Fmt.SizeMult;
                        float visH2 = (font.MaxHeight * sy2) - 2f * padY2;
                        float thk2  = Math.Max(1f, visH2 * 0.07f);
                        RGBA  col2  = atom.Fmt.Color ?? elem.FontColor;
                        if (atom.Fmt.Underline)
                            decorations.Add((rx, ry + padY2 + visH2 * 0.85f, sw + padX2, thk2, col2));
                        if (atom.Fmt.Strikethrough)
                            decorations.Add((rx, ry + padY2 + visH2 * 0.45f, sw + padX2, thk2, col2));
                    }

                    if (tf != null && charCount == tf._rendererCursorSlot)
                    { cursorX = rx; cursorY = ry; cursorH = font.MaxHeight * scaleY; tf._cursorLineHeight = cursorH; }

                    if (tf != null && tfXs != null && charCount < tfXs.Length)
                        tfXs[charCount] = rx - elem.GetAbsX() + tf._xOffset;

                    if (tf != null && tfYs != null && charCount < tfYs.Length)
                        tfYs[charCount] = ry - elem.GetAbsY() + tf._yOffset;

                    charCount++;
                    rx += sw;
                    return;
                }
                if (atom.Fmt.IconTag != null)
                {

                    if (!IconRegistry.TryGet(atom.Fmt.IconTag, out var icon))
                        return;

                    float fontH = font.MaxHeight * scaleY;
                    float iconH = fontH * atom.Fmt.SizeMult * icon.Scale;
                    float iconW = iconH * (icon.TexH > 0 ? icon.TexW / (float)icon.TexH : 1f);
                    float offX = fontH * atom.Fmt.SizeMult * (icon.OffsetX + 0.1f);  // 0.1f = vizuální korekce, aby ikona nebyla moc vlevo
                    float offY = fontH * atom.Fmt.SizeMult * icon.OffsetY;

                    //Console.WriteLine($"[ICON-POS] rx={rx} offX={offX} iconW={iconW} drawX={rx + offX} drawXEnd={rx + offX + iconW} texW={icon.TexW} texH={icon.TexH}");

                    if (tf != null && charCount == tf._rendererCursorSlot)
                    { cursorX = rx; cursorY = ry; cursorH = fontH; }

                    if (tf != null && tfXs != null && charCount < tfXs.Length)
                        tfXs[charCount] = rx - elem.GetAbsX() + tf._xOffset;
                    if (tf != null && tfYs != null && charCount < tfYs.Length)
                        tfYs[charCount] = ry - elem.GetAbsY() + tf._yOffset;
                    charCount++;

                    if (emit)
                    {
                        RGBA tint = icon.Color ?? (atom.Fmt.Color ?? elem.FontColor);
                        FlushBatch();   // vykresli rozpracovanou dávku glyphů dřív, než přepneme shader/VAO
                        DrawQuad(rx + offX, ry + offY, iconW, iconH,
                                 new RGBA(tint.R, tint.G, tint.B, tint.A * fade), icon.TextureID);
                        textShader.Use();          // vrať stav zpátky pro pokračování v batchování textu
                        gl.BindVertexArray(textVao);
                    }

                    rx += iconW;
                    return;
                }

                float sx         = scaleX * atom.Fmt.SizeMult * (atom.Fmt.Bold ? 1.08f : 1f);
                float sy         = scaleY * atom.Fmt.SizeMult;
                float charGap    = elem.CharSpacing * atom.Fmt.SizeMult;
                float slant      = atom.Fmt.Italic ? 0.25f : 0f;
                float atomStartX = rx;

                // Italic korekce: posuň start doleva o cca slant * descent
                if (atom.Fmt.Italic)
                {
                    float visH = (font.MaxHeight - 2f * font.SdfPadding) * sy;
                    rx -= slant * visH * 0.25f;  // 0.25 = přibližný poměr descend/height
                }

                foreach (char c in atom.Text)
                {
                    if (tf != null && charCount == tf._rendererCursorSlot)
                    { cursorX = rx; cursorY = ry; cursorH = font.MaxHeight * scaleY; tf._cursorLineHeight = cursorH; }

                    if (tf != null && tfXs != null && charCount < tfXs.Length)
                        tfXs[charCount] = rx - elem.GetAbsX() + tf._xOffset;

                    if (tf != null && tfYs != null && charCount < tfYs.Length)
                        tfYs[charCount] = ry - elem.GetAbsY() + tf._yOffset;

                    charCount++;

                    if (c == ' ') { rx += font.SpaceWidth * sx + charGap; continue; }
                    if (!font.TryGetCharInfo(c, out var ci)) continue;
                    if (ci.TextureID < 0) { rx += ci.AdvanceWidth * sx + charGap; continue; }  // advance-only (např. NBSP fallback)
                    if (emit)
                    {
                        if (_batchCount >= _maxTextChars) FlushBatch();

                        float x = rx, y = ry;
                        float w = ci.DrawWidth * sx;
                        float h = ci.DrawHeight * sy;
                        float so = h * slant;
                        float ti = (float)ci.TextureID;

                        int b = _batchCount * 20;
                        _textVerts[b + 0] = x + so; _textVerts[b + 1] = y; _textVerts[b + 2] = ci.U0; _textVerts[b + 3] = ci.V1; _textVerts[b + 4] = ti;
                        _textVerts[b + 5] = x + w + so; _textVerts[b + 6] = y; _textVerts[b + 7] = ci.U1; _textVerts[b + 8] = ci.V1; _textVerts[b + 9] = ti;
                        _textVerts[b + 10] = x + w; _textVerts[b + 11] = y + h; _textVerts[b + 12] = ci.U1; _textVerts[b + 13] = ci.V0; _textVerts[b + 14] = ti;
                        _textVerts[b + 15] = x; _textVerts[b + 16] = y + h; _textVerts[b + 17] = ci.U0; _textVerts[b + 18] = ci.V0; _textVerts[b + 19] = ti;
                        _batchCount++;
                    }
                    rx += ci.AdvanceWidth * sx + charGap;
                    //Console.WriteLine($"[CHAR] c='{c}' rx_before={rx} advance={ci.AdvanceWidth * sx + charGap}");
                }

                // Po atomu: přidej trailing korekci
                if (atom.Fmt.Italic)
                {
                    float visH = (font.MaxHeight - 2f * font.SdfPadding) * sy;
                    rx += slant * visH * 0.75f;  // ascend část
                }

                if (collectDecos)
                {
                    float charH = font.MaxHeight * sy;
                    float padY  = font.SdfPadding * sy;
                    float padX  = font.SdfPadding * sx;
                    float visH  = charH - 2f * padY;
                    float atomW = rx - atomStartX;
                    float thick = Math.Max(1f, visH * 0.07f);
                    float decoX = atomStartX + padX;
                    RGBA  col   = atom.Fmt.Color ?? elem.FontColor;
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
                    bool? shadowLastBold = null;
                    foreach (var atom in lines[li])
                    {
                        if (atom.IsContent)
                        {
                            if (!shadowLastBold.HasValue || shadowLastBold.Value != atom.Fmt.Bold)
                            {
                                FlushBatch();
                                PrepareAtom(atom, 2);
                                shadowLastBold = atom.Fmt.Bold;
                            }
                        }
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
                    bool? outlineLastBold = null;
                    foreach (var atom in lines[li])
                    {
                        if (atom.IsContent)
                        {
                            if (!outlineLastBold.HasValue || outlineLastBold.Value != atom.Fmt.Bold)
                            {
                                FlushBatch();
                                PrepareAtom(atom, 1);
                                outlineLastBold = atom.Fmt.Bold;
                            }
                        }
                        DrawAtomChars(atom, ref curX, curY, false);
                    }
                }
                FlushBatch();
            }

            // ── Fill pass ─────────────────────────────────────────────────────────
            textShader.SetInt("renderMode", 2);

            float lastCurX      = lineStartXs[0];
            float lastCurY      = lineStartYs[0];
            RGBA? lastFillColor = null;
            bool? lastBold      = null;
            
            if (tf != null)
            {
                tfXs = new float[elem.Text.Length + 1];
                tfYs = new float[elem.Text.Length + 1];
                tf.UpdateRendererCursorSlot();
            }

            for (int li = 0; li < lines.Count; li++)
            {
                float lineTop = lineStartYs[li];
                //if (lineTop + lineHeights[li] <= clipTop || lineTop >= clipBottom) continue;
                bool lineVisible = !(lineTop + lineHeights[li] <= clipTop || lineTop >= clipBottom);
                string txt = string.Concat(lines[li].Select(a => a.Text));
                //Console.WriteLine($"[FILL] li={li} lineTop={lineTop} visible={lineVisible} text=\"{txt}\""); 
                float curX = lineStartXs[li];
                float curY = lineTop;

                lastCurX = curX;   // ← přidej: i prázdná řádka musí mít vlastní pozici pro cursor fallback
                lastCurY = curY;
                foreach (var atom in lines[li])
                {
                    if (atom.IsContent)
                    {
                        RGBA c = atom.Fmt.Color ?? elem.FontColor;
                        bool colorChanged = !lastFillColor.HasValue || !lastFillColor.Value.Equals(c);
                        bool boldChanged  = !lastBold.HasValue || lastBold.Value != atom.Fmt.Bold;
                        if (lineVisible && (colorChanged || boldChanged))
                        {
                            FlushBatch();               // flush BEFORE changing uniforms
                            PrepareAtom(atom, 2);       // pak nastavit nové rangeMin/rangeMax
                            if (colorChanged)
                            {
                                textShader.SetVector4("color", new Vector4(c.R, c.G, c.B, c.A * fade));
                                lastFillColor = c;
                            }
                            lastBold = atom.Fmt.Bold;
                        }
                    }
                    //float curXBefore = curX;
                    DrawAtomChars(atom, ref curX, curY, lineVisible, lineVisible);
                    //Console.WriteLine($"[OUTER] text='{atom.Text}' isIcon={atom.Fmt.IconTag != null} before={curXBefore} after={curX}");

                    lastCurX = curX;
                    lastCurY = curY;
                }
            }

            FlushBatch();

            if (tf != null)
            {
                if (tfXs != null && charCount < tfXs.Length)
                    tfXs[charCount] = lastCurX - elem.GetAbsX();

                if (tfYs != null && charCount < tfYs.Length)
                    tfYs[charCount] = lastCurY - elem.GetAbsY() + tf._yOffset;

                tf._charXPositions = tfXs;
                tf._charYPositions = tfYs;
                tf._charCount = charCount; // charCount = počet vizuálních znaků
            
                if (cursorX < 0)
                {
                    float spaceW     = font.SpaceWidth * scaleX;
                    int   extraSpaces = tf._rendererCursorSlot - charCount;
                    cursorX = lastCurX + spaceW * Math.Max(0, extraSpaces);
                    cursorY = lastCurY;
                    cursorH = font.MaxHeight * scaleY;
                    //Console.WriteLine($"[CURSOR-END] slot={tf._rendererCursorSlot} charCount={charCount} lastCurX={lastCurX} cursorX={cursorX}");

                }
            }

            if (tf != null && tf._rendererCursorSlot == 0)
            { cursorX = lineStartXs[0]; cursorY = lineStartYs[0]; cursorH = lineHeights[0]; }

            if (tf != null) tf._cursorRawX = cursorX - elem.GetAbsX() + tf._xOffset;

            if (tf != null && cursorX >= 0 && tf.cursorShow)
                DrawQuad(cursorX + 2, cursorY, 2f, cursorH, tf.CursorColor);

            gl.BindVertexArray(0);

            // ── Decorations (underline / strikethrough) ───────────────────────────
            if (decorations.Count > 0)
            {
                shader.Use();
                shader.SetInt("useTexture", 0);
                foreach (var (x, y, w, h, col) in decorations)
                    DrawQuad(x, y, w, h, new RGBA(col.R, col.G, col.B, col.A * fade));
            }

            _lastBoundTextureId = -1;
        }
    }
}
