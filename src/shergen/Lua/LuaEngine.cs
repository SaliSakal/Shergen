using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static Shergen.Lua.Native;
using static Shergen.Utils;

namespace Shergen.Lua
{

    public abstract class LuaTableEntry { }

    public sealed class LuaFunctionEntry : LuaTableEntry
    {
        public string Name;
        public LuaFunctionDelegate Function;
    }

    public sealed class LuaValueEntry : LuaTableEntry
    {
        public string Name;
        public object Value;
    }

    public partial class Engine : IDisposable
    {
        public string AppDataPath { get; private set; } = ""; // Výchozí: stejný adresář jako exe // Default: same directory as exe

        Native _lua;

        public IntPtr MainState { get; private set; }

        private GCHandle _handle;
        public IntPtr HandlePtr => GCHandle.ToIntPtr(_handle);

        public RegistryCallbacks EngineDelegates { get; } = new RegistryCallbacks();

        public Engine()
        {
            _lua = new Native();
            MainState = _lua.MainState;

            // základní funkce
            // basic functions
            RegBaseFunctions();

            // vytvoř handle na TUHLE instanci
            // create handle to THIS instance
            _handle = GCHandle.Alloc(this, GCHandleType.Normal);


            // uložit do registry
            // save to registry
            PushLuaLightUserdata(MainState, HandlePtr);
            SetField(MainState, LUA_REGISTRYINDEX, "__instance");

            // Registrace hooku pro slicer
            // Register hook for slicer
            hookDelegate = new LuaHook(HookCallback);
        }

        public static bool IsLoaded = false;

        public void Init(string baseFile = "init", string customPath = null)
        {
            AppDataPath = ""; // Výchozí cesta pro LUA skripty // Default path for LUA scripts";
            if (!string.IsNullOrEmpty(customPath))
            {
                AppDataPath = customPath.TrimEnd('/', '\\') + "/"; // Přidání lomítka na konec / Add a slash at the end
            }
            string firstLuaFile = "init";
            if (!string.IsNullOrEmpty(baseFile))
            {
                firstLuaFile = baseFile.TrimEnd('/', '\\') + ""; // Přidání lomítka na konec / Add a slash at the end
            }

            // Načítám LUA skripty 🟡
            Console.WriteLine("🌙 Loading LUA scripts...");

            string relativePath = FindFileCaseInsensitive(AppDataPath + firstLuaFile + ".lua");

            if (!File.Exists(relativePath))
            {
                Console.WriteLine("❌ Error: Init file " + relativePath + " doesn't exist!");
            }
            else
            {

                FileToLua(relativePath);
                Console.WriteLine("✅ LUA loaded!");
            }
            IsLoaded = true;

        }

        public void ToLua(string script)
        {
            _lua.RunString(script);
        }

        public void FileToLua(string script, int envRef)
        {
            _lua.RunScript(script, envRef);
        }

        public void FileToLua(string script)
        {
            _lua.RunScript(script);
        }


        // Výpis do konzole z Lua, podpora všech základních formátů
        // Print to console from Lua, support for all basic formats
        private static int SafePrint(IntPtr L)
        {
            int nargs = GetTop(L);
            var message = new StringBuilder();

            for (int i = 1; i <= nargs; i++)
            {
                string s = AnyToLuaString(L, i);

                if (i > 1)
                    message.Append(' ');

                message.Append(s);

            }

            Console.WriteLine("[LUA] " + message.ToString());
            return 0;
        }







        private static int IncludeLua(IntPtr L)
        {
            int nargs = GetTop(L); // Počet argumentů // Number of arguments

            if (nargs == 0)
                return 0;

            int envRef = -1;
            int endIndex = nargs;

            if (IsLuaTable(L, nargs))
            {
                envRef = LuaLRef(L);
                endIndex = endIndex - 1;
            }

            string[] scripts = new string[endIndex];
            for (int i = 1; i <= endIndex; i++)
            {
                scripts[i - 1] = ToLuaString(L, i);

            }
            Engine instance = GetInstanceFromLua(L);
            for (int i = 0; i < endIndex; i++)
            {
                string scriptFile = instance.AppDataPath + scripts[i] + ".lua"; // Přidáme .lua na konec / Add .lua at the end


                try
                {

                    scriptFile = FindFileCaseInsensitive(scriptFile);
                    instance.FileToLua(scriptFile, envRef);

                }
                catch (Exception ex)
                {
                    // Chyba při načítání
                    // Error loading
                    Console.WriteLine($"❌ LoadFile error {scriptFile}: {ex.Message}");
                }

            }

            if (envRef != -1)
            {
                LuaLUnref(L, envRef);
            }


            return 0;
        }



        private static int TryIncludeLua(IntPtr L)
        {

            int nargs = GetTop(L); // Počet argumentů // Number of arguments
            if (nargs == 0)
                return 0;

            int envRef = -1;
            int endIndex = nargs;

            if (IsLuaTable(L, nargs))
            {
                envRef = LuaLRef(L);
                endIndex = endIndex - 1;
            }


            string[] scripts = new string[endIndex];
            for (int i = 1; i <= endIndex; i++)
            {
                scripts[i - 1] = ToLuaString(L, i);

            }
            Engine instance = GetInstanceFromLua(L);
            for (int i = 0; i < endIndex; i++)
            {
                string scriptFile = instance.AppDataPath + scripts[i] + ".lua"; // Přidáme .lua na konec / Add .lua at the end
                try
                {
                    scriptFile = FindFileCaseInsensitive(scriptFile);

                    if (File.Exists(scriptFile))


                        instance.FileToLua(scriptFile, envRef);
                }
                catch (Exception ex)
                {
                    // Chyba při načítání
                    // Error loading
                    Console.WriteLine($"❌ LoadFile error: {scriptFile}: {ex.Message}");
                }

            }

            if (envRef != -1)
            {
                LuaLUnref(L, envRef);
            }

            return 0;
        }

        public static int ImportModule(IntPtr L)
        {
            string path = ToLuaString(L, 1);
            Engine instance = GetInstanceFromLua(L);
            IntPtr MainState = instance.MainState;
            string filePath = instance.AppDataPath + path + ".lua";
            try
            {
                filePath = FindFileCaseInsensitive(filePath);
            }
            catch (Exception ex)
            {
                // Chyba při načítání
                // Error loading
                Console.WriteLine($"❌ LoadFile error: {filePath}: {ex.Message}");
                PushLuaNil(L);
                return 1;
            }

            if (!FileManager.FileExists(filePath))
            {
                Console.WriteLine("❌ import: File not found: " + filePath);
                PushLuaNil(L);
                return 1;
            }

            int envRef = 0;
            if (GetTop(L) == 2)
            {
                if (IsLuaTable(L, 2))
                {
                    envRef = LuaLRef(L);
                }
                else
                {
                    envRef = -1;
                }

            }

            try
            {

                // 📦 Vytvoříme nové prostředí (env = {})
                // 📦 Create a new environment (env = {})
                NewTable(MainState); // env je teď na stacku nahoře // env is now on the top of the stack
                int envIndex = GetTop(MainState);

                NewTable(MainState);                // metatable
                bool setGlobals = true;
                if (envRef == 0)
                {
                    GetGlobal(MainState, "_G"); // _G jako fallback / _G as fallback
                    SetField(MainState, -2, "__index");           // mt.__index = env

                    PushLuaBoolean(MainState, false);
                    SetField(MainState, -2, "__metatable");       // mt.__metatable = false

                    SetMetatable(MainState, envIndex);            // setmetatable(env, mt)
                    setGlobals = false;
                }
                else if (envRef > 0)
                {
                    RawGetI(MainState, envRef); // z registru / get from registry
                    SetField(MainState, -2, "__index");           // mt.__index = env

                    SetField(MainState, -2, "__metatable");       // mt.__metatable = false
                    SetMetatable(MainState, envIndex);            // setmetatable(env, mt)
                }

                instance.SetupEnvApi(envIndex, setGlobals);

                // 🧠 Nahrajeme chunk s prostředím
                // 🧠 Load the chunk with the environment
                int loadResult = luaLoadFile(MainState, filePath, "bt");
                if (loadResult != 0)
                {
                    PrintLuaError(L, "❌ Lua LoadFile error", -1);
                    PushLuaNil(MainState);
                    goto EndImport;
                }

                // 🌍 Nastavíme prostředí přes 5.4 způsob (set _ENV jako první upvalue)
                // 🌍 Set the environment using Lua 5.4 method (set _ENV as first upvalue)
                PushValue(MainState, envIndex);           // zkopírujeme env // copy env
                SetUpValue(MainState, -2, 1);             // nastavíme jako první upvalue (_ENV) // set as first upvalue (_ENV)

                // ▶️ Spustíme chunk
                // ▶️ Run the chunk
                Console.WriteLine("📄 Importing lua modul " + filePath);

                int callResult = Docall(MainState, 0, 1);
                if (callResult != 0)
                {
                    PrintLuaError(L, "❌ Lua import module runtime error", -1);
                    PushLuaNil(L);
                    goto EndImport;

                }
                // 📤 Vrátíme prostředí jako výsledek
                // 📤 Return the environment as the result
                PushValue(L, envIndex);


            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Exception in LuaImportModule: " + ex.Message);
                PushLuaNil(L);
            }

            EndImport:

            if (envRef > 0)
            {
                LuaLUnref(MainState, envRef);
            }
            return 1;
        }

        readonly string[] BasicGlobals =
        {
            // Basic functions
            "assert", "error", "warn",
            "type", "tostring", "tonumber",
            "next", "pairs", "ipairs", "select",

            // Function calling
            "pcall",
            "xpcall",

            // Metatable
            "getmetatable", "setmetatable",
            "rawequal", "rawget", "rawlen", "rawset",

            // Libraries
            "math", "string", "table", "utf8", "coroutine",

            // Dangerous (pro sandbox vynulovat)
            "debug", "io", "os", "package",
            "dofile", "load", "loadfile", "loadstring",
            "collectgarbage", "require",

            // Version info
            "_VERSION",

            // NEEXISTUJÍ v Lua 5.4 (deprecated)
            "getfenv", "setfenv", "newproxy", "getenv",

            // Lua Instance specific
            "include",
            "tryInclude",
            "import",
            "print",

           };

        private List<string> customGlobals = new List<string>();

        /// <summary>
        /// Registers global variable for all environments, created in the future. 
        /// This method should be called once during application startup. Before call Init();
        /// </summary>
        /// <remarks>The registered global persist across RestarLua and will be available in any environment.
        /// initialization.</remarks>
        /// <param name="name">A global variable name to register for all future environments.</param>
        public void RegisterGlobalForENV(string name)
        {
            customGlobals.Add(name);
        }
        // RegisterGloballsForENV registruje globály pro všechny Enviromenty (budoucích)
        // Pamatuje si je i po restartu, takže je voláme jen jednou při startu
        // RegisterGlobals musí být před Init
        /// <summary>
        /// Registers global variables for all environments, created in the future. 
        /// This method should be called once during application startup. Before call Init();
        /// </summary>
        /// <remarks>The registered globals persist across RestarLua and will be available in any environment.
        /// initialization.</remarks>
        /// <param name="names">An array of global variable names to register for all future environments.</param>
        public void RegisterGlobalsForENV(params string[] names)
        {
            customGlobals.AddRange(names);
        }

        void SetupEnvApi(int envIndex, bool setGlobals = true)
        {
            if (setGlobals)
            {
                foreach (var name in BasicGlobals)
                {
                    GetGlobal(MainState, name);
                    SetField(MainState, envIndex, name);
                }

                // ⭐ App-specific funkce
                foreach (var name in customGlobals)
                {
                    GetGlobal(MainState, name);
                    SetField(MainState, envIndex, name);
                }
            }

            const string bootstrap = @" 
      local native_include    = include
      local native_tryInclude = tryInclude
      local native_import     = import

      function include(...)
        return native_include(..., _ENV)
      end

      function tryInclude(...)
        return native_tryInclude(..., _ENV)
      end

      function import(path, base)
        if base == nil then
          return native_import(path, _ENV)
        else
          return native_import(path, base)
        end
      end
    ";

            byte[] bytes = Encoding.UTF8.GetBytes(bootstrap);

            unsafe
            {
                fixed (byte* p = bytes)
                {
                    LoadBuffer(_lua.MainState, (IntPtr)p, (UIntPtr)bytes.Length, "env_api", "bt");
                    // nastavit _ENV upvalue na tenhle env
                    // Set the _ENV upvalue to this env
                    PushValue(_lua.MainState, envIndex);
                    SetUpValue(_lua.MainState, -2, 1);
                    Docall(_lua.MainState, 0, 0);
                }
            }
        }

        internal static Engine GetInstanceFromLua(IntPtr L)
        {
            GetField(L, LUA_REGISTRYINDEX, "__instance");
            IntPtr handlePtr = ToLuaUserdata(L, -1);
            Pop(L, 1);

            if (handlePtr == IntPtr.Zero)
                throw new Exception("Lua Instance not found in registry");

            GCHandle handle = GCHandle.FromIntPtr(handlePtr);
            return (Engine)handle.Target;
        }

        public void RegisterNewFunction(string name, LuaFunctionDelegate function)
        {
            // Použijeme instanci _lua a zaregistrujeme callback přes centrální metodu
            // Use the _lua instance and register a callback through the central method
            EngineDelegates.RegisterCallback(_lua, name, function);
        }

        public void RegisterNewConstant(string name, object value)
        {
            // Použijeme instanci _lua a zaregistrujeme novou konstantu přes centrální metodu
            // Use the _lua instance and register a new constant through the central method
            EngineDelegates.RegisterGlobalConstant(_lua, name, value);
        }

     


        public void RegisterTable(string tableName, IEnumerable<LuaTableEntry> entries)
        {
            // vytvoříme tabulku
            // create table
            NewTable(MainState);

            foreach (var entry in entries)
            {
                switch (entry)
                {
                    case LuaFunctionEntry fn:
                        // funkce MUSÍ přes delegate registry
                        // functions MUST go through delegate registry
                        RegisterNewFunction($"__{tableName}_{fn.Name}", fn.Function);

                        GetGlobal(MainState, $"__{tableName}_{fn.Name}");
                        SetField(MainState, -2, fn.Name);
                        break;

                    case LuaValueEntry val:
                        PushLuaValue(MainState, val.Value);
                        SetField(MainState, -2, val.Name);
                        break;
                }
            }

            // xlsx = { ... }
            SetGlobal(MainState, tableName);
        }

        // NIKDY NEVOLEJ V BĚHU LUA, po restartu je třeba znovu zaregistrovat všechny funkce a inicializovat první soubor lua!
        // NEVER CALL IT DURING LUA EXECUTION, after reset all functions must be re-registered and the first lua file initialized!
        public int ResetLua()
        {
            Console.WriteLine("🔄 Resetting CLua...");
            try
            {
                _lua.Dispose();               // přímo pole
                EngineDelegates.clearRegister();
                executions.Clear();

                _lua = new Native();
                MainState = _lua.MainState;   // přímo

                _handle = GCHandle.Alloc(this, GCHandleType.Normal);
                RegBaseFunctions();
                PushLuaLightUserdata(MainState, HandlePtr);
                SetField(MainState, LUA_REGISTRYINDEX, "__instance");

                Console.WriteLine("✅ LUA Engine has been successfully reset!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error resetting LUA Engine: {ex.Message}");
            }
            return 1;
        }

        public void Dispose()
        {
            _lua.Dispose();
            if (_handle.IsAllocated)
                _handle.Free();
            GC.SuppressFinalize(this);
        }


        class ExecutionContext
        {
            public int Id;
            public IntPtr Thread;          // lua_State* coroutine // lua_State* coroutine
            public int ThreadRef;          // Lua registry ref — keeps coroutine alive until done
            public bool CancelRequested;
            public bool Finished;
            public int NumArgs;
        }

        readonly Stopwatch sliceTimer = new Stopwatch();
        int nextExecId = 1;
        readonly Dictionary<int, ExecutionContext> executions = new();
        ExecutionContext? currentExec;


        public void CallLuaFunction(string script, bool sliced = true)
        {
            if (string.IsNullOrEmpty(script))
                return;

            // Převod řetězce na UTF8 bajty
            // Console.WriteLine($"[DEBUG] Odesílám do Lua: {script}");
            byte[] scriptBytes = Encoding.UTF8.GetBytes(script);
            int loadResult = 0;

            unsafe
            {
                fixed (byte* pScript = scriptBytes)
                {
                    // Předáváme pointer, délku a chunk name
                    // Passing pointer, length and chunk name
                    loadResult = LoadBuffer(MainState, (IntPtr)pScript, (UIntPtr)scriptBytes.Length, new string(script.Take(60).ToArray()), null);
                }
            }

            if (loadResult != 0)
            {
                // Chyba při načítání LUA kódu
                // Error loading LUA code
                PrintLuaError(MainState, "❌ Error loading LUA code", -1);
                return;
            }

            RawGetI(MainState, LUA_RIDX_GLOBALS);
            // stack: [ func, _G ]
            SetUpValue(MainState, -2, 1); // func._ENV = _G

            // Vytvoření nového vlákna (coroutine) pro spuštění kódu
            // Creating a new thread (coroutine) for code execution
            IntPtr thread = NewThread(MainState);

            // stack: [ func, thread ]
            LuaRotate(MainState, -2, 1);
            // stack: [ thread, func ]

            // přesun funkce do coroutine
            // moving function to coroutine
            LuaXMove(MainState, thread, 1);

            // Uloží thread do registry (popne ho ze stacku) → GC ho nesebere
            int threadRef = LuaLRef(MainState);
            // slicer hook
            if (sliced)
                LuaSetHook(thread, HookCallback, LUA_MASKCOUNT, 1000);

            var exec = new ExecutionContext
            {
                Id = nextExecId++,
                Thread = thread,
                ThreadRef = threadRef,
                CancelRequested = false,
                Finished = false
            };

            executions.Add(exec.Id, exec);
        }


        // Nová metoda pro volání Lua funkce přes referenci
        // New method to call Lua function via reference
        public void CallLuaFunction(int funcRef, bool sliced = true, params object[] args)
        {
            IntPtr L = MainState;
            // ⚡ Vytvoř thread pro sliced execution
            // ⚡ Create thread for sliced execution

            IntPtr thread = NewThread(L);


            // Načti funkci z registry
            // Load function from registry
            RawGetI(L, funcRef);

            if (!IsLuaFunction(L, -1))
            {
                Console.WriteLine($"⚠️ CRITICAL: Invalid function reference {funcRef}! Possible double-unref or corruption.");
                Pop(L, 2);
                return;
            }


            // Push argumenty (pokud jsou)
            // Push arguments (if any)
            foreach (var arg in args)
                PushLuaValue(L, arg);
            //DumpStack(L, "AFTER pushing args");


            // stack: [func, args..., thread]

            // Přesuň funkce + args do threadu
            // Move function + args to thread
            int nargs = args.Length;
            LuaXMove(L, thread, nargs + 1); // +1 pro funkci // +1 for function
                                            // stack thread: [func, arg1, arg2, ...]
                                            // stack MainState: [thread]  ← thread zůstal


            // Uloží thread do registry (popne ho ze stacku) → GC ho nesebere
            int threadRef = LuaLRef(L);

            // Nastav slicing hook
            // Set slicing hook
            if (sliced)
                LuaSetHook(thread, HookCallback, LUA_MASKCOUNT, 1000);

            var exec = new ExecutionContext
            {
                Id = nextExecId++,
                Thread = thread,
                ThreadRef = threadRef,
                CancelRequested = false,
                Finished = false,
                NumArgs = nargs
            };

            executions.Add(exec.Id, exec);
        }


        void RunSlice(ExecutionContext exec)
        {
            if (exec.Finished)
                return;

            int st = LuaStatus(exec.Thread);

            // NIKDY neresumuj mrtvou coroutine
            // NEVER resume a dead coroutine
            if (st != LUA_OK && st != LUA_YIELD)
            {
                exec.Finished = true;
                return;

            }

            currentExec = exec;
            sliceTimer.Restart();
            //Console.WriteLine($"LuaRun ID {exec.Id}");
            try
            {
                int nresults;
                //DumpStack(exec.Thread);
                int nargs = (st == LUA_OK) ? exec.NumArgs : 0;  // První volání vs. po yield
                int status = LuaRosume(exec.Thread, IntPtr.Zero, nargs, out nresults);


                if (status == LUA_OK)
                {
                    exec.Finished = true;
                }
                else if ((status == LUA_YIELD))
                {
                    // pokračujeme příště
                    // Klasický LUA spadne do hadleru
                }
                else
                {
                    // LUA_ERRMEM, ...
                    exec.Finished = true;

                    // 1) přečti error
                    string msg = ToLuaString(exec.Thread, -1);
                    if (msg == "")
                    {
                        if (LuaCallMeta(exec.Thread, -1, "__tostring") != 0 && LuaType(exec.Thread, -1) == LUA_TSTRING)
                        {
                            // __tostring výsledek je na stacku → přečti ho

                            msg = ToLuaString(exec.Thread, -1); //sPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(sPtr) : null;
                        }
                        else
                        {
                            msg = $"(error object is a {LuaTypeName(exec.Thread, -1)} value)";

                        }
                    }
                    // 2) vyrob traceback
                    LuaTraceback(exec.Thread, exec.Thread, msg, 1);

                    // 3) přečti traceback
                    string tbPtr = ToLuaString(exec.Thread, -1);
                    if (tbPtr != "")
                    {
                        Console.WriteLine("❌ Error when running LUA script: " + tbPtr);
                    }

                }

            }
            finally
            {
                currentExec = null;
            }
        }




        public void RunExecTick()
        {
            var toRemove = new List<int>();
            foreach (var exec in executions.Values)
            {
                if (!exec.Finished)
                {
                    RunSlice(exec);
                }

                // ⭐ Pokud právě dokončil, označ ho
                if (exec.Finished)
                {
                    toRemove.Add(exec.Id);
                }
            }

            foreach (var id in toRemove)
            {
                if (executions.TryGetValue(id, out var finished) && finished.ThreadRef != 0)
                    LuaLUnref(MainState, finished.ThreadRef);
                executions.Remove(id);
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void HookLua(IntPtr L, IntPtr ar);

        static LuaHook hookDelegate;

        static void HookCallback(IntPtr L, IntPtr ar)
        {
            var instance = GetInstanceFromLua(L);
            // Cancel má přednost
            if (instance.currentExec.CancelRequested)
            {
                PushLuaError(L, "Execution cancelled");
                instance.currentExec.CancelRequested = false;
                return;
            }

            // Time slicing
            if (instance.sliceTimer.ElapsedMilliseconds > 10)
            {
                //Console.WriteLine($"Lua Hook called {currentExec.Id}");
                instance.sliceTimer.Restart();
                LuaYield(L, 0, IntPtr.Zero, IntPtr.Zero);
                return; // formální, ale důležité pro čitelnost
            }
        }

        static public int GetCurrentExecID(IntPtr L)
        {
            var instance = GetInstanceFromLua(L);
            PushLuaInteger(L, instance.currentExec.Id);
            return 1;
        }

        static public int StopExec(IntPtr L)
        {
            var instance = GetInstanceFromLua(L);
            int id = ToLuaInteger(L, 1);
            if (instance.executions.TryGetValue(id, out var exec))
                exec.CancelRequested = true;
            return 0;
        }
    }
}