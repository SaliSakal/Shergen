using System;
using System.Collections.Generic;
using System.Text;

namespace Shergen
{
    /// <summary>
    /// Virtual cursor element — NOT part of the render or hit-test tree.
    /// Hotspot je uložen v rendereru, nastaví se přes SetCursor(path, x, y).
    /// </summary>
    public class UIMouse : UIElement
    {
        // Pamatujeme si cestu pro GetTexture — bez spouštění OnTextureChanged

        public int HotX { get; protected set; }
        public int HotY { get; protected set; }
        public UIMouse() : base(UIElementType.Mouse, isVirtual: true) { }

        /// <summary>No-op: OS manages cursor position.</summary>
        public void UpdatePosition(float mouseX, float mouseY) { }

        /// <summary>
        /// Nastaví texturu kurzoru + hotspot najednou.
        /// Lua: GUI.SetCursor(path, x or 0, y or 0)
        /// </summary>
        public void SetCursor(string path, int hotX = 0, int hotY = 0)
        {
            Texture = path;
            HotX = hotX;
            HotY = hotY;
            _renderer.EnqueueCommand(() => _renderer.SetCursor(path, hotX, hotY));
        }

        /// <summary>
        /// Změní pouze texturu — hotspot zůstane z posledního SetCursor.
        /// Lua: GUI.SetCursorTexture(path)
        /// </summary>
        public void SetCursorTexture(string path)
        {

            Texture = path;
            _renderer.EnqueueCommand(() => _renderer.SetCursorTexture(path));
        }

        /// <summary>
        /// Přiřazení přes SetProperty(PROP.TEXTURE) → SetCursorTexture.
        /// </summary>
        protected override void OnTextureChanged()
        {
            _renderer.EnqueueCommand(() => _renderer.SetCursorTexture(Texture));
        }

        /// <summary>GetProperty(PROP.TEXTURE) vrátí naposledy nastavenou cestu.</summary>
        public override object GetProperty(UIProperty property)
        {
            if (property == UIProperty.Texture) return Texture;
            return base.GetProperty(property);
        }

        public override void Draw(bool forceDraw = false) { }

    }
}
