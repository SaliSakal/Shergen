using Silk.NET.Core;
using Silk.NET.Input;
using StbImageSharp;
using System.Runtime.InteropServices;

namespace Shergen.Renderer
{
    public partial class RendererGL : IRenderer
    {
        // Renderer si pamatuje hotspot — SetCursorTexture ho nepotřebuje znovu předávat
        private int _cursorHotX;
        private int _cursorHotY;

        /// <summary>
        /// Nastaví texturu kurzoru + hotspot najednou.
        /// Lua: mouse:SetCursor(path, x or 0, y or 0)
        /// </summary>
        public void SetCursor(string path, int hotX = 0, int hotY = 0)
        {
            if (_mouse == null) return;

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                ResetCursor();
                return;
            }

            _cursorHotX = hotX;
            _cursorHotY = hotY;

            ApplyCursorImage(path);
        }

        /// <summary>
        /// Změní pouze texturu kurzoru — hotspot zůstane z posledního SetCursor.
        /// Lua: mouse:SetCursorTexture(path)
        /// </summary>
        public void SetCursorTexture(string path)
        {
            if (_mouse == null) return;

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                ResetCursor();
                return;
            }

            ApplyCursorImage(path);
        }

        /// <summary>Vrátí aktuální hotspot (nastaven posledním SetCursor).</summary>
        public (int hotX, int hotY) GetCursorHotSpot() => (_cursorHotX, _cursorHotY);

        /// <summary>Přesune OS kurzor na zadané souřadnice.</summary>
        public void SetCursorPos(float x, float y)
        {
            if (_mouse == null) return;
            _mouse.Position = new System.Numerics.Vector2(x, y);
        }

        /// <summary>Uzamkne / odemkne kurzor v okně (jako "Lock Mouse" v nastavení her).</summary>
        public void SetCursorLocked(bool locked)
        {
            if (_mouse == null) return;
            _mouse.Cursor.IsConfined = locked;
        }

        /// <summary>Skryje / zobrazí kurzor (pohybuje se dál).</summary>
        public void SetCursorHidden(bool hidden)
        {
            if (_mouse == null) return;
            _mouse.Cursor.CursorMode = hidden ? CursorMode.Hidden : CursorMode.Normal;
        }

        /// <summary>
        /// FPS režim — kurzor skrytý + zamčený uprostřed, raw input.
        /// CursorMode.Disabled = GLFW_CURSOR_DISABLED.
        /// </summary>
        public void SetCursorCaptured(bool captured)
        {
            if (_mouse == null) return;
            _mouse.Cursor.CursorMode = captured ? CursorMode.Disabled : CursorMode.Normal;
        }

        /// <summary>Obnoví výchozí kurzor OS.</summary>
        public void ResetCursor()
        {
            if (_mouse == null) return;
            _cursorHotX = 0;
            _cursorHotY = 0;
            _mouse.Cursor.Type = CursorType.Standard;
        }

        // ── Interní helper ────────────────────────────────────────────────────────
        private void ApplyCursorImage(string path)
        {
            using var stream = File.OpenRead(path);
            var img = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            var bytes = img.Data; // přímo byte[] RGBA

            _mouse.Cursor.Type     = CursorType.Custom;
            _mouse.Cursor.Image    = new RawImage(img.Width, img.Height, new Memory<byte>(bytes));
            _mouse.Cursor.HotspotX = _cursorHotX;
            _mouse.Cursor.HotspotY = _cursorHotY;
        }
    }
}
