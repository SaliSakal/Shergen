using Shergen.Lua;
using Shergen.Renderer;
using Silk.NET.Windowing;
using static Shergen.Lua.Engine;

namespace Shergen
{



    public partial class Shergen
    {
        ManualResetEventSlim rendererReady;
        static IRenderer renderer;
        static Engine mainLua;

        static Audio audio;

        private bool _firstFramePending;

        public Shergen(string name = "Shergen Engine", int width = 1280, int height = 800, string shaderPath = "shaders", bool vSync = true, WindowBorder wBorder = WindowBorder.Hidden, WindowState wState = WindowState.Normal)
        {

            // 🔧 Inicializace LuaEnginu
            Console.WriteLine("ℹ️ Initializing... " + name);
            rendererReady = new ManualResetEventSlim(false);
            renderer = new RendererGL(rendererReady, name: name, width: width, height: height, shaderPath : shaderPath, vSync: vSync, wBorder : wBorder, wState : wState);
            audio = new Audio();
            mainLua = new Engine();
            UIElement.Init(mainLua, renderer);
            RegisterGUIFunctionsAConstants();

            // Tick → render thread enqueues; Lua thread dequeues and fires
            renderer.OnFrameTick = dt => renderer.IEventQueue.Enqueue(() => FireTick(dt));
            // Resize → stejný pattern
            renderer.OnResized   = (w, h) => renderer.IEventQueue.Enqueue(() => FireResize(w, h));
        }

        public void Run(string firstLuaFile = "init", string luaFolder = null)
        {

            var luaThread = new Thread(() =>
            {
                rendererReady.Wait(); // čeká než renderer řekne "jsem ready"
                UIElement.InitializeDesktop(renderer.screenWidth, renderer.screenHeight);

                mainLua.Init(firstLuaFile, luaFolder);
                renderer.SetLuaReady();
                _firstFramePending = true;

                // ── Main event loop ───────────────────────────────────────────────
                // Drains input events posted by the render thread and runs Lua coroutine
                // slices (e.g. event callbacks launched via CallLuaFunction).
                while (renderer.IsRunning)
                {
                    if (_firstFramePending)
                    {
                        _firstFramePending = false;
                        mainLua.CallLuaFunction("FirstFrame()",false);
                    }
                    var tLua = Profiler.Ts();

                    while (renderer.IEventQueue.TryDequeue(out var ev))
                        ev();

                    while (Audio.Instance.FinishedCallbacks.TryDequeue(out var finished))
                    {
                        if (finished.cb.FuncRef > 0)
                            mainLua.CallLuaFunction(finished.cb.FuncRef, sliced: true, finished.cb.ExtraArgs ?? Array.Empty<object>());
                        else if (finished.cb.FuncStr != null)
                            mainLua.CallLuaFunction(finished.cb.FuncStr, sliced: true);
                    }

                    Profiler.Add("Queue", Profiler.Ms(tLua));

                    var tLuaEx = Profiler.Ts();
                    mainLua.RunExecTick();

                    Profiler.Add("Slacer", Profiler.Ms(tLuaEx));
                    Profiler.Add("Lua Loop", Profiler.Ms(tLua));

                    Thread.Sleep(1);
                }
            });

            audio.Init();

            luaThread.Start();

            Console.WriteLine("🖥️ Running Renderer...!");
            try
            {
                renderer.Run(); // hlavní vlákno, blokující — GLFW je spokojený
            }
            catch (Exception ex)
            {
                // Došlo k chybě v Rendereru
                Console.WriteLine($"❌ An error occurred in the Renderer: {ex.Message}");
            }

        }

        public void RegisterLuaFunction(string name, LuaFunctionDelegate function)
            => mainLua.RegisterNewFunction(name, function);

        public void RegisterLuaConstant(string name, object value)
            => mainLua.RegisterNewConstant(name, value);

        public void RegisterLuaFTable(string name, IEnumerable<LuaTableEntry> entries)
            => mainLua.RegisterTable(name, entries);

        public void ToLua(string script) => mainLua.ToLua(script);

        public void FileToLua(string path) => mainLua.FileToLua(path);

        public void ApplyLuaSandbox() => mainLua.SandboxLua();

        public void SetLoadingTexture(string path) => renderer.LoadingTexturePath = path;


        // ───────────── Modules ───────────────────────────────────────────────────────

        public void LoadModule(IShergenModule module) => module.Register(this);
    }
}