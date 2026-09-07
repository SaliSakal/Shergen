using Silk.NET.OpenGL;
using System.Drawing;

namespace Shergen.Renderer
{
    /// <summary>
    /// Spravuje zásobník scissor obdélníků pro OpenGL.
    /// Každý nový scissor je průnikem s předchozím (vnořené clipping).
    /// Souřadnice se zadávají v screen-space (Y=0 nahoře); Y-flip pro OpenGL se provádí interně.
    /// </summary>
    public class ScissorStack
    {
        private readonly GL gl;
        private readonly List<Rectangle> scissors = new();

        public ScissorStack(GL gl)
        {
            this.gl = gl;
        }

        public void ApplyScissor(int x, int y, int width, int height)
        {
            var newRect = new Rectangle(x, y, width, height);

            if (scissors.Count > 0)
            {
                var last = scissors.Last();
                newRect = Rectangle.Intersect(last, newRect);
            }

            scissors.Add(newRect);

            SetGLScissor(newRect);

            if (scissors.Count == 1)
                gl.Enable(EnableCap.ScissorTest);
        }

        public void RemoveScissor()
        {
            if (scissors.Count > 0)
                scissors.RemoveAt(scissors.Count - 1);

            if (scissors.Count == 0)
            {
                gl.Disable(EnableCap.ScissorTest);
            }
            else
            {
                SetGLScissor(scissors.Last());
            }
        }

        /// <summary>Current clip rect in GL coordinates (Y=0 at bottom), or null if no scissor active.</summary>
        public Rectangle? Current => scissors.Count > 0 ? scissors.Last() : (Rectangle?)null;

        public void Clear(bool disableTest = false)
        {
            scissors.Clear();
            if (disableTest)
                gl.Disable(EnableCap.ScissorTest);
        }

        private void SetGLScissor(Rectangle r)
        {
            gl.Scissor(r.X, r.Y, (uint)r.Width, (uint)r.Height);
        }

    }
}
