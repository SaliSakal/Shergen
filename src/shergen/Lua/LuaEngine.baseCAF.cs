namespace Shergen.Lua
{
    public partial class Engine
    {
        public void SandboxLua()
        {
            // Bezpečnostní sandbox pro LUA!

            ToLua("os = {clock = os.clock, date = os.date, difftime = os.difftime, time = os.time };");
            ToLua("debug = { traceback = debug.traceback };");
            ToLua("io = nil");
            ToLua("package = nil");
            ToLua("require = nil");

            ToLua("loadfile = nil");
            ToLua("dofile = nil");
            ToLua("loadstring = nil");
            ToLua("load = nil");
            ToLua("getmetatable = nil");
            ToLua("collectgarbage = nil");
            ToLua("setfenv = nil");
            ToLua("getenv = nil");
            ToLua("newproxy = nil");

            ToLua("setmetatable(_G, { __metatable = false })");
            ToLua("setmetatable(coroutine, { __metatable = false })");
            ToLua("setmetatable(string, { __metatable = false })");
            ToLua("setmetatable(table, { __metatable = false })");
            ToLua("setmetatable(math, { __metatable = false })");
            ToLua("setmetatable(utf8, { __metatable = false })");

            Console.WriteLine("🔒 Secure sandbox for LUA activated!");
        }

        public void RegBaseFunctions()
        {
            //  Registrace základních funkcí...
            //  Register base functions...

            RegisterNewFunction("print", SafePrint);
            RegisterNewFunction("include", IncludeLua);
            RegisterNewFunction("tryInclude", TryIncludeLua);
            RegisterNewFunction("import", ImportModule);
            RegisterNewFunction("getCurrentRunID", GetCurrentExecID);
            RegisterNewFunction("stopRunByID", StopExec);
            ToLua("function FirstFrame()  end;");


        }

    }
}
