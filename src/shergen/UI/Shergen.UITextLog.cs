namespace Shergen
{
    /// <summary>
    /// Scrollable text log — like UILabel but with built-in vertical scroll and AddLine().
    /// WordWrap is always true. Supports scrollbar binding via IScrollable.
    /// </summary>
    public class UITextLog : UILabel, IScrollable
    {
        // ── Lines storage ─────────────────────────────────────────────────────────
        internal readonly List<string> _lines = new();

        //Maximum number of stored lines. 0 = unlimited.
        public int MaxLines { get; set; } = 0;

        //When true new lines are appended at the bottom; false = prepended at top.
        public bool AddAtBottom { get; set; } = true;

        // ── Scroll state ──────────────────────────────────────────────────────────
        internal float _logScrollOffset = 0f;   // current visual scroll (interpolated)
        internal float _logScrollTarget = 0f;   // target scroll (set by wheel/touch/AddLine)
        internal float _logContentHeight = 0f;  // updated from DrawText each frame

        // Flags set in AddLine, consumed in OnContentHeightUpdated
        private bool _snapToBottomOnUpdate = false;  // AddAtBottom + byl na dně
        private bool _snapToTopOnUpdate    = false;  // AddAtTop + byl na vrcholu
        private bool _adjustTopOnUpdate    = false;  // AddAtTop + nebyl na vrcholu → posun o delta
        private float _prevContentHeight   = 0f;

        // ── IScrollable ───────────────────────────────────────────────────────────
        public float ContentHeight  => _logContentHeight;
        public float ContentWidth   => Width;
        public float ScrollOffsetY  { get => _logScrollOffset; set { _logScrollOffset = value; _logScrollTarget = value; } }
        public float ScrollOffsetX  { get => 0f; set { } }
        public float MaxScrollY     => Math.Max(0f, _logContentHeight - Height);
        public float MaxScrollX     => 0f;
        public float ScrollTargetY  { get => _logScrollTarget; set => _logScrollTarget = value; }
        public float ScrollTargetX  { get => 0f; set { } }

        // ── Scrollbar binding ─────────────────────────────────────────────────────
        public int ScrollBarID { get; private set; } = -1;

        public UITextLog(UIElement parent = null, bool isSpecial = false)
            : base(UIElementType.TextLog, parent, isSpecial)
        {
            WordWrap              = true;
            UseScissorForChildren = true;
        }

        // ── Draw: smooth scroll interpolation only ────────────────────────────────
        public override void Draw(bool forceDraw = false)
        {
            float maxY = MaxScrollY;

            //_logScrollTarget = Math.Clamp(_logScrollTarget, 0f, maxY);

            float diff = _logScrollTarget - _logScrollOffset;
            if (Math.Abs(diff) > 0.5f)
                _logScrollOffset += diff * 0.2f;
            else
                _logScrollOffset = _logScrollTarget;

            _logScrollOffset = Math.Clamp(_logScrollOffset, 0f, maxY);

            base.Draw(forceDraw);
        }

        // ── Called by DrawText after computing totalHeight ────────────────────────
        internal void OnContentHeightUpdated(float newHeight)
        {
            float newMaxY = Math.Max(0f, newHeight - Height);

            if (_snapToBottomOnUpdate)
            {
                _logScrollTarget   = newMaxY;
                _snapToBottomOnUpdate = false;
            }
            else if (_snapToTopOnUpdate)
            {
                _logScrollTarget = 0f;
                _snapToTopOnUpdate = false;
            }
            else if (_adjustTopOnUpdate && newHeight > _prevContentHeight)
            {
                // AddAtTop, nebyl na vrcholu — posuň o delta aby čtená pozice zůstala
                float delta = newHeight - _prevContentHeight;
                _logScrollTarget = Math.Clamp(_logScrollTarget + delta, 0f, newMaxY);
                _adjustTopOnUpdate = false;
            }

            _prevContentHeight = newHeight;
            _logContentHeight  = newHeight;
        }

        // ── AddLine ───────────────────────────────────────────────────────────────
        public void AddLine(string line)
        {
            if (AddAtBottom)
            {
                // Zachyť jestli byl scroll na dně PŘED přidáním
                bool wasAtBottom = _logContentHeight <= Height || _logScrollOffset >= MaxScrollY - 1f;
                _snapToBottomOnUpdate = wasAtBottom;

                _lines.Add(line);
                if (MaxLines > 0 && _lines.Count > MaxLines)
                    _lines.RemoveAt(0);
            }
            else
            {
                // Zachyť jestli byl scroll nahoře PŘED přidáním
                bool wasAtTop = _logScrollOffset <= 1f;
                _snapToTopOnUpdate  = wasAtTop;
                _adjustTopOnUpdate  = !wasAtTop;

                _lines.Insert(0, line);
                if (MaxLines > 0 && _lines.Count > MaxLines)
                    _lines.RemoveAt(_lines.Count - 1);
            }

            RebuildText();
        }

        public void ClearLines()
        {
            _lines.Clear();
            Text               = "";
            _logScrollTarget   = 0f;
            _logScrollOffset   = 0f;
            _logContentHeight  = 0f;
            _prevContentHeight = 0f;
            _snapToBottomOnUpdate = false;
            _snapToTopOnUpdate    = false;
            _adjustTopOnUpdate    = false;
            _layoutValid       = false;
        }

        private void RebuildText()
        {
            Text         = string.Join("\n", _lines);
            _layoutValid = false;
            _rtCacheKey  = null;
        }

        // ── SetProperty ───────────────────────────────────────────────────────────
        public override void SetProperty(UIProperty property, string value)
        {
            switch (property)
            {
                case UIProperty.Text:
                    _lines.Clear();
                    if (!string.IsNullOrEmpty(value))
                        foreach (var line in value.Split('\n'))
                            _lines.Add(line);
                    RebuildText();
                    break;

                default: base.SetProperty(property, value); break;
            }
        }
        public override void SetProperty(UIProperty property, float value)
        {
            switch (property)
            {
                case UIProperty.ScrollBar:
                    ScrollBarID = (int)value;
                    var bar = UIElement.GetByID(ScrollBarID) as UIScrollBar;
                    if (bar != null) bar.Target = this;
                    break;
                case UIProperty.ScrollOffsetY:
                    _logScrollTarget = Math.Clamp(value, 0f, MaxScrollY);
                    _logScrollOffset = _logScrollTarget;
                    break;
                case UIProperty.MaxLines:
                    MaxLines = (int)value;
                    if (MaxLines > 0)
                    {
                        while (_lines.Count > MaxLines)
                            _lines.RemoveAt(AddAtBottom ? 0 : _lines.Count - 1);
                        RebuildText();
                    }
                    break;
                default: base.SetProperty(property, value); break;
            }
        }

        public override void SetProperty(UIProperty property, bool value)
        {
            switch (property)
            {
                case UIProperty.WordWrap:            WordWrap    = true;  break; // always true
                case UIProperty.AddAtBottom: AddAtBottom = value; break;
                default: base.SetProperty(property, value); break;
            }
        }

        public override object GetProperty(UIProperty property)
        {
            return property switch
            {
                UIProperty.ScrollOffsetY => _logScrollOffset,
                UIProperty.ScrollBar => ScrollBarID,
                UIProperty.AddAtBottom => AddAtBottom,
                UIProperty.MaxLines => MaxLines,
                _ => base.GetProperty(property)
            };
        }
    }
}

