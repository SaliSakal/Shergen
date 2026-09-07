using System.Runtime.InteropServices;
using System.Text;


namespace Shergen.Lua
{
    public partial class Native
    {

        public delegate void LuaHook(IntPtr L, IntPtr ar);

        const string LuaLibrary = "lua55"; // Název DLL souboru

        [LibraryImport(LuaLibrary)]
        private static partial IntPtr luaL_newstate();

        [LibraryImport(LuaLibrary)]
        private static partial void luaL_openselectedlibs(IntPtr L, int load, int preload);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_close(IntPtr L);

        [LibraryImport(LuaLibrary, StringMarshalling = StringMarshalling.Utf8)]
        private static partial int luaL_loadfilex(IntPtr L, string filename, string mode);

        [LibraryImport(LuaLibrary)]
        private static partial int luaL_loadbufferx(IntPtr L, IntPtr buff, UIntPtr size, IntPtr name, IntPtr mode);

        // `lua_pcallk` místo `lua_pcall`
        [LibraryImport(LuaLibrary)]
        private static partial int lua_pcallk(IntPtr L, int nargs, int nresults, int errfunc, int ctx, IntPtr k);

        [LibraryImport(LuaLibrary)]
        private static partial int lua_callk(IntPtr L, int nargs, int nresults, int errfunc, int ctx, IntPtr k);


        [LibraryImport(LuaLibrary, StringMarshalling = StringMarshalling.Utf8)]
        private static partial int luaL_loadstring(IntPtr L, string script);
        [LibraryImport(LuaLibrary)]
        private static partial IntPtr lua_tolstring(IntPtr L, int index, out IntPtr len);

        [LibraryImport(LuaLibrary)]
        private static partial int lua_gettop(IntPtr L);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_pushcclosure(IntPtr L, IntPtr fn, int n);

        [LibraryImport(LuaLibrary, StringMarshalling = StringMarshalling.Utf8)]
        private static partial void lua_setglobal(IntPtr L, string name);

        [LibraryImport(LuaLibrary, StringMarshalling = StringMarshalling.Utf8)]
        private static partial int lua_getglobal(IntPtr luaState, string name);

        [LibraryImport(LuaLibrary)]
        private static partial double lua_tonumberx(IntPtr L, int index, IntPtr isnum);

        [LibraryImport(LuaLibrary)]
        private static partial long lua_tointegerx(IntPtr L, int index, IntPtr isnum);

        [LibraryImport(LuaLibrary)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool lua_toboolean(IntPtr L, int index);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_pushinteger(IntPtr L, int value);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_insert(IntPtr L, int index);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_remove(IntPtr L, int index);

        [LibraryImport(LuaLibrary, StringMarshalling = StringMarshalling.Utf8)]
        private static partial int luaL_callmeta(IntPtr L, int obj, string e);

        [LibraryImport(LuaLibrary)]
        private static partial int lua_type(IntPtr L, int index);

        [LibraryImport(LuaLibrary)]
        private static partial IntPtr lua_typename(IntPtr L, int type);

        [LibraryImport(LuaLibrary, StringMarshalling = StringMarshalling.Utf8)]
        private static partial void lua_pushstring(IntPtr L, string str);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_pushnumber(IntPtr L, double number);

        [LibraryImport(LuaLibrary, StringMarshalling = StringMarshalling.Utf8)]
        private static partial void luaL_traceback(IntPtr L, IntPtr L1, string msg, int level);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_rotate(IntPtr L, int idx, int n);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_settop(IntPtr L, int idx);

        [LibraryImport(LuaLibrary, StringMarshalling = StringMarshalling.Utf8)]
        private static partial void lua_getfield(IntPtr L, int index, string key);

        // Import pro lua_pushnil - vloží nil do Lua zásobníku
        [LibraryImport(LuaLibrary)]
        private static partial void lua_pushnil(IntPtr luaState);
        // Import pro lua_pushlstring - vloží řetězec do Lua zásobníku (s explicitní délkou)
        [LibraryImport(LuaLibrary)]
        private static partial IntPtr lua_pushlstring(IntPtr luaState, IntPtr s, UIntPtr len);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_createtable(IntPtr L, int narr, int nrec);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_settable(IntPtr luaState, int index);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_pushboolean(IntPtr L, int b);

        [LibraryImport(LuaLibrary)]
        private static partial IntPtr lua_setupvalue(IntPtr luaState, int funcIndex, int n);

        [LibraryImport(LuaLibrary)]
        private static partial IntPtr lua_getupvalue(IntPtr luaState, int funcIndex, int n);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_pushvalue(IntPtr luaState, int index);

        [LibraryImport(LuaLibrary, StringMarshalling = StringMarshalling.Utf8)]
        private static partial void lua_setfield(IntPtr luaState, int index, string key);

        [LibraryImport(LuaLibrary)]
        private static partial int lua_setmetatable(IntPtr luaState, int index);

        [LibraryImport(LuaLibrary)]
        private static partial int luaL_ref(IntPtr luaState, int t);

        [LibraryImport(LuaLibrary)]
        private static partial void luaL_unref(IntPtr luaState, int t, int reference);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_rawseti(IntPtr L, int index, long n);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_rawgeti(IntPtr luaState, int index, int n);

        [LibraryImport(LuaLibrary)]
        private static partial int lua_next(IntPtr L, int index);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_rawget(IntPtr L, int index);

        [LibraryImport(LuaLibrary)]
        private static partial IntPtr lua_newthread(IntPtr L);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_xmove(IntPtr from, IntPtr to, int n);

        [LibraryImport(LuaLibrary)]
        private static partial int lua_resume(IntPtr L, IntPtr from, int nargs, out int nresults);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_sethook(IntPtr L, LuaHook hook, int mask, int count);

        [LibraryImport(LuaLibrary)]
        private static partial IntPtr lua_gethook(IntPtr L);

        [LibraryImport(LuaLibrary)]
        private static partial int lua_gethookmask(IntPtr L);

        [LibraryImport(LuaLibrary)]
        private static partial int lua_gethookcount(IntPtr L);

        [LibraryImport(LuaLibrary)]
        private static partial int lua_status(IntPtr L);

        [LibraryImport(LuaLibrary)]
        private static partial int lua_yieldk(IntPtr L, int nresults, IntPtr ctx, IntPtr k);

        [LibraryImport(LuaLibrary)]
        private static partial int lua_error(IntPtr L);

        [LibraryImport(LuaLibrary, StringMarshalling = StringMarshalling.Utf8)]
        private static partial int luaL_error(IntPtr L, [MarshalAs(UnmanagedType.LPUTF8Str)] string message);

        [LibraryImport(LuaLibrary)]
        private static partial IntPtr luaL_tolstring(IntPtr L, int idx, IntPtr len);

        [LibraryImport(LuaLibrary)]
        private static partial void lua_pushlightuserdata(IntPtr L, IntPtr p);

        [LibraryImport(LuaLibrary)]
        private static partial IntPtr lua_touserdata(IntPtr L, int index);

        // když se volá lua_type(luaState, index)  vrátí jednu z těhle konstant
        public const int LUA_TNONE = -1; // Neplatný index
        public const int LUA_TNIL = 0;  // nil (prázdná hodnota)
        public const int LUA_TBOOLEAN = 1;  // true/false
        public const int LUA_TLIGHTUSERDATA = 2;  // Lehká userdata
        public const int LUA_TNUMBER = 3;  // Číslo (int/float)
        public const int LUA_TSTRING = 4;  // Řetězec
        public const int LUA_TTABLE = 5;  // Tabulka
        public const int LUA_TFUNCTION = 6;  // Funkce
        public const int LUA_TUSERDATA = 7;  // Uživatelská data (objekty v C#)
        public const int LUA_TTHREAD = 8;  // Vlákno (coroutine)

        // konstanty pro regsitry a volání
        // Nový výpočet pro Lua 5.5:
        public const int LUA_REGISTRYINDEX = -1073742823; // Registry index (globální proměnné)
        public const int LUA_MULTRET = -1;       // Návrat více hodnot z Lua funkce
        public const int LUA_RIDX_MAINTHREAD = 1;
        public const int LUA_RIDX_GLOBALS = 2;

        // Konstanty pro lua_gettable() a lua_settable()
        public const int LUA_OPADD = 0; // +
        public const int LUA_OPSUB = 1; // -
        public const int LUA_OPMUL = 2; // *
        public const int LUA_OPMOD = 3; // %
        public const int LUA_OPPOW = 4; // ^
        public const int LUA_OPDIV = 5; // /
        public const int LUA_OPIDIV = 6; // //
        public const int LUA_OPBAND = 7; // &
        public const int LUA_OPBOR = 8; // |
        public const int LUA_OPBXOR = 9; // ~
        public const int LUA_OPSHL = 10; // <<
        public const int LUA_OPSHR = 11; // >>
        public const int LUA_OPUNM = 12; // Unary minus
        public const int LUA_OPBNOT = 13; // Bitový NOT (~)

        // Konstanty pro stav Lua (lua_status())
        public const int LUA_OK = 0;  // Žádná chyba
        public const int LUA_YIELD = 1;  // Coroutine yield
        public const int LUA_ERRRUN = 2;  // Runtime error
        public const int LUA_ERRSYNTAX = 3;  // Syntax error
        public const int LUA_ERRMEM = 4;  // Memory error
        public const int LUA_ERRERR = 5;  // Chyba v chybovém handleru
        public const int LUA_ERRFILE = 6;

        // Konstanty pro lua_pcall()
        public const int LUA_NOREF = -2; // Žádná reference
        public const int LUA_REFNIL = -1; // Nil reference

        // konstanty pro hooky
        public const int LUA_MASKCOUNT = 0x08;


        public static IntPtr NewState()
        {
            return luaL_newstate();
        }

        public static IntPtr NewThread(IntPtr L)
        {
            return lua_newthread(L);
        }

        public static void OpenLibs(IntPtr L)
        {
            // load = -1 (všechny bity zapnuté -> vše se načte do globálů)
            // preload = 0 (nic se neschovává jen do require)
            luaL_openselectedlibs(L,-1,0);   // načte všechno
        }

        public static double ToLuaNumber(IntPtr L, int index)
        {
            return lua_tonumberx(L, index, IntPtr.Zero);
        }
        public static int ToLuaInteger(IntPtr L, int index)
        {
            return (int)lua_tointegerx(L, index, IntPtr.Zero);
        }

        public static bool ToLuaBoolean(IntPtr L, int index)
        {
            return lua_toboolean(L, index);
        }

        public static bool IsLuaString(IntPtr L, int index)
        {
            return lua_type(L, index) == LUA_TSTRING;
        }
        public static bool IsLuaNumber(IntPtr L, int index)
        {
            return lua_type(L, index) == LUA_TNUMBER;
        }

        public static bool IsLuaBoolean(IntPtr L, int index)
        {
            return lua_type(L, index) == LUA_TBOOLEAN;
        }

        public static bool IsLuaUserdata(IntPtr L, int index)
        {
            return lua_type(L, index) == LUA_TUSERDATA;
        }

        public static bool IsLuaInteger(IntPtr L, int index)
        {
            if (IsLuaNumber(L, index))
            {
                double num = ToLuaNumber(L, index);
                return num == Math.Truncate(num);
            }
            return false;
        }

        public static bool IsLuaTable(IntPtr L, int index)
        {
            return lua_type(L, index) == LUA_TTABLE;
        }
        public static bool IsLuaNil(IntPtr L, int index)
        {
            return lua_type(L, index) == LUA_TNIL;
        }

        public static bool IsLuaFunction(IntPtr L, int index)
        {
            return lua_type(L, index) == LUA_TFUNCTION;
        }

        public static void GetField(IntPtr L, int index, string key)
        {
            lua_getfield(L, index, key);
        }

        public static int GetGlobal(IntPtr L, string name)
        {
            return lua_getglobal(L, name);
        }

        public static void SetGlobal(IntPtr L, string name)
        {
            lua_setglobal(L, name);
        }

        public static void PushLuaCClosure(IntPtr L, IntPtr fn, int n)
        {
            lua_pushcclosure(L, fn, n);
        }

        public static void PushLuaNumber(IntPtr L, double number)
        {
            lua_pushnumber(L, number);
        }
        public static void PushLuaInteger(IntPtr L, int value)
        {
            lua_pushinteger(L, value);
        }

        public static void PushLuaString(IntPtr L, string str)
        {
            if (str == null)
            {
                lua_pushnil(L); // případně pro null řetězec poslat Lua nil
                return;
            }

            // Převedeme řetězec na UTF8 bajty
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(str);

            unsafe
            {
                fixed (byte* p = utf8Bytes)
                {
                    // Předáme pointer na bajty a délku řetězce
                    lua_pushlstring(L, (IntPtr)p, (UIntPtr)utf8Bytes.Length);
                }
            }
        }

        public static void PushLuaBoolean(IntPtr L, bool value)
        {
            lua_pushboolean(L, value ? 1 : 0);
        }

        public static void PushLuaNil(IntPtr L)
        {
            lua_pushnil(L);
        }

        public static void PushLuaLightUserdata(IntPtr L, IntPtr p)
        {
            lua_pushlightuserdata(L, p);
        }

        public static void Insert(IntPtr L, int index)
        {
            lua_insert(L, index);
        }


        public static void NewTable(IntPtr L)
        {
            lua_createtable(L, 0, 0);
        }


        public static void SetTable(IntPtr L, int index)
        {
            lua_settable(L, index);
        }


        public static int LoadBuffer(IntPtr L, IntPtr buff, UIntPtr size, string name, string mode)
        {
            // ⭐ Manuální UTF8 encoding
            byte[] nameBytes = Encoding.UTF8.GetBytes(name + '\0');  // Null terminator

            unsafe
            {
                fixed (byte* pName = nameBytes)
                {
                    if (mode != null)
                    {
                        byte[] modeBytes = Encoding.UTF8.GetBytes(mode + '\0');
                        fixed (byte* pMode = modeBytes)
                        {
                            return luaL_loadbufferx(L, buff, size, (IntPtr)pName, (IntPtr)pMode);
                        }
                    }
                    else
                    {
                        return luaL_loadbufferx(L, buff, size, (IntPtr)pName, IntPtr.Zero);
                    }
                }
            }
        }

        public static int luaLoadFile(IntPtr L, string filename, string mode)
        {
            // ⭐ Načti soubor přes C# File API (podporuje UTF8 cesty)
            if (!File.Exists(filename))
            {
                PushLuaString(L, $"cannot open {filename}: No such file or directory");
                return LUA_ERRFILE;
            }

            try
            {
                byte[] fileBytes = File.ReadAllBytes(filename);

                unsafe
                {
                    fixed (byte* pData = fileBytes)
                    {
                        // Chunk name s @ prefixem
                        string chunkName = "@" + filename;
                        return LoadBuffer(L, (IntPtr)pData, (UIntPtr)fileBytes.Length,
                                         chunkName, mode);
                    }
                }
            }
            catch (Exception ex)
            {
                PushLuaString(L, $"cannot open {filename}: {ex.Message}");
                return LUA_ERRFILE;
            }
        }


        public static int PCall(IntPtr L, int nargs, int nresults, int errfunc, int ctx, IntPtr k)
        {
            return lua_pcallk(L, nargs, nresults, errfunc, ctx, k);
        }


        public static IntPtr SetUpValue(IntPtr L, int funcIndex, int n)
        {
            return lua_setupvalue(L, funcIndex, n);
        }

        public static IntPtr GetUpValue(IntPtr L, int funcIndex, int upvalueIndex)
        {
            return lua_getupvalue(L, funcIndex, upvalueIndex);
        }

        public static void PushValue(IntPtr L, int index)
        {
            lua_pushvalue(L, index);
        }

        public static void SetField(IntPtr L, int index, string key)
        {
            if (lua_type(L, index) != 5) // 5 je LUA_TTABLE
            {
                Console.WriteLine($"Chyba: Pokoušíš se nastavit field {key} do něčeho, co není tabulka {index}!");
                return;
            }
            lua_setfield(L, index, key);
        }


        public static int SetMetatable(IntPtr L, int index)
        {
            return lua_setmetatable(L, index);
        }


        public static int UpvalueIndex(int i)
        {
            return LUA_REGISTRYINDEX - i;
        }

        public static int GetTop(IntPtr L)
        {
            return lua_gettop(L);
        }

        // Reads string/number only; does NOT modify Lua stack
        public static string ToLuaString(IntPtr L, int index)
        {

            IntPtr length;
            IntPtr strPtr = lua_tolstring(L, index, out length);
            if (strPtr == IntPtr.Zero) return "";

            //  Správné dekódování UTF-8
            byte[] utf8Bytes = new byte[length.ToInt32()];
            Marshal.Copy(strPtr, utf8Bytes, 0, utf8Bytes.Length);
            return Encoding.UTF8.GetString(utf8Bytes);

        }

        // Converts any Lua value to string using Lua tostring(); modifies stack
        public static string AnyToLuaString(IntPtr L, int index)
        {

            IntPtr strPtr = luaL_tolstring(L, index, IntPtr.Zero);
            if (strPtr == IntPtr.Zero) return "";

            //  Správné dekódování UTF-8
            string result = Marshal.PtrToStringUTF8(strPtr);

            Pop(L, 1); // remove tostring result from stack

            return result;
        }

        public static IntPtr ToLuaUserdata(IntPtr L, int index)
        {
            return lua_touserdata(L, index);
        }


        // Refuje hodnotu na vrcholu stacku a vrátí její referenční ID
        public static int LuaLRef(IntPtr L, int index = -1, int reg = LUA_REGISTRYINDEX)
        {
            if (index > 0)
                PushValue(L, index);
            return luaL_ref(L, reg);
        }

        // Uvolní referenci (zahodí objekt z registry podle ID)
        public static void LuaLUnref(IntPtr L, int reference, int reg = LUA_REGISTRYINDEX)
            => luaL_unref(L, reg, reference);

        public static void TableRawSetI(IntPtr L, int index, long n)
            => lua_rawseti(L, index, n);

        // Pushne hodnotu podle ref ID z registru na stack
        public static void RawGetI(IntPtr L, int n, int reg = LUA_REGISTRYINDEX)
            => lua_rawgeti(L, reg, n);






        public static int LuaType(IntPtr L, int index)
        {
            return lua_type(L, index);
        }

        public static string LuaTypeName(IntPtr L, int index)
        {
            int type = lua_type(L, index);
            IntPtr namePtr = lua_typename(L, type);  // ⭐ type, ne index!
            return Marshal.PtrToStringAnsi(namePtr);
        }



        public static void PushLuaError(IntPtr L, string message)
        {
            // Převedeme řetězec na UTF8 bajty

            luaL_error(L, message);

        }

        public static void CleanStackTo(IntPtr L, int ToTop)
        {
            lua_settop(L, ToTop);
        }

        /// <summary>
        /// Advances a table iterator.  Pops a key from the stack and pushes the next
        /// key-value pair.  Returns non-zero while pairs remain, 0 when done.
        /// Usage: push nil, then loop while Next(L, tableIdx) != 0, pop value each iteration.
        /// </summary>
        public static int Next(IntPtr L, int tableIndex)
        {
            return lua_next(L, tableIndex);
        }

        /// <summary>
        /// Pushes t[key] where t is the table at <paramref name="tableIndex"/> and
        /// key is an integer.  Raw (no metamethods).
        /// </summary>
        public static void TableRawGetI(IntPtr L, int tableIndex, int key)
        {
            lua_rawgeti(L, tableIndex, key);
        }

        public static void LuaRemove(IntPtr L, int idx)
        {
            int top = lua_gettop(L);
            LuaRotate(L, idx, -1);
            CleanStackTo(L, top - 1);
        }

        public static void LuaRotate(IntPtr L, int idx, int n)
        {
            lua_rotate(L, idx, n);
        }

        public static void LuaXMove(IntPtr from, IntPtr to, int n)
        {
            lua_xmove(from, to, n);
        }
        public static void LuaSetHook(IntPtr L, LuaHook hook, int mask, int count)
        {
            lua_sethook(L, hook, mask, count);
        }

        public static void Pop(IntPtr L, int n)
        {
            lua_settop(L, -n - 1);
        }

        public static void LuaClose(IntPtr L)
        {
            lua_close(L);
        }

        public static void LuaYield(IntPtr L, int nresults, IntPtr ctx, IntPtr k)
        {
            lua_yieldk(L, nresults, ctx, k);
        }

        public static void LuaTraceback(IntPtr L, IntPtr L1, string msg, int level)
        {
            luaL_traceback(L, L1, msg, level);
        }

        public static int LuaCallMeta(IntPtr L, int obj, string e)
        {
            return luaL_callmeta(L, obj, e);
        }

        public static int LuaStatus(IntPtr L)
        {
            return lua_status(L);
        }

        public static int LuaError(IntPtr L)
        {
            return lua_error(L);
        }

        public static int LuaRosume(IntPtr L, IntPtr from, int nargs, out int nresults)
        {
            return lua_resume(L, from, nargs, out nresults);
        }
        // Definuj struct odpovídající lua_Debug
        [StructLayout(LayoutKind.Sequential)]
        unsafe struct lua_Debug
        {
            public int @event;

            public IntPtr name;
            public IntPtr namewhat;
            public IntPtr what;
            public IntPtr source;

            public UIntPtr srclen;

            public int currentline;
            public int linedefined;
            public int lastlinedefined;

            public byte nups;
            public byte nparams;
            public byte isvararg;
            public byte istailcall;

            public ushort ftransfer;
            public ushort ntransfer;

            public fixed byte short_src[60];
        }

    }
}

