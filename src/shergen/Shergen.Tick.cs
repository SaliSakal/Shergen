using System.Globalization;
using static Shergen.Lua.Engine;
using static Shergen.Lua.Native;

namespace Shergen
{
    public partial class Shergen
    {
        // ── Tick callback storage (Lua thread only) ───────────────────────────────
        static readonly Dictionary<int, (int funcRef, string funcStr)> tickCallbacks = new();
        static int tickCallbackCounter = 0;

        // ── Resize callback (single, Lua thread only) ─────────────────────────────
        static int resizeCallbackRef = 0;
        static string resizeCallbackStr = null;

        // ── Called from IEventQueue on Lua thread ─────────────────────────────────
        internal static void FireTick(double dt)
        {
            float frametime = (float)dt;
            string frametimeStr = frametime.ToString(CultureInfo.InvariantCulture);

            foreach (var (_, cb) in tickCallbacks)
            {
                if (cb.funcRef > 0)
                    mainLua.CallLuaFunction(cb.funcRef, sliced: true, frametime);
                else if (cb.funcStr != null)
                    mainLua.CallLuaFunction(cb.funcStr.Replace("%frametime", frametimeStr));
            }
        }

        internal static void FireResize(int w, int h)
        {
            if (resizeCallbackRef > 0)
                mainLua.CallLuaFunction(resizeCallbackRef, sliced: true, w, h);
            else if (resizeCallbackStr != null)
            {
                string script = resizeCallbackStr
                    .Replace("%w", w.ToString())
                    .Replace("%h", h.ToString());
                mainLua.CallLuaFunction(script);
            }
        }

        // ── CAF: GUI.RegisterTickCallback(func_or_string) → id ───────────────────
        static int RegisterTickCallback(IntPtr L)
        {
            if (IsLuaFunction(L, 1))
            {
                PushValue(L, 1);
                int luaRef = LuaLRef(L);
                int id = ++tickCallbackCounter;
                tickCallbacks[id] = (luaRef, null);
                PushLuaInteger(L, id);
                return 1;
            }
            else if (IsLuaString(L, 1))
            {
                string script = ToLuaString(L, 1);
                int id = ++tickCallbackCounter;
                tickCallbacks[id] = (0, script);
                PushLuaInteger(L, id);
                return 1;
            }

            PushLuaNil(L);
            return 1;
        }

        // ── CAF: GUI.UnregisterTickCallback(id) ───────────────────────────────────
        static int UnregisterTickCallback(IntPtr L)
        {
            int id = ToLuaInteger(L, 1);
            if (tickCallbacks.TryGetValue(id, out var cb))
            {
                if (cb.funcRef > 0)
                    LuaLUnref(mainLua.MainState, cb.funcRef);
                tickCallbacks.Remove(id);
            }
            return 0;
        }

        // ── CAF: GUI.SetResizeCallback(func_or_string) ────────────────────────────
        static int SetResizeCallback(IntPtr L)
        {
            // Uvolni případný starý callback
            if (resizeCallbackRef > 0)
            {
                LuaLUnref(mainLua.MainState, resizeCallbackRef);
                resizeCallbackRef = 0;
            }
            resizeCallbackStr = null;

            if (IsLuaFunction(L, 1))
            {
                PushValue(L, 1);
                resizeCallbackRef = LuaLRef(L);
            }
            else if (IsLuaString(L, 1))
            {
                resizeCallbackStr = ToLuaString(L, 1);
            }

            return 0;
        }
    }
}
