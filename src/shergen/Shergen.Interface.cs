using System.Collections.Concurrent;

namespace Shergen
{
    public interface IRenderer
    {
        bool IsRunning { get; }
        ConcurrentQueue<Action> IEventQueue { get; }

        ManualResetEventSlim RendererReady { get; }

        void Run();

        int screenWidth { get; }
        int screenHeight { get; }

        public Task<T> EnqueueCommand<T>(Func<T> func);
        Task EnqueueCommand(Action cmd);

        void DrawRectangle(UIElement elem);
        void DrawText(UILabel elem);
        void MeasureEl(UILabel elem);
        void DrawQuad(float x, float y, float w, float h, RGBA color, int textureID = -1, Coords? subCoords = null, float grayscale = 0);

        void ApplyScissor(int x, int y, int w, int h);

        void RemoveScissor();

        void ApplyScissorMask(int texId, float threshold, float x, float y, float w, float h);
        void RemoveScissorMask();

        (int id, int width, int height) LoadTexture(string path);

        (int id, int width, int height) LoadTextureAsync(string path);
        void SetCursor(string path, int hotX = 0, int hotY = 0);
        void SetCursorTexture(string path);
        void ResetCursor();
        (int hotX, int hotY) GetCursorHotSpot();
        void SetCursorPos(float x, float y);
        void SetCursorLocked(bool locked);
        void SetCursorHidden(bool hidden);
        void SetCursorCaptured(bool captured);

        public void SetFocus(UIElement elem);

        UIElement GetFocus();
        void Exit();

        Action<int, int> OnResized { get; set; }
        Action<double> OnFrameTick { get; set; }

        string DefaultDFFFont { get; set; }
        string DefaultTTFFont { get; set; }
        void SetFontFallbackChar(string fontName, char fallback);

        void ClearFocus();

        float LastDeltaTime { get; set; }

        bool IsKeyPressed(int KeyCode);

        bool IsMousePressed(int button);

        string GetClipboard();
        void SetClipboard(string text);



        void SetLuaReady();
        void SetLoadingTexture(string path);
        
        string LoadingTexturePath { get; set; }

        (byte[] data, int w, int h) LoadMask(string path);

    }


    public interface IShergenModule
    {
        void Register(Shergen engine);
    }



}
