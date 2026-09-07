namespace Shergen.Renderer
{
    // ─────────────────────────────────────────────
    //  Format state carried by every text run
    // ─────────────────────────────────────────────

    public struct FormatState
    {
        /// <summary>null → use the label's FontColor</summary>
        public RGBA? Color;
        public bool   Bold;
        public bool   Italic;
        public float  SizeMult;       // 1.0 = normal, 2.0 = double size, etc.
        public bool   Underline;
        public bool   Strikethrough;
        public string IconTag;

        public static FormatState Default => new FormatState { SizeMult = 1f };
    }

    // ─────────────────────────────────────────────
    //  A contiguous run of text sharing one format
    // ─────────────────────────────────────────────

    public struct TextRun
    {
        public string      Text;
        public FormatState Format;
    }

    // ─────────────────────────────────────────────
    //  Layout atom – output of the line-builder
    //  (a word segment, a space, or a newline)
    // ─────────────────────────────────────────────

    public struct Atom
    {
        public string      Text;       // non-empty for content atoms
        public FormatState Fmt;
        public bool        IsSpace;
        public bool        IsNewline;
        public bool        IsStripped;  

        public bool IsContent => !IsSpace && !IsNewline && !IsStripped;
    }

    // ─────────────────────────────────────────────
    //  Parser  –  text → List<TextRun>
    // ─────────────────────────────────────────────

    public static class RichTextParser
    {
        /// <summary>
        /// Parses inline markup tags out of <paramref name="text"/> and
        /// returns a list of runs, each carrying its FormatState.
        ///
        /// Supported tags (case-insensitive):
        ///   <b> </b>;            bold
        ///   <i> </i>;            italic
        ///   <u> </u>;            underline
        ///   <st> </st>;          strikethrough
        ///   <c=RRGGBB(AA)> </c>;   color (hex code (6 or 8 number), #-prefix optional)
        ///   <s=N> </s>;          size multiplier (float)
        /// </summary>
        /// 
        public static bool IsRecognizedTag(string tag)
        {
            tag = tag.Trim().ToLowerInvariant();
            return tag is "b" or "/b" or "i" or "/i" or "u" or "/u" or "st" or "/st" or "/c" or "/s"
                || tag.StartsWith("c=")
                || (tag.StartsWith("s=") && float.TryParse(tag[2..],
                       System.Globalization.NumberStyles.Float,
                       System.Globalization.CultureInfo.InvariantCulture, out float v) && v > 0f)
                || (tag.StartsWith("icon=") && IconRegistry.Contains(tag[5..].Trim()));
        }
        public static List<TextRun> Parse(string text)
        {
            var runs   = new List<TextRun>();
            var sb     = new System.Text.StringBuilder();
            var state  = FormatState.Default;

            // Per-tag stacks so nested / mismatched tags restore correctly
            var boldStack   = new Stack<bool>();
            var italicStack = new Stack<bool>();
            var ulineStack  = new Stack<bool>();
            var strikeStack = new Stack<bool>();
            var colorStack  = new Stack<RGBA?>();
            var sizeStack   = new Stack<float>();

            void FlushRun()
            {
                if (sb.Length > 0)
                {
                    runs.Add(new TextRun { Text = sb.ToString(), Format = state });
                    sb.Clear();
                }
            }

            int i = 0;
            while (i < text.Length)
            {
                if (text[i] != '<') { sb.Append(text[i++]); continue; }

                int end      = text.IndexOf('>', i + 1);
                int nextOpen = text.IndexOf('<', i + 1);
                // broken tag: no '>' at all, or another '<' comes before '>' → treat '<' as literal
                if (end < 0 || (nextOpen >= 0 && nextOpen < end)) { sb.Append(text[i++]); continue; }

                string tag = text.Substring(i + 1, end - i - 1).Trim().ToLowerInvariant();
                bool recognized = true;

                switch (tag)
                {
                    case "b":
                        FlushRun(); boldStack.Push(state.Bold); state.Bold = true;
                        break;
                    case "/b":
                        FlushRun(); if (boldStack.Count > 0) state.Bold = boldStack.Pop();
                        break;

                    case "i":
                        FlushRun(); italicStack.Push(state.Italic); state.Italic = true;
                        break;
                    case "/i":
                        FlushRun(); if (italicStack.Count > 0) state.Italic = italicStack.Pop();
                        break;

                    case "u":
                        FlushRun(); ulineStack.Push(state.Underline); state.Underline = true;
                        break;
                    case "/u":
                        FlushRun(); if (ulineStack.Count > 0) state.Underline = ulineStack.Pop();
                        break;

                    case "st":
                        FlushRun(); strikeStack.Push(state.Strikethrough); state.Strikethrough = true;
                        break;
                    case "/st":
                        FlushRun(); if (strikeStack.Count > 0) state.Strikethrough = strikeStack.Pop();
                        break;

                    case "/c":
                        FlushRun(); if (colorStack.Count > 0) state.Color = colorStack.Pop();
                        break;

                    case "/s":
                        FlushRun(); if (sizeStack.Count > 0) state.SizeMult = sizeStack.Pop();
                        break;

                    default:
                        if (tag.StartsWith("c="))
                        {
                            FlushRun();
                            colorStack.Push(state.Color);
                            state.Color = HexToColor(tag.Substring(2)) ?? state.Color;
                        }
                        else if (tag.StartsWith("s="))
                        {
                            FlushRun();
                            sizeStack.Push(state.SizeMult);
                            string numStr = tag.Substring(2);
                            bool parsed =
                                float.TryParse(numStr,
                                    System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out float m)
                                || float.TryParse(numStr,
                                    System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.CurrentCulture,
                                    out m);
                            if (parsed && m > 0f)
                                state.SizeMult = m;
                            else
                                Console.WriteLine($"⚠️ RichText: <s={numStr}> — parse failed or value ≤ 0, keeping SizeMult={state.SizeMult}");
                        }
                        else if (tag.StartsWith("icon="))
                        {
                            string iconTag = tag.Substring(5).Trim();
                            if (IconRegistry.Contains(iconTag))
                            {
                                FlushRun();
                                var iconFmt = state;
                                iconFmt.IconTag = iconTag;
                                runs.Add(new TextRun { Text = "\uE000", Format = iconFmt }); // 1 sentinel znak = 1 "slot"
                            }
                            else
                            {
                                recognized = false;   // neexistující ikonka → spadne do literal fallbacku níž
                            }
                        }
                        else
                        {
                            recognized = false;
                        }
                        break;
                }

                if (recognized)
                    i = end + 1;
                else
                {
                    // Not a known tag — output the whole unknown tag as literal text
                    sb.Append(text.Substring(i, end - i + 1));
                    i = end + 1;
                }
            }

            FlushRun();
            return runs;
        }

        // ── Hex color parser ──────────────────────────────────────────────

        private static RGBA? HexToColor(string hex)
        {
            hex = hex.Trim().TrimStart('#');
            try
            {
                if (hex.Length == 6)
                {
                    byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                    return new RGBA(r / 255f, g / 255f, b / 255f, 1f);
    
                }
                if (hex.Length == 8)
                {
                    byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                    byte a = Convert.ToByte(hex.Substring(6, 2), 16);
                    return new RGBA(r / 255f, g / 255f, b / 255f, a / 255f);

                }
            }
            catch { /* malformed hex → return null */ }
            return null;
        }
    }
}
