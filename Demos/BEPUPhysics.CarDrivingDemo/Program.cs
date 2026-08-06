using BepuUtilities;
using BEPU.DemoUtilities;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using Gaunt.Car.Placeholder;

namespace BEPUPhysics.CarDrivingDemo
{
    class Program
    {
        static void Main()
        {
            MonitorInfo primaryMonitor = Monitors.GetPrimaryMonitor();
            Box2i videoMode = primaryMonitor.ClientArea;

            Window window = new("Cityroad Racer", new Int2((int)(videoMode.Size.X * 0.75f), (int)(videoMode.Size.Y * 0.75f)), WindowMode.Windowed);

            GameLoop loop = new(window);
            DemoHost demo = new(loop);
            loop.Run(demo);
            loop.Dispose();
            window.Dispose();
        }
    }
}