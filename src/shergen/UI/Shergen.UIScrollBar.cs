

namespace Shergen
{
    public class UIScrollBar : UIElement
    {
        public string Texture2 { get; private set; }
        public string Texture2_Fallback { get; private set; }

        public int Texture2ID { get; set; } = -1;
        public int Texture2Width { get; set; }
        public int Texture2Height { get; set; }

        public RGBA Color2 { get; set; } = new RGBA(1, 1, 1, 1); // (1f, 1f, 1f, 1f); // defaultní barva thumbu

        public UIScrollBarType ScrollBarType { get; set; } = UIScrollBarType.Vertical;

        public IScrollable Target { get; set; }

        internal float _thumbPos = 0f;
        internal float _thumbSize = 0f;
        internal bool _dragging = false;
        internal float _dragStartMouse = 0f;
        internal float _dragStartOffset = 0f;



        public UIScrollBar(UIElement parent = null)
            : base(UIElementType.ScrollBar, parent)
        {
            Color = new RGBA(0f, 0f, 0f, 0.3f); // defaultní barva pozadí scrollbar
            Color2 = new RGBA(1f, 1f, 1f, 0.7f); // defaultní barva thumbu

        }

        public override void SetProperty(UIProperty property, float value)
        {
            switch (property)
            {
                case UIProperty.ScrollBarType: ScrollBarType = (UIScrollBarType)value; break;
                default: base.SetProperty(property, value); break;
            }
        }

        public override void SetProperty(UIProperty property, string value)
        {
            switch (property)
            {
                case UIProperty.Texture2_Fallback:
                    Texture2_Fallback = "interface/" + value;
                    break;
                case UIProperty.Texture2:
                    Texture2 = "interface/" + value;
                    OnTexture2Changed();
                    break;
                default: base.SetProperty(property, value); break;
            }
        }

        private void OnTexture2Changed()
        {
            var texturePath = File.Exists(Texture2) ? Texture2 : Texture2_Fallback;

            if (!File.Exists(texturePath))
            {
                Texture2ID = -1;
                Texture2Width = 0;
                Texture2Height = 0;
                return;
            }

            var result = _renderer.EnqueueCommand(() => _renderer.LoadTextureAsync(texturePath)).Result;
            Texture2ID = result.id;
            Texture2Width = result.width;
            Texture2Height = result.height;

        }

        public override void SetProperty(UIProperty property, RGBA color)
        {
            switch (property)
            {
                case UIProperty.Color2: Color2 = color; break;
                default: base.SetProperty(property, color); break;
            }
        }

        public override object GetProperty(UIProperty property)
        {
            return property switch
            {
                UIProperty.ScrollBarType => (int)ScrollBarType,
                UIProperty.Texture2 => Texture2,
                UIProperty.Texture2_Fallback => Texture2_Fallback,
                UIProperty.Color2 => Color2,
                UIProperty.Texture2Width => Texture2Width,
                UIProperty.Texture2Height => Texture2Height,
                _ => base.GetProperty(property)
            };
        }

        public IScrollable GetTarget() => Target;

        protected override void DoDraw()
        {
            // Track — celý scrollbar jako obdélník

            if (Target == null) return;

            bool isV = ScrollBarType == UIScrollBarType.Vertical;

            float contentSize = isV ? Target.ContentHeight : Target.ContentWidth;
            float viewSize = isV ? Target.Height : Target.Width;
            //float maxScroll = isV ? Target.MaxScrollY : Target.MaxScrollX;
            float offset = isV ? Target.ScrollOffsetY : Target.ScrollOffsetX;
            float barSize = isV ? Height : Width;

            RGBA fadeColor = new RGBA(Color.R, Color.G, Color.B, Color.A * GetEffectiveFade());

            if (TextureID == -1)
                _renderer.DrawRectangle(this);
            else
                _renderer.DrawQuad(GetAbsX(), GetAbsY(), Width, Height, fadeColor, textureID: TextureID, grayscale: Grayscale); // tohle se musí dodělat, aby se správně vykresloval scrollbar, když má texturu


            if (contentSize <= viewSize) return;

            float maxScroll = isV ? Target.MaxScrollY : Target.MaxScrollX;

            float thumbSize = Math.Max(20f, (viewSize / contentSize) * barSize);
            //float thumbTrack = barSize - thumbSize;
            float thumbTrack = Math.Max(0f, barSize - thumbSize);
            float thumbPos = maxScroll > 0f ? Math.Clamp((offset / maxScroll) * thumbTrack, 0f, thumbTrack) : 0f;

            float absX = GetAbsX();
            float absY = GetAbsY();

            float tx = isV ? absX : absX + thumbPos;
            float ty = isV ? absY + thumbPos : absY;
            float tw = isV ? Width : thumbSize;
            float th = isV ? thumbSize : Height;

            RGBA thumbColor = new RGBA(Color2.R, Color2.G, Color2.B, Color2.A * GetEffectiveFade());

            _thumbPos = thumbPos;
            _thumbSize = thumbSize;

            if (Texture2ID == -1)
            {


                _renderer.DrawQuad(tx, ty, tw, th, thumbColor);
            }
            else
            { 
                float sliceSize = isV ? Texture2Height / 3 : Texture2Width / 3;
                float centerLenght = isV ? th - 2 * sliceSize : tw - 2 * sliceSize;

                _renderer.DrawQuad(tx, ty, isV ? tw : sliceSize, isV ? sliceSize : th, thumbColor, Texture2ID, new ( 0, 0, isV ? 1 : 0.333f, isV ? 0.333f : 1), grayscale: Grayscale);

                if (isV)
                {
                    for (float y = ty + sliceSize; y < ty + th - sliceSize; y += sliceSize)
                    {
                        float drawHeight = Math.Min(sliceSize, ty + th - sliceSize - y);
                        _renderer.DrawQuad(tx, y, tw, drawHeight, thumbColor, Texture2ID, new (0, 0.333f, 1, 0.333f), grayscale: Grayscale);
                    }
                } else
                {
                    for (float x = tx + sliceSize; x < tx + tw - sliceSize; x += sliceSize)
                    {
                        float drawWidth = Math.Min(sliceSize, tx + tw - sliceSize - x);
                        _renderer.DrawQuad(x, ty, drawWidth, th, thumbColor, Texture2ID, new (0.333f, 0, 0.333f, 1), grayscale: Grayscale);
                    }
                }


                _renderer.DrawQuad(tx + (isV ? 0 : (tw -  sliceSize) ), ty + (isV ? (th - sliceSize) : 0), isV ? tw : Texture2Width / 3,  isV ? Texture2Height / 3 : th, thumbColor, Texture2ID, new (isV ? 0 :0.666f, isV ? 0.666f : 0, isV ? 1 : 0.333f,  isV ? 0.333f : 1 ), grayscale: Grayscale);
 
            }
        }
        

        protected override void OnInput(UICallback type, CallbackEventData ev)
        {

            if (Target == null) return;

            bool isV = ScrollBarType == UIScrollBarType.Vertical;
            float mousePos = isV ? ev.LocalY : ev.LocalX;

            switch (type)
            {
                case UICallback.LDown:
                    if (mousePos >= _thumbPos && mousePos <= _thumbPos + _thumbSize)
                    {
                        // Klik na thumb — začni tažení
                        _dragging = true;
                        _dragStartMouse = mousePos;
                        _dragStartOffset = isV ? Target.ScrollOffsetY : Target.ScrollOffsetX;
                    }
                    else
                    {
                        // Klik na track — skoč na pozici
                        float barSize = isV ? Height : Width;
                        float ratio = (mousePos - _thumbSize / 2f) / (barSize - _thumbSize);
                        float maxScroll = isV ? Target.MaxScrollY : Target.MaxScrollX;
                        float target;
                        float step = 60f;
                        if (mousePos < _thumbPos)
                            target = Math.Max(0f, (isV ? Target.ScrollTargetY : Target.ScrollTargetX) - step);
                        else
                            target = Math.Min(maxScroll, (isV ? Target.ScrollTargetY : Target.ScrollTargetX) + step);

                        if (isV) Target.ScrollTargetY = target;
                        else Target.ScrollTargetX = target;
                    }
                    break;

                case UICallback.MouseMove:
                case UICallback.LDrag:
                    if (_dragging)
                    {
                        float barSize = isV ? Height : Width;
                        float maxScroll = isV ? Target.MaxScrollY : Target.MaxScrollX;
                        float delta = mousePos - _dragStartMouse;
                        float ratio = delta / (barSize - _thumbSize);
                        float newOffset = Math.Clamp(_dragStartOffset + ratio * maxScroll, 0f, maxScroll);
                        if (isV) Target.ScrollTargetY = newOffset;
                        else Target.ScrollTargetX = newOffset;
                    }
                    break;

                case UICallback.LUp:
                    _dragging = false;
                    break;
            }
        }
    }
}