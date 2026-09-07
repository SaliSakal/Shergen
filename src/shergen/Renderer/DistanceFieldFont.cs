using Silk.NET.OpenGL;
using System.Globalization;
using System.IO.Compression;

namespace Shergen.DistanceFieldFont
{
    public class DistanceFieldFont
    {
        public string Signature { get; private set; }
        public byte Version { get; private set; }
        public ushort MaxWidth { get; private set; }
        public ushort MaxHeight { get; private set; }
        public int Padding { get; private set; }
        public ushort Rows { get; private set; }
        public ushort Columns { get; private set; }
        public ushort TextureCount { get; private set; }
        public List<byte[]> Textures { get; private set; } = new();
        public Dictionary<int, DistanceFieldCharacter> Characters { get; private set; } = new();
        public bool HasScale { get; private set; }
        public ushort SpaceWidth { get; private set; }

        public class DistanceFieldCharacter
        {
            public ushort Link  { get; set; }
            public ushort CW    { get; set; }
            public ushort CH    { get; set; }
            public ushort Index { get; set; }
        }

        public static DistanceFieldFont LoadFromFile(string filename,bool metricOnly = false)
        {
            using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            var font = new DistanceFieldFont();
            font.Signature = new string(reader.ReadChars(3));
            font.Version   = reader.ReadByte();

            if (font.Signature != "DFF")
                throw new Exception("Invalid DFF file.");

            ushort charCount = reader.ReadUInt16();
            font.MaxWidth    = reader.ReadUInt16();
            font.MaxHeight   = reader.ReadUInt16();
            font.Padding     = reader.ReadInt32();
            font.Rows        = reader.ReadUInt16();
            font.Columns     = reader.ReadUInt16();
            font.TextureCount = reader.ReadUInt16();


            for (int i = 0; i < font.TextureCount; i++)
                {
                    int    compressedSize = reader.ReadInt32();
                if (!metricOnly)
                {
                    byte[] compressedData = reader.ReadBytes(compressedSize);
                    font.Textures.Add(DecompressZLib(compressedData));
                } else
                    stream.Seek(compressedSize, SeekOrigin.Current);  // přeskočí bytes
            }

            for (int i = 0; i < charCount; i++)
            {
                ushort id  = reader.ReadUInt16();
                var    chr = new DistanceFieldCharacter();
                chr.Link   = reader.ReadUInt16();

                if (chr.Link == id)
                {
                    chr.Index = reader.ReadUInt16();
                    chr.CW    = reader.ReadUInt16();
                    chr.CH    = reader.ReadUInt16();
                }

                font.Characters[id] = chr;
            }

            if (font.Version >= 2) font.HasScale   = reader.ReadBoolean();
            if (font.Version >= 3) font.SpaceWidth  = reader.ReadUInt16();

            return font;
        }

        private static byte[] DecompressZLib(byte[] data)
        {
            using var ms     = new MemoryStream(data);
            using var zlib   = new ZLibStream(ms, CompressionMode.Decompress);
            using var result = new MemoryStream();
            zlib.CopyTo(result);
            return result.ToArray();
        }

        public int[] TextureIDs { get; private set; }
        public int ArrayTextureID { get; private set; }

        public void UploadToGPU(GL gl)
        {
            uint tex = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2DArray, tex);

            gl.TexImage3D<byte>(
                TextureTarget.Texture2DArray, 0, 
                InternalFormat.R8,
                512, 512, 
                (uint)Textures.Count, 0,
                PixelFormat.Red, 
                PixelType.UnsignedByte, 
                Span<byte>.Empty
            );


            for (int i = 0; i < Textures.Count; i++)
            {
                gl.TexSubImage3D<byte>(
                    TextureTarget.Texture2DArray, 0,
                    0, 0, i,
                    512, 512, 1,
                    PixelFormat.Red, PixelType.UnsignedByte,
                    Textures[i].AsSpan());
            }
            gl.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            gl.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            gl.BindTexture(TextureTarget.Texture2DArray, 0);

            ArrayTextureID = (int)tex;

            // TextureID v CharUV teď znamená layer index (0..N), ne GL texture ID
            TextureIDs = new int[Textures.Count];
            for (int i = 0; i < Textures.Count; i++)
                TextureIDs[i] = i;
        }

        public struct CharUV
        {
            public int   TextureID;
            public float U0, V0, U1, V1;
            public float AdvanceWidth;
        }

        public bool TryGetCharUV(char c, out CharUV uv)
        {
            uv = default;
            if (!Characters.TryGetValue(c, out var chr))
                return false;

            var realChr = chr.Link != c ? Characters[chr.Link] : chr;

            int charPerTex = Rows * Columns;
            int index      = realChr.Index;
            int texIndex   = index / charPerTex;
            int posInTex   = index % charPerTex;
            int col        = posInTex % Columns;
            int row        = posInTex / Columns;

            int pixX = col * MaxWidth;
            int pixY = row * MaxHeight;

            uv.TextureID    = (TextureIDs != null && texIndex < TextureIDs.Length) ? TextureIDs[texIndex] : -1;
            uv.U0           = pixX / 512f;
            uv.V0           = pixY / 512f;
            uv.U1           = (pixX + realChr.CW + Padding * 2) / 512f;
            uv.V1           = (pixY + realChr.CH + Padding * 2) / 512f;
            uv.AdvanceWidth = realChr.CW;

            return true;
        }
    }

    public class DFFConfig
    {
        public string Path;
        public float  Scale;
        public float  RangeMin;
        public float  RangeMax;
        public int    Mode;

        public static DFFConfig Parse(string def)
        {
            var parts = def.Split(';');
            return new DFFConfig
            {
                Path     = parts[0],
                Scale    = float.Parse(parts[1], CultureInfo.InvariantCulture),
                RangeMin = float.Parse(parts[2], CultureInfo.InvariantCulture),
                RangeMax = float.Parse(parts[3], CultureInfo.InvariantCulture),
                Mode     = int.Parse(parts[4])
            };
        }
    }
}
