using Silk.NET.OpenGL;
using System.Numerics;

namespace Shergen.Renderer
{
    public class ShaderGL
    {
        private readonly GL gl;
        public uint Handle { get; private set; }

        private readonly Dictionary<string, int>   _locationCache  = new();
        private readonly Dictionary<int, int>     _intCache     = new();
        private readonly Dictionary<int, float>   _floatCache   = new();
        private readonly Dictionary<int, Vector2> _vector2Cache = new();
        private readonly Dictionary<int, Vector4> _vector4Cache = new();

        public ShaderGL(GL gl, string vertexPath, string fragmentPath)
        {
            this.gl = gl;

            string vertexCode   = File.ReadAllText(vertexPath);
            string fragmentCode = File.ReadAllText(fragmentPath);

            uint vertex   = CompileShader(vertexCode,   ShaderType.VertexShader);
            uint fragment = CompileShader(fragmentCode, ShaderType.FragmentShader);

            Handle = gl.CreateProgram();
            gl.AttachShader(Handle, vertex);
            gl.AttachShader(Handle, fragment);
            gl.LinkProgram(Handle);

            gl.DeleteShader(vertex);
            gl.DeleteShader(fragment);
        }

        private uint CompileShader(string source, ShaderType type)
        {
            uint shader = gl.CreateShader(type);
            gl.ShaderSource(shader, source);
            gl.CompileShader(shader);
            gl.GetShader(shader, ShaderParameterName.CompileStatus, out int success);
            if (success == 0)
            {
                gl.GetShaderInfoLog(shader, out string infoLog);
                throw new Exception($"Chyba při kompilaci {type} shaderu: {infoLog}");
            }
            return shader;
        }

        public void Use() => gl.UseProgram(Handle);

        /// <summary>Returns (and caches) the uniform location for <paramref name="name"/>.</summary>
        public int Loc(string name)
        {
            if (!_locationCache.TryGetValue(name, out int loc))
            {
                loc = gl.GetUniformLocation(Handle, name);
                _locationCache[name] = loc;
            }
            return loc;
        }

        public void SetInt(string name, int value)
        {
            int loc = Loc(name);
            if (_intCache.TryGetValue(loc, out int c) && c == value) return;
            _intCache[loc] = value;
            gl.Uniform1(loc, value);
        }

        public void SetFloat(string name, float value)
        {
            int loc = Loc(name);
            if (_floatCache.TryGetValue(loc, out float c) && c == value) return;
            _floatCache[loc] = value;
            gl.Uniform1(loc, value);
        }

        public void SetVector2(string name, Vector2 value)
        {
            int loc = Loc(name);
            if (_vector2Cache.TryGetValue(loc, out Vector2 c) && c == value) return;
            _vector2Cache[loc] = value;
            gl.Uniform2(loc, value.X, value.Y);
        }

        public void SetVector4(string name, Vector4 value)
        {
            int loc = Loc(name);
            if (_vector4Cache.TryGetValue(loc, out Vector4 c) && c == value) return;
            _vector4Cache[loc] = value;
            gl.Uniform4(loc, value.X, value.Y, value.Z, value.W);
        }

        public void SetMatrix4(string name, Matrix4x4 matrix)
        {
            int location = Loc(name);
            if (location == -1)
            {
                Console.WriteLine($"[Shader] Warning: Uniform '{name}' not found!");
                return;
            }

            float[] data = {
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
            };
            gl.UniformMatrix4(location, 1, false, data.AsSpan());
        }
    }
}
