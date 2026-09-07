namespace Shergen
{
    public class UILabel : UIElement
    {
        public bool AutoSizeWidth { get; set; } = false;
        public bool AutoSizeHeight { get; set; } = false;

        public float AutoSizeWidth_Max = 0;  // 0 = bez limitu
        public float AutoSizeHeight_Max = 0;

        public string Text { get; set; } = "";
        public RGBA FontColor { get; set; } = new RGBA(1, 1, 1, 1);
        public string Font { get; set; } = "";   // "" → renderer uses RendererTK.DefaultDFFFont
        public float FontSize { get; set; } = 16f;

        public float RangeMin { get; set; } = -1f;
        public float RangeMax { get; set; } = -1f;
        public int FontMode { get; set; } = -1;

        public float CharScaleX { get; set; } = 100f;  // % šířky písmen (100 = default)
        public float CharScaleY { get; set; } = 100f;  // % výšky písmen (100 = default)
        public float CharSpacing { get; set; } = 0f;   // extra mezera mezi znaky (px)
        public float LineSpacing { get; set; } = 0f;   // extra mezera mezi řádky (px)
        public bool WordWrap { get; set; } = false;    // zalamování textu (default vypnuto)
        public UIAling HAling { get; set; } = UIAling.Left;    // horizontální alignment
        public UIAling VAling { get; set; } = UIAling.Top;     // vertikální alignment

        public bool ShadowEnabled { get; set; } = false;
        public RGBA ShadowColor { get; set; } = new RGBA(0f, 0f, 0f, 0.7f);
        public float ShadowOffsetX { get; set; } = 1f;
        public float ShadowOffsetY { get; set; } = 1f;

        public bool OutlineEnabled { get; set; } = false;
        public RGBA OutlineColor { get; set; } = new RGBA(0f, 0f, 0f, 1f);
        public float OutlineWidth { get; set; } = 1f;  // >= 1 = pixely, < 1 = SDF jednotky

        //scrolling text (marquee)
        public bool ScrollText { get; set; } = false;
        public float ScrollTextSpeed { get; set; } = 50f;   // px/s
        public float ScrollTextDelay { get; set; } = 1500f; // ms

        internal float _scrollOffset = 0f;
        internal float _scrollDir = 1f;      // 1 = dopředu, -1 = dozadu
        internal float _scrollDelayMs = 0f;      // čítač delay
        internal DateTime _scrollLastTick = DateTime.MinValue;
        internal bool _scrollWaiting = false;   // jsme v delay fázi?

        // Rich-text parse cache — invalidated when Text changes
        internal string                 _rtCacheKey;
        internal List<Renderer.TextRun> _rtRuns;

        // Layout cache — invalidated when any layout-affecting property changes
        internal (string text, float width, float fontSize,
                  float csX, float csY, float charSpacing, float lineSpacing,
                  bool wordWrap, string font, int iconVersion)  _layoutKey;
        internal bool                          _layoutValid;
        internal List<List<Renderer.Atom>>     _layoutLines;
        internal float[]                       _layoutLineWidths;
        internal float[]                       _layoutLineHeights;

        public UILabel(UIElement parent = null, bool isSpecial = false)
            : this(UIElementType.Label, parent, isSpecial) { }

        protected UILabel(UIElementType type, UIElement parent, bool isSpecial)
            : base(type, parent, isSpecial)
        {
            SetProperty(UIProperty.Color1, new RGBA(0, 0, 0, 0)); // Label má průhledné pozadí
        }

        /// <summary>
        /// Invalidates the layout cache. If AutoSize is active, immediately remeasures
        /// on the calling (Lua) thread so the caller sees the updated size right away.
        /// </summary>
        protected void InvalidateLayout()
        {
            _layoutValid = false;
            if (AutoSizeWidth || AutoSizeHeight)
                _renderer.MeasureEl(this);
        }

        public override void SetProperty(UIProperty property, float value)
        {
            switch (property)
            {
                case UIProperty.Font_Size:      FontSize      = value; InvalidateLayout(); break;
                case UIProperty.RangeMin:       RangeMin      = value; break;
                case UIProperty.RangeMax:       RangeMax      = value; break;
                case UIProperty.CharScaleX:     CharScaleX    = value; InvalidateLayout(); break;
                case UIProperty.CharScaleY:     CharScaleY    = value; InvalidateLayout(); break;
                case UIProperty.CharSpacing:    CharSpacing   = value; InvalidateLayout(); break;
                case UIProperty.LineSpacing:    LineSpacing   = value; InvalidateLayout(); break;
                case UIProperty.ShadowOffsetX:  ShadowOffsetX = value; break;
                case UIProperty.ShadowOffsetY:  ShadowOffsetY = value; break;
                case UIProperty.OutlineWidth:   OutlineWidth  = value; break;
                case UIProperty.ScrollText_Speed: ScrollTextSpeed = value; break;
                case UIProperty.ScrollText_Delay: ScrollTextDelay = value; break;
                case UIProperty.AutoSizeHeight_Max: AutoSizeHeight_Max = value; InvalidateLayout(); break;
                case UIProperty.AutoSizeWidth_Max: AutoSizeWidth_Max = value; InvalidateLayout(); break;
                default: base.SetProperty(property, value); break;
            }
        }

        public override void SetProperty(UIProperty property, bool value)
        {
            switch (property)
            {
                case UIProperty.WordWrap:       WordWrap       = value; InvalidateLayout(); break;
                case UIProperty.ShadowEnabled:  ShadowEnabled  = value; break;
                case UIProperty.OutlineEnabled: OutlineEnabled = value; break;
                case UIProperty.ScrollText:     ScrollText     = value; break;
                case UIProperty.AutoSizeHeight: AutoSizeHeight = value; InvalidateLayout(); break;
                case UIProperty.AutoSizeWidth:  AutoSizeWidth  = value; InvalidateLayout(); break;
                default: base.SetProperty(property, value); break;
            }
        }

        public override void SetProperty(UIProperty property, string value)
        {
            switch (property)
            {
                case UIProperty.Text: Text = value; InvalidateLayout(); break;
                case UIProperty.Font: Font = value; InvalidateLayout(); break;
                default: base.SetProperty(property, value); break;
            }
        }

        public override void SetProperty(UIProperty property, int value)
        {
            switch (property)
            {
                case UIProperty.HAling: HAling = (UIAling)value; break;
                case UIProperty.VAling: VAling = (UIAling)value; break;
                default: base.SetProperty(property, value); break;
            }
        }

        public override void SetProperty(UIProperty property, RGBA color)
        {
            switch (property)
			{
				case UIProperty.Font_Color: FontColor = color; break;
				case UIProperty.ShadowColor: ShadowColor = color; break;
				case UIProperty.OutlineColor: OutlineColor = color; break;
				default: base.SetProperty(property, color); break;
			}
        }

        public override object GetProperty(UIProperty property)
        {
            return property switch
            {
                UIProperty.Text => Text,
                UIProperty.Font => Font,
                UIProperty.Font_Size => FontSize,
                UIProperty.Font_Color => FontColor,
                UIProperty.RangeMin => RangeMin,
                UIProperty.RangeMax => RangeMax,
                UIProperty.CharScaleX => CharScaleX,
                UIProperty.CharScaleY => CharScaleY,
                UIProperty.CharSpacing => CharSpacing,
                UIProperty.LineSpacing => LineSpacing,
                UIProperty.ShadowEnabled => ShadowEnabled,
                UIProperty.ShadowColor => ShadowColor,
                UIProperty.ShadowOffsetX => ShadowOffsetX,
                UIProperty.ShadowOffsetY => ShadowOffsetY,
                UIProperty.OutlineWidth => OutlineWidth,
                UIProperty.OutlineEnabled => OutlineEnabled,
                UIProperty.OutlineColor => OutlineColor,
                UIProperty.ScrollText_Speed => ScrollTextSpeed,
                UIProperty.ScrollText_Delay => ScrollTextDelay,
                UIProperty.WordWrap => WordWrap,
                UIProperty.AutoSizeWidth => AutoSizeWidth,
                UIProperty.AutoSizeHeight => AutoSizeHeight,
                UIProperty.AutoSizeWidth_Max => AutoSizeWidth_Max,
                UIProperty.AutoSizeHeight_Max => AutoSizeHeight_Max,
                _ => base.GetProperty(property)
            };
        }

        protected override void DoDrawText()
        {
            _renderer.DrawText(this);
        }
    }
}
