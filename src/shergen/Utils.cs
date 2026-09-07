using static Shergen.Lua.Native;

namespace Shergen
{
    public struct RGBA : IEquatable<RGBA>
    {
        public float R, G, B, A;

        public RGBA(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
        public string ToHex()
        {
            int r = (int)(R*255);
            int g = (int)(G*255);
            int b = (int)(B*255);
            int a = (int)(A*255);
            return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }

        public static RGBA FromHex(string hex)
        {
            hex = hex.TrimStart('#');
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            byte a = hex.Length >= 8 ? Convert.ToByte(hex.Substring(6, 2), 16) : (byte)255;

            return new RGBA(r / 255f, g / 255f, b /255f, a / 255f);
        }

        public static bool operator ==(RGBA left, RGBA right)
        {
            // Zde porovnáte všechny vnitřní hodnoty structu
            return left.R == right.R &&
                   left.G == right.G &&
                   left.B == right.B &&
                   left.A == right.A;
        }

        public static bool operator !=(RGBA left, RGBA right)
        {
            return !(left == right);
        }

        public bool Equals(RGBA other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is RGBA other && Equals(other);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B, A);
        }
    }



    public struct Coords
    {
        public float X, Y, W, H;

        public Coords(float x, float y, float w, float h) 
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        
        }
    }

    public struct LuaCallback
    {
        public int FuncRef;   // Lua registry ref (> 0), nebo 0
        public string FuncStr;   // inline kód, nebo null
        public object[] ExtraArgs;

        public bool IsEmpty => FuncRef == 0 && FuncStr == null;
    }



    public static class Utils
    {
        public static string FindFileCaseInsensitive(string relativePath, bool checkOutSide = true)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;

            string fullPath = Path.GetFullPath(Path.Combine(basePath, relativePath));
            if (checkOutSide && !fullPath.StartsWith(basePath))
                throw new UnauthorizedAccessException("File access outside of application folder is forbidden!");

            string[] parts = relativePath.Replace('\\', '/').Split('/');
            string current = basePath;
            List<string> resolvedParts = new List<string>();

            foreach (string part in parts)
            {
                if (!Directory.Exists(current))
                {
                    resolvedParts.Add(part);
                    current = Path.Combine(current, part);
                    continue;
                }

                var matches = Directory.EnumerateFileSystemEntries(current)
                    .Where(e => string.Equals(Path.GetFileName(e), part, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matches.Count > 1)
                    Console.WriteLine($"Warning: multiple case variants found for '{part}', using first match");

                if (!matches.Any())
                {
                    resolvedParts.Add(part);
                    current = Path.Combine(current, part);
                    continue;
                }

                string matchedName = Path.GetFileName(matches.First());
                resolvedParts.Add(matchedName);
                current = matches.First();
            }

            return string.Join('/', resolvedParts);
        }

        static public LuaCallback ReadCallback(IntPtr L, int index)
        {
            LuaCallback cb = default;

            if (GetTop(L) >= index && !IsLuaNil(L, index))
            {
                if (IsLuaFunction(L, index))
                {
                    PushValue(L, index);
                    cb.FuncRef = LuaLRef(L);
                }
                else if (IsLuaString(L, index))
                    cb.FuncStr = ToLuaString(L, index);

                int top = GetTop(L);
                if (top >= index+1)
                {
                    cb.ExtraArgs = new object[top - index];
                    for (int i = index+1; i <= top; i++)
                    {
                        if (IsLuaBoolean(L, i)) cb.ExtraArgs[i - index - 1] = ToLuaBoolean(L, i);
                        else if (IsLuaNumber(L, i)) cb.ExtraArgs[i - index - 1] = ToLuaNumber(L, i);
                        else if (IsLuaString(L, i)) cb.ExtraArgs[i - index - 1] = ToLuaString(L, i);
                    }
                }
            }
            return cb;
        }

    }
}