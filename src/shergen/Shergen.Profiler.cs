using System.Diagnostics;

namespace Shergen
{
    /// <summary>
    /// Simple per-frame profiler. Toggle overlay with F3.
    /// GL-thread sections are accumulated in OnUpdate/OnRender.
    /// Lua-thread section is accumulated in Shergen.cs event loop.
    /// Values are averages over one second, updated alongside the FPS counter.
    /// </summary>
    public static class Profiler
    {
        public static bool Visible;
        public static void Toggle() => Visible = !Visible;

        private static readonly List<Section> sections = new();

        static Profiler()
        {
            Register("GL Loop", null, new RGBA(0.4f, 0.8f, 1f, 1f));
            Register("CommQueue", "GL Loop", new RGBA(0.6f, 0.9f, 1f, 1f));
            Register("Input", "GL Loop", new RGBA(0.4f, 0.7f, .8f, 1f));
            Register("Rendering", "GL Loop", new RGBA(1f, 0.8f, 0.4f, 1f));
            Register("Drawing", "Rendering", new RGBA(1f, 0.9f, 0.6f, 1f));
            Register("Drawing Text", "Rendering", new RGBA(0.85f, 0.75f, 0.45f, 1f));

            Register("Lua Loop", null, new RGBA(0.4f, 1f, 0.6f, 1f));
            Register("Queue", "Lua Loop", new RGBA(0.9f, 0.5f, 0.6f, 1));
            Register("Slacer", "Lua Loop", new RGBA(1f, 0.6f, 0.7f, 1));

        }

        private class Section
        {
            public string Name;
            public string Parent;
            public double Acc;
            public double Value;

            public string hexColor;

            public double[] History = new double[2000];
            public int HistoryIndex;
            public double Max;
        }

        public static void Register(string name, string parent = null, string hex = "#FFFFFF")
        {
            if (sections.Any(s => s.Name == name)) return;
            sections.Add(new Section { Name = name, Parent = parent, hexColor = hex });
        }

        public static void Register(string name, string parent, RGBA color)
        {
            Register(name, parent, color.ToHex());
        }
        public static void Add(string name, double ms)
        {
            var s = sections.FirstOrDefault(s => s.Name == name);
            if (s != null) s.Acc += ms;
        }


        // ── Flush: compute averages, reset accumulators ───────────────────────────
        public static void Flush(int frames)
        {
            if (frames <= 0) frames = 1;
            foreach (var s in sections)
            {
                s.Value = s.Acc / frames;
                s.History[s.HistoryIndex] = s.Value;
                s.HistoryIndex = (s.HistoryIndex + 1) % s.History.Length;
                s.Max = s.History.Max();
                s.Acc = 0;
            }
        }
        public static IEnumerable<(string Name, string Parent, double Value, double Max, string HexColor)> GetSections()
            => sections.Select(s => (s.Name, s.Parent, s.Value, s.Max, s.hexColor));



        // ── Timing helpers ────────────────────────────────────────────────────────
        public static long Ts() => Stopwatch.GetTimestamp();

        public static double Ms(long start) =>
            (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
    }
}
