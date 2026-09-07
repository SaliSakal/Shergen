namespace Shergen
{
    public partial class UIElement
    {
        // ── Event data passed when firing callbacks ───────────────────────────────
        /// <summary>
        /// Current event data — placeholders in extra args are replaced with these values.
        /// Supported placeholders: %id  %ax  %ay  %x  %y  %b  %d %dh  %k  %c  %dx %dy
        /// </summary>
        public struct CallbackEventData
        {
            public int    Id;             // element ID              (%id)
            public float  X,  Y;          // absolute mouse position (%ax, %ay)
            public float  LocalX, LocalY; // local (element-relative) position (%x, %y)
            public int    Button;          // mouse button index 0-4  (%b)
            public double ScrollDelta;     // scroll wheel delta       (%d)
            public double ScrollDeltaX;    // scroll second wheel delta (%dh)
            public float DeltaX, DeltaY; // movement delta (drag/move) (%dx, %dy)
            
            public int    CharCode;        // Unicode code point       (%c)

            public int KeyCode;         // keyboard key code        (%k)
            public int ScanCode;        // fyzicaly key on keyboard (%sc)
            public bool ShiftR, CtrlR, AltR, Shift, Ctrl, Alt, AltL, ShiftL, CtrlL; // Modificators %shift %shiftr %shiftl %ctrl %ctrlr %ctrl %ctrll %alt %altr %altl

            public bool visibility;     // %vis
            public float width, height; // %w %h
        }

        // ── Callback storage ──────────────────────────────────────────────────────

        private Dictionary<UICallback, LuaCallback> _callbacks;

        /// <summary>
        /// Register or replace a callback. Call from the Lua thread only.
        /// funcRef = 0 and funcStr = null → clears the callback.
        /// </summary>
        public void SetCallback(UICallback type, int funcRef, string funcStr, object[] extraArgs)
        {
            _callbacks ??= new();

            // Unref the old Lua function so it can be GC'd
            if (_callbacks.TryGetValue(type, out var old) && old.FuncRef > 0)
               Lua.Native.LuaLUnref(_lua.MainState, old.FuncRef);

            if (funcRef == 0 && funcStr == null)
            {
                _callbacks.Remove(type);
                return;
            }

            _callbacks[type] = new LuaCallback
            {
                FuncRef   = funcRef,
                FuncStr   = funcStr,
                ExtraArgs = extraArgs ?? Array.Empty<object>()
            };
        }

        public bool HasCallback(UICallback type)
            => _callbacks != null && _callbacks.ContainsKey(type);

        /// <summary>
        /// Fire a callback.  Must be called on the Lua thread.
        ///
        /// The callback receives ONLY the extra args that were stored at SetCallback time.
        /// If any of those args is a %placeholder string it is replaced with the
        /// corresponding value from <paramref name="ev"/>:
        ///   %id → element ID (int)
        ///   %x  → local mouse X (float)
        ///   %y  → local mouse Y (float)
        ///   %ax → absolute mouse X (float)
        ///   %ay → absolute mouse Y (float)
        ///   %b  → button index 0-4 (int)
        ///   %d  → scroll delta (double)
        ///   %k  → key code (int)
        ///   %c  → character string for the key (string)
        ///   %dx → movement delta X (float, drag/move)
        ///   %dy → movement delta Y (float, drag/move)
        /// </summary>
        internal void FireCallback(UICallback type, CallbackEventData ev)
        {
            OnInput(type, ev);

            if (_callbacks == null || !_callbacks.TryGetValue(type, out var cb)) return;

            if (cb.FuncRef > 0)
            {
                var args = ProcessArgs(cb.ExtraArgs, ev);
                _lua.CallLuaFunction(cb.FuncRef, sliced: true, args);
            }
            else if (cb.FuncStr != null)
            {
                _lua.CallLuaFunction(cb.FuncStr, sliced: true);
            }
        }

        protected virtual void OnInput(UICallback type, CallbackEventData ev) 
        { 
            if (type == UICallback.ClickTap) 
            {
                if (CanFocus) Shergen.SetFocus(this);
            }

            // Hint systém — volá se nezávisle na user callbacku
            if (Hint != null && Hint != "")
            {

                if (type == UICallback.MouseOver)
                {
                    var x = ev.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    var y = ev.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    _lua.CallLuaFunction($"HINT_SHOW([[{Hint}]], {x}, {y})", false);
                }
                else if (type == UICallback.MouseLeave)
                    _lua.CallLuaFunction("HINT_HIDE()", false);
            }

        }

        // ── Placeholder substitution ──────────────────────────────────────────────
        private static object[] ProcessArgs(object[] extraArgs, CallbackEventData ev)
        {
            if (extraArgs.Length == 0) return Array.Empty<object>();

            var result = new object[extraArgs.Length];
            for (int i = 0; i < extraArgs.Length; i++)
            {
                if (extraArgs[i] is string s && s.Length >= 2 && s[0] == '%')
                {
                    result[i] = s switch
                    {
                        "%id" => (object)ev.Id,
                        "%x" => ev.LocalX,      // local (element-relative)
                        "%y" => ev.LocalY,
                        "%ax" => ev.X,            // absolute screen position
                        "%ay" => ev.Y,
                        "%b" => ev.Button,
                        "%d" => ev.ScrollDelta,
                        "%dh" => ev.ScrollDeltaX,
                        "%dx" => ev.DeltaX,       // movement delta
                        "%dy" => ev.DeltaY,

                        "%c" => ((char)ev.CharCode).ToString(),

                        "%k" => ev.KeyCode,
                        "%sc" => ev.ScanCode,
                        "%shift" => ev.Shift,
                        "%ctrl" => ev.Ctrl,
                        "%alt" => ev.Alt,
                        "%shiftl" => ev.ShiftL,
                        "%shiftr" => ev.ShiftR,
                        "%ctrll" => ev.CtrlL,
                        "%ctrlr" => ev.CtrlR,
                        "%altl" => ev.AltL,
                        "%altr" => ev.AltR,
                        "%ev" => ev,

                        "%vis" => ev.visibility,
                        "%w"   => ev.width,
                        "%h"   => ev.height,
                        _ => s
                    };
                }
                else
                {
                    result[i] = extraArgs[i];
                }
            }
            return result;
        }

        // ── Input state (written by render thread, read by Lua thread) ────────────
        internal bool   IsMouseOver;
        internal bool[] ButtonPressedHere = new bool[5]; // buttons 0-4

        // ── Last-event globals (for GUI.Get* getters) ─────────────────────────────
        internal static float  LastMouseX;       // absolute
        internal static float  LastMouseY;       // absolute
        internal static float  LastMouseDeltaX;  // akumulovaný pohyb — reset při čtení
        internal static float  LastMouseDeltaY;
        internal static float  LastMouseLocalX;  // relative to last hovered element
        internal static float  LastMouseLocalY;
        internal static int    LastButton;
        internal static double LastScrollDelta;
        internal static int    LastKeyCode;
        internal static int    LastCharCode;
    }
}
