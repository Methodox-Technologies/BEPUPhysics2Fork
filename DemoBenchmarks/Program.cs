using BenchmarkDotNet.Running;

public class BepuPhysics2Benchmarks
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(BepuPhysics2Benchmarks).Assembly).Run(args);
    }
}
