using BepuUtilities;
using DemoContentLoader;
using DemoRenderer;
using System;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using System.Diagnostics;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;

namespace Demos.SpecializedTests;

public class MeshSerializationTestDemo : Demo
{
    public override void Initialize(ContentArchive content, Camera camera)
    {
        camera.Position = new Vector3(-30, 8, -60);
        camera.Yaw = MathHelper.Pi * 3f / 4;
        camera.Pitch = 0;

        Simulation = Simulation.Create(BufferPool, new DemoNarrowPhaseCallbacks(new SpringSettings(30, 1)), new DemoPoseIntegratorCallbacks(new Vector3(0, -10, 0)), new SolveDescription(8, 1));

        var startTime = Stopwatch.GetTimestamp();
        Mesh originalMesh = DemoMeshHelper.CreateDeformedPlane(1025, 1025, (x, y) => new Vector3(x * 0.125f, MathF.Sin(x) + MathF.Sin(y), y * 0.125f), Vector3.One, BufferPool);
        Simulation.Statics.Add(new StaticDescription(new Vector3(0, 0, 0), Simulation.Shapes.Add(originalMesh)));
        var endTime = Stopwatch.GetTimestamp();
        var freshConstructionTime = (endTime - startTime) / (double)Stopwatch.Frequency;
        Console.WriteLine($"Fresh construction time (ms): {freshConstructionTime * 1e3}");

        BufferPool.Take<byte>(originalMesh.GetSerializedByteCount(), out Buffer<byte> serializedMeshBytes);
        originalMesh.Serialize(serializedMeshBytes);
        startTime = Stopwatch.GetTimestamp();
        Mesh loadedMesh = new(serializedMeshBytes, BufferPool);
        endTime = Stopwatch.GetTimestamp();
        var loadTime = (endTime - startTime) / (double)Stopwatch.Frequency;
        Console.WriteLine($"Load time (ms): {(endTime - startTime) * 1e3 / Stopwatch.Frequency}");
        Console.WriteLine($"Relative speedup: {freshConstructionTime / loadTime}");
        Simulation.Statics.Add(new StaticDescription(new Vector3(128, 0, 0), Simulation.Shapes.Add(loadedMesh)));


        BufferPool.Return(ref serializedMeshBytes);

        Random random = new(5);
        Box shapeToDrop = new(1, 1, 1);
        BodyDescription descriptionToDrop = BodyDescription.CreateDynamic(new Vector3(), shapeToDrop.ComputeInertia(1), Simulation.Shapes.Add(shapeToDrop), 0.01f);
        for (int i = 0; i < 1024; ++i)
        {
            descriptionToDrop.Pose.Position = new Vector3(8 + 240 * random.NextSingle(), 10 + 10 * random.NextSingle(), 8 + 112 * random.NextSingle());
            Simulation.Bodies.Add(descriptionToDrop);
        }

    }
}
