using Shergen.Lua;
using static Shergen.Lua.Engine;
using static Shergen.Lua.Native;
using Shergen.Renderer;

namespace Shergen
{
    public static class Extensions
    {
        public static bool In<T>(this T item, params T[] values)
        {
            return values.Contains(item);
        }
    }

    public partial class Shergen
    {

        public static void UpdateDesktopSize(int width, int height)
        {
            if (UIElement.Desktop != null)
            {
                UIElement.Desktop.Width = width;
                UIElement.Desktop.Height = height;
            }

            mainLua.ToLua("ScrWidth = " + width);
            mainLua.ToLua("ScrHeight = " + height);
        }

        // ── NewElement ────────────────────────────────────────────────────────────

        /// <summary>
        /// GUI.NewElement(type, parentID, isSpecial [, propTable])
        ///
        /// Optional 4th argument is a Lua table that can contain:
        ///   [PROP.*]  = value          — property values (same types as GUI.SetProperty)
        ///   [PROP.*]  = {r,g,b,a}      — color table for color properties
        ///   callbacks = {
        ///       [CALLBACK.*] = func             — function, no extra args
        ///       [CALLBACK.*] = {func, ...args}  — function + extra args (with %placeholders)
        ///       [CALLBACK.*] = "lua code"       — string callback
        ///   }
        ///   anchor = {left=bool, right=bool, top=bool, bottom=bool}
        /// </summary>
        static int NewElement(IntPtr L)
        {
            UIElementType type = (UIElementType)ToLuaInteger(L, 1);
            int parentID = ToLuaInteger(L, 2);
            bool isSpecial = ToLuaBoolean(L, 3);

            UIElement parent = UIElement.GetByID(parentID);
            UIElement element = type switch
            {
                UIElementType.Label => new UILabel(parent, isSpecial),
                UIElementType.ScrollBox => new UIScrollBox(parent),
                UIElementType.ScrollBar => new UIScrollBar(parent),
                UIElementType.TextLog  => new UITextLog(parent, isSpecial),
                UIElementType.TextField => new UITextField(parent, isSpecial),
                _ => new UIElement(UIElementType.Element, parent, isSpecial)
            };


            PushLuaInteger(L, element.ID);
            return 1;
        }

        /// <summary>
        /// Reads {func, arg1, arg2, ...} from a Lua array table and registers the callback.
        /// </summary>
        private static void ApplyCallbackFromTable(UIElement element, UICallback cbType, IntPtr L, int tableIdx)
        {
            // First element: function or string
            TableRawGetI(L, tableIdx, 1);
            int funcIdx = GetTop(L);

            if (IsLuaNil(L, funcIdx)) { Pop(L, 1); return; }

            // Collect extra args (indices 2..n, stop at nil)
            var extraArgs = new List<object>();
            int i = 2;
            while (true)
            {
                TableRawGetI(L, tableIdx, i);
                if (IsLuaNil(L, GetTop(L))) { Pop(L, 1); break; }

                int si = GetTop(L);
                if (IsLuaBoolean(L, si)) extraArgs.Add(ToLuaBoolean(L, si));
                else if (IsLuaNumber(L, si)) extraArgs.Add(ToLuaNumber(L, si));
                else if (IsLuaString(L, si)) extraArgs.Add(ToLuaString(L, si));
                else extraArgs.Add(null);

                Pop(L, 1);
                i++;
            }

            object[] args = extraArgs.ToArray();

            if (IsLuaFunction(L, funcIdx))
            {
                PushValue(L, funcIdx);
                int luaRef = LuaLRef(L);
                element.SetCallback(cbType, luaRef, null, args);
            }
            else if (IsLuaString(L, funcIdx))
            {
                element.SetCallback(cbType, 0, ToLuaString(L, funcIdx), args);
            }

            Pop(L, 1); // pop the function/string
        }

        // ── Shared property-application helpers ───────────────────────────────────
        // Used by both the Lua SetProperty function and the NewElement table parser.

        private static readonly HashSet<UIProperty> _floatProps = new()
        {
            UIProperty.X, UIProperty.Y, UIProperty.Width, UIProperty.Height,
            UIProperty.Border_Size, UIProperty.Fade,
            UIProperty.Font_Size, UIProperty.RangeMin, UIProperty.RangeMax,
            UIProperty.CharScaleX, UIProperty.CharScaleY, UIProperty.CharSpacing, UIProperty.LineSpacing,
            UIProperty.ShadowOffsetX, UIProperty.ShadowOffsetY, UIProperty.OutlineWidth,
            UIProperty.ScrollText_Speed, UIProperty.ScrollText_Delay,
            UIProperty.ScrollOffsetX, UIProperty.ScrollOffsetY, UIProperty.ScrollBarType , UIProperty.ScrollBar, UIProperty.ScrollBar2,
            UIProperty.Grayscale,
            UIProperty.CursorSpeed,
            UIProperty.AutoSizeWidth_Max, UIProperty.AutoSizeHeight_Max,
            

        };

        private static readonly HashSet<UIProperty> _boolProps = new()
        {
            UIProperty.Visible, UIProperty.Scissor, UIProperty.PassThrough, UIProperty.EventCapture,
            UIProperty.AutoSizeWidth, UIProperty.AutoSizeHeight,
            UIProperty.WordWrap, UIProperty.ShadowEnabled, UIProperty.OutlineEnabled, UIProperty.ScrollText, UIProperty.ScrollBoxHorizontal,
            UIProperty.ShowRich, UIProperty.ReadOnly, UIProperty.PasswordMode, UIProperty.CanFocus
        };

        private static readonly HashSet<UIProperty> _stringProps = new()
        {
            UIProperty.Text, UIProperty.Hint, UIProperty.Font, UIProperty.Texture, UIProperty.Texture2, UIProperty.Texture3, UIProperty.Texture_Fallback, UIProperty.Texture2_Fallback, UIProperty.Texture3_Fallback,
            UIProperty.PlaceHolder, UIProperty.HitMask, UIProperty.ScissorMask
        
        };

        private static readonly HashSet<UIProperty> _colorProps = new()
        {
            UIProperty.Color1, UIProperty.Color2, UIProperty.Color3, UIProperty.Border_Color, 
            UIProperty.Font_Color, UIProperty.OutlineColor, UIProperty.ShadowColor,
            UIProperty.CursorColor,

        
        };
        // Vše ostatní (Border_Type, HAling, VAling, ...) → int overload

        /// <summary>Apply a single property from a Lua value at <paramref name="valueIdx"/>.</summary>
        private static void ApplyPropertyFromStack(UIElement element, UIProperty property, IntPtr L, int valueIdx)
        {
            if (_floatProps.Contains(property)) element.SetProperty(property, (float)ToLuaNumber(L, valueIdx));
            else if (_boolProps.Contains(property)) element.SetProperty(property, ToLuaBoolean(L, valueIdx));
            else if (_stringProps.Contains(property)) element.SetProperty(property, ToLuaString(L, valueIdx));
            else element.SetProperty(property, (int)ToLuaNumber(L, valueIdx));
        }

        /// <summary>
        /// Apply a color property from a Lua table {red=, green=, blue=, alpha=} at <paramref name="tableIdx"/>.
        /// </summary>
        private static void ApplyTableFromStack(UIElement element, UIProperty property, IntPtr L, int tableIdx)
        {
            if (_colorProps.Contains(property))
            {
                GetField(L, tableIdx, "R"); float r = (float)ToLuaNumber(L, -1) / 255f; Pop(L, 1);
                GetField(L, tableIdx, "G"); float g = (float)ToLuaNumber(L, -1) / 255f; Pop(L, 1);
                GetField(L, tableIdx, "B"); float b = (float)ToLuaNumber(L, -1) / 255f; Pop(L, 1);
                GetField(L, tableIdx, "A"); float a = (float)ToLuaNumber(L, -1) / 255f; Pop(L, 1);

                var color = new RGBA(r, g, b, a);

                element.SetProperty(property, color);
            }
            else
            {
                GetField(L, tableIdx, "X"); float x = (float)ToLuaNumber(L, -1); Pop(L, 1);
                GetField(L, tableIdx, "Y"); float y = (float)ToLuaNumber(L, -1); Pop(L, 1);
                GetField(L, tableIdx, "W"); float w = (float)ToLuaNumber(L, -1); Pop(L, 1);
                GetField(L, tableIdx, "H"); float h = (float)ToLuaNumber(L, -1); Pop(L, 1);

                var coords = new Coords(x, y, w, h);

                element.SetProperty(property, coords);
            }
        }

        // ── Lua-callable SetProperty / SetColor ──────────────────────────────────

        public static int SetProperty(IntPtr L)
        {
            if (IsLuaNil(L, 3)) return 0;

            int id = ToLuaInteger(L, 1);
            UIProperty property = (UIProperty)ToLuaInteger(L, 2);
            UIElement element = UIElement.GetByID(id);

            if (element == null)
            {
                Console.WriteLine($"❌ Element ID {id} does not exist");
                return 0;
            }
            //if (IsLuaNil(L,3)) return 0;
        
            if (IsLuaTable(L, 3)) ApplyTableFromStack(element, property, L, 3);
            else ApplyPropertyFromStack(element, property, L, 3);

            return 0;
        }

        public static int GetProperty(IntPtr L)
        {
            int id = ToLuaInteger(L, 1);
            UIProperty property = (UIProperty)ToLuaInteger(L, 2);
            UIElement element = UIElement.GetByID(id);
            if (element == null)
            {
                Console.WriteLine($"❌ Element ID {id} does not exist");
                return 0;
            }

            object value = element.GetProperty(property);
            
            if (value is float f) PushLuaNumber(L, f);
            else if (value is bool b) PushLuaBoolean(L, b);
            else if (value is string s) PushLuaString(L, s);
            else if (value is int i) PushLuaInteger(L, i);
            else if (value is RGBA c)
            {
                NewTable(L);
                PushLuaNumber(L, c.R * 255f); SetField(L, -2, "R");
                PushLuaNumber(L, c.G * 255f); SetField(L, -2, "G");
                PushLuaNumber(L, c.B * 255f); SetField(L, -2, "B");
                PushLuaNumber(L, c.A * 255f); SetField(L, -2, "A");
            }
            else if (value is Coords cs)
            {
                NewTable(L);
                PushLuaNumber(L, cs.X); SetField(L, -2, "X");
                PushLuaNumber(L, cs.Y); SetField(L, -2, "Y");
                PushLuaNumber(L, cs.W); SetField(L, -2, "W");
                PushLuaNumber(L, cs.H); SetField(L, -2, "H");
            }
            else PushLuaNil(L);
            return 1;
        }

        // ── Font helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// GUI.SetDefaultFont(type, name)
        ///   type = "DFF" → sets RendererTK.DefaultDFFFont
        ///   type = "TTF" → sets RendererTK.DefaultTTFFont (future)
        /// </summary>
        public static int SetDefaultFont(IntPtr L)
        {
            if (IsLuaNil(L, 1) || IsLuaNil(L, 2)) return 0;
            string type = ToLuaString(L, 1)?.ToUpperInvariant();
            string name = ToLuaString(L, 2);
            if (string.IsNullOrEmpty(name)) return 0;

            if (type == "DFF")
            {
                renderer.DefaultDFFFont = name;
                Console.WriteLine($"🔤 Default DFF font → {name}");
            }
            else if (type == "TTF")
            {
                renderer.DefaultTTFFont = name;
                Console.WriteLine($"🔤 Default TTF font → {name}");
            }
            return 0;
        }

        /// <summary>
        /// GUI.SetFontFallback(fontName, charStr)
        ///   Sets the replacement glyph rendered when a character is missing.
        /// </summary>
        public static int SetFontFallback(IntPtr L)
        {
            if (IsLuaNil(L, 1) || IsLuaNil(L, 2)) return 0;
            string fontName = ToLuaString(L, 1);
            string charStr = ToLuaString(L, 2);
            if (string.IsNullOrEmpty(fontName) || string.IsNullOrEmpty(charStr)) return 0;

            renderer.SetFontFallbackChar(fontName, charStr[0]);
            Console.WriteLine($"🔤 Fallback char for '{fontName}' → '{charStr[0]}'");
            return 0;
        }

        // ── Callbacks ─────────────────────────────────────────────────────────────

        /// <summary>
        /// GUI.SetCallback(id, CALLBACK_TYPE, func_or_string_or_nil, ...extraArgs)
        /// </summary>
        public static int SetCallback(IntPtr L)
        {
            int id = ToLuaInteger(L, 1);
            UICallback type = (UICallback)ToLuaInteger(L, 2);

            UIElement elem = UIElement.GetByID(id);
            if (elem == null)
            {
                Console.WriteLine($"❌ GUI.SetCallback: element {id} not found");
                return 0;
            }

            if (IsLuaNil(L, 3))
            {
                elem.SetCallback(type, 0, null, null);
                return 0;
            }

            // Collect extra args (arg 4 onwards)
            int top = GetTop(L);
            object[] extraArgs = Array.Empty<object>();
            if (top >= 4)
            {
                extraArgs = new object[top - 3];
                for (int i = 4; i <= top; i++)
                {
                    int si = i;
                    if (IsLuaBoolean(L, si)) extraArgs[i - 4] = ToLuaBoolean(L, si);
                    else if (IsLuaNumber(L, si)) extraArgs[i - 4] = ToLuaNumber(L, si);
                    else if (IsLuaString(L, si)) extraArgs[i - 4] = ToLuaString(L, si);
                    else extraArgs[i - 4] = null;
                }
            }

            if (IsLuaFunction(L, 3))
            {
                PushValue(L, 3);
                int luaRef = LuaLRef(L);
                elem.SetCallback(type, luaRef, null, extraArgs);
            }
            else if (IsLuaString(L, 3))
            {
                elem.SetCallback(type, 0, ToLuaString(L, 3), extraArgs);
            }

            return 0;
        }

        // ── Focus ─────────────────────────────────────────────────────────────────

        /// <summary>GUI.SetFocus(id) — keyboard events go to this element (0 = Desktop)</summary>
        public static int SetFocus(IntPtr L)
        {
            int id = ToLuaInteger(L, 1);
            SetFocus( id == 0 ? null : UIElement.GetByID(id) );
            return 0;
        }

        public static void SetFocus(UIElement? elem)
        {
            UIElement prev = renderer.GetFocus();

            if (prev != null) prev.SetFocus(false);

            if (elem != null && elem.Visible == true)
            {
                elem.SetFocus(true);
            }
            else if (prev != null) renderer.ClearFocus();

        }

        // ── Anchor ────────────────────────────────────────────────────────────────

        public static int SetAnchor(IntPtr L)
        {
            int id = ToLuaInteger(L, 1);

            if (!IsLuaTable(L, 2))
            {
                Console.WriteLine("❌ Error: GUI.SetAnchor expects table {left, right, top, bottom}");
                return 0;
            }

            GetField(L, 2, "left"); bool left = ToLuaBoolean(L, -1); Pop(L, 1);
            GetField(L, 2, "right"); bool right = ToLuaBoolean(L, -1); Pop(L, 1);
            GetField(L, 2, "top"); bool top = ToLuaBoolean(L, -1); Pop(L, 1);
            GetField(L, 2, "bottom"); bool bottom = ToLuaBoolean(L, -1); Pop(L, 1);

            UIElement element = UIElement.GetByID(id);
            if (element != null)
                element.SetAnchor(left, right, top, bottom);

            return 0;
        }

        // ── Event data getters ────────────────────────────────────────────────────

        /// <summary>GUI.GetMouseX() → absolute mouse X</summary>
        public static int GetMouseX(IntPtr L) { PushLuaNumber(L, UIElement.LastMouseX); return 1; }

        /// <summary>GUI.GetMouseY() → absolute mouse Y</summary>
        public static int GetMouseY(IntPtr L) { PushLuaNumber(L, UIElement.LastMouseY); return 1; }

        /// <summary>GUI.GetMouseDeltaX() → pohyb myši X od minulého render framu</summary>
        public static int GetMouseDeltaX(IntPtr L) { PushLuaNumber(L, UIElement.LastMouseDeltaX); return 1; }
        /// <summary>GUI.GetMouseDeltaY() → pohyb myši Y od minulého render framu</summary>
        public static int GetMouseDeltaY(IntPtr L) { PushLuaNumber(L, UIElement.LastMouseDeltaY); return 1; }

        /// <summary>GUI.GetMouseLocalX() → mouse X relative to element under cursor</summary>
        public static int GetMouseLocalX(IntPtr L) { PushLuaNumber(L, UIElement.LastMouseLocalX); return 1; }

        /// <summary>GUI.GetMouseLocalY() → mouse Y relative to element under cursor</summary>
        public static int GetMouseLocalY(IntPtr L) { PushLuaNumber(L, UIElement.LastMouseLocalY); return 1; }

        /// <summary>GUI.GetButton() → last mouse button index (0=left, 1=right, 2=middle, 3, 4)</summary>
        public static int GetButton(IntPtr L) { PushLuaInteger(L, UIElement.LastButton); return 1; }

        /// <summary>GUI.GetScroll() → last scroll wheel delta (positive = up)</summary>
        public static int GetScroll(IntPtr L) { PushLuaNumber(L, UIElement.LastScrollDelta); return 1; }

        /// <summary>GUI.GetKey() → last keyboard key code (OpenTK Keys enum value)</summary>
        public static int GetKey(IntPtr L) { PushLuaInteger(L, UIElement.LastKeyCode); return 1; }

        /// <summary>GUI.GetChar() → last text input character as a string</summary>
        public static int GetChar(IntPtr L) { PushLuaString(L, ((char)UIElement.LastCharCode).ToString()); return 1; }

        /// <summary>GUI.GetCursorID() → ID of the virtual cursor element (always 1).</summary>
        public static int GetCursorID(IntPtr L) { PushLuaInteger(L, UIElement.Mouse?.ID ?? -1); return 1; }

        /// <summary>GUI.Exit() → close the application.</summary>
        public static int Exit(IntPtr L) { renderer?.Exit(); return 0; }

        /// <summary>
        /// GUI.SetCursor(path, x, y)  — nastaví texturu + hotspot najednou.
        /// x, y jsou volitelné (default 0).
        /// </summary>
        public static int SetCursor(IntPtr L)
        {
            string path = ToLuaString(L, 1) ?? "";
            int hotX = IsLuaNil(L, 2) ? 0 : ToLuaInteger(L, 2);
            int hotY = IsLuaNil(L, 3) ? 0 : ToLuaInteger(L, 3);
            (UIElement.Mouse as UIMouse)?.SetCursor(path, hotX, hotY);
            return 0;
        }

        /// <summary>
        /// GUI.SetCursorTexture(path) — změní pouze texturu, hotspot zůstane.
        /// </summary>
        public static int SetCursorTexture(IntPtr L)
        {
            string path = ToLuaString(L, 1) ?? "";
            (UIElement.Mouse as UIMouse)?.SetCursorTexture(path);
            return 0;
        }

        /// <summary>GUI.ResetCursor() — obnoví výchozí kurzor OS.</summary>
        public static int ResetCursor(IntPtr L)
        {
            renderer?.ResetCursor();
            return 0;
        }

        /// <summary>GUI.GetCursorHotSpot() → hotX, hotY</summary>
        public static int GetCursorHotSpot(IntPtr L)
        {
            var (hotX, hotY) = renderer?.GetCursorHotSpot() ?? (0, 0);
            PushLuaInteger(L, hotX);
            PushLuaInteger(L, hotY);
            return 2;
        }

        /// <summary>GUI.SetCursorPos(x, y) — přesune OS kurzor.</summary>
        public static int SetCursorPos(IntPtr L)
        {
            float x = (float)ToLuaNumber(L, 1);
            float y = (float)ToLuaNumber(L, 2);
            renderer?.SetCursorPos(x, y);
            return 0;
        }

        /// <summary>GUI.SetCursorLocked(bool) — uzamkne kurzor do okna.</summary>
        public static int SetCursorLocked(IntPtr L)
        {
            renderer?.SetCursorLocked(ToLuaBoolean(L, 1));
            return 0;
        }

        /// <summary>GUI.SetCursorHidden(bool) — skryje / zobrazí kurzor.</summary>
        public static int SetCursorHidden(IntPtr L)
        {
            renderer?.SetCursorHidden(ToLuaBoolean(L, 1));
            return 0;
        }

        /// <summary>GUI.SetCursorCaptured(bool) — FPS režim (skrytý + zamčený, raw input).</summary>
        public static int SetCursorCaptured(IntPtr L)
        {
            renderer?.SetCursorCaptured(ToLuaBoolean(L, 1));
            return 0;
        }



        public static int GetChildrenIDs(IntPtr L)
        {
            int id = ToLuaInteger(L, 1);
            UIElement element = UIElement.GetByID(id);
            if (element == null) { PushLuaNil(L); return 1; }

            NewTable(L);
            for (int i = 0; i < element.Children.Count; i++)
            {
                PushLuaInteger(L, element.Children[i].ID);
                TableRawSetI(L, -2, i + 1);
            }
            return 1;
        }

        public static int GetParentID(IntPtr L)
        {
            int id = ToLuaInteger(L, 1);
            UIElement element = UIElement.GetByID(id);
            if (element == null || element.Parent == null) { PushLuaNil(L); return 1; }
            PushLuaInteger(L, element.Parent.ID);
            return 1;
        }

        public static int BringToFront(IntPtr L)
        {
            int id = ToLuaInteger(L, 1);
            UIElement element = UIElement.GetByID(id);
            if (element != null)
                element.Parent.BringToFront(element);
            return 0;
        }

        public static int BringToBack(IntPtr L)
        {
            int id = ToLuaInteger(L, 1);
            UIElement element = UIElement.GetByID(id);
            if (element != null)
                element.Parent.BringToBack(element);
            return 0;
        }

        public static int BringBy(IntPtr L)
        {
            int id = ToLuaInteger(L, 1);
            int offset = ToLuaInteger(L, 2);
            UIElement element = UIElement.GetByID(id);
            if (element != null)
                element.Parent.BringBy(element,offset);
            return 0;
        }

        public static int SetParent(IntPtr L)
        {
            int id = ToLuaInteger(L, 1);
            int parentID = ToLuaInteger(L, 2);
            UIElement element = UIElement.GetByID(id);
            UIElement parent = UIElement.GetByID(parentID);
            try
            {
                if (element != null && parent != null)
                    element.SetParent(parent);
            }
            catch (Exception ex)
            {
                PushLuaError(L, $"Error in SetParent: {ex.Message}");
                return 1;
            }
            return 0;
        }

        /// GUI.RegisterIcon(tag, path, color, scale, offsetX, offsetY)
        ///   tag    — použití v textu jako <icon=tag>
        ///   path   — cesta k textuře
        ///   color  — nil nebo {R,G,B,A} (0-255); nil = barva textu
        ///   scale  — násobek výšky písma (nil = 1.0)
        ///   offsetX, offsetY — poměrový posun vůči výšce písma (nil = 0)
        public static int RegisterIcon(IntPtr L)
        {
            if (IsLuaNil(L, 1) || IsLuaNil(L, 2)) return 0;
            string tag = ToLuaString(L, 1);
            string path = ToLuaString(L, 2);
            if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(path)) return 0;

            RGBA? color = null;
            if (IsLuaTable(L, 3))
            {
                GetField(L, 3, "R"); float r = (float)ToLuaNumber(L, -1) / 255f; Pop(L, 1);
                GetField(L, 3, "G"); float g = (float)ToLuaNumber(L, -1) / 255f; Pop(L, 1);
                GetField(L, 3, "B"); float b = (float)ToLuaNumber(L, -1) / 255f; Pop(L, 1);
                GetField(L, 3, "A"); float a = (float)ToLuaNumber(L, -1) / 255f; Pop(L, 1);
                color = new RGBA(r, g, b, a);
            }

            float scale = IsLuaNil(L, 4) ? 1f : (float)ToLuaNumber(L, 4);
            float offsetX = IsLuaNil(L, 5) ? 0f : (float)ToLuaNumber(L, 5);
            float offsetY = IsLuaNil(L, 6) ? 0f : (float)ToLuaNumber(L, 6);

            var (texId, texW, texH) = renderer.EnqueueCommand(() => renderer.LoadTextureAsync(path)).Result;
            if (texId < 0)
            {
                Console.WriteLine($"❌ GUI.RegisterIcon: nepodařilo se načíst '{path}'");
                return 0;
            }

            IconRegistry.Register(tag, new IconDef
            {
                TextureID = texId,
                TexW = texW,
                TexH = texH,
                Color = color,
                Scale = scale,
                OffsetX = offsetX,
                OffsetY = offsetY
            });

            Console.WriteLine($"🖼️ Icon registered: '{tag}' → {path}");
            return 0;
        }

        /// GUI.UnregisterIcon(tag)
        public static int UnregisterIcon(IntPtr L)
        {
            if (IsLuaNil(L, 1)) return 0;
            string tag = ToLuaString(L, 1);
            if (string.IsNullOrEmpty(tag)) return 0;

            bool removed = IconRegistry.Remove(tag);
            Console.WriteLine(removed ? $"🗑️ Icon unregistered: '{tag}'" : $"⚠️ GUI.UnregisterIcon: '{tag}' doesn't exist");
            return 0;
        }

        // ------------------------------------------------------------------------------------------

        public static int DestroyElement(IntPtr L)
        {
            if (IsLuaNil(L, 1)) return 0;



            int id = ToLuaInteger(L, 1);
            UIElement element = UIElement.GetByID(id);

            if (id <= 2)
            {
                Console.WriteLine($"❌ Cannot destroy system element ID {id}");
                return 0;
            }

            if (element != null)
                element.Destroy();
            return 0;
        }

        public static int DestroyChildren(IntPtr L)
        {
            if (IsLuaNil(L, 1)) return 0;



            int id = ToLuaInteger(L, 1);
            UIElement element = UIElement.GetByID(id);
            if (element != null)
                element.DestroyChildren();
            return 0;
        }

        // ── TextLog ───────────────────────────────────────────────────────────────

        /// <summary>GUI.AddLine(id, text)</summary>
        public static int AddLine(IntPtr L)
        {
            if (IsLuaNil(L, 1)) return 0;
            int id = ToLuaInteger(L, 1);
            string line = ToLuaString(L, 2) ?? "";
            if (UIElement.GetByID(id) is UITextLog log)
                log.AddLine(line);
            return 0;
        }

        /// <summary>GUI.ClearLines(id)</summary>
        public static int ClearLines(IntPtr L)
        {
            if (IsLuaNil(L, 1)) return 0;
            int id = ToLuaInteger(L, 1);
            if (UIElement.GetByID(id) is UITextLog log)
                log.ClearLines();
            return 0;
        }


        public static int IsKeyPressed(IntPtr L)
        {
            if (IsLuaNil(L, 1) || renderer == null) return 0;
            int keyCode = ToLuaInteger(L, 1);
            bool result = renderer.IsKeyPressed(keyCode);
            PushLuaBoolean(L, result);
            return 1;
            
        }

        public static int IsMousePressed(IntPtr L)
        {
            if (IsLuaNil(L, 1) || renderer == null) return 0;
            int button = ToLuaInteger(L, 1);
            bool result = renderer.IsMousePressed(button);
            PushLuaBoolean(L, result);
            return 1;
        }
    }
}