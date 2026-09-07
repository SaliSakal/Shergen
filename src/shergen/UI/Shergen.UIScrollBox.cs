

namespace Shergen
{
    public class UIScrollBox : UIElement, IScrollable
    {
        public float ScrollOffsetX { get; set; } = 0f;
        public float ScrollOffsetY { get; set; } = 0f;

        internal float _scrollTargetY = 0f;
        internal float _scrollTargetX = 0f;

        // IScrollable
        public float ContentHeight  => GetContentHeight();
        public float ContentWidth   => GetContentWidth();
        public float ScrollTargetY  { get => _scrollTargetY; set => _scrollTargetY = value; }
        public float ScrollTargetX  { get => _scrollTargetX; set => _scrollTargetX = value; }

        public int ScrollBarVID { get; set; } = -1;
        public int ScrollBarHID { get; set; } = -1;

        public bool HorizontalScroll { get; set; } = false;

        public UIScrollBox(UIElement parent = null)
            : base(UIElementType.ScrollBox, parent)
        {
            UseScissorForChildren = true; // vždy zapnuto
        }

        public override void SetProperty(UIProperty property, bool value)
        {
            switch (property)
            {
                case UIProperty.ScrollBoxHorizontal: HorizontalScroll = value; break;
                default: base.SetProperty(property, value); break;
            }
        }
        public override void SetProperty(UIProperty property, float value)
        {
            switch (property)
            {
                case UIProperty.ScrollOffsetX: ScrollOffsetX = value; break;
                case UIProperty.ScrollOffsetY: ScrollOffsetY = value; break;
                case UIProperty.ScrollBar:
                    var bar = UIElement.GetByID((int)value) as UIScrollBar;
                    if (bar != null)
                    {
                        ScrollBarVID = (int)value;
                        bar.Target = this;
                    }
                    break;
                case UIProperty.ScrollBar2:
                    var bar2 = UIElement.GetByID((int)value) as UIScrollBar;
                    if (bar2 != null)
                    {
                        ScrollBarHID = (int)value;
                        bar2.Target = this;
                    }
                    break;
                default: base.SetProperty(property, value); break;
            }
        }

        public override object GetProperty(UIProperty property)
        {
            return property switch
            {
                UIProperty.ScrollOffsetX => ScrollOffsetX,
                UIProperty.ScrollOffsetY => ScrollOffsetY,
                UIProperty.ScrollBar => ScrollBarVID,
                UIProperty.ScrollBar2 => ScrollBarHID,
                UIProperty.ScrollBoxHorizontal => HorizontalScroll,

                _ => base.GetProperty(property)
            };
        }
        public float GetContentWidth()
        {
            float max = 0f;
            foreach (var child in Children)
                max = Math.Max(max, child.X + child.Width);
            return max;
        }

        public float GetContentHeight()
        {
            float max = 0f;
            foreach (var child in Children)
                max = Math.Max(max, child.Y + child.Height);
            return max;
        }

        private float _contentWidth = 0f;
        private float _contentHeight = 0f;
        private bool _contentDirty = true;

        public void InvalidateContent() => _contentDirty = true;

        private void RecalcContent()
        {
            _contentWidth = 0f;
            _contentHeight = 0f;
            foreach (var child in Children)
            {
                _contentWidth = Math.Max(_contentWidth, child.X + child.Width);
                _contentHeight = Math.Max(_contentHeight, child.Y + child.Height);
            }
            _contentDirty = false;
        }

        public float MaxScrollX
        {
            get
            {
                if (_contentDirty) RecalcContent();
                return Math.Max(0f, _contentWidth - Width);
            }
        }

        public float MaxScrollY
        {
            get
            {
                if (_contentDirty) RecalcContent();
                return Math.Max(0f, _contentHeight - Height);
            }
        }

        public override void Draw(bool forceDraw = false)
        {
            // Interpolace scrollu Y
            float diffY = _scrollTargetY - ScrollOffsetY;
            if (Math.Abs(diffY) > 0.5f)
                ScrollOffsetY += diffY * 0.2f;
            else
                ScrollOffsetY = _scrollTargetY;

            // Interpolace scrollu X
            float diffX = _scrollTargetX - ScrollOffsetX;
            if (Math.Abs(diffX) > 0.5f)
                ScrollOffsetX += diffX * 0.2f;
            else
                ScrollOffsetX = _scrollTargetX;

            base.Draw(forceDraw);
        }


    }
}
