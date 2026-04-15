using BepuUtilities;
using DemoContentLoader;
using DemoUtilities;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using System.IO;

namespace Demos;

class Program
{
    static void Main()
    {
        MonitorInfo primaryMonitor = Monitors.GetPrimaryMonitor();
        Box2i videoMode = primaryMonitor.ClientArea;

        Window window = new(
            "pretty cool multicolored window",
            new Int2((int)(videoMode.Size.X * 0.75f), (int)(videoMode.Size.Y * 0.75f)),
            WindowMode.Windowed);

        GameLoop loop = new(window);
        ContentArchive content;
        using (Stream stream = typeof(Program).Assembly.GetManifestResourceStream("Demos.Demos.contentarchive"))
        {
            content = ContentArchive.Load(stream);
        }
        //HeadlessTest.Test<ShapePileTestDemo>(content, 4, 32, 512);
        DemoHarness demo = new(loop, content);
        loop.Run(demo);
        loop.Dispose();
        window.Dispose();
    }
}