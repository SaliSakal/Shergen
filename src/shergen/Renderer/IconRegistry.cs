using System;
using System.Collections.Generic;
using System.Text;

namespace Shergen.Renderer
{
    public struct IconDef
    {
        public int TextureID;
        public int TexW, TexH;
        public RGBA? Color;    // null = zdědí barvu textu
        public float Scale;    // násobek výšky písma
        public float OffsetX;  // poměrově k výšce písma
        public float OffsetY;
    }

    public static class IconRegistry
    {
        private static readonly Dictionary<string, IconDef> _icons = new(StringComparer.OrdinalIgnoreCase);
        public static int Version { get; private set; } = 0;

        public static void Register(string tag, IconDef def)
        {
            _icons[tag] = def;
            Version++;
        }

        public static bool Remove(string tag)
        {
            bool removed = _icons.Remove(tag);
            if (removed) Version++;
            return removed;
        }

        public static bool Contains(string tag) => _icons.ContainsKey(tag);
        public static bool TryGet(string tag, out IconDef def) => _icons.TryGetValue(tag, out def);
    }
}
