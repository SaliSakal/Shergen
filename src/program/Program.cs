using Shergen.Renderer;
using Silk.NET.Windowing;
using System.Diagnostics;

namespace Program
{
#if ANDROID   // Android specific code - Not works yet, but will be implemented in the future
    [Activity(MainLauncher = true)]
    public class Program : Android.App.Activity
    {
            protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // sem přijde inicializace Shergen

            var engine = new Shergen.Shergen(name: "Shergen");

            engine.applyLuaSandbox();
            engine.Run(firstLuaFile: "init");
        }

#else
    public class Program
    {

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8; // Správné kódování
            Console.InputEncoding = System.Text.Encoding.UTF8;

            // Program.cs
            var engine = new Shergen.Shergen(name: "Shergen", wState: WindowState.Maximized);

            engine.ApplyLuaSandbox();
            engine.SetLoadingTexture("interface/loading.png");
            engine.LoadModule(new Shergen.Json.Module());
            engine.LoadModule(new Shergen.XLSX.Module());
            engine.RegisterLuaConstant("VERSION", "0.1.0");
            engine.Run(firstLuaFile: "init");


            Console.WriteLine("🔚 Program Ended!");
        }
#endif



    }
}