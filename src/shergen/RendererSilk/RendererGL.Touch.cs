using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Shergen.Renderer
{
    public partial class RendererGL : IRenderer
    {
        // ── Inicializace / cleanup ────────────────────────────────────────────────

        private void InitTouch()
        {
            if (OperatingSystem.IsWindows())
                InitTouchWindows();
            else if (OperatingSystem.IsLinux())
                InitTouchLinux();
        }

        private void CleanupTouch()
        {
            if (OperatingSystem.IsWindows())
                CleanupTouchWindows();
            else if (OperatingSystem.IsLinux())
                CleanupTouchLinux();
        }

        // ── Shared pipeline ───────────────────────────────────────────────────────

        private record struct RawTouch(int Id, float X, float Y, int Type);
        private readonly System.Collections.Concurrent.ConcurrentQueue<RawTouch> _rawTouchQueue = new();

        private void ProcessTouchQueue()
        {
            while (_rawTouchQueue.TryDequeue(out var t))
            {
                if      (t.Type == 0) EnqueueTouchDown(t.Id, t.X, t.Y);
                else if (t.Type == 1) EnqueueTouchUp(t.Id, t.X, t.Y);
                else                  EnqueueTouchMove(t.Id, t.X, t.Y);
            }
        }

        // ── Sdílená logika ────────────────────────────────────────────────────────

        private readonly Dictionary<int, UIElement>      _touchPressedOn  = new();
        private readonly Dictionary<int, (float x, float y)> _touchPrevPos = new();
        private readonly HashSet<int>                     _touchDidScroll  = new();
        private UIElement _touchLastTapElem;
        private DateTime  _touchLastTapTime;
        private DateTime  _lastTouchActivity = DateTime.MinValue;
        private const int TouchSuppressMs    = 150;
        private const float TouchDragThreshold = 8f;

        private void EnqueueTouchDown(int id, float x, float y)
        {
            var elem = HitTest(x, y);
            if (elem == null) return;

            _touchPressedOn[id] = elem;
            _touchPrevPos[id]   = (x, y);
            _touchDidScroll.Remove(id);
            UIElement.LastMouseX      = x;
            UIElement.LastMouseY      = y;
            UIElement.LastMouseLocalX = x - elem.GetAbsX();
            UIElement.LastMouseLocalY = y - elem.GetAbsY();
            UIElement.LastButton      = 0;

            _lastTouchActivity = DateTime.Now;

            var ev = MouseEv(elem, x, y, 0);
            InputEventQueue.Enqueue(() => elem.FireCallback(UICallback.TouchDown, ev));
        }

        private void EnqueueTouchUp(int id, float x, float y)
        {
            var elem = HitTest(x, y);
            UIElement.LastMouseX = x;
            UIElement.LastMouseY = y;
            UIElement.LastButton = 0;

            // Prst zvednut — zruš hover
            if (_mouseOverElement != null)
            {
                _mouseOverElement.IsMouseOver = false;
                var prev = _mouseOverElement;
                InputEventQueue.Enqueue(() => prev.FireCallback(UICallback.MouseLeave, MouseEv(prev, x, y)));
                _mouseOverElement = null;
            }

            if (elem != null)
            {
                UIElement.LastMouseLocalX = x - elem.GetAbsX();
                UIElement.LastMouseLocalY = y - elem.GetAbsY();

                var evU = MouseEv(elem, x, y, 0);
                InputEventQueue.Enqueue(() => elem.FireCallback(UICallback.TouchUp, evU));

                if (_touchPressedOn.TryGetValue(id, out var pressed) && pressed == elem
                    && !_touchDidScroll.Contains(id))   // potlač tap pokud byl drag scroll
                {
                    bool isDbl = _touchLastTapElem == elem
                              && (DateTime.Now - _touchLastTapTime).TotalMilliseconds <= DblClickMs;

                    _touchLastTapElem = elem;
                    _touchLastTapTime = DateTime.Now;

                    var evC = evU;
                    if (isDbl)
                    {
                        InputEventQueue.Enqueue(() => elem.FireCallback(UICallback.TouchDblTap, evC));
                        InputEventQueue.Enqueue(() => elem.FireCallback(UICallback.DblClickTap, evC));
                    }
                    else
                    {
                        InputEventQueue.Enqueue(() => elem.FireCallback(UICallback.TouchTap, evC));
                        InputEventQueue.Enqueue(() => elem.FireCallback(UICallback.ClickTap, evC));
                    }
                }
            }

            _touchPressedOn.Remove(id);
            _touchPrevPos.Remove(id);
            _touchDidScroll.Remove(id);
        }

        private void EnqueueTouchMove(int id, float x, float y)
        {
            var elem = HitTest(x, y);
            UIElement.LastMouseX = x;
            UIElement.LastMouseY = y;
            _lastTouchActivity = DateTime.Now;

            if (elem != null)
            {
                UIElement.LastMouseLocalX = x - elem.GetAbsX();
                UIElement.LastMouseLocalY = y - elem.GetAbsY();
            }

            // ── Delta pohybu (vypočítáme jednou, použijeme pro scroll i TouchMove) ─
            float moveDX = 0f, moveDY = 0f;
            if (_touchPrevPos.TryGetValue(id, out var prevPos))
            {
                moveDX = x - prevPos.x;
                moveDY = y - prevPos.y;
            }
            _touchPrevPos[id] = (x, y);

            // ── Touch drag scroll ─────────────────────────────────────────────────
            if (Math.Abs(moveDX) > TouchDragThreshold || Math.Abs(moveDY) > TouchDragThreshold ||
                _touchDidScroll.Contains(id))
            {
                IScrollable dragScroll = null;
                if (_touchPressedOn.TryGetValue(id, out var pressed))
                {
                    if (pressed is IScrollable direct)
                        dragScroll = direct;
                    else
                    {
                        var anc = pressed.Parent;
                        while (anc != null)
                        {
                            if (anc is IScrollable sc) { dragScroll = sc; break; }
                            anc = anc.Parent;
                        }
                    }
                }

                if (dragScroll != null)
                {
                    _touchDidScroll.Add(id);
                    var  sc = dragScroll;
                    float dX = moveDX;
                    float dY = moveDY;
                    InputEventQueue.Enqueue(() =>
                    {
                        float newX = Math.Clamp(sc.ScrollTargetX - dX, 0f, sc.MaxScrollX);
                        float newY = Math.Clamp(sc.ScrollTargetY - dY, 0f, sc.MaxScrollY);
                        sc.ScrollTargetX = newX;
                        sc.ScrollTargetY = newY;
                    });
                }
            }

            // ── Hover jen pro primární prst ───────────────────────────────────────
            if (IsPrimaryTouch(id) && elem != _mouseOverElement)
            {
                if (_mouseOverElement != null)
                {
                    _mouseOverElement.IsMouseOver = false;
                    var prev = _mouseOverElement;
                    InputEventQueue.Enqueue(() => prev.FireCallback(UICallback.MouseLeave, MouseEv(prev, x, y)));
                }
                _mouseOverElement = elem;
                if (elem != null)
                {
                    elem.IsMouseOver = true;
                    var curr = elem;
                    InputEventQueue.Enqueue(() => curr.FireCallback(UICallback.MouseOver, MouseEv(curr, x, y)));
                }
            }

            if (elem != null)
                InputEventQueue.Enqueue(() => elem.FireCallback(UICallback.MouseMove, MouseEv(elem, x, y)));

            // ── TouchMove na element kde prst začal ───────────────────────────────
            if (_touchPressedOn.TryGetValue(id, out var touchOrigin) && touchOrigin != null)
            {
                var evDrag = MouseEv(touchOrigin, x, y);
                evDrag.DeltaX = moveDX;
                evDrag.DeltaY = moveDY;
                var origin = touchOrigin;
                InputEventQueue.Enqueue(() => origin.FireCallback(UICallback.TouchMove, evDrag));
            }
        }

        private bool IsPrimaryTouch(int id)
        {
            foreach (var k in _touchPressedOn.Keys) return k == id;
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // WINDOWS — WM_TOUCH přes subclassing WndProc
        // ─────────────────────────────────────────────────────────────────────────

        private delegate IntPtr WndProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WndProcDelegate _touchWndProc;   // reference zabrání GC
        private IntPtr _origWndProc;
        private IntPtr _touchHwnd;

        const string winTouchLibrary = "user32.dll";

        [DllImport(winTouchLibrary, EntryPoint = "SetWindowLongPtrW")]
        static extern IntPtr Win_SetWindowLongPtr(IntPtr hWnd, int idx, IntPtr val);
        [DllImport(winTouchLibrary, EntryPoint = "GetWindowLongPtrW")]
        static extern IntPtr Win_GetWindowLongPtr(IntPtr hWnd, int idx);
        [DllImport(winTouchLibrary)]
        static extern IntPtr CallWindowProc(IntPtr prev, IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport(winTouchLibrary)]
        static extern bool RegisterTouchWindow(IntPtr hwnd, uint flags);
        [DllImport(winTouchLibrary)]
        static extern bool UnregisterTouchWindow(IntPtr hwnd);
        [DllImport(winTouchLibrary)]
        static extern bool GetTouchInputInfo(IntPtr hTouch, uint n, [Out] TOUCHINPUT[] arr, int size);
        [DllImport(winTouchLibrary)]
        static extern bool CloseTouchInputHandle(IntPtr hTouch);
        [DllImport(winTouchLibrary)]
        static extern bool ScreenToClient(IntPtr hwnd, ref POINT pt);
        [DllImport(winTouchLibrary)]
        static extern IntPtr GetMessageExtraInfo();

        const int  GWLP_WNDPROC    = -4;
        const uint WM_TOUCH        = 0x0240;
        const int  TOUCHEVENTF_MOVE = 0x0001;
        const int  TOUCHEVENTF_DOWN = 0x0002;
        const int  TOUCHEVENTF_UP   = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        struct TOUCHINPUT
        {
            public int    x, y;
            public IntPtr hSource;
            public int    dwID, dwFlags, dwMask, dwTime;
            public IntPtr dwExtraInfo;
            public int    cxContact, cyContact;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int X, Y; }

        [SupportedOSPlatform("windows")]
        private void InitTouchWindows()
        {
            // Silk.NET: window.Native?.Win32 vrací (Hwnd, Hdc, HInstance)?
            if (window.Native?.Win32 is { } w32)
                _touchHwnd = (IntPtr)w32.Hwnd;

            if (_touchHwnd == IntPtr.Zero) { Console.WriteLine("⚠️  Touch: nepodařilo se získat HWND"); return; }

            RegisterTouchWindow(_touchHwnd, 0);

            _touchWndProc = TouchWndProc;
            _origWndProc  = Win_GetWindowLongPtr(_touchHwnd, GWLP_WNDPROC);
            Win_SetWindowLongPtr(_touchHwnd, GWLP_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_touchWndProc));
        }

        [SupportedOSPlatform("windows")]
        private void CleanupTouchWindows()
        {
            if (_touchHwnd == IntPtr.Zero) return;
            Win_SetWindowLongPtr(_touchHwnd, GWLP_WNDPROC, _origWndProc);
            UnregisterTouchWindow(_touchHwnd);
        }

        [SupportedOSPlatform("windows")]
        private IntPtr TouchWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            const uint WM_MOUSEMOVE    = 0x0200;
            const uint WM_LBUTTONDOWN  = 0x0201;
            const uint WM_LBUTTONUP    = 0x0202;
            const uint WM_LBUTTONDBLCLK = 0x0203;
            if (msg == WM_MOUSEMOVE || msg == WM_LBUTTONDOWN ||
                msg == WM_LBUTTONUP || msg == WM_LBUTTONDBLCLK)
            {
                long extra = GetMessageExtraInfo().ToInt64();
                if ((extra & 0xFFFFFF80) == 0xFF515700)
                    return IntPtr.Zero; // potlač — syntetická zpráva z touche
            }

            if (msg == WM_TOUCH)
            {
                int count  = wParam.ToInt32() & 0xFFFF;
                var inputs = new TOUCHINPUT[count];
                if (GetTouchInputInfo(lParam, (uint)count, inputs, Marshal.SizeOf<TOUCHINPUT>()))
                {
                    foreach (var t in inputs)
                    {
                        var pt = new POINT { X = t.x / 100, Y = t.y / 100 };
                        ScreenToClient(hwnd, ref pt);

                        int type = (t.dwFlags & TOUCHEVENTF_DOWN) != 0 ? 0
                                 : (t.dwFlags & TOUCHEVENTF_UP)   != 0 ? 1 : 2;
                        _rawTouchQueue.Enqueue(new RawTouch(t.dwID, pt.X, pt.Y, type));
                    }
                    CloseTouchInputHandle(lParam);
                }
                return IntPtr.Zero;
            }
            return CallWindowProc(_origWndProc, hwnd, msg, wParam, lParam);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // LINUX — libinput
        // Požadavek: uživatel musí být ve skupině 'input'  (sudo usermod -aG input $USER)
        // ─────────────────────────────────────────────────────────────────────────

        private Thread _linuxTouchThread;
        private bool   _linuxTouchRunning;
        private IntPtr _libinputCtx;

        const int LIBINPUT_EVENT_TOUCH_DOWN   = 500;
        const int LIBINPUT_EVENT_TOUCH_UP     = 501;
        const int LIBINPUT_EVENT_TOUCH_MOTION = 502;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int OpenRestrictedDelegate([MarshalAs(UnmanagedType.LPStr)] string path, int flags, IntPtr ud);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void CloseRestrictedDelegate(int fd, IntPtr ud);

        [StructLayout(LayoutKind.Sequential)]
        struct LibinputInterface
        {
            public IntPtr open_restricted;
            public IntPtr close_restricted;
        }

        [DllImport("libc")] 
        static extern int open([MarshalAs(UnmanagedType.LPStr)] string path, int flags);
        [DllImport("libc")] 
        static extern int close(int fd);

        const string linTouchLibrary = "libinput.so.10";
        [DllImport(linTouchLibrary)] 
        static extern IntPtr libinput_path_create_context(ref LibinputInterface iface, IntPtr ud);
        [DllImport(linTouchLibrary)] 
        static extern IntPtr libinput_path_add_device(IntPtr li, [MarshalAs(UnmanagedType.LPStr)] string path);
        [DllImport(linTouchLibrary)] 
        static extern int    libinput_dispatch(IntPtr li);
        [DllImport(linTouchLibrary)] 
        static extern IntPtr libinput_get_event(IntPtr li);
        [DllImport(linTouchLibrary)] 
        static extern int    libinput_event_get_type(IntPtr e);
        [DllImport(linTouchLibrary)] 
        static extern IntPtr libinput_event_get_touch_event(IntPtr e);
        [DllImport(linTouchLibrary)] 
        static extern int    libinput_event_touch_get_slot(IntPtr te);
        [DllImport(linTouchLibrary)] 
        static extern double libinput_event_touch_get_x_transformed(IntPtr te, uint w);
        [DllImport(linTouchLibrary)] 
        static extern double libinput_event_touch_get_y_transformed(IntPtr te, uint h);
        [DllImport(linTouchLibrary)] 
        static extern void   libinput_event_destroy(IntPtr e);
        [DllImport(linTouchLibrary)] 
        static extern void   libinput_unref(IntPtr li);

        private OpenRestrictedDelegate  _openRestricted;
        private CloseRestrictedDelegate _closeRestricted;

        [SupportedOSPlatform("linux")]
        private void InitTouchLinux()
        {
            string device = FindLinuxTouchDevice();
            if (device == null)
            {
                Console.WriteLine("⚠️  Touchscreen nenalezen. Zkontroluj /proc/bus/input/devices");
                return;
            }

            try
            {
                _openRestricted  = (path, flags, _) => open(path, flags);
                _closeRestricted = (fd, _) => close(fd);

                var iface = new LibinputInterface
                {
                    open_restricted  = Marshal.GetFunctionPointerForDelegate(_openRestricted),
                    close_restricted = Marshal.GetFunctionPointerForDelegate(_closeRestricted),
                };

                _libinputCtx = libinput_path_create_context(ref iface, IntPtr.Zero);
                if (_libinputCtx == IntPtr.Zero) { Console.WriteLine("⚠️  libinput init selhala"); return; }

                libinput_path_add_device(_libinputCtx, device);

                _linuxTouchRunning = true;
                _linuxTouchThread  = new Thread(LinuxTouchLoop) { IsBackground = true, Name = "TouchInput" };
                _linuxTouchThread.Start();

                Console.WriteLine($"✅ Touch: {device}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Touch init: {ex.Message}");
            }
        }

        [SupportedOSPlatform("linux")]
        private void LinuxTouchLoop()
        {
            while (_linuxTouchRunning)
            {
                libinput_dispatch(_libinputCtx);
                IntPtr ev;
                while ((ev = libinput_get_event(_libinputCtx)) != IntPtr.Zero)
                {
                    int type = libinput_event_get_type(ev);
                    if (type is LIBINPUT_EVENT_TOUCH_DOWN
                             or LIBINPUT_EVENT_TOUCH_UP
                             or LIBINPUT_EVENT_TOUCH_MOTION)
                    {
                        var  te = libinput_event_get_touch_event(ev);
                        int  id = libinput_event_touch_get_slot(te);
                        // Silk.NET: window.Size namísto window.ClientSize
                        var  sz = window.Size;
                        float x = (float)libinput_event_touch_get_x_transformed(te, (uint)sz.X);
                        float y = (float)libinput_event_touch_get_y_transformed(te, (uint)sz.Y);

                        int t = type == LIBINPUT_EVENT_TOUCH_DOWN ? 0
                              : type == LIBINPUT_EVENT_TOUCH_UP   ? 1 : 2;
                        _rawTouchQueue.Enqueue(new RawTouch(id, x, y, t));
                    }
                    libinput_event_destroy(ev);
                }
                Thread.Sleep(1);
            }
        }

        [SupportedOSPlatform("linux")]
        private void CleanupTouchLinux()
        {
            _linuxTouchRunning = false;
            _linuxTouchThread?.Join(500);
            if (_libinputCtx != IntPtr.Zero) libinput_unref(_libinputCtx);
        }

        [SupportedOSPlatform("linux")]
        private static string FindLinuxTouchDevice()
        {
            try
            {
                string raw = File.ReadAllText("/proc/bus/input/devices");
                foreach (var section in raw.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!section.Contains("ABS_MT") &&
                        !section.Contains("ouchscreen", StringComparison.OrdinalIgnoreCase)) continue;

                    foreach (var line in section.Split('\n'))
                    {
                        if (!line.StartsWith("H: Handlers=")) continue;
                        foreach (var part in line.Split(' '))
                            if (part.StartsWith("event"))
                                return "/dev/input/" + part.Trim();
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
