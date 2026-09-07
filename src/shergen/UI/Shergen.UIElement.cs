
using Shergen.Lua;
using System.ComponentModel.Design;
using System.Numerics;
using System.Runtime.InteropServices.Marshalling;

namespace Shergen
{
    public partial class UIElement
    {
        public static UIElement Desktop { get; protected set; } // Hlavní taťka všech elementů
        public static UILabel FPSCounter    { get; protected set; }
        public static UILabel ProfilerLabel { get; protected set; }
        public static UIMouse Mouse { get; protected set; }   // Speciální dcera Myš

        public static List<UIElement> Elements = new List<UIElement>(); // Běžné elementy

        public static List<UIElement> SpecialElements = new List<UIElement>(); // Speciální elementy 

        public static int nextID { get; set; } = 0;

        protected static IRenderer _renderer;
        protected static Engine _lua;

        public int ID { get; protected set; }
        public UIElementType Type { get; protected set; } // Přidáno: typ elementu
        public UIElement Parent { get; protected set; } // Taťka
        public List<UIElement> Children { get; protected set; } = new List<UIElement>(); // Synové

        protected float _x;
        protected float _y;
        protected float _width;
        protected float _height;

        public float X
        {
            get => _x;
            set
            {
                if (_x != value)
                {
                    _x = value;
                    /*
                    // Synové se musí přizpůsobit změně pozice!
                    foreach (var child in Children)
                    {
                        child.UpdateLayout();

                    }*/
                }
            }
        }

        public float Y
        {
            get => _y;
            set
            {
                if (_y != value)
                {
                    _y = value;
                    /*
                    // Synové se musí přizpůsobit změně pozice!
                    foreach (var child in Children)
                    {
                        child.UpdateLayout();
                    }*/
                }
            }
        }
        public float Width
        {
            get => _width;
            set
            {
                if (_width != value)
                {
                    _width = value;

                    // Synové se musí přizpůsobit změně velikosti!
                    foreach (var child in Children)
                    {
                        child.UpdateLayout();

                    }
                }
            }
        }

        public float Height
        {
            get => _height;
            set
            {
                if (_height != value)
                {
                    _height = value;

                    // Synové se musí přizpůsobit změně velikosti!
                    foreach (var child in Children)
                    {
                        child.UpdateLayout();
                    }
                }
            }
        }

        public float OffsetLeft { get; protected set; }
        public float OffsetTop { get; protected set; }
        public float OffsetRight { get; protected set; }
        public float OffsetBottom { get; protected set; }
        public float OffsetLeftRatio { get; protected set; }
        public float OffsetTopRatio { get; protected set; }
        public float OffsetRightRatio { get; protected set; }
        public float OffsetBottomRatio { get; protected set; }

        public RGBA Color { get; protected set; } = new RGBA(1, 1, 1, 1); // Bíla barva

        public bool IsSpecial { get; protected set; } = false;

        //
        // When PassThrough true this element (and its descendants) is invisible to hit testing.
        // The effect cascades: all children inherit PassThrough unless they set
        // <see cref="EventCapture"/> = true to opt back in.
        public bool PassThrough { get; protected set; } = false;
        public bool EventCapture { get; protected set; } = false;

        //
        // Alpha multiplikátor (0.0 = neviditelný, 1.0 = plně viditelný).
        // Kaskáduje přes děti — efektivní fade = součin všech předků.
        // Nemění uložené barvy elementu, aplikuje se pouze při vykreslování.
        public float Fade { get; protected set; } = 1.0f;

        /// <summary>Vrátí efektivní fade tohoto elementu = Fade × rodičovský fade.</summary>
        public float GetEffectiveFade()
        {
            float f = Fade;
            if (Parent != null) f *= Parent.GetEffectiveFade();
            return f;
        }

        public bool AnchorLeft { get; protected set; } = false;
        public bool AnchorRight { get; protected set; } = false;
        public bool AnchorTop { get; protected set; } = false;
        public bool AnchorBottom { get; protected set; } = false;

        public bool Visible { get; protected set; } = false;

        public bool NoDrawOutsideParent { get; protected set; }
        public bool IsDrawing { get; protected set; }

        public static int RendererHeight { get; protected set; }

        // Border
        public RGBA BorderColor { get; protected set; } = new RGBA(0, 0, 0, 0); // Průhledná barva
        public UIBorder BorderType { get; protected set; } = UIBorder.None;
        public float BorderSize { get; protected set; } = 0;
        // Textury

        public string Texture_Fallback { get; protected set; }

        public string Texture { get; protected set; }

        public int TextureID { get; protected set; } = -1;

        public int TextureWidth { get; protected set; } = 0;
        public int TextureHeight { get; protected set; } = 0;

        public float Grayscale { get; protected set; } = 0f;

        public Coords? SubCoords { get; protected set; } = null;

        public Coords SubCoordsNorm { get; protected set; } = new Coords(0, 0, 1, 1);

        public string Hint { get; set; } = null;

        public static void Init(Engine lua, IRenderer renderer)
        {
            _lua = lua;
            _renderer = renderer;

        }

        public bool UseScissorForChildren { get; protected set; } = false;

        public string ScissorMaskPath { get; private set; } = null;
        public int ScissorMaskThreshold { get; private set; } = 50;

        public string HitMaskPath { get; private set; } = null;
        public byte[] HitMaskData { get; set; }
        public int HitMaskW, HitMaskH;
        public bool HitMaskUseGrayscale { get; set; } = false; // false = alfa, true = red kanál
        public int HitMaskThreshold { get; set; } = 50;        // per-element

        public bool CanFocus { get; protected set; } = false;
        public bool HasFocus { get; protected set; } = false;




        public static void InitializeDesktop(int width, int height)
        {
            //Console.WriteLine($"🌍 Nastavuji velikost Desktopu na {width}x{height}");

            // Guaranteed ID layout:
            //   ID 0 → Desktop
            //   ID 1 → Mouse (virtual cursor element)
            //   ID 2 → FPSCounter
            //   ID 3 → ProfilerLabel
            nextID = 0;

            Desktop = new UIElement(UIElementType.Element, null, false); // ID 0

            Desktop.Width = width;
            Desktop.Height = height;
            Desktop.SetProperty(UIProperty.Color1, new RGBA(0, 0, 0, 1));
            Desktop.SetAnchor(true, true, true, true);
            Desktop.SetVisible(true);

            Mouse = new UIMouse();              // ID 1

            FPSCounter = new UILabel(Desktop, true);  // ID 2
            FPSCounter.Text = "FPS: 0";
            FPSCounter.Visible = false;
            FPSCounter.AnchorTop = true;
            FPSCounter.AnchorLeft = true;
            FPSCounter.AnchorRight = true;
            FPSCounter.Width = Desktop.Width;
            FPSCounter.FontSize = 12;
            FPSCounter.HAling = UIAling.Middle;
            FPSCounter.ShadowEnabled = true;
            FPSCounter.PassThrough = true;

            ProfilerLabel = new UILabel(Desktop, true);  // ID 3
            ProfilerLabel.Visible = false;
            ProfilerLabel.AnchorTop = true;
            ProfilerLabel.AnchorLeft = true;
            ProfilerLabel.X = Desktop.Width-200;
            ProfilerLabel.Y = 10;
            ProfilerLabel.Width = 200;
            ProfilerLabel.AutoSizeHeight = true;
            ProfilerLabel.FontSize = 11;
            ProfilerLabel.RangeMin = 0.14f;
            ProfilerLabel.RangeMax = 0.5f;
            ProfilerLabel.CharSpacing = 1f;
            ProfilerLabel.LineSpacing = -5f;
            ProfilerLabel.ShadowEnabled = true;
            ProfilerLabel.PassThrough = true;
            ProfilerLabel.WordWrap = false;
        }

        /// <summary>
        /// Virtual element — has an ID and can hold properties but is NOT part of the
        /// render/hit-test tree.  Used for the cursor element (OS manages rendering).
        /// </summary>
        internal UIElement(UIElementType type, bool isVirtual)
        {
            if (!isVirtual) throw new InvalidOperationException("Use the normal constructor for tree elements.");
            Type = type;
            ID = nextID++;
            // Intentionally NOT added to Elements, SpecialElements, or any Children list.
        }

        public UIElement(UIElementType type, UIElement parent = null, bool isSpecial = false)
        {
            Type = type;
            ID = nextID++;

            if (isSpecial)
            {
                if (parent != null && parent.IsSpecial)
                    Parent = parent; // Speciální element může mít speciálního taťku
                else
                    Parent = Desktop; // Jinak se připojí přímo pod Desktop
                IsSpecial = true;
                SpecialElements.Add(this);
            }
            else
            {
                Parent = parent ?? Desktop; // Pokud není rodič, použije se Desktop
                Elements.Add(this);

            }

            Color = new RGBA(1, 1, 1, 1);

            if (Parent != null)
              _renderer.EnqueueCommand(() => Parent.Children.Add(this));

            IfParrentIs<UIScrollBox>(parent => parent.InvalidateContent());

            UpdateLayout(); // 🟢 Prvek se hned přizpůsobí velikosti a pozici taťky!


        }

        public static UIElement GetByID(int id)
        {
            if (id == 0) return Desktop;
            if (Mouse != null && id == Mouse.ID) return Mouse;
            return Elements.Find(e => e.ID == id) ?? SpecialElements.Find(e => e.ID == id);
        }

        public virtual void IfParrentIs<T>(Action<T> action) where T : UIElement
        {
            if (Parent is T parent)
                action(parent);
        }

        public virtual void SetProperty(UIProperty property, float value)
        {
            switch (property)
            {
                case UIProperty.X: X = value; IfParrentIs<UIScrollBox>(parent => parent.InvalidateContent()); break;
                case UIProperty.Y: Y = value; IfParrentIs<UIScrollBox>(parent => parent.InvalidateContent()); break;
                case UIProperty.Width: Width = value; IfParrentIs<UIScrollBox>(parent => parent.InvalidateContent()); break;
                case UIProperty.Height: Height = value; IfParrentIs<UIScrollBox>(parent => parent.InvalidateContent()); break;
                case UIProperty.Border_Size: BorderSize = value; break;
                case UIProperty.Fade: Fade = value / 255f; break;
                case UIProperty.Grayscale: Grayscale = value / 255f; break;
 

            }
        }

        public virtual void SetProperty(UIProperty property, bool value)
        {
            switch (property)
            {
                case UIProperty.Visible: Visible = value; 
                    var ev = new CallbackEventData { Id = ID, visibility = value };
                    FireCallback(UICallback.Visibility, ev); 
                    break;
                case UIProperty.Scissor: SetScissor(value); break;
                case UIProperty.PassThrough: PassThrough = value; break;
                case UIProperty.EventCapture: EventCapture = value; break;


            }
        }

        public virtual void SetProperty(UIProperty property, int value)
        {
            switch (property)
            {
                case UIProperty.Border_Type: BorderType = (UIBorder)value; break;
                case UIProperty.ScissorMaskThreshold: ScissorMaskThreshold = value; break;
            }
        }

        public virtual void SetProperty(UIProperty property, string value)
        {
            switch (property)
            {
                case UIProperty.Texture_Fallback:
                    Texture_Fallback = value;
                    break;

                case UIProperty.Texture:
                    Texture = value;
                    OnTextureChanged();
                    break;
                case UIProperty.HitMask:
                    HitMaskPath = value;
                    SetHitMask(value);
                    break;
                case UIProperty.ScissorMask:
                    ScissorMaskPath = value;
                    break;
                case UIProperty.Hint: Hint = value; break;
            }
        }

        public virtual object GetProperty(UIProperty property)
        {
            return property switch
            {
                UIProperty.X => X,
                UIProperty.Y => Y,
                UIProperty.Width => Width,
                UIProperty.Height => Height,
                UIProperty.Color1 => Color,
                UIProperty.Visible => Visible,
                UIProperty.Scissor => UseScissorForChildren,
                UIProperty.Border_Size => BorderSize,
                UIProperty.Border_Type => BorderType,
                UIProperty.Border_Color => BorderColor,
                UIProperty.Texture_Fallback => Texture_Fallback,
                UIProperty.Texture => Texture,
                UIProperty.PassThrough => PassThrough,
                UIProperty.EventCapture => EventCapture,
                UIProperty.Fade => Fade * 255f,
                UIProperty.TextureID => TextureID,
                UIProperty.SubCoords => SubCoords,
                UIProperty.TextureWidth => TextureWidth,
                UIProperty.TextureHeight => TextureHeight,
                UIProperty.Grayscale => Grayscale * 255f,
                UIProperty.Hint => Hint,
                UIProperty.HitMask => HitMaskPath,
                UIProperty.ScissorMask => ScissorMaskPath,
                UIProperty.ScissorMaskThreshold => ScissorMaskThreshold,


                _ => null
            };
        }
        /// <summary>
        /// Called after <see cref="Texture"/> is updated.
        /// Default: loads the image into a GL texture.
        /// Override in subclasses (e.g. UIMouse) for different behaviour.
        /// </summary>
        protected virtual void OnTextureChanged()
        {
            var texturePath = File.Exists(Texture) ? Texture : Texture_Fallback;

            if (!File.Exists(texturePath))
            {
                TextureID = -1;
                TextureWidth = 0;
                TextureHeight = 0;
                return;
            }

            var result = _renderer.EnqueueCommand(() => _renderer.LoadTextureAsync(texturePath)).Result;

            TextureID = result.id;
            TextureWidth = result.width;
            TextureHeight = result.height;

            //_renderer.EnqueueCommand(() => _renderer.LoadTexture(texturePath)).ContinueWith(t => {
            //    // zavolá se až bude textura načtená
            //    TextureID = t.Result.id;
            //    TextureWidth = t.Result.width;
            //    TextureHeight = t.Result.height;
            //});


            UpdateSubCoords();

        }

        public virtual void SetProperty(UIProperty property, Coords value)
        {
            switch (property)
            {
                case UIProperty.SubCoords: SubCoords = value; UpdateSubCoords(); break;
            }
        }

        public virtual void UpdateSubCoords()
        {
            if (TextureID < 0) return;

            if (SubCoords == null)
                SubCoordsNorm = new Coords(0, 0, 1, 1);
            else
            {
                SubCoordsNorm = new Coords(
                        SubCoords.Value.X / TextureWidth,
                        SubCoords.Value.Y / TextureHeight,
                        SubCoords.Value.W / TextureWidth,
                        SubCoords.Value.H / TextureHeight
                    );
            }
        }

        public virtual void SetProperty(UIProperty property, RGBA color)
        {
            switch (property)
            {
                case UIProperty.Color1: Color = color; break;
                case UIProperty.Border_Color: BorderColor = color; break;
            }

        }

        public virtual void SetAnchor(bool left, bool right, bool top, bool bottom)
        {
            AnchorLeft = left;
            AnchorRight = right;
            AnchorTop = top;
            AnchorBottom = bottom;

            // 🛠️ Nastavíme relativní pozici a vzdálenost od rodiče při prvním nastavení anchoru
            if (Parent != null)
            {
                UpdateOffsets();


            }
        }

        public void SetVisible(bool visible)
        {
            Visible = visible;
            if (visible == false && HasFocus) { this.SetFocus(false); _renderer.ClearFocus();  }
        }

        public void SetFocus(bool focus)
        { 
            HasFocus = focus;
            if (focus)
            {
                _renderer.SetFocus(this);
                var ev = new CallbackEventData { Id = ID };
                FireCallback(UICallback.Focus, ev);
            }
            else {
                var ev = new CallbackEventData { Id = ID };
                FireCallback(UICallback.Blur, ev);
            }

            

        }

        public void SetScissor(bool scissor)
        {
            UseScissorForChildren = scissor;
        }

        //Aktualizace offsetů
        public void UpdateOffsets()
        {
            if (Parent == null)
                return;

            // Pokud je ukotven vlevo, přepočítej RelativeX
            if (AnchorLeft)
                OffsetLeft = X;
            else
                OffsetLeftRatio = (float)X / (Parent.Width - Width);


            // Pokud je ukotven vpravo, přepočítej RelativeRight
            if (AnchorRight)
                OffsetRight = Parent.Width - (X + Width);


            // Pokud je ukotven nahoře, přepočítej RelativeY
            if (AnchorTop)
                OffsetTop = Y;
            else
                OffsetTopRatio = (float)Y / (Parent.Height - Height);

            // Pokud je ukotven dole, přepočítej RelativeBottom
            if (AnchorBottom)
                OffsetBottom = Parent.Height - (Y + Height);


        }


        public void UpdateLayout()
        {
            if (Parent == null) return;

            /* if (AnchorLeft && !AnchorRight)
                 X = OffsetLeft;
             else */
            if (!AnchorLeft && AnchorRight)
                X = Parent.Width - OffsetRight - Width;
            else if (AnchorLeft && AnchorRight)
                Width = Parent.Width - (OffsetLeft + OffsetRight);
            else if (!AnchorLeft && !AnchorRight)
                X = (int)(OffsetLeftRatio * (Parent.Width - Width));


            /* if (AnchorTop && !AnchorBottom)
                    Y = OffsetTop;
                else */
            if (!AnchorTop && AnchorBottom)
                Y = Parent.Height - OffsetBottom - Height;
            else if (AnchorTop && AnchorBottom)
                Height = Parent.Height - (OffsetTop + OffsetBottom);
            else if (!AnchorTop && !AnchorBottom)
                Y = (int)(OffsetTopRatio * (Parent.Height - Height));

            //Console.WriteLine($"[Po] Element {ID}: X={X}, Y={Y}, W={Width}, H={Height}");

            var ev = new CallbackEventData { Id = ID };
            ev.height = Height;
            ev.width = Width;
            FireCallback(UICallback.Resized, ev);

        }

        public virtual void Draw(bool forceDraw = false)
        {
            // Pokud prvek není viditelný, nebo (je úplně mimo rodiče a je to nastaveno) – pak se nevykreslí.
            if ((!Visible || (NoDrawOutsideParent && OutsideParent())) && !forceDraw)
                return;

            IsDrawing = true;
            var t = Profiler.Ts();
            if (ScissorMaskPath != null)
            {
                var (texId, _, _) = _renderer.LoadTexture(ScissorMaskPath);
                _renderer.ApplyScissorMask(texId, ScissorMaskThreshold, GetAbsX(), GetAbsY(), Width, Height);
            }
            // Vykresli sám sebe – zde se volá metoda, která vykreslí obdélník apod.
            if (!NoDraw())
                DoDraw();
            


            // Pokud má tento prvek nastaveno UseScissorForChildren, aplikuješ scissor před vykreslením dětí

            if(ScissorMaskPath == null && UseScissorForChildren)
            {
                int absX = (int)GetAbsX();
                int absY = (int)GetAbsY();
                // Přepočet Y pro OpenGL (0,0 vlevo dole)
                int scissorY = _renderer.screenHeight - (absY + (int)Height);
                _renderer.ApplyScissor(absX, scissorY, (int)Width, (int)Height);
            }
            Profiler.Add("Drawing", Profiler.Ms(t));
            t = Profiler.Ts();
            if (!NoDraw())
                DoDrawText();

            Profiler.Add("Drawing Text", Profiler.Ms(t));
            /*
            // Nejprve vykreslíme děti, které nejsou speciální
            List<UIElement> nonSpecial, special;
            lock (Children)
            {
                nonSpecial = Children.Where(c => c != null && !c.IsSpecial).ToList();
                special    = Children.Where(c => c != null &&  c.IsSpecial).ToList();
            }
            foreach (var child in nonSpecial) child.Draw();
            // Poté vykreslíme děti, které jsou speciální – ty se vykreslí "navrchu"
            foreach (var child in special)    child.Draw();
            */

            foreach (var child in Children)
            {
                if (child != null && !child.IsSpecial) child.Draw();
            }
            foreach (var child in Children)
            {
                if (child != null && child.IsSpecial) child.Draw();
            }

            // Po vykreslení dětí, pokud byl scissor aplikován, odstraň ho
            if (ScissorMaskPath != null)
                _renderer.RemoveScissorMask(); 
            else if (UseScissorForChildren)
                _renderer.RemoveScissor();


            IsDrawing = false;
        }

        protected virtual bool OutsideParent()
        {
            // Implementovat logiku, zda je element úplně mimo rodiče
            return false;
        }

        protected virtual bool RequiresScissor()
        {
            // Např. pokud máš vlastnost FScissor, vrátíš true
            return NoDrawOutsideParent;
        }

        protected virtual bool NoDraw()
        {
            // Logika zda element má kreslit (nebo je třeba z nějakého důvodu vynechat kreslení)
            return false;
        }

        protected virtual void DoDraw()
        {
            // Tady se zavolá DrawRectangle či jiný kód pro kreslení elementu
            // Například:
            _renderer.DrawRectangle(this);
        }

        protected virtual void DoDrawText() { }

        public virtual float GetAbsX()
        {
            if (Parent is UIScrollBox sb)
                return sb.GetAbsX() + X - sb.ScrollOffsetX;
            return Parent != null ? Parent.GetAbsX() + X : X;
        }

        public virtual float GetAbsY()
        {
            if (Parent is UIScrollBox sb)
                return sb.GetAbsY() + Y - sb.ScrollOffsetY;
            return Parent != null ? Parent.GetAbsY() + Y : Y;
        }

        public virtual void BringToFront(UIElement child)
            => _renderer.EnqueueCommand(() =>
            {
                Children.Remove(child);
                Children.Add(child);
            });

        public virtual void BringToBack(UIElement child)
            => _renderer.EnqueueCommand(() => {
                Children.Remove(child);
                Children.Insert(0, child);
            });

        public virtual void BringBy(UIElement child, int offset)
            => _renderer.EnqueueCommand(() =>
            {
                int i = Children.IndexOf(child);
                if (i < 0) return;
                int newIndex = Math.Clamp(i + offset, 0, Children.Count - 1);
                Children.Remove(child);
                Children.Insert(newIndex, child);
            });


        public virtual void SetParent(UIElement newParent)
        {
            if (newParent == null) throw new ArgumentNullException(nameof(newParent));
            if (newParent == this) throw new InvalidOperationException("Cannot set parent to self.");
            if (newParent.IsDescendantOf(this)) throw new InvalidOperationException("Cannot set parent to a descendant.");
            // Remove from current parent
            var oldParent = Parent;
            Parent = newParent;
            _renderer.EnqueueCommand(() => {
                oldParent?.Children.Remove(this);
                newParent.Children.Add(this);
                UpdateLayout();
            });
        }


        private bool IsDescendantOf(UIElement potentialAncestor)
        {
            UIElement current = this;
            while (current.Parent != null)
            {
                if (current.Parent == potentialAncestor) return true;
                current = current.Parent;
            }
            return false;
        }



        public void SetHitMask(string path)
        {
            //Console.WriteLine($"[HITMASK] SetHitMask voláno na {ID} s {path}");
            if (path == null) { HitMaskData = null; return; }
            var (data, w, h) = _renderer.LoadMask(path);
            HitMaskData = data;
            HitMaskW = w;
            HitMaskH = h;
        }











        public static int DebugListOfElements(IntPtr L)
        {
            /* Console.WriteLine($"Speciální Prvky: {string.Join(", ", SpecialElements.Select(e => e.ID))}");
            foreach (var element in UIElement.Elements)
            {

                Console.WriteLine($"Element {element.ID} na ({element.X}, {element.Y}), {element.Width}x{element.Height}");
                if (element.Parent != null)
                    Console.WriteLine($"   → Má taťku: {element.Parent.ID}");
                if (element.Children.Count > 0)
                    Console.WriteLine($"   → Má syny: {string.Join(", ", element.Children.Select(e => e.ID))}");


            }
            return 0;*/

            if (UIElement.Desktop != null)
            {
                //  Master taťka (Desktop): {UIElement.Desktop.ID} na ({UIElement.Desktop.X}, {UIElement.Desktop.Y}), {UIElement.Desktop.Width}x{UIElement.Desktop.Height}
                Console.WriteLine($"Master Dad (Desktop): {UIElement.Desktop.ID} at ({UIElement.Desktop.X}, {UIElement.Desktop.Y}), {UIElement.Desktop.Width}x{UIElement.Desktop.Height}");
                Console.WriteLine($": Special sons");
                Console.WriteLine($"  ╠ {UIElement.Mouse.ID} (type {UIElement.Mouse.Type}) with HotSpot ({UIElement.Mouse.HotX}, {UIElement.Mouse.HotY}) " + (UIElement.Mouse.Texture != "" ? $"T={UIElement.Mouse.Texture}" : ""));
                DebugListOfSpecialChild(UIElement.Desktop);
                Console.WriteLine($": Sons");
                DebugListOfChild(UIElement.Desktop);



            }
            return 0;
        }

        private static string DebugExtra(UIElement elem) => elem switch
        {
            UITextLog tl  => $"ls={tl._lines.Count} sy={tl._logScrollOffset:F0}/{tl.MaxScrollY:F0} b={tl.AddAtBottom}",
            UILabel   lb  => $"f={lb.Font ?? "-"} fs={lb.FontSize} wr={lb.WordWrap}" + (lb.TextureID >= 0 ? $" T={lb.TextureID}" : ""),
            UIScrollBox sb=> $"s={sb.ScrollOffsetY:F0}/{sb.MaxScrollY:F0}",
            UIScrollBar bar=>$"typ={bar.ScrollBarType} tar={bar.Target?.GetType().Name ?? "-"}",
            _             => elem.TextureID >= 0 ? $"T={elem.TextureID} {elem.Texture}" : "",
        };

        private static void DebugListOfChild(UIElement elem, string odsazení = "")
        {
            if (!(elem == UIElement.Desktop))
                Console.WriteLine(odsazení + $"╠ {elem.ID} (type {elem.Type}) at ({elem.X}, {elem.Y}), {elem.Width}x{elem.Height}, V={elem.Visible}, S={elem.NoDrawOutsideParent}, B={elem.BorderType}-{elem.BorderSize} | {DebugExtra(elem)}");

            foreach (var child in elem.Children.ToList())
            {
                if (!child.IsSpecial)
                    DebugListOfChild(child, odsazení + "  ");
            }
        }

        private static void DebugListOfSpecialChild(UIElement elem, string odsazení = "")
        {
            // vypíšeme samotný element
            if (!(elem == UIElement.Desktop))
                Console.WriteLine(odsazení + $"╠ {elem.ID} (type {elem.Type}) at ({elem.X}, {elem.Y}), {elem.Width}x{elem.Height}, V={elem.Visible}, S={elem.NoDrawOutsideParent} ");

            // Poté rekurzivně vykreslíme jeho syny
            foreach (var child in elem.Children.ToList())
            {
                if (child.IsSpecial)
                    DebugListOfSpecialChild(child, odsazení + "  ");

            }



        }


        public void Destroy()
        {
            // 1. Rekurzivně zničit děti (kopie listu, protože ho budeme modifikovat)
            foreach (var child in Children.ToList())
                child.Destroy();

            // 2. Odebrat z parenta
            if (Parent != null)
              _renderer.EnqueueCommand(() => Parent.Children.Remove(this));


            IfParrentIs<UIScrollBox>(sb => sb.InvalidateContent());

            // 3. Odebrat z globálních listů
            Elements.Remove(this);
            SpecialElements.Remove(this);

            // 4. Uvolnit Lua callbacks
            if (_callbacks != null)
                foreach (var cb in _callbacks.Values)
                    if (cb.FuncRef > 0)
                       Native.LuaLUnref(_lua.MainState, cb.FuncRef);
            _callbacks = null;
        }

        public void DestroyChildren()
        {
            List<UIElement> copy;
            lock (Children) { copy = Children.ToList(); }
            foreach (var child in copy)
                child.Destroy();
        }


    }
}

