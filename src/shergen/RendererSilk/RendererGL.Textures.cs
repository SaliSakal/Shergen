using Silk.NET.OpenGL;
using StbImageSharp;
using System.Collections.Concurrent;

namespace Shergen.Renderer
{
    public partial class RendererGL : IRenderer
    {
        private readonly Dictionary<string, (uint id, int w, int h)> _textureCache = new();
        private readonly Dictionary<string, (byte[] data, int w, int h)> _maskData = new();

        public (int id, int width, int height) LoadTexture(string path)
        {
            // Cache check BEFORE loading from disk
            if (_textureCache.TryGetValue(path, out var cached))
                return ((int)cached.id, cached.w, cached.h);

            Console.WriteLine($"🖼️ Loading texture: {path}");

            using var stream = File.OpenRead(path);
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);


            uint texId = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, texId);

            // Generic overload — no GCHandle needed
            gl.TexImage2D(
                TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                (uint)image.Width, (uint)image.Height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte,
                image.Data.AsSpan());

            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            gl.BindTexture(TextureTarget.Texture2D, 0);

            _textureCache[path] = (texId, image.Width, image.Height);
            return ((int)texId, image.Width, image.Height);
        }

        public void BindTexture(int textureHandle)
        {
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, (uint)textureHandle);
        }

        // asingroní load texture, vrací id a rozměry, ale texture se nemusí načíst hned
        private readonly ConcurrentQueue<(uint texId, byte[] data, int w, int h, string path)> _pendingUploads = new();

        public (int id, int width, int height) LoadTextureAsync(string path)
        {
            if (_textureCache.TryGetValue(path, out var cached))
                return ((int)cached.id, cached.w, cached.h);

            var (w, h) = ReadPngDimensions(path);

            // Placeholder textura správné velikosti — zatím prázdná
            uint texId = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, texId);
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                (uint)w, (uint)h, 0, PixelFormat.Rgba, PixelType.UnsignedByte, Span<byte>.Empty);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            gl.BindTexture(TextureTarget.Texture2D, 0);

            _textureCache[path] = (texId, w, h);

            Task.Run(() =>
            {
                using var stream = File.OpenRead(path);
                var img = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                _pendingUploads.Enqueue((texId, img.Data, img.Width, img.Height, path));
            });

            return ((int)texId, w, h);
        }

        private static (int w, int h) ReadPngDimensions(string path)
        {
            Span<byte> header = stackalloc byte[24];
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            fs.Read(header);
            int w = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            int h = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            return (w, h);
        }

        // Volat z render loopu každý frame
        internal void ProcessPendingUploads()
        {
            while (_pendingUploads.TryDequeue(out var p))
            {
                gl.BindTexture(TextureTarget.Texture2D, p.texId);
                gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0,
                    (uint)p.w, (uint)p.h, PixelFormat.Rgba, PixelType.UnsignedByte,
                    p.data.AsSpan());
                gl.BindTexture(TextureTarget.Texture2D, 0);
                Console.WriteLine($"🖼️ Texture uploaded: {p.path}");
            }
        }

        public (byte[] data, int w, int h) LoadMask(string path)
        {
            if (_maskData.TryGetValue(path, out var cached))
                return cached;

            using var stream = File.OpenRead(path);
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            var full = image.Data; // RGBA
            var compact = new byte[image.Width * image.Height * 2];
            for (int i = 0; i < image.Width * image.Height; i++)
            {
                compact[i * 2 + 0] = full[i * 4 + 0]; // R (grayscale)
                compact[i * 2 + 1] = full[i * 4 + 3]; // A (alpha)
            }
            _maskData[path] = (compact, image.Width, image.Height);

            /*
            _maskData[patch] = (image.Data, image.Width, image.Height);
            */
            return (compact, image.Width, image.Height);
        }

    }
}
