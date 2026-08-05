using BEPU.DemoUtilities;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BEPUPhysics.OpenGLDemos.Types;
using BepuUtilities;
using BepuUtilities.Memory;
using DemoContentLoader;
using DemoRenderer;
using DemoRenderer.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace BEPUPhysics.OpenGLDemos
{
    struct DeformableSolidCollisionFilter
    {
        ushort x;
        ushort y;
        ushort z;
        int instanceId;

        public DeformableSolidCollisionFilter(int x, int y, int z, int instanceId)
        {
            Debug.Assert(x >= 0 && x < ushort.MaxValue);
            Debug.Assert(y >= 0 && y < ushort.MaxValue);
            Debug.Assert(z >= 0 && z < ushort.MaxValue);

            this.x = (ushort)x;
            this.y = (ushort)y;
            this.z = (ushort)z;
            this.instanceId = instanceId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Test(in DeformableSolidCollisionFilter a, in DeformableSolidCollisionFilter b, int minimumDistance)
        {
            if (a.instanceId != b.instanceId)
                return true;

            int differenceX = a.x - b.x;
            if (differenceX < -minimumDistance || differenceX > minimumDistance)
                return true;

            int differenceY = a.y - b.y;
            if (differenceY < -minimumDistance || differenceY > minimumDistance)
                return true;

            int differenceZ = a.z - b.z;
            if (differenceZ < -minimumDistance || differenceZ > minimumDistance)
                return true;

            return false;
        }
    }

    struct DeformableSolidCallbacks : INarrowPhaseCallbacks
    {
        public CollidableProperty<DeformableSolidCollisionFilter> Filters;
        public PairMaterialProperties Material;
        public int MinimumDistanceForSelfCollisions;

        public DeformableSolidCallbacks(CollidableProperty<DeformableSolidCollisionFilter> filters, int minimumDistanceForSelfCollisions = 2)
        {
            Filters = filters;
            MinimumDistanceForSelfCollisions = minimumDistanceForSelfCollisions;
            Material = new PairMaterialProperties
            {
                FrictionCoefficient = 0.65f,
                MaximumRecoveryVelocity = 1.5f,
                SpringSettings = new SpringSettings(25, 1.5f)
            };
        }

        public void Initialize(Simulation simulation)
        {
            Filters.Initialize(simulation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
        {
            if (a.Mobility != CollidableMobility.Static && b.Mobility != CollidableMobility.Static)
                return DeformableSolidCollisionFilter.Test(Filters[a.BodyHandle], Filters[b.BodyHandle], MinimumDistanceForSelfCollisions);

            return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
        {
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
        {
            pairMaterial = Material;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    /// Demonstrates a volumetric Lagrangian deformable solid descending a staircase.
    /// The solid is represented by material-point bodies connected by elastic edge and tetrahedral volume constraints.
    /// </summary>
    /// <remarks>
    /// The unbreakable spring constraint creates a solid jello like effect.
    /// </remarks>
    public class LagrangianDeformableStairsDemo : DemoBase
    {
        BodyHandle[,,] CreateParticleGrid(Vector3 origin, int width, int height, int depth, float spacing, float radius, float massPerParticle,
            int instanceId, CollidableProperty<DeformableSolidCollisionFilter> filters)
        {
            Sphere particleShape = new(radius);
            TypedIndex shape = Simulation.Shapes.Add(particleShape);
            BodyInertia inertia = particleShape.ComputeInertia(massPerParticle);

            BodyDescription description = BodyDescription.CreateDynamic(Quaternion.Identity, inertia, shape, 0.01f);
            description.Velocity.Linear = new Vector3(0.55f, 0, 0); // A forward launch

            BodyHandle[,,] handles = new BodyHandle[width, height, depth];

            Vector3 centeringOffset = new(
                (width - 1) * spacing * 0.5f,
                (height - 1) * spacing * 0.5f,
                (depth - 1) * spacing * 0.5f);

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    for (int z = 0; z < depth; ++z)
                    {
                        description.Pose.Position = origin + new Vector3(x * spacing, y * spacing, z * spacing) - centeringOffset;

                        BodyHandle handle = Simulation.Bodies.Add(description);
                        handles[x, y, z] = handle;
                        filters.Allocate(handle) = new DeformableSolidCollisionFilter(x, y, z, instanceId);
                    }
                }
            }

            return handles;
        }
        void AddDistanceConstraint_StillJello(BodyHandle aHandle, BodyHandle bHandle, SpringSettings springSettings)
        {
            Vector3 a = Simulation.Bodies[aHandle].Pose.Position;
            Vector3 b = Simulation.Bodies[bHandle].Pose.Position;
            float distance = Vector3.Distance(a, b);

            Simulation.Solver.Add(
                aHandle,
                bHandle,
                new CenterDistanceLimit(distance * 0.3f, distance, springSettings));
        }
        void AddDistanceConstraint_Jelly(BodyHandle aHandle, BodyHandle bHandle, SpringSettings springSettings)
        {
            Vector3 a = Simulation.Bodies[aHandle].Pose.Position;
            Vector3 b = Simulation.Bodies[bHandle].Pose.Position;
            Simulation.Solver.Add(aHandle, bHandle, new CenterDistanceConstraint(Vector3.Distance(a, b), springSettings));
        }
        void CreateElasticConstraints(BodyHandle[,,] handles, SpringSettings springSettings)
        {
            int width = handles.GetLength(0);
            int height = handles.GetLength(1);
            int depth = handles.GetLength(2);

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    for (int z = 0; z < depth; ++z)
                    {
                        BodyHandle handle = handles[x, y, z];

                        if (x + 1 < width)
                            AddDistanceConstraint_StillJello(handle, handles[x + 1, y, z], springSettings);

                        if (y + 1 < height)
                            AddDistanceConstraint_StillJello(handle, handles[x, y + 1, z], springSettings);

                        if (z + 1 < depth)
                            AddDistanceConstraint_StillJello(handle, handles[x, y, z + 1], springSettings);
                    }
                }
            }
        }
        void AddVolumeConstraint(BodyHandle aHandle, BodyHandle bHandle, BodyHandle cHandle, BodyHandle dHandle, SpringSettings springSettings)
        {
            Vector3 a = Simulation.Bodies[aHandle].Pose.Position;
            Vector3 b = Simulation.Bodies[bHandle].Pose.Position;
            Vector3 c = Simulation.Bodies[cHandle].Pose.Position;
            Vector3 d = Simulation.Bodies[dHandle].Pose.Position;

            Simulation.Solver.Add(aHandle, bHandle, cHandle, dHandle, new VolumeConstraint(a, b, c, d, springSettings));
        }

        void CreateVolumeConstraints(BodyHandle[,,] handles, SpringSettings volumeSpringSettings)
        {
            int width = handles.GetLength(0);
            int height = handles.GetLength(1);
            int depth = handles.GetLength(2);

            for (int x = 0; x < width - 1; ++x)
            {
                for (int y = 0; y < height - 1; ++y)
                {
                    for (int z = 0; z < depth - 1; ++z)
                    {
                        BodyHandle v000 = handles[x, y, z];
                        BodyHandle v100 = handles[x + 1, y, z];
                        BodyHandle v010 = handles[x, y + 1, z];
                        BodyHandle v110 = handles[x + 1, y + 1, z];
                        BodyHandle v001 = handles[x, y, z + 1];
                        BodyHandle v101 = handles[x + 1, y, z + 1];
                        BodyHandle v011 = handles[x, y + 1, z + 1];
                        BodyHandle v111 = handles[x + 1, y + 1, z + 1];

                        //Six consistently wound tetrahedra sharing the v000-v111 diagonal.
                        AddVolumeConstraint(v000, v100, v110, v111, volumeSpringSettings);
                        AddVolumeConstraint(v000, v110, v010, v111, volumeSpringSettings);
                        AddVolumeConstraint(v000, v010, v011, v111, volumeSpringSettings);
                        AddVolumeConstraint(v000, v011, v001, v111, volumeSpringSettings);
                        AddVolumeConstraint(v000, v001, v101, v111, volumeSpringSettings);
                        AddVolumeConstraint(v000, v101, v100, v111, volumeSpringSettings);
                    }
                }
            }
        }
        void CreateStairs()
        {
            const int stepCount = 72;
            const int stepsBeforeOrigin = 10;
            const float stepWidth = 0.72f;
            const float stepHeight = 0.24f;
            const float stairDepth = 10;
            const float firstStepTop = 8;
            const float treadThickness = 0.3f;

            TypedIndex stepShape = Simulation.Shapes.Add(new Box(stepWidth, treadThickness, stairDepth));

            const float baseY = -14;

            for (int stepIndex = -stepsBeforeOrigin; stepIndex < stepCount; ++stepIndex)
            {
                float topY = firstStepTop - stepIndex * stepHeight;
                float centerX = stepIndex * stepWidth;
                float height = topY - baseY;

                Simulation.Statics.Add(new StaticDescription(
                    new Vector3(centerX, baseY + height * 0.5f, 0),
                    Simulation.Shapes.Add(new Box(stepWidth + 0.02f, height, stairDepth))));
            }

            const float topLandingWidth = 8;
            float topLandingEndX = -stepsBeforeOrigin * stepWidth;

            Simulation.Statics.Add(new StaticDescription(
                new Vector3(topLandingEndX - topLandingWidth * 0.5f, firstStepTop + stepsBeforeOrigin * stepHeight - treadThickness * 0.5f, 0),
                Simulation.Shapes.Add(new Box(topLandingWidth, treadThickness, stairDepth))));

            const float bottomLandingWidth = 32;
            float bottomLandingTop = firstStepTop - stepCount * stepHeight;
            float bottomLandingStartX = stepCount * stepWidth;

            Simulation.Statics.Add(new StaticDescription(
                new Vector3(bottomLandingStartX + bottomLandingWidth * 0.5f, baseY + (bottomLandingTop - baseY) * 0.5f, 0),
                Simulation.Shapes.Add(new Box(bottomLandingWidth, bottomLandingTop - baseY, stairDepth))));

            //A shallow retaining curb at the far end of the landing.
            Simulation.Statics.Add(new StaticDescription(
                new Vector3(bottomLandingStartX + bottomLandingWidth, bottomLandingTop + 0.3f, 0),
                Simulation.Shapes.Add(new Box(0.5f, 0.6f, stairDepth))));
        }
        public override void Initialize(ContentArchive content, Camera camera)
        {
            camera.Position = new Vector3(20, 11, 28);
            camera.Yaw = 0;
            camera.Pitch = -0.2f;

            CollidableProperty<DeformableSolidCollisionFilter> filters = new();

            Simulation = Simulation.Create(
                BufferPool,
                new DeformableSolidCallbacks(filters, 2),
                new DemoPoseIntegratorCallbacks(new Vector3(0, -10, 0), 0.001f, 0.005f),
                new SolveDescription(4, 12));

            CreateStairs();

            const float spacing = 0.28f;
            BodyHandle[,,] solid = CreateParticleGrid(new Vector3(-4.5f, 14.5f, 0), 19, 13, 13, spacing, 0.155f, 0.0075f, 0, filters);

            // Low frequency allows visible deformation. The high damping ratio creates Kelvin-Voigt-like internal viscosity along the material links.
            // Volume preservation is stronger than shear/stretch resistance, but still soft.
            // Jello
            //CreateElasticConstraints(solid, new SpringSettings(2.8f, 2.2f));
            //CreateVolumeConstraints(solid, new SpringSettings(5.5f, 1.8f));
            // Slimy Sauce
            CreateElasticConstraints(solid, new SpringSettings(2.8f, 2.2f));
            CreateVolumeConstraints(solid, new SpringSettings(5.5f, 1.8f));
        }
        public override void Render(Renderer renderer, Camera camera, Input input, TextBuilder text, Font font)
        {
            Int2 resolution = renderer.Surface.Resolution;

            renderer.TextBatcher.Write(
                text.Clear().Append("Lagrangian deformable solid: particle material points, elastic edges and tetrahedral volume constraints."),
                new Vector2(16, resolution.Y - 32),
                16,
                Vector3.One,
                font);

            renderer.TextBatcher.Write(
                text.Clear().Append("The solid begins with forward momentum and deforms as it descends the staircase."),
                new Vector2(16, resolution.Y - 16),
                16,
                Vector3.One,
                font);

            base.Render(renderer, camera, input, text, font);
        }
    }
}
