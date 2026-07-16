using BepuUtilities;
using DemoRenderer;
using BepuPhysics;
using BepuPhysics.Collidables;
using System.Numerics;
using System;
using DemoContentLoader;
using Demos.Demos;
using DemoUtilities;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using DemoRenderer.UI;
using BepuUtilities.Memory;

namespace Demos.SpecializedTests.Media;

/// <summary>
/// Subjects a bunch of unfortunate ragdolls to a tumble dry cycle.
/// </summary>
public class RagdollTubeVideoDemo : Demo
{
    public override void Initialize(ContentArchive content, Camera camera)
    {
        camera.Position = new Vector3(0, 9, -40);
        camera.Yaw = MathHelper.Pi;
        camera.Pitch = 0;
        CollidableProperty<SubgroupCollisionFilter> filters = new();
        //Note the lowered material stiffness compared to many of the other demos. Ragdolls aren't made of concrete.
        //Increasing the maximum recovery velocity helps keep deeper contacts strong, stopping objects from interpenetrating.
        //Higher friction helps the bodies clump and flop, rather than just sliding down the slope in the tube.
        Simulation = Simulation.Create(BufferPool, new SubgroupFilteredCallbacks(filters, new PairMaterialProperties(2, float.MaxValue, new SpringSettings(10, 1))), new DemoPoseIntegratorCallbacks(new Vector3(0, -10, 0)), new SolveDescription(4, 1));

        int ragdollIndex = 0;
        Vector3 spacing = new(1.7f, 1.8f, 0.5f);
        int width = 4;
        int height = 4;
        int length = 120;
        Vector3 origin = -0.5f * spacing * new Vector3(width - 1, 0, length - 1) + new Vector3(0, 5f, 0);
        for (int i = 0; i < width; ++i)
        {
            for (int j = 0; j < height; ++j)
            {
                for (int k = 0; k < length; ++k)
                {
                    RagdollDemo.AddRagdoll(origin + spacing * new Vector3(i, j, k), QuaternionEx.CreateFromAxisAngle(new Vector3(0, 1, 0), MathHelper.Pi * 0.05f), ragdollIndex++, filters, Simulation);
                }
            }
        }

        ragdollCount = ragdollIndex;
        ragdollBodyCount = Simulation.Bodies.ActiveSet.Count;
        ragdollConstraintCount = Simulation.Solver.CountConstraints();

        Vector3 tubeCenter = new(0, 8, 0);
        const int panelCount = 20;
        const float tubeRadius = 6;
        Box panelShape = new(MathF.PI * 2 * tubeRadius / panelCount, 1, 100);
        TypedIndex panelShapeIndex = Simulation.Shapes.Add(panelShape);
        CompoundBuilder builder = new(BufferPool, Simulation.Shapes, panelCount + 1);
        for (int i = 0; i < panelCount; ++i)
        {
            Quaternion rotation = QuaternionEx.CreateFromAxisAngle(Vector3.UnitZ, i * MathHelper.TwoPi / panelCount);
            QuaternionEx.TransformUnitY(rotation, out Vector3 localUp);
            Vector3 position = localUp * tubeRadius;
            builder.AddForKinematic(panelShapeIndex, (position, rotation), 1);
        }
        builder.AddForKinematic(Simulation.Shapes.Add(new Box(1, 2, panelShape.Length)), new Vector3(0, tubeRadius - 1, 0), 0);
        builder.BuildKinematicCompound(out Buffer<CompoundChild> children);
        BigCompound compound = new(children, Simulation.Shapes, BufferPool);
        BodyHandle tubeHandle = Simulation.Bodies.Add(BodyDescription.CreateKinematic(tubeCenter, (default, new Vector3(0, 0, .25f)), Simulation.Shapes.Add(compound), 0f));
        filters[tubeHandle] = new SubgroupCollisionFilter(int.MaxValue);
        builder.Dispose();

        Box staticShape = new(300, 1, 300);
        TypedIndex staticShapeIndex = Simulation.Shapes.Add(staticShape);
        StaticDescription staticDescription = new(new Vector3(0, -0.5f, 0), staticShapeIndex);
        Simulation.Statics.Add(staticDescription);

        Mesh newtMesh = DemoMeshHelper.LoadModel(content, BufferPool, @"Content\newt.obj", new Vector3(15, 15, 15));
        Simulation.Statics.Add(new StaticDescription(new Vector3(0, 0.5f, 80), Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI), Simulation.Shapes.Add(newtMesh)));

    }
    int ragdollBodyCount;
    int ragdollConstraintCount;
    int ragdollCount;

    public override void Render(Renderer renderer, Camera camera, Input input, TextBuilder text, Font font)
    {
        Int2 resolution = renderer.Surface.Resolution;
        renderer.TextBatcher.Write(text.Clear().Append("Ragdoll count:"), new Vector2(16, resolution.Y - 64), 16, Vector3.One, font);
        renderer.TextBatcher.Write(text.Clear().Append("Ragdoll body count:"), new Vector2(16, resolution.Y - 48), 16, Vector3.One, font);
        renderer.TextBatcher.Write(text.Clear().Append("Ragdoll constraint count:"), new Vector2(16, resolution.Y - 32), 16, Vector3.One, font);
        renderer.TextBatcher.Write(text.Clear().Append("Collision constraint count:"), new Vector2(16, resolution.Y - 16), 16, Vector3.One, font);
        const float xOffset = 192;
        renderer.TextBatcher.Write(text.Clear().Append(ragdollCount), new Vector2(xOffset, resolution.Y - 64), 16, Vector3.One, font);
        renderer.TextBatcher.Write(text.Clear().Append(ragdollBodyCount), new Vector2(xOffset, resolution.Y - 48), 16, Vector3.One, font);
        renderer.TextBatcher.Write(text.Clear().Append(ragdollConstraintCount), new Vector2(xOffset, resolution.Y - 32), 16, Vector3.One, font);
        var collisionConstraintCount = Simulation.Solver.CountConstraints() - ragdollConstraintCount;
        renderer.TextBatcher.Write(text.Clear().Append(collisionConstraintCount), new Vector2(xOffset, resolution.Y - 16), 16, Vector3.One, font);
        base.Render(renderer, camera, input, text, font);
    }
}


