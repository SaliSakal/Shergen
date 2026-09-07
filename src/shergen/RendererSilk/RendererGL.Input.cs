using Silk.NET.Input;
using System.Collections.Concurrent;

namespace Shergen.Renderer
{
    public partial class RendererGL : IRenderer
    {
        // ── Per-button UICallback lookup tables ───────────────────────────────────
        private static readonly UICallback[] _cbDown  = { UICallback.LDown,  UICallback.RDown,  UICallback.MDown,  UICallback.B4Down,  UICallback.B5Down  };
        private static readonly UICallback[] _cbUp    = { UICallback.LUp,    UICallback.RUp,    UICallback.MUp,    UICallback.B4Up,    UICallback.B5Up    };
        private static readonly UICallback[] _cbClick = { UICallback.LClick, UICallback.RClick, UICallback.MClick, UICallback.B4Click, UICallback.B5Click };
        private static readonly UICallback[] _cbDbl   = { UICallback.LDblClick, UICallback.RDblClick, UICallback.MDblClick, UICallback.B4DblClick, UICallback.B5DblClick };

        // ── Input state (render thread) ───────────────────────────────────────────
        private UIElement _mouseOverElement;
        private UIElement[] _pressedOn    = new UIElement[5];
        private bool[]      _prevButtonDown = new bool[5];

        // Double-click detection
        private UIElement[] _lastClickElem = new UIElement[5];
        private DateTime[]  _lastClickTime = new DateTime[5];
        private const double DblClickMs = 300.0;

        // ── Silk.NET input handles ────────────────────────────────────────────────
        private IInputContext _input;
        private IMouse        _mouse;
        private IKeyboard     _keyboard;

        // Scroll captured in event, consumed in ProcessInput
        private float _scrollDeltaThisFrame;   // kolečko Y (hlavní)
        private float _scrollDeltaXThisFrame;  // kolečko X (druhé)

        // Previous mouse position for delta detection
        private float _prevMx, _prevMy;

        // ── Public event queue — drained by Lua thread ───────────────────────────
        public ConcurrentQueue<Action> IEventQueue => InputEventQueue;
        private readonly ConcurrentQueue<Action> InputEventQueue = new();

        // ── Focused element (keyboard target) ────────────────────────────────────
        private UIElement _focusedElement;

        public void SetFocus(UIElement elem) => _focusedElement = elem;

        public void ClearFocus() => _focusedElement = null;

        public UIElement GetFocus() => _focusedElement;
        
        public bool IsKeyPressed(int KeyCode) => _keyboard?.IsKeyPressed((Key)KeyCode) ?? false;

        public bool IsMousePressed(int button) => _mouse?.IsButtonPressed((MouseButton)button) ?? false;

        // ── Init — called from OnLoad after gl is ready ───────────────────────────
        private void InitInput()
        {
            _input    = window.CreateInput();
            _mouse    = _input.Mice.Count    > 0 ? _input.Mice[0]    : null;
            _keyboard = _input.Keyboards.Count > 0 ? _input.Keyboards[0] : null;

            if (_mouse != null)
                _mouse.Scroll += OnScroll;

            if (_keyboard != null)
            {
                _keyboard.KeyDown += OnKeyDown;
                _keyboard.KeyUp   += OnKeyUp;
                _keyboard.KeyChar += OnKeyChar;
            }
        }

        // ── Called from OnUpdate every frame ─────────────────────────────────────
        private void ProcessInput()
        {
            if (_mouse == null) return;

            float mx = _mouse.Position.X;
            float my = _mouse.Position.Y;

            UIElement.Mouse?.UpdatePosition(mx, my);
            UIElement.LastMouseX = mx;
            UIElement.LastMouseY = my;

            // ── Hit test ──────────────────────────────────────────────────────────
            var hovered = HitTest(mx, my);

            if (hovered != null)
            {
                UIElement.LastMouseLocalX = mx - hovered.GetAbsX();
                UIElement.LastMouseLocalY = my - hovered.GetAbsY();
            }

            // ── MouseMove / LDrag ─────────────────────────────────────────────────
            bool mouseMoving = mx != _prevMx || my != _prevMy;
            if (mouseMoving && (hovered != null || _pressedOn[0] != null))
            {
                float dX = mx - _prevMx;
                float dY = my - _prevMy;
                var moveTarget = _pressedOn[0] ?? hovered;
                var ev = MouseEv(moveTarget, mx, my);
                ev.DeltaX = dX;
                ev.DeltaY = dY;

                // MouseMove jde na hovered (nebo na _pressedOn pokud je aktivní drag)
                if (hovered != null)
                    InputEventQueue.Enqueue(() => moveTarget.FireCallback(UICallback.MouseMove, ev));

                // LDrag jde vždy na _pressedOn[0], i když je myš mimo jakýkoli element
                if (_pressedOn[0] != null)
                {
                    var dragTarget = _pressedOn[0];
                    var evDrag = ev;
                    InputEventQueue.Enqueue(() => dragTarget.FireCallback(UICallback.LDrag, evDrag));
                }
            }

            // ── MouseOver / MouseLeave ────────────────────────────────────────────
            if (hovered != _mouseOverElement)
            {
                //Console.WriteLine($"[HOVER] {_mouseOverElement?.ID.ToString() ?? "null"} -> {hovered?.ID.ToString() ?? "null"}");

                if (hovered != null)
                {
                    hovered.IsMouseOver = true;
                    var curr = hovered;
                    var ev = MouseEv(curr, mx, my);
                    //InputEventQueue.Enqueue(() => curr.FireCallback(UICallback.MouseOver, ev));
                    InputEventQueue.Enqueue(() => {
                        if (curr.IsMouseOver)
                            curr.FireCallback(UICallback.MouseOver, ev);
                    });
                }

                if (_mouseOverElement != null)
                {
                    _mouseOverElement.IsMouseOver = false;
                    var prev = _mouseOverElement;
                    var ev   = MouseEv(prev, mx, my);
                    InputEventQueue.Enqueue(() => prev.FireCallback(UICallback.MouseLeave, ev));
                }
                _mouseOverElement = hovered;

            }


            // ── Mouse buttons 0-4 ─────────────────────────────────────────────────
            bool touchRecent = (DateTime.Now - _lastTouchActivity).TotalMilliseconds < TouchSuppressMs;
            if (!touchRecent)
            {
                for (int btn = 0; btn < 5; btn++)
                {
                    bool down    = _mouse.IsButtonPressed((MouseButton)btn);
                    bool wasDown = _prevButtonDown[btn];
                    int  b       = btn;

                    if (down && !wasDown)
                    {
                        _pressedOn[btn]         = hovered;
                        UIElement.LastButton    = b;

                        if (hovered != null)
                        {
                            hovered.ButtonPressedHere[btn] = true;
                            var h  = hovered;
                            var ev = MouseEv(h, mx, my, b);
                            InputEventQueue.Enqueue(() => h.FireCallback(_cbDown[b], ev));
                        }
                    }
                    else if (!down && wasDown)
                    {
                        var pressedElem = _pressedOn[btn];
                        if (pressedElem != null)
                        {
                            pressedElem.ButtonPressedHere[btn] = false;
                            var evU = MouseEv(pressedElem, mx, my, b);
                            InputEventQueue.Enqueue(() => pressedElem.FireCallback(_cbUp[b], evU));
                        }

                        UIElement.LastButton = b;

                        if (hovered != null)
                        {
                            var h   = hovered;
                            var evU = MouseEv(h, mx, my, b);
                            InputEventQueue.Enqueue(() => h.FireCallback(_cbUp[b], evU));

                            if (pressedElem == hovered)
                            {
                                var  pe    = pressedElem;
                                var  evC   = MouseEv(pe, mx, my, b);
                                bool isDbl = _lastClickElem[btn] == hovered
                                          && (DateTime.Now - _lastClickTime[btn]).TotalMilliseconds <= DblClickMs;
                                _lastClickElem[btn] = hovered;
                                _lastClickTime[btn] = DateTime.Now;

                                if (isDbl)
                                {
                                    InputEventQueue.Enqueue(() => pe.FireCallback(_cbDbl[b], evC));
                                    if (b == 0)
                                        InputEventQueue.Enqueue(() => pe.FireCallback(UICallback.DblClickTap, evC));
                                }
                                else
                                {
                                    InputEventQueue.Enqueue(() => pe.FireCallback(_cbClick[b], evC));
                                    if (b == 0)
                                        InputEventQueue.Enqueue(() => pe.FireCallback(UICallback.ClickTap, evC));
                                }
                            }
                        }
                        _pressedOn[btn] = null;
                    }

                    _prevButtonDown[btn] = down;
                }
            }

            // ── Scroll wheel ──────────────────────────────────────────────────────
            float scrollDelta  = _scrollDeltaThisFrame;
            float scrollDeltaX = _scrollDeltaXThisFrame;
            _scrollDeltaThisFrame  = 0f;
            _scrollDeltaXThisFrame = 0f;

            if ((scrollDelta != 0f || scrollDeltaX != 0f) && hovered != null)
            {
                var h = hovered;
                UIElement.LastScrollDelta = scrollDelta;
                var ev = MouseEv(h, mx, my);
                ev.ScrollDelta = scrollDelta;
                ev.ScrollDeltaX = scrollDeltaX;
                InputEventQueue.Enqueue(() => h.FireCallback(UICallback.MouseWheel, ev));

                if (!h.HasCallback(UICallback.MouseWheel))
                {
                    var ancestor = (UIElement)h; // start from h itself — handles UITextLog where text is on the element directly
                    while (ancestor != null)
                    {
                        if (ancestor is IScrollable sc)
                        {
                            var   horzBox = sc as UIScrollBox;
                            float dY      = scrollDelta;
                            float dX      = scrollDeltaX;
                            InputEventQueue.Enqueue(() =>
                            {
                                float newY, newX;
                                if (horzBox != null && horzBox.HorizontalScroll)
                                {
                                    newX = Math.Clamp(sc.ScrollTargetX - dY * 30f, 0f, sc.MaxScrollX);
                                    newY = Math.Clamp(sc.ScrollTargetY - dX * 30f, 0f, sc.MaxScrollY);
                                }
                                else
                                {
                                    newY = Math.Clamp(sc.ScrollTargetY - dY * 30f, 0f, sc.MaxScrollY);
                                    newX = Math.Clamp(sc.ScrollTargetX - dX * 30f, 0f, sc.MaxScrollX);
                                }
                                sc.ScrollTargetY = newY;
                                sc.ScrollTargetX = newX;
                            });
                            break;
                        }
                        ancestor = ancestor.Parent;
                    }
                }
            }

            UIElement.LastMouseDeltaX = mx - _prevMx;
            UIElement.LastMouseDeltaY = my - _prevMy;
            _prevMx = mx;
            _prevMy = my;

            // ── Touch ─────────────────────────────────────────────────────────────
            ProcessTouchQueue();
        }

        // ── Scroll event (fired on render thread during window.Run) ───────────────
        private void OnScroll(IMouse mouse, ScrollWheel scroll)
        {
            _scrollDeltaThisFrame  += scroll.Y;
            _scrollDeltaXThisFrame += scroll.X;
        }

        // ── Keyboard events ───────────────────────────────────────────────────────
        private void OnKeyDown(IKeyboard kb, Key key, int scanCode)
        {
            if (key == Key.F9) { Profiler.Toggle(); return; }


            var target = _focusedElement ?? UIElement.Desktop;
            //Console.WriteLine($"[KEYDOWN] key={key} target={target?.ID.ToString() ?? "null"} focused={_focusedElement?.ID.ToString() ?? "null"}");

            if (target == null) return;

            int keyCode = (int)key;       // Silk.NET Key maps to GLFW codes — same values as OpenTK
            UIElement.LastKeyCode = keyCode;

            var t  = target;
            var ev = new UIElement.CallbackEventData
            {
                Id = t.ID,
                KeyCode = keyCode,
                ScanCode = scanCode,
                Shift = kb.IsKeyPressed(Key.ShiftLeft) || kb.IsKeyPressed(Key.ShiftRight),
                ShiftL = kb.IsKeyPressed(Key.ShiftLeft),
                ShiftR = kb.IsKeyPressed(Key.ShiftRight),
                Ctrl = kb.IsKeyPressed(Key.ControlLeft) || kb.IsKeyPressed(Key.ControlRight),
                CtrlL = kb.IsKeyPressed(Key.ControlLeft),
                CtrlR = kb.IsKeyPressed(Key.ControlRight),
                Alt = kb.IsKeyPressed(Key.AltLeft) || kb.IsKeyPressed(Key.AltRight),
                AltL = kb.IsKeyPressed(Key.AltLeft),
                AltR = kb.IsKeyPressed(Key.AltRight),
            };
            InputEventQueue.Enqueue(() => t.FireCallback(UICallback.KeyDown, ev));
        }

        private void OnKeyUp(IKeyboard kb, Key key, int scanCode)
        {
            var target = _focusedElement ?? UIElement.Desktop;
            if (target == null) return;

            int keyCode = (int)key;
            UIElement.LastKeyCode = keyCode;

            var t  = target;
            var ev = new UIElement.CallbackEventData
            {
                Id = t.ID,
                KeyCode = keyCode,
                ScanCode = scanCode,
                Shift = kb.IsKeyPressed(Key.ShiftLeft) || kb.IsKeyPressed(Key.ShiftRight),
                ShiftL = kb.IsKeyPressed(Key.ShiftLeft),
                ShiftR = kb.IsKeyPressed(Key.ShiftRight),
                Ctrl = kb.IsKeyPressed(Key.ControlLeft) || kb.IsKeyPressed(Key.ControlRight),
                CtrlL = kb.IsKeyPressed(Key.ControlLeft),
                CtrlR = kb.IsKeyPressed(Key.ControlRight),
                Alt = kb.IsKeyPressed(Key.AltLeft) || kb.IsKeyPressed(Key.AltRight),
                AltL = kb.IsKeyPressed(Key.AltLeft),
                AltR = kb.IsKeyPressed(Key.AltRight),
            };
            InputEventQueue.Enqueue(() => t.FireCallback(UICallback.KeyUp, ev));
        }

        private void OnKeyChar(IKeyboard kb, char c)
        {
            var target = _focusedElement ?? UIElement.Desktop;
            if (target == null) return;

            int charCode = c;
            UIElement.LastCharCode = charCode;

            var t  = target;
            var ev = new UIElement.CallbackEventData { Id = t.ID, CharCode = charCode };
            InputEventQueue.Enqueue(() => t.FireCallback(UICallback.KeyPress, ev));
        }

        // ── Event data helpers ────────────────────────────────────────────────────
        private static UIElement.CallbackEventData MouseEv(UIElement elem, float mx, float my, int btn = 0)
            => new UIElement.CallbackEventData
            {
                Id     = elem.ID,
                X      = mx,
                Y      = my,
                LocalX = mx - elem.GetAbsX(),
                LocalY = my - elem.GetAbsY(),
                Button = btn,
            };

        // ── Hit testing ───────────────────────────────────────────────────────────
        private UIElement HitTest(float x, float y)
        {
            if (UIElement.Desktop == null) return null;
            return HitTestElement(UIElement.Desktop, x, y);
        }

        private UIElement HitTestElement(UIElement elem, float x, float y, bool passThroughActive = false)
        {
            if (!elem.Visible) return null;

            bool myPassThrough = passThroughActive
                ? !elem.EventCapture
                :  elem.PassThrough;

            float ax = elem.GetAbsX();
            float ay = elem.GetAbsY();
            if (x < ax || y < ay || x > ax + elem.Width || y > ay + elem.Height)
                return null;

            if (elem.HitMaskData != null)
            {

                // převeď absolutní x,y na lokální souřadnice textury
                float lx = (x - ax) / elem.Width;
                float ly = (y - ay) / elem.Height;
                int px = Math.Clamp((int)(lx * elem.HitMaskW), 0, elem.HitMaskW - 1);
                int py = Math.Clamp((int)(ly * elem.HitMaskH), 0, elem.HitMaskH - 1);
                //int index = (py * elem.HitMaskW + px) * 4 + 3; // alfa kanál
                //if (elem.HitMaskData[index] < elem.HitMaskThreshold) return null;
                int byteIndex = (py * elem.HitMaskW + px) * 2 + (elem.HitMaskUseGrayscale ? 0 : 1);
                if (elem.HitMaskData[byteIndex] < elem.HitMaskThreshold) return null;

            }

            
            for (int i = elem.Children.Count - 1; i >= 0; i--)
            {
                if (!elem.Children[i].IsSpecial) continue;
                var hit = HitTestElement(elem.Children[i], x, y, myPassThrough);
                if (hit != null) return hit;
            }
            for (int i = elem.Children.Count - 1; i >= 0; i--)
            {
                if (elem.Children[i].IsSpecial) continue;
                var hit = HitTestElement(elem.Children[i], x, y, myPassThrough);
                if (hit != null) return hit;
            }

            return myPassThrough ? null : elem;
        }

 
    }
}
