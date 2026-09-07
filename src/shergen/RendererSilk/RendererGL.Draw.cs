using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;

namespace Shergen.Renderer
{
    public partial class RendererGL : IRenderer
    {

        internal int _frameDrawCalls = 0;
        internal int _frameTextCalls = 0;
        static readonly Coords baseSubCoords = new Coords(0, 0, 1, 1);

        private void OnRender(double deltaTime)
        {
            if (!_luaReady)
            {
                gl.Clear(ClearBufferMask.ColorBufferBit);
                if (_loadingTexId >= 0)
                    DrawQuad(0, 0, screenWidth, screenHeight, new RGBA(1, 1, 1, 1), _loadingTexId);
                return;
            }

            //Console.WriteLine($"Draw calls: {_frameDrawCalls}, Text calls: {_frameTextCalls}");
            _frameDrawCalls = 0;
            _frameTextCalls = 0; 

            gl.BeginQuery(GLEnum.TimeElapsed, _glTimerQuery);

            gl.ClearColor(0.1f, 0.1f, 1f, 1.0f);
            gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit);


            if (UIElement.Desktop != null)
                try
                {
                    var tRender = Profiler.Ts();
                    UIElement.Desktop.Draw();
                    Profiler.Add("Rendering", Profiler.Ms(tRender));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Draw crash: {ex.GetType().Name}: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    throw;
                }
            
            CheckGLError("Render Frame");

            //window.SwapBuffers();

            gl.EndQuery(GLEnum.TimeElapsed);
            gl.GetQueryObject(_glTimerQuery, QueryObjectParameterName.ResultNoWait, out ulong ns);
            if (ns > 0) _lastGpuMs = ns / 1_000_000.0;


            Profiler.Add("GL Loop", Profiler.Ms(tGl));
        }

        public void DrawRectangle(UIElement elem)
        {

            //float realX = elem.X + (elem.Parent?.X ?? 0f);
            //float realY = elem.Y + (elem.Parent?.Y ?? 0f);
            float realX = elem.GetAbsX();
            float realY = elem.GetAbsY();
            //Console.WriteLine($"id={elem.ID} type={elem.Type} realX={realX} realY={realY} scrollOffsetY={((elem.Parent as UIScrollBox)?.ScrollOffsetY ?? -1)}");
            float fade = elem.GetEffectiveFade();


            RGBA fadeColor = new RGBA(elem.Color.R, elem.Color.G, elem.Color.B, elem.Color.A * fade);

            if (elem.TextureID > -1)
            {
                DrawQuad(realX, realY, elem.Width, elem.Height, fadeColor, elem.TextureID, elem.SubCoordsNorm, elem.Grayscale);
            }else
            {
                DrawQuad(realX, realY, elem.Width, elem.Height, fadeColor);
            }

            if (elem.BorderType != UIBorder.None && elem.BorderSize > 0)
                DrawBorder(elem, realX, realY, fade);

        }
        

        public void DrawBorder(UIElement elem, float realX, float realY, float fade)
        {
            float border = elem.BorderSize;
            float x, y, w, h;
            var bc = new RGBA(elem.BorderColor.R, elem.BorderColor.G, elem.BorderColor.B, elem.BorderColor.A * fade);

            if (elem.BorderType == UIBorder.Outer)
            {
                x = realX - border; y = realY - border;
                w = elem.Width + 2 * border; h = border;
                DrawQuad(x, y, w, h, bc);

                x = realX - border; y = realY + elem.Height;
                DrawQuad(x, y, w, h, bc);

                x = realX - border; y = realY - border;
                w = border; h = elem.Height + 2 * border;
                DrawQuad(x, y, w, h, bc);

                x = realX + elem.Width; y = realY - border;
                DrawQuad(x, y, w, h, bc);
            }
            else if (elem.BorderType == UIBorder.Inner)
            {
                x = realX; y = realY; w = elem.Width; h = border;
                DrawQuad(x, y, w, h, bc);

                x = realX; y = realY + elem.Height - border;
                DrawQuad(x, y, w, h, bc);

                x = realX; y = realY; w = border; h = elem.Height;
                DrawQuad(x, y, w, h, bc);

                x = realX + elem.Width - border; y = realY;
                DrawQuad(x, y, w, h, bc);
            }
        }

        private void DrawQuad(float x, float y, float w, float h, RGBA color)
        {
            shader.Use();

            shader.SetVector2("position", new Vector2(x, y));
            shader.SetVector2("size", new Vector2(w, h));
            shader.SetVector4("color", new Vector4(color.R, color.G, color.B, color.A));

            shader.SetInt("useTexture", 0);

            gl.BindVertexArray(vao);

            unsafe
            {
                gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
            }
            gl.BindVertexArray(0);
        }


        public void DrawQuad(float x, float y, float w, float h, RGBA color, int textureID = -1, Coords? subCoords = null, float grayscale = 0)
        {
            Coords sc = subCoords ?? baseSubCoords;

            shader.Use();

            shader.SetVector2("position", new Vector2(x, y));
            shader.SetVector2("size", new Vector2(w, h));
            shader.SetVector4("color", new Vector4(color.R, color.G, color.B, color.A));

            shader.SetVector2("uvOffset", new Vector2(sc.X, sc.Y));
            shader.SetVector2("uvScale", new Vector2(sc.W, sc.H));

            if (textureID >= 0)
            {
                //Console.WriteLine($"[DRAWQUAD] x={x} y={y} w={w} h={h} textureID={textureID}");

                gl.ActiveTexture(TextureUnit.Texture0);
                if (_lastBoundTextureId != textureID) { gl.BindTexture(GLEnum.Texture2D, (uint)textureID); _lastBoundTextureId = textureID; }
                shader.SetInt("useTexture", 1);
                shader.SetFloat("grayscale", grayscale);
            }
            else
            {
                shader.SetInt("useTexture", 0);
            }

            gl.BindVertexArray(vao);

            unsafe
            {
                gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
            }

            CheckGLError("DrawElements");
            gl.BindVertexArray(0);

            _frameDrawCalls++;
        }


        private void DrawMaskQuad(float x, float y, float w, float h, int texId, float threshold)
        {
            stencilShader.Use();
            stencilShader.SetVector2("position", new Vector2(x, y));
            stencilShader.SetVector2("size", new Vector2(w, h));
            stencilShader.SetVector2("uvOffset", new Vector2(0, 0));
            stencilShader.SetVector2("uvScale", new Vector2(1, 1));
            stencilShader.SetFloat("threshold", threshold/255f);

            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(GLEnum.Texture2D, (uint)texId);

            gl.BindVertexArray(vao);
            unsafe { gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null); }
            gl.BindVertexArray(0);
        }




    }

}
