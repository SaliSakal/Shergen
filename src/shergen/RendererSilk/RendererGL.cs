using Silk.NET.GLFW;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;


namespace Shergen.Renderer
{

    public partial class RendererGL : IRenderer
    {
 
        public void ApplyScissor(int x, int y, int w, int h) => _scissorStack.ApplyScissor(x, y, w, h);
        public void RemoveScissor() => _scissorStack.RemoveScissor();

        public bool IsRunning => isRunning; // deleguje na stávající statickou
        public ManualResetEventSlim RendererReady => _rendererReady;

        public Action<int, int> OnResized  { get; set; }
        public Action<double>   OnFrameTick { get; set; }


        private IWindow window;
        private GL gl;

        private ScissorStack _scissorStack = null!; // inicializováno v InitGL po vytvoření gl

        private ConcurrentQueue<Action> CommandQueue = new();

        ReaderWriterLockSlim treeLock = new ReaderWriterLockSlim();
        private readonly ManualResetEventSlim _rendererReady;

        public int screenWidth { get; set; }
        public int screenHeight { get; set; }

        public static bool isRunning { get; private set; } = false;

        public static string shadersPath = "shaders/";

        private uint vao, vbo, ebo;
        private ShaderGL shader;

        // ── Draw call caches ──────────────────────────────────────────────────────
        private int _lastBoundTextureId = -1;  // -1 = none bound yet

        private uint textVao, textVbo, _textEbo;
        private ShaderGL textShader;

        private uint stencilVao, stencilVbo, _stencilEbo;
        private ShaderGL stencilShader;
        //public SDFFont sdfFont;

        // ── Text batching ─────────────────────────────────────────────────────────
        private const int _maxTextChars = 4096;
        private readonly float[] _textVerts = new float[_maxTextChars * 20]; // 4 verts × 5 floats



        // ── Font cache (DFF and future TTF fonts share one dict via IFont) ──────────
        private Dictionary<string, IFont> _fonts = new();
        private Dictionary<string, IFont> _fontsMetrics = new();

        /// <summary>Default DFF font name used when UILabel.Font is empty.</summary>
        public string DefaultDFFFont { get; set; } = "Tahoma_70_Fixed_16_2.DFF";

        /// <summary>Default TTF font name used when UILabel.Font is empty (future).</summary>
        public string DefaultTTFFont { get; set; } = "";

        /// <summary>Pending fallback chars to apply when a font is first loaded.</summary>
        private static readonly Dictionary<string, char> _pendingFallbacks = new();

        public float LastDeltaTime { get; set; } = 0;
        public RendererGL(ManualResetEventSlim rendererReady, string name = "Shergen Engine", int width = 1280, int height = 800, string shaderPath = "shaders", bool vSync = true, WindowBorder wBorder = WindowBorder.Hidden, WindowState wState = WindowState.Normal)
        {
            _rendererReady = rendererReady;

            var options = WindowOptions.Default;
            options.Title = name;
            options.Size = new Vector2D<int>(width, height);
            options.Position = new Vector2D<int>(0, 0);
            options.WindowBorder = wBorder;
            options.WindowState = wState;
            options.FramesPerSecond = 0;   // 0 = unlimited
            options.UpdatesPerSecond = 0;  // 0 = unlimited
            options.VSync = vSync;
            options.PreferredStencilBufferBits = 8;
            window = Window.Create(options);

            window.Load += OnLoad;

            window.Render += OnRender;
            window.Resize += OnResize;
            window.Update += OnUpdate;

            if (!string.IsNullOrEmpty(shaderPath))
            {
                shadersPath = shaderPath.TrimEnd('/', '\\') + "/";
            }

            window.Closing += OnClose;
        }

        public void Run()
        {
            window.Run();
        }

        private void OnLoad()
        {
            InitGL();
            

            screenWidth = (int)window.Size.X;
            screenHeight = (int)window.Size.Y;

            gl.Viewport(0, 0, (uint)window.Size.X, (uint)window.Size.Y);


            Console.WriteLine($"🖥️ Window initialized: {screenWidth}x{screenHeight}");

            _glTimerQuery = gl.GenQuery();
            _lastCpuTime = Process.GetCurrentProcess().TotalProcessorTime;

            InitInput();
            InitTouch();
            Shergen.UpdateDesktopSize(window.Size.X, window.Size.Y);

            if (LoadingTexturePath != null)
                SetLoadingTexture(LoadingTexturePath);

            isRunning = true;
            _rendererReady.Set();
        }

        DateTime _lastTime;
        uint _framesRendered;
        private TimeSpan _lastCpuTime;
        private uint _glTimerQuery;
        private bool _gpuQueryPending;
        private double _lastGpuMs;

        private long tGl;
        private void OnUpdate(double deltaTime)
        {
            tGl = Profiler.Ts();
            ProcessPendingUploads();

            var tComm = Profiler.Ts();
            lock (CommandQueue)
            {
                while (CommandQueue.TryDequeue(out var action))
                    action();
            }
            Profiler.Add("CommQueue", Profiler.Ms(tComm));

            if (isRunning)
            {
                LastDeltaTime = (float)deltaTime;
                if (_justBecameReady)
                {
                    LastDeltaTime = 0;
                    _justBecameReady = false;
                }

                var tInput = Profiler.Ts();
                ProcessInput();
                Profiler.Add("Input", Profiler.Ms(tInput));

                if (_luaReady)
                    OnFrameTick?.Invoke(LastDeltaTime);
            }


            _framesRendered++;

            if ((DateTime.Now - _lastTime).TotalSeconds >= 1)
            {
                var proc = Process.GetCurrentProcess();
                long ramMB = proc.WorkingSet64 / 1024 / 1024;

                TimeSpan cpuNow = proc.TotalProcessorTime;
                double cpuMs = (cpuNow - _lastCpuTime).TotalMilliseconds;
                int cpuPct = (int)(cpuMs / (Environment.ProcessorCount * 1000.0) * 100.0);
                _lastCpuTime = cpuNow;

                double framems = _framesRendered > 0 ? 1000.0 / _framesRendered : 0.0;

                UIElement.FPSCounter.Text = $"FPS: {_framesRendered}  {framems:F1}ms" +
                                            $"  RAM: {ramMB}MB  CPU: {cpuPct}%  GPU: {_lastGpuMs:F1}ms";

                Profiler.Flush((int)_framesRendered);

                if (UIElement.ProfilerLabel != null && Profiler.Visible)
                {
                    UIElement.ProfilerLabel.SetVisible(true);
                    var sb = new System.Text.StringBuilder();
                    foreach (var (name, parent, value, max, hexColor) in Profiler.GetSections())
                    {
                        string indent = parent == null ? "" : parent == "Rendering" ? "    " : "  ";
                        sb.AppendLine($"<c={hexColor}>{indent}{name}\n{indent}{value:F2}ms (Max: {max:F2}ms)</c>");
                    }
                    UIElement.ProfilerLabel.Text = sb.ToString();
                }
                else if (UIElement.ProfilerLabel != null)
                {
                    UIElement.ProfilerLabel.SetVisible(false);
                }

                _framesRendered = 0;
                _lastTime = DateTime.Now;
            }
        }

        private void OnClose()
        {
            CleanupTouch();
            isRunning = false;
        }

        public void Exit()
        {
            isRunning = false;
            window.Close();
        }

        private void OnResize(Vector2D<int> size)
        {
            var mon = window.Monitor;
            Console.WriteLine($"[RESIZE] size={size} monitor.Bounds={mon?.Bounds} monitor.VideoMode={mon?.VideoMode.Resolution}");

            gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);

            Matrix4x4 ortho = Matrix4x4.CreateOrthographicOffCenter(0, size.X, size.Y, 0, -1, 1);
            shader.Use();
            shader.SetMatrix4("projection", ortho);

            textShader.Use();
            textShader.SetMatrix4("projection", ortho);

            stencilShader.Use();
            stencilShader.SetMatrix4("projection", ortho);

            screenWidth = size.X;
            screenHeight = size.Y;


            Shergen.UpdateDesktopSize(size.X, size.Y);
            OnResized?.Invoke(size.X, size.Y);
        }

        private void InitGL()
        {

            gl = window.CreateOpenGL();
            gl.Enable(EnableCap.StencilTest);
            _scissorStack = new ScissorStack(gl);

            shader = new ShaderGL(gl, shadersPath + "base.vglsl", shadersPath + "base.fglsl");

            textShader = new ShaderGL(gl, shadersPath + "font.vglsl", shadersPath + "font.fglsl");



            float[] vertices = {
                0.0f, 0.0f,
                1.0f, 0.0f,
                1.0f, 1.0f,
                0.0f, 1.0f
            };

            uint[] indices = {
                0u, 1u, 2u,
                2u, 3u, 0u
            };

            vao = gl.GenVertexArray();
            vbo = gl.GenBuffer();
            ebo = gl.GenBuffer();

            gl.BindVertexArray(vao);

            gl.BindBuffer(GLEnum.ArrayBuffer, vbo);
            gl.BufferData(GLEnum.ArrayBuffer, (nuint)vertices.Length * sizeof(float), vertices, GLEnum.StaticDraw);

            gl.BindBuffer(GLEnum.ElementArrayBuffer, ebo);
            gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)indices.Length * sizeof(float), indices, GLEnum.StaticDraw);

            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            gl.EnableVertexAttribArray(0);

            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            gl.EnableVertexAttribArray(1);

            gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            gl.BindVertexArray(0);

            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            //float[] proj = {
            //    2.0f / window.Size.X,  0,                     0,   0,
            //    0,                     -2.0f / window.Size.Y,  0,   0,
            //    0,                     0,                    -1,   0,
            //   -1,                     1,                     0,   1
            //};
            //int projLoc = gl.GetUniformLocation(shader.Handle, "projection");
            //gl.UniformMatrix4(projLoc, 1, false, proj.AsSpan());



            Matrix4x4 ortho = Matrix4x4.CreateOrthographicOffCenter(0, window.Size.X, window.Size.Y, 0, -1, 1);
            shader.Use();
            shader.SetMatrix4("projection", ortho);
            shader.SetInt("texture0", 0);  // sampler unit 0 — set once, never changes


            // ── Text VAO / VBO / EBO ──────────────────────────────────────────────
            textVao = gl.GenVertexArray();
            textVbo = gl.GenBuffer();
            _textEbo = gl.GenBuffer();

            gl.BindVertexArray(textVao);

            // Dynamic VBO: capacity for _maxTextChars quads (world-space XY + UV baked in)
            gl.BindBuffer(GLEnum.ArrayBuffer, textVbo);
            //gl.BufferData<float>(GLEnum.ArrayBuffer, (nuint)(_maxTextChars * 16 * sizeof(float)),   // old one
            //         null, GLEnum.DynamicDraw);

            gl.BufferData<float>(GLEnum.ArrayBuffer, (nuint)(_maxTextChars * 20 * sizeof(float)), null, GLEnum.DynamicDraw);



            // Pre-built EBO: 6 indices per quad, never changes
            var textIdx = new uint[_maxTextChars * 6];
            for (int qi = 0; qi < _maxTextChars; qi++)
            {
                textIdx[qi * 6 + 0] = (uint)(qi * 4);
                textIdx[qi * 6 + 1] = (uint)(qi * 4 + 1);
                textIdx[qi * 6 + 2] = (uint)(qi * 4 + 2);
                textIdx[qi * 6 + 3] = (uint)(qi * 4);
                textIdx[qi * 6 + 4] = (uint)(qi * 4 + 2);
                textIdx[qi * 6 + 5] = (uint)(qi * 4 + 3);
            }
            gl.BindBuffer(GLEnum.ElementArrayBuffer, _textEbo);
            gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)textIdx.Length * sizeof(uint),
                          textIdx, GLEnum.StaticDraw);
            /*  old one
            // aPos (location 0): vec2 world position (baked, not normalized 0-1)
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            gl.EnableVertexAttribArray(0);
            // aUV (location 1): vec2 texture coordinates

            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            gl.EnableVertexAttribArray(1);
            */

            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 2 * sizeof(float));
            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, 5 * sizeof(float), 4 * sizeof(float));
            gl.EnableVertexAttribArray(2);

            gl.BindVertexArray(0);

            textShader.Use();
            textShader.SetMatrix4("projection", ortho);
            
            
            // Set identity values for uniforms now handled by baked vertex data
            gl.Uniform2(gl.GetUniformLocation((uint)textShader.Handle, "position"), 0f, 0f);
            gl.Uniform2(gl.GetUniformLocation((uint)textShader.Handle, "size"), 1f, 1f);
            gl.Uniform1(gl.GetUniformLocation((uint)textShader.Handle, "italicSlant"), 0f);


            stencilShader = new ShaderGL(gl, shadersPath + "stencil.vglsl", shadersPath + "stencil.fglsl");

            gl.BindVertexArray(vao);

            gl.BindBuffer(GLEnum.ArrayBuffer, vbo);
            gl.BufferData(GLEnum.ArrayBuffer, (nuint)vertices.Length * sizeof(float), vertices, GLEnum.StaticDraw);

            gl.BindBuffer(GLEnum.ElementArrayBuffer, ebo);
            gl.BufferData(GLEnum.ElementArrayBuffer, (nuint)indices.Length * sizeof(float), indices, GLEnum.StaticDraw);

            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            gl.EnableVertexAttribArray(0);

            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            gl.EnableVertexAttribArray(1);

            gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            gl.BindVertexArray(0);

            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);


            stencilShader.Use();
            stencilShader.SetMatrix4("projection", ortho);
            stencilShader.SetInt("maskTex", 0);

        }

        private void CheckGLError(string stage)
        {
            var error = gl.GetError();
            if (error != GLEnum.NoError)
                Console.WriteLine($"[GL ERROR] {stage}: {error}");
        }

        [DllImport("glfw3", CallingConvention = CallingConvention.Cdecl,
           EntryPoint = "glfwGetClipboardString")]
        private static extern unsafe byte* NativeGetClipboardString(void* window);

        public unsafe string GetClipboard()
        {
            try
            {
                byte* ptr = NativeGetClipboardString(null);
                return ptr != null ? Marshal.PtrToStringUTF8((IntPtr)ptr) ?? "" : "";
            }
            catch { return ""; }
        }
        /*
        // Clipboard access
        public unsafe string GetClipboard()
        {
            var glfw = Glfw.GetApi();
            string clip = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(glfw.GetClipboardString((WindowHandle*)null) ?? ""));


            Console.WriteLine($"[Clipboard] '{clip}' len={clip.Length}");
            return clip;
        }
        */
        public void SetClipboard(string text)
        {
            unsafe
            {
                var glfw = Glfw.GetApi();
                glfw.SetClipboardString((WindowHandle*)null, text);
            }
        }

        // ── Cross-thread command dispatch ─────────────────────────────────────────
        // Lua runs on a background thread; GL calls must happen on the render thread.
        // EnqueueCommand posts a func to the CommandQueue and returns a Task so the
        // caller can await or .Result-block until the render thread executes it.

        public Task<T> EnqueueCommand<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (CommandQueue)
            {
                CommandQueue.Enqueue(() =>
                {
                    try { tcs.SetResult(func()); }
                    catch (Exception ex) { tcs.SetException(ex); }
                });
            }
            return tcs.Task;
        }

        public Task EnqueueCommand(Action action)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (CommandQueue)
            {
                CommandQueue.Enqueue(() =>
                {
                    try { action(); tcs.SetResult(); }
                    catch (Exception ex) { tcs.SetException(ex); }
                });
            }
            return tcs.Task;
        }


        private bool _luaReady = false;
        private int _loadingTexId = -1;
        public string LoadingTexturePath { get; set; }
        private bool _justBecameReady = false;

        public void SetLuaReady()
        {
            _luaReady = true;
            _justBecameReady = true;
        }

        public void SetLoadingTexture(string path)
        {
            if (!File.Exists(path)) return; 
            var (id, _, _) = LoadTexture(path); // sync — načte se před Lua
            _loadingTexId = id;
        }



        public void ApplyScissorMask(int texId, float threshold, float x, float y, float w, float h)
        {
            gl.StencilFunc(StencilFunction.Always, 1, 0xFF);
            gl.StencilMask(0xFF);
            gl.Clear(ClearBufferMask.StencilBufferBit);
            gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            gl.ColorMask(false, false, false, false);

            DrawMaskQuad(x, y, w, h, texId, threshold);

            gl.ColorMask(true, true, true, true);
            gl.StencilFunc(StencilFunction.Equal, 1, 0xFF);
            //gl.StencilFunc(StencilFunction.Always, 1, 0xFF);
            gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
        }

        public void RemoveScissorMask()
        {
            gl.StencilFunc(StencilFunction.Always, 0, 0xFF);
            gl.StencilMask(0xFF);
        }
    }
}
