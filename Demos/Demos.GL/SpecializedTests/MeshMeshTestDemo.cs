using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using DemoContentLoader;
using DemoRenderer;

namespace Demos.SpecializedTests;

/// <summary>
/// Shows how to be mean to the physics engine by using meshes as dynamic colliders. Why would someone be so cruel? Be nice to the physics engine, save your CPU some work.
/// </summary>
public class MeshMeshTestDemo : Demo
{
    public override void Initialize(ContentArchive content, Camera camera)
    {
        camera.Position = new Vector3(0, 8, -10);
        camera.Yaw = MathHelper.Pi;

        Simulation = Simulation.Create(BufferPool, new DemoNarrowPhaseCallbacks(new SpringSettings(30, 1)), new DemoPoseIntegratorCallbacks(new Vector3(0, -10, 0)), new SolveDescription(8, 1));

        Mesh mesh = DemoMeshHelper.LoadModel(content, BufferPool, @"Content\newt.obj", Vector3.One);
        BodyInertia approximateInertia = new Box(2.5f, 1, 4).ComputeInertia(1);
        TypedIndex meshShapeIndex = Simulation.Shapes.Add(mesh);
        for (int meshIndex = 0; meshIndex < 3; ++meshIndex)
        {
            Simulation.Bodies.Add(
                BodyDescription.CreateDynamic(new Vector3(0, 2 + meshIndex * 2, 0), approximateInertia, meshShapeIndex, 0.01f));
        }

        CompoundBuilder compoundBuilder = new(BufferPool, Simulation.Shapes, 12);
        for (int i = 0; i < mesh.Triangles.Length; ++i)
        {
            compoundBuilder.Add(mesh.Triangles[i], RigidPose.Identity, 1);
        }
        compoundBuilder.BuildDynamicCompound(out Buffer<CompoundChild> children, out BodyInertia compoundInertia);
        BigCompound compound = new(children, Simulation.Shapes, BufferPool);
        TypedIndex compoundShapeIndex = Simulation.Shapes.Add(compound);
        compoundBuilder.Dispose();
        for (int i = 0; i < 3; ++i)
        {
            Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(5, 2 + i * 2, 0), compoundInertia, compoundShapeIndex, 0.01f));
        }

        Box staticShape = new(1500, 1, 1500);
        TypedIndex staticShapeIndex = Simulation.Shapes.Add(staticShape);

        Simulation.Statics.Add(new StaticDescription(new Vector3(0, -0.5f, 0), staticShapeIndex));
    }
}
