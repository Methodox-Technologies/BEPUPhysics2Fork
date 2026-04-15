using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using DemoContentLoader;
using DemoRenderer;
using Demos.Demos;
using System;
using System.Numerics;

namespace Demos.SpecializedTests.Media;

/// <summary>
/// Shows a bundle of ropes being tangled up by spinning weights.
/// </summary>
public class RopeTwistVideoDemo : Demo
{
    public override void Initialize(ContentArchive content, Camera camera)
    {
        camera.Position = new Vector3(0, 20, 20);
        camera.Yaw = 0;
        camera.Pitch = 0;

        CollidableProperty<RopeFilter> filters = new();
        Simulation = Simulation.Create(BufferPool,
            new RopeNarrowPhaseCallbacks(filters, new PairMaterialProperties(1.0f, float.MaxValue, new SpringSettings(1200, 1))),
            new DemoPoseIntegratorCallbacks(new Vector3(0, -10, 0)), new SolveDescription(1, 60));

        for (int twistIndex = 0; twistIndex < 10; ++twistIndex)
        {
            const int ropeCount = 4;
            Vector3 startLocation = new(0 + twistIndex * 30, 30, 0);

            Sphere bigWreckingBall = new(3);
            //This wrecking ball is much, much heavier.
            BodyInertia bigWreckingBallInertia = bigWreckingBall.ComputeInertia(10000);
            TypedIndex bigWreckingBallIndex = Simulation.Shapes.Add(bigWreckingBall);
            const float ropeBodySpacing = -0.1f;
            const float ropeBodyRadius = 0.1f;
            const int ropeBodyCount = 130;
            Vector3 wreckingBallPosition = startLocation - new Vector3(0, ropeBodyRadius + (ropeBodyRadius * 2 + ropeBodySpacing) * ropeBodyCount + bigWreckingBall.Radius, 0);
            BodyDescription description = BodyDescription.CreateDynamic(wreckingBallPosition, bigWreckingBallInertia, bigWreckingBallIndex, 0.01f);
            BodyHandle wreckingBallBodyHandle = Simulation.Bodies.Add(description);
            BodyReference wreckingBallBody = Simulation.Bodies[wreckingBallBodyHandle];
            wreckingBallBody.Velocity.Angular = new Vector3(0, 20, 0);
            filters.Allocate(wreckingBallBodyHandle) = new RopeFilter { RopeIndex = (short)(16384 + twistIndex), IndexInRope = ropeBodyCount };

            for (int ropeIndex = 0; ropeIndex < ropeCount; ++ropeIndex)
            {
                var angle = ropeIndex * MathF.PI * 2 / ropeCount;
                const float ropeDistributionRadius = 1f;
                Vector3 horizontalOffset = ropeDistributionRadius * new Vector3(MathF.Sin(angle), 0, MathF.Cos(angle));
                Vector3 ropeStartLocation = startLocation + horizontalOffset;

                SpringSettings springSettings = new(600, 100);
                BodyHandle[] bodyHandles = RopeStabilityDemo.BuildRopeBodies(Simulation, ropeStartLocation, ropeBodyCount, ropeBodyRadius, ropeBodySpacing, 1f, 0);
                for (int i = 0; i < bodyHandles.Length; ++i)
                {
                    filters.Allocate(bodyHandles[i]) = new RopeFilter { RopeIndex = (short)ropeIndex, IndexInRope = (short)i };
                }

                bool TryCreateConstraint(int handleIndexA, int handleIndexB)
                {
                    if (handleIndexA >= bodyHandles.Length || handleIndexB >= bodyHandles.Length)
                        return false;
                    var maximumDistance = Vector3.Distance(
                        new BodyReference(bodyHandles[handleIndexA], Simulation.Bodies).Pose.Position,
                        new BodyReference(bodyHandles[handleIndexB], Simulation.Bodies).Pose.Position);
                    Simulation.Solver.Add(bodyHandles[handleIndexA], bodyHandles[handleIndexB], new DistanceLimit(default, default, .01f, maximumDistance, springSettings));
                    return true;
                }
                const int constraintsPerBody = 1;
                for (int i = 0; i < bodyHandles.Length - 1; ++i)
                {
                    //Note that you could also create constraints which span even more links. For example, connect i and i+1, i+2, i+4, i+8 and i+16 rather than just the nearest bodies.
                    //That tends to make mass ratios less of an issue, but this demo is a worst case stress test.
                    for (int j = 1; j <= constraintsPerBody; ++j)
                    {
                        if (!TryCreateConstraint(i, i + j))
                            break;
                    }
                }

                Vector3 wreckingBallConnectionOffset = horizontalOffset + new Vector3(0, bigWreckingBall.Radius, 0);
                Vector3 ropeConnectionToBall = wreckingBallBody.Pose.Position + wreckingBallConnectionOffset;
                for (int i = 1; i <= constraintsPerBody; ++i)
                {
                    var targetBodyHandleIndex = bodyHandles.Length - i;
                    if (targetBodyHandleIndex < 0)
                        break;
                    var maximumDistance = Vector3.Distance(
                        new BodyReference(bodyHandles[targetBodyHandleIndex], Simulation.Bodies).Pose.Position,
                        ropeConnectionToBall);
                    Simulation.Solver.Add(bodyHandles[targetBodyHandleIndex], wreckingBallBodyHandle, new DistanceLimit(default, wreckingBallConnectionOffset, 0.01f, maximumDistance, springSettings));
                }

            }
        }

        Console.WriteLine($"body count: {Simulation.Bodies.ActiveSet.Count}");

        Simulation.Statics.Add(new StaticDescription(new Vector3(0, 0, 0), Simulation.Shapes.Add(new Box(200, 1, 200))));
    }
}
