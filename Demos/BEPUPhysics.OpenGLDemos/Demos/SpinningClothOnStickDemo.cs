using BEPU.DemoUtilities;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BEPUPhysics.OpenGLDemos.Types;
using BepuUtilities;
using DemoContentLoader;
using DemoRenderer;
using DemoRenderer.UI;
using System;
using System.Numerics;

namespace BEPUPhysics.OpenGLDemos.Demos
{
    /// <summary>
    /// No self collision, use in place of <see cref="ClothCallbacks"/>.
    /// Looks bad - currently not used.
    /// </summary> 
    struct SpinningClothCallbacks : INarrowPhaseCallbacks
    {
        public CollidableProperty<ClothCollisionFilter> Filters;
        public PairMaterialProperties Material;

        public SpinningClothCallbacks(CollidableProperty<ClothCollisionFilter> filters, PairMaterialProperties material)
        {
            Filters = filters;
            Material = material;
        }

        public void Initialize(Simulation simulation)
        {
            Filters.Initialize(simulation);
        }

        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
        {
            //Disable self-collision within the spinning cloth.
            if (a.Mobility != CollidableMobility.Static && b.Mobility != CollidableMobility.Static)
                return false;

            return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
        }

        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            return true;
        }

        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial = Material;
            return true;
        }

        public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
        {
            return true;
        }

        public void Dispose()
        {
            Filters.Dispose();
        }
    }

    /// <summary>
    /// Shows a single horizontal cloth layer spinning from a driven hub on top of a vertical stick.
    /// </summary>
    public class SpinningClothOnStickDemo : DemoBase
    {
        const int ClothWidth = 41;
        const int ClothDepth = 41;
        const float ClothSpacing = 0.35f;
        const float ClothHeight = 10;
        /// <summary>
        /// Raise AngularSpeed toward 4 for stronger outward flaring.
        /// </summary>
        const float AngularSpeed = 2.5f;

        BodyHandle[,] clothNodes;
        Vector3[,] anchorLocalOffsets;
        bool[,] anchorMask;
        float hubAngle;
        float elapsedTime;

        BodyHandle[,] CreateCloth(CollidableProperty<ClothCollisionFilter> filters)
        {
            const float particleRadius = 0.16f;
            const float particleMass = 0.08f;

            Sphere particleShape = new(particleRadius);
            TypedIndex particleShapeIndex = Simulation.Shapes.Add(particleShape);
            BodyInertia particleInertia = particleShape.ComputeInertia(particleMass);

            BodyHandle[,] handles = new BodyHandle[ClothWidth, ClothDepth];
            anchorLocalOffsets = new Vector3[ClothWidth, ClothDepth];
            anchorMask = new bool[ClothWidth, ClothDepth];

            float halfWidth = (ClothWidth - 1) * ClothSpacing * 0.5f;
            float halfDepth = (ClothDepth - 1) * ClothSpacing * 0.5f;
            int centerX = ClothWidth / 2;
            int centerZ = ClothDepth / 2;

            for (int x = 0; x < ClothWidth; ++x)
            {
                for (int z = 0; z < ClothDepth; ++z)
                {
                    const float hubAnchorRadius = 1.4f;

                    Vector3 localOffset = new Vector3(x * ClothSpacing - halfWidth, 0, z * ClothSpacing - halfDepth);
                    bool anchor = localOffset.X * localOffset.X + localOffset.Z * localOffset.Z <= hubAnchorRadius * hubAnchorRadius;

                    BodyDescription description = BodyDescription.CreateDynamic(Quaternion.Identity, particleInertia, particleShapeIndex, 0.01f);
                    description.Pose.Position = new Vector3(0, ClothHeight, 0) + localOffset;
                    description.Velocity.Linear = Vector3.Zero;

                    if (anchor)
                        description.LocalInertia = default;

                    BodyHandle handle = Simulation.Bodies.Add(description);
                    handles[x, z] = handle;
                    anchorMask[x, z] = anchor;
                    anchorLocalOffsets[x, z] = localOffset;
                    filters.Allocate(handle) = new ClothCollisionFilter(x, z, 0);
                }
            }

            return handles;
        }

        void AddDistanceConstraint(BodyHandle aHandle, BodyHandle bHandle, SpringSettings springSettings)
        {
            BodyReference a = Simulation.Bodies[aHandle];
            BodyReference b = Simulation.Bodies[bHandle];

            //Constraints between two kinematic anchor particles have no dynamic degrees of freedom.
            if (a.LocalInertia.InverseMass == 0 && b.LocalInertia.InverseMass == 0)
                return;

            float distance = Vector3.Distance(a.Pose.Position, b.Pose.Position);
            Simulation.Solver.Add(aHandle, bHandle, new CenterDistanceLimit(distance * 0.2f, distance, springSettings));
        }

        void AddAreaConstraint(BodyHandle aHandle, BodyHandle bHandle, BodyHandle cHandle, SpringSettings springSettings)
        {
            BodyReference a = Simulation.Bodies[aHandle];
            BodyReference b = Simulation.Bodies[bHandle];
            BodyReference c = Simulation.Bodies[cHandle];

            //An all-kinematic constraint cannot affect any body.
            if (a.LocalInertia.InverseMass == 0 && b.LocalInertia.InverseMass == 0 && c.LocalInertia.InverseMass == 0)
                return;

            Simulation.Solver.Add(aHandle, bHandle, cHandle, new AreaConstraint(a.Pose.Position, b.Pose.Position, c.Pose.Position, springSettings));
        }

        void CreateClothConstraints()
        {
            // Lower `distanceSpring` and `areaSpring` for more folding and lag.
            SpringSettings distanceSpring = new(6, 1);
            SpringSettings areaSpring = new(8, 1);

            for (int x = 0; x < ClothWidth - 1; ++x)
            {
                for (int z = 0; z < ClothDepth; ++z)
                {
                    AddDistanceConstraint(clothNodes[x, z], clothNodes[x + 1, z], distanceSpring);
                }
            }

            for (int x = 0; x < ClothWidth; ++x)
            {
                for (int z = 0; z < ClothDepth - 1; ++z)
                {
                    AddDistanceConstraint(clothNodes[x, z], clothNodes[x, z + 1], distanceSpring);
                }
            }

            for (int x = 0; x < ClothWidth - 1; ++x)
            {
                for (int z = 0; z < ClothDepth - 1; ++z)
                {
                    AddDistanceConstraint(clothNodes[x, z], clothNodes[x + 1, z + 1], distanceSpring);
                    AddDistanceConstraint(clothNodes[x + 1, z], clothNodes[x, z + 1], distanceSpring);

                    AddAreaConstraint(clothNodes[x, z], clothNodes[x + 1, z], clothNodes[x, z + 1], areaSpring);
                    AddAreaConstraint(clothNodes[x + 1, z], clothNodes[x + 1, z + 1], clothNodes[x, z + 1], areaSpring);
                }
            }
        }

        void CreateStick()
        {
            const float stickRadius = 0.45f;
            const float stickLength = 9.5f;
            const float hubRadius = 1.1f;
            const float hubHeight = 0.35f;

            //The capsule is aligned with its local Y axis, placing its upper end immediately below the cloth hub.
            Simulation.Statics.Add(new StaticDescription(new Vector3(0, stickLength * 0.5f, 0), Simulation.Shapes.Add(new Capsule(stickRadius, stickLength))));

            //The top disk provides a visible and collidable support under the driven cloth center.
            Simulation.Statics.Add(new StaticDescription(new Vector3(0, ClothHeight - hubHeight * 0.5f - 0.25f, 0), Simulation.Shapes.Add(new Cylinder(hubRadius, hubHeight))));

            //Ground catches the cloth if the constraints are weakened enough for it to detach or collapse.
            Simulation.Statics.Add(new StaticDescription(new Vector3(0, -0.5f, 0), Simulation.Shapes.Add(new Box(40, 1, 40))));
        }
        void UpdateDrivenHub()
        {
            elapsedTime += TimestepDuration;

            // Long periods, higher peak speed and smooth direction reversals.
            float angularSpeed = 1.15f * MathF.Sin(elapsedTime * 0.11f) + 0.38f * MathF.Sin(elapsedTime * 0.29f);
            float nextHubAngle = hubAngle + angularSpeed * TimestepDuration;

            Quaternion currentRotation = QuaternionEx.CreateFromAxisAngle(Vector3.UnitY, hubAngle);
            Quaternion nextRotation = QuaternionEx.CreateFromAxisAngle(Vector3.UnitY, nextHubAngle);
            Vector3 hubPosition = new(0, ClothHeight, 0);

            for (int x = 0; x < ClothWidth; ++x)
            {
                for (int z = 0; z < ClothDepth; ++z)
                {
                    if (!anchorMask[x, z])
                        continue;

                    QuaternionEx.TransformWithoutOverlap(anchorLocalOffsets[x, z], currentRotation, out Vector3 currentOffset);
                    QuaternionEx.TransformWithoutOverlap(anchorLocalOffsets[x, z], nextRotation, out Vector3 nextOffset);

                    BodyReference anchor = Simulation.Bodies[clothNodes[x, z]];
                    Vector3 currentTarget = hubPosition + currentOffset;
                    Vector3 nextTarget = hubPosition + nextOffset;

                    //Correct accumulated numerical drift gradually rather than teleporting the anchor.
                    Vector3 positionError = currentTarget - anchor.Pose.Position;
                    anchor.Velocity.Linear = (nextTarget - currentTarget) / TimestepDuration + positionError * 8;
                    anchor.Velocity.Angular = new Vector3(0, angularSpeed, 0);
                    anchor.Activity.TimestepsUnderThresholdCount = 0;
                }
            }

            hubAngle = nextHubAngle;
        }
        public override void Initialize(ContentArchive content, Camera camera)
        {
            camera.Position = new Vector3(0, 14, 24);
            camera.Yaw = 0;
            camera.Pitch = -0.28f;

            CollidableProperty<ClothCollisionFilter> filters = new();
            PairMaterialProperties material = new()
            {
                FrictionCoefficient = 0.35f,
                MaximumRecoveryVelocity = 0.5f,
                SpringSettings = new SpringSettings(20, 1)
            };

            Simulation = Simulation.Create(BufferPool, new ClothCallbacks(filters, material), new DemoPoseIntegratorCallbacks(new Vector3(0, -10, 0), 0.01f, 0.04f), new SolveDescription(8, 8));

            CreateStick();
            clothNodes = CreateCloth(filters);
            CreateClothConstraints();
        }

        public override void Update(Window window, Camera camera, Input input, float dt)
        {
            UpdateDrivenHub();
            base.Update(window, camera, input, dt);
        }

        public override void Render(Renderer renderer, Camera camera, Input input, TextBuilder text, Font font)
        {
            Int2 resolution = renderer.Surface.Resolution;

            renderer.TextBatcher.Write(text.Clear().Append("A single horizontal cloth layer is driven by a rotating kinematic hub on top of a stick."), new Vector2(16, resolution.Y - 32), 16, Vector3.One, font);
            renderer.TextBatcher.Write(text.Clear().Append("Centrifugal motion stretches and lifts the cloth while gravity and flexible constraints produce waves and folding."), new Vector2(16, resolution.Y - 16), 16, Vector3.One, font);

            base.Render(renderer, camera, input, text, font);
        }
    }
}
