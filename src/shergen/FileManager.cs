using System.Text;
using static Shergen.Lua.Native;

namespace Shergen
{
    public static class FileManager
    {
        // ── Core: otevře soubor pro zápis (security check + tvorba složek) ─────────
        public static Stream OpenFile(string relativePath)
        {
            string fullPath = Utils.FindFileCaseInsensitive(relativePath);

            string directoryPath = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            return new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        }

        // ── Core: uloží soubor ────────────────────────────────────────────────────
        public static void SaveFile(string path, string content, Encoding encoding = null)
        {
            using var stream = OpenFile(path);
            using var writer = new StreamWriter(stream, encoding ?? new UTF8Encoding(false));
            writer.Write(content);
        }

        public static int SaveFile(IntPtr L)
        {
            string fileName     = ToLuaString(L, 1);
            string data         = ToLuaString(L, 2);
            string encodingName = ToLuaString(L, 3).ToLower();

            Encoding encoding = encodingName switch
            {
                "ansi"    => Encoding.Default,
                "utf8"    => new UTF8Encoding(false),
                "utf8bom" => Encoding.UTF8,
                "utf16"   => Encoding.Unicode,
                "utf16le" => Encoding.Unicode,
                "utf16be" => Encoding.BigEndianUnicode,
                _         => new UTF8Encoding(false)
            };

            try
            {
                SaveFile(fileName, data, encoding);
                PushLuaBoolean(L, true);
                return 1;
            }
            catch (Exception ex)
            {
                PushLuaError(L, "❌ Error saving file: " + ex.Message);
                return 1;
            }
        }

        // ── Core: existence souboru ───────────────────────────────────────────────
        public static bool FileExists(string relativePath)
        {
            string fullPath = Utils.FindFileCaseInsensitive(relativePath);
            return File.Exists(fullPath);
        }

        public static int FileExists(IntPtr L)
        {
            try
            {
                PushLuaBoolean(L, FileExists(ToLuaString(L, 1)));
            } catch (Exception ex) {
                PushLuaError(L, "❌ Error checking file existence: " + ex.Message);
            }
            return 1;
        }

        // ── Core: načte soubor ────────────────────────────────────────────────────
        public static string LoadFile(string path, string encodingName = null)
        {
            string fullPath = Utils.FindFileCaseInsensitive(path);

            Encoding encoding = encodingName == null ? null : encodingName switch
            {
                "ansi"    => Encoding.Default,
                "utf8"    => new UTF8Encoding(false),
                "utf8bom" => Encoding.UTF8,
                "utf16"   => Encoding.Unicode,
                "utf16le" => Encoding.Unicode,
                "utf16be" => Encoding.BigEndianUnicode,
                _         => null
            };

            return encoding != null
                ? File.ReadAllText(fullPath, encoding)
                : File.ReadAllText(fullPath);
        }

        public static int LoadFile(IntPtr L)
        {
            string path = ToLuaString(L, 1);
            string encodingName = null;
            if (GetTop(L) >= 2 && !IsLuaNil(L, 2))
                encodingName = ToLuaString(L, 2).ToLower();

            try
            {
                PushLuaString(L, LoadFile(path, encodingName));
                return 1;
            }
            catch (Exception ex)
            {
                PushLuaError(L, "LoadFile error: " + ex.Message);
                return 0;
            }
        }

        public static byte[] ReadAllBytes(string path)
        {
            string fullPath = Utils.FindFileCaseInsensitive(path);
            return File.ReadAllBytes(fullPath);
        }

        // ── Core: seznam souborů ──────────────────────────────────────────────────
        public static List<string> GetFiles(string relativePath, string extension = null, bool stripExtension = false)
        {
            string fullPath = Utils.FindFileCaseInsensitive(relativePath);

            if (!Directory.Exists(fullPath))
                return new List<string>();

            var files = Directory.GetFiles(fullPath);

            if (!string.IsNullOrWhiteSpace(extension) && extension != "*")
            {
                string ext = "." + extension.TrimStart('.');
                files = files
                    .Where(f => Path.GetExtension(f).Equals(ext, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            return files
                .Select(f =>
                {
                    var name = Path.GetFileName(f);
                    return stripExtension ? Path.GetFileNameWithoutExtension(name) : name;
                })
                .ToList();
        }

        public static int GetFilesLua(IntPtr L)
        {
            string path = ToLuaString(L, 1);
            string ext  = IsLuaNil(L, 2) ? null : ToLuaString(L, 2);
            bool strip  = !IsLuaNil(L, 3) && ToLuaBoolean(L, 3);
            try
            {
                var files = GetFiles(path, ext, strip);
                NewTable(L);
                int i = 1;
                foreach (var file in files)
                {
                    PushLuaInteger(L, i++);
                    PushLuaString(L, file);
                    SetTable(L, -3);
                }
                return 1;
            }
            catch (Exception e)
            {
                PushLuaError(L, "GetFiles error: " + path + " " + e.Message);
                return 0;
            }
        }

        // ── Core: seznam složek ───────────────────────────────────────────────────
        public static List<string> GetDirectories(string relativePath)
        {
            string fullPath = Utils.FindFileCaseInsensitive(relativePath);

            if (!Directory.Exists(fullPath))
                return new List<string>();

            return Directory.GetDirectories(fullPath)
                .Select(d => Path.GetFileName(d)!)
                .ToList();
        }

        public static int GetDirsLua(IntPtr L)
        {
            string path = ToLuaString(L, 1);
            try
            {
                var dirs = GetDirectories(path);
                NewTable(L);
                int i = 1;
                foreach (var dir in dirs)
                {
                    PushLuaInteger(L, i++);
                    PushLuaString(L, dir);
                    SetTable(L, -3);
                }
                return 1;
            }
            catch (Exception e)
            {
                PushLuaError(L, "❌ GetDirs error: " + e.Message);
                return 0;
            }
        }

        public static string EscapeLuaString(string s)
            => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
