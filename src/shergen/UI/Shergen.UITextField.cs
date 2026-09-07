using Shergen.Renderer;
using Silk.NET.Input;
using System.ComponentModel.Design;

namespace Shergen
{
    public class UITextField : UILabel
    {

        public string PlaceHolderText { get; protected set; } = "";

        public bool ShowRich { get; protected set; } = false;

        public int CursorPosition { get; protected set; } = 0; // number of characters 0 is at start, when lua call settext, have to move on end
        public RGBA CursorColor { get; protected set; } = new RGBA(0.8f, 0.8f, 0.8f, 1);
        public float CursorSpeed { get; protected set; } = 1; // fast of blinking cursor in seconds
        public (int Start, int End) Selected { get; protected set; } = (-1, -1); // -1 -1  = nothing

        public bool ReadOnly { get; protected set; } = false;
        public int MaxLenght { get; protected set; } = 0; // 0 = unlimited
        public bool PasswordMode { get; protected set; } = false;

        public bool cursorShow { get; protected set; } = true;

        public float cursorCycle = 0;

        public float _xOffset { get; set; } = 0;

        public float _yOffset { get; set; } = 0;
        internal float _cursorRawX = 0f;
        internal float _maxY = 0f;

        private int _prevCursorSlot = -1;

        private bool _cursorNeedsScroll = false;
        internal float _cursorLineHeight = 0f;


        public UITextField(UIElement parent = null, bool isSpecial = false)
            : this(UIElementType.TextField, parent, isSpecial) { }

        protected UITextField(UIElementType type, UIElement parent, bool isSpecial)
            : base(type, parent, isSpecial)
        {
            SetProperty(UIProperty.Color1, new RGBA(0, 0, 0, 0.5f)); // TextFiled má v základu poločerné pozadí
            CanFocus = true;
            SetProperty(UIProperty.Scissor, true);
        }

        public override void SetProperty(UIProperty property, bool value)
        {
            switch (property)
            {
                case UIProperty.ShowRich: ShowRich = value; break;
                case UIProperty.PasswordMode: PasswordMode = value; break;
                case UIProperty.ReadOnly: ReadOnly = value; break;
                default: base.SetProperty(property, value); break;
            }
        }

        public override void SetProperty(UIProperty property, RGBA color)
        {
            switch (property)
            {
                case UIProperty.CursorColor: CursorColor = color; break;
                default: base.SetProperty(property, color); break;
            }
        }

        public override void SetProperty(UIProperty property, float value) 
        { 
            switch (property) 
            {
                case UIProperty.CursorSpeed: CursorSpeed = value; break;
                default: base.SetProperty(property, value); break;
            }
        }

        public override void SetProperty(UIProperty property, int value)
        {
            switch (property)
            {
                case UIProperty.CursorPosition: CursorPosition = value; break;
                case UIProperty.MaxLength: MaxLenght = value; break;
                default: base.SetProperty(property, value); break;
            }
        }

        public override void SetProperty(UIProperty property, string value)
        {
            switch (property)
            {
                case UIProperty.PlaceHolder: PlaceHolderText = value; break;
                case UIProperty.Text: Text = value;
                    if (RawToVisual(Text.Length) < CursorPosition) CursorPosition = RawToVisual(Text.Length);
                    break;
                default: base.SetProperty(property, value); break;
            }
        }

        public override object GetProperty(UIProperty property)
        {
            return property switch
            {
                UIProperty.CursorColor => CursorColor,
                UIProperty.CursorPosition => CursorPosition,
                UIProperty.CursorSpeed => CursorSpeed,
                UIProperty.PlaceHolder => PlaceHolderText,
                UIProperty.ShowRich => ShowRich,
                UIProperty.ReadOnly => ReadOnly,
                UIProperty.MaxLength => MaxLenght,
                UIProperty.PasswordMode => PasswordMode,
                _ => base.GetProperty(property)
            };
        }


        public override void Draw(bool forceDraw = false)
        {
            if (_repeatKey >= 0 && HasFocus)
            {
                _repeatAccum += _renderer.LastDeltaTime;
                double threshold = _repeatStarted ? RepeatInterval : RepeatDelay;
                if (_repeatAccum >= threshold)
                {
                    _repeatAccum -= threshold;
                    _repeatStarted = true;
                    if (_renderer.IsKeyPressed(_repeatKey))
                        HandleSpecialKey(_repeatKey, null);
                    else
                        _repeatKey = -1;
                }
            }
            base.Draw(forceDraw);
            if (HasFocus)
            {
                const float pad = 4f;
                if (!WordWrap)
                {
                    if (_cursorRawX - _xOffset < pad)
                        _xOffset = Math.Max(0f, _cursorRawX - pad);
                    else if (_cursorRawX - _xOffset > Width - pad)
                        _xOffset = _cursorRawX - Width + pad;
                    _yOffset = 0;
                }
                else
                {
                    _xOffset = 0;
                    if (_cursorNeedsScroll)
                    {
                        _cursorNeedsScroll = false;
                        if (_charYPositions != null && _rendererCursorSlot >= 0 && _rendererCursorSlot < _charYPositions.Length)
                        {
                            float cursorY = _charYPositions[_rendererCursorSlot];
                            if (cursorY - _yOffset < pad)
                                _yOffset = Math.Max(0f, cursorY - pad);
                            else if (cursorY + _cursorLineHeight - _yOffset > Height - pad)
                                _yOffset = cursorY + _cursorLineHeight - Height + pad;
                        }
                    }
                    _yOffset = Math.Clamp(_yOffset, 0, _maxY);
                }
            }




        }

        internal void OnContentHeightUpdated(float newHeight)
        {
            _maxY = Math.Max(0f, newHeight - Height);
        }

        protected override void DoDrawText()
        {
            if (HasFocus)
            {
                cursorCycle += _renderer.LastDeltaTime;
                if (cursorCycle > CursorSpeed)
                {
                    cursorCycle = 0;
                    cursorShow = !cursorShow;
                }
            }
            else cursorShow = false;
            _renderer.DrawText(this);
        }


        protected override void OnInput(UICallback type, CallbackEventData ev)
        {
            if (type == UICallback.KeyPress)
            {


                //Text = Text.Insert(VisualToRaw(CursorPosition), ((char)ev.CharCode).ToString());
                int rawPos = Math.Clamp(VisualToRaw(CursorPosition), 0, Text.Length);
                Text = Text.Insert(rawPos, ((char)ev.CharCode).ToString());
                CursorPosition = RawToVisual(rawPos + 1);
                _cursorNeedsScroll = true;
                //CursorPosition++;
                cursorShow = true;
                cursorCycle = 0;


            }

            if (type == UICallback.KeyDown)
            {
                _repeatKey     = ev.KeyCode;
                _repeatAccum   = 0;
                _repeatStarted = false;
                if (type == UICallback.KeyDown && ev.Ctrl && ev.KeyCode == (int)Key.V)
                {
                    string clip = _renderer.GetClipboard();
                    if (!WordWrap)
                           clip.Replace("\r\n", " ")
                               .Replace("\n", " ")
                               .Replace("\r", " ");
                    
                    if (!string.IsNullOrEmpty(clip))
                    {
                        //Text = Text.Insert(VisualToRaw(CursorPosition), clip);
                        //CursorPosition += clip.Length;
                        int rawPos = VisualToRaw(CursorPosition);
                        Text = Text.Insert(rawPos, clip);
                        CursorPosition = RawToVisual(rawPos + clip.Length);
                        _cursorNeedsScroll = true;
                        cursorShow = true; cursorCycle = 0;
                    }
                }
                HandleSpecialKey(ev.KeyCode, ev);
            }

            if (type == UICallback.KeyUp)
            {
                if (ev.KeyCode == _repeatKey) _repeatKey = -1;
            }

            if (type == UICallback.LClick && _charXPositions != null)
            {
                float localX = UIElement.LastMouseLocalX + _xOffset;
                float localY = UIElement.LastMouseLocalY + _yOffset;
                int rendererSlot = HitTestChar(localX, localY);
                //Console.WriteLine($"[CLICK] localX={localX} localY={localY} _yOffset={_yOffset} resolvedSlot={rendererSlot} charCount={_charCount}");
                CursorPosition = RawToVisual(RendererSlotToRaw(rendererSlot));
                _cursorNeedsScroll = true;
                cursorShow = true; cursorCycle = 0;
            }

            if (type == UICallback.MouseWheel)
            {
                float delta = (float)ev.ScrollDelta * 20f;
                _yOffset = Math.Clamp(_yOffset - delta, 0, _maxY);
            }

            base.OnInput(type, ev);
        }

        private int _repeatKey = -1;
        private double _repeatAccum = 0;
        private bool _repeatStarted = false;
        private const double RepeatDelay = 0.4;   // 400ms před prvním opakováním
        private const double RepeatInterval = 0.05;  // 50ms = 20x/s

        private void HandleSpecialKey(int keyCode, CallbackEventData? ev)
        {
            bool handled = true;
            if (keyCode == (int)Key.Backspace && CursorPosition > 0)
            {
                int rawPos = Math.Clamp(VisualToRaw(CursorPosition) - 1, 0, Text.Length);
                if (rawPos < 0 || rawPos >= Text.Length) return;
                Text = Text.Remove(rawPos, 1);
                CursorPosition = RawToVisual(rawPos);
            }
            else if (keyCode == (int)Key.Delete && VisualToRaw(CursorPosition) < Text.Length)
            {
                int rawPos = Math.Clamp(VisualToRaw(CursorPosition), 0, Text.Length - 1);
                Text = Text.Remove(rawPos, 1);
                CursorPosition = RawToVisual(rawPos);
            }
            else if (keyCode == (int)Key.Left && CursorPosition > 0) { CursorPosition--;  }
            else if (keyCode == (int)Key.Right && CursorPosition < RawToVisual(Text.Length)) { CursorPosition++;}
            else if (keyCode == (int)Key.Up) NavigateLine(-1);
            else if (keyCode == (int)Key.Down) NavigateLine(1);
            else if (keyCode == (int)Key.Home) CursorPosition = 0;
            else if (keyCode == (int)Key.End) CursorPosition = RawToVisual(Text.Length);
            else if (keyCode == (int)Key.Escape)
                Shergen.SetFocus(null);
            else if (keyCode == (int)Key.Enter && ev?.Shift == true)
            {
                int rawPos = Math.Clamp(VisualToRaw(CursorPosition), 0, Text.Length);
                Text = Text.Insert(rawPos, "\n");
                CursorPosition = RawToVisual(rawPos)+1;
            }
            else handled = false;

            if (handled) { cursorShow = true; cursorCycle = 0; _cursorNeedsScroll = true; }


        }

        // Pomocná heuristika: '<' je broken pokud se před '>' najde další '<'
        private static bool IsBrokenTag(string text, int rawLt)
        {
            int end      = text.IndexOf('>', rawLt + 1);
            int nextOpen = text.IndexOf('<', rawLt + 1);
            return end < 0 || (nextOpen >= 0 && nextOpen < end);
        }

        private int VisualToRaw(int visualPos)
        {
            if (!ShowRich) return visualPos;
            int visual = 0, raw = 0;
            while (raw < Text.Length && visual < visualPos)
            {
                if (Text[raw] == '<')
                {
                    if (IsBrokenTag(Text, raw))
                    { visual++; raw++; }                        // broken < → literal
                    else
                    {
                        int end = Text.IndexOf('>', raw + 1);
                        string tag = Text.Substring(raw + 1, end - raw - 1);
                        if (RichTextParser.IsRecognizedTag(tag))
                        { visual++; raw = end + 1; }            // platný tag = 1 phantom slot
                        else
                        { visual++; raw++; }                    // neznámý tag → '<' literal, zbytek přes outer loop
                    }
                }
                else { visual++; raw++; }
            }
            return raw;
        }

        private int RawToVisual(int rawPos)
        {
            if (!ShowRich) return rawPos;
            int visual = 0, raw = 0;
            while (raw < Text.Length && raw < rawPos)
            {
                if (Text[raw] == '<')
                {
                    if (IsBrokenTag(Text, raw))
                    { visual++; raw++; }
                    else
                    {
                        int end = Text.IndexOf('>', raw + 1);
                        string tag = Text.Substring(raw + 1, end - raw - 1);
                        if (RichTextParser.IsRecognizedTag(tag)) { visual++; raw = end + 1; }
                        else { visual++; raw++; }
                    }
                }
                else { visual++; raw++; }
            }
            return visual;
        }

        // Převede renderer slot → raw index (platné tagy přeskakuje bez visual++)
        private int RendererSlotToRaw(int slot)
        {
            if (!ShowRich) return slot;
            int visual = 0, raw = 0;
            while (raw < Text.Length && visual < slot)
            {
                if (Text[raw] == '<')
                {
                    if (IsBrokenTag(Text, raw))
                    { visual++; raw++; }
                    else
                    {
                        int end = Text.IndexOf('>', raw + 1);
                        string tag = Text.Substring(raw + 1, end - raw - 1);
                        if (RichTextParser.IsRecognizedTag(tag))
                        {
                            if (tag.Trim().ToLowerInvariant().StartsWith("icon="))
                                visual++;
                            raw = end + 1;
                        }
                        else { visual++; raw++; }
                    }
                }
                else { visual++; raw++; }
            }
            return raw;
        }

        // Převede raw index → renderer slot
        private int RawToRendererSlot(int rawPos)
        {
            if (!ShowRich) return rawPos;
            int visual = 0, raw = 0;
            while (raw < Text.Length && raw < rawPos)
            {
                if (Text[raw] == '<')
                {
                    if (IsBrokenTag(Text, raw))
                    { visual++; raw++; }
                    else
                    {
                        int end = Text.IndexOf('>', raw + 1);
                        string tag = Text.Substring(raw + 1, end - raw - 1);
                        if (RichTextParser.IsRecognizedTag(tag))
                        {
                            if (tag.Trim().ToLowerInvariant().StartsWith("icon="))
                                visual++;   // ikonka = 1 renderer slot
                            raw = end + 1;

                        }
                        else { visual++; raw++; }
                    }
                }
                else { visual++; raw++; }
            }
            return visual;
        }

        // Renderer používá tento slot pro kreslení kurzoru (přepočítáno z CursorPosition)
        internal int _rendererCursorSlot = 0;
        internal void UpdateRendererCursorSlot()
        {
            if (!ShowRich) { _rendererCursorSlot = CursorPosition; return; }
            _rendererCursorSlot = RawToRendererSlot(VisualToRaw(CursorPosition));
        }

        internal float[] _charXPositions; // plněno DrawText každý frame
        internal float[] _charYPositions; // plněno DrawText každý frame
        internal int _charCount = 0;  // počet vizuálních znaků, renderer nastaví

        private int HitTestChar(float localX, float localY, float[] xsIn = null, float[] ysIn = null, int cntIn = -1)
        {
            var xs = xsIn ?? _charXPositions;
            var ys = ysIn ?? _charYPositions;
            int cnt = cntIn >= 0 ? cntIn : _charCount;
            if (xs == null || xs.Length == 0) return 0;

            float targetY = ys[0];
            for (int i = 0; i < cnt + 1 && i < ys.Length; i++)
                if (ys[i] <= localY) targetY = ys[i];

            int lastOnLine = -1;
            for (int i = 0; i < cnt + 1 && i < ys.Length; i++)
            {
                if (ys[i] != targetY) continue;

                bool isLineBreak = xs[i] >= float.MaxValue / 2f;
                if (isLineBreak) { lastOnLine = i; continue; }

                lastOnLine = i;

                bool nextSameLine = (i + 1 <= cnt && i + 1 < ys.Length && ys[i + 1] == targetY);
                bool nextIsLineBreak = nextSameLine && xs[i + 1] >= float.MaxValue / 2f;

                float rightX;
                if (nextSameLine && !nextIsLineBreak)
                    rightX = xs[i + 1];
                else if (i > 0 && ys[i - 1] == targetY)
                    rightX = xs[i] + (xs[i] - xs[i - 1]);
                else
                    rightX = xs[i] + 10f;

                if (localX <= (xs[i] + rightX) / 2f) return i;
            }

            if (lastOnLine < 0) return 0;
            bool lastIsLineBreak = xs[lastOnLine] >= float.MaxValue / 2f;
            return lastIsLineBreak ? lastOnLine : lastOnLine + 1;
        }

        private void NavigateLine(int direction)
        {
            var xs = _charXPositions;
            var ys = _charYPositions;
            int cnt = _charCount;
            if (xs == null || ys == null || cnt == 0) return;

            UpdateRendererCursorSlot();
            int slot = Math.Min(_rendererCursorSlot, ys.Length - 1);

            float currentY = ys[slot];
            float prefX = _cursorRawX;

            float targetY = direction < 0 ? float.MinValue : float.MaxValue;
            for (int i = 0; i <= cnt && i < ys.Length; i++)
            {
                float y = ys[i];
                if (direction < 0 && y < currentY && y > targetY) targetY = y;
                if (direction > 0 && y > currentY && y < targetY) targetY = y;
            }
            if (targetY == float.MinValue || targetY == float.MaxValue) return;

            int rendererSlot = HitTestChar(prefX, targetY, xs, ys, cnt);
            CursorPosition = RawToVisual(RendererSlotToRaw(rendererSlot));
            cursorShow = true; cursorCycle = 0;
        }
    }

}


