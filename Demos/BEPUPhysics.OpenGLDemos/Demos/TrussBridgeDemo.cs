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
    /// Shows a dynamically deforming pin-jointed truss bridge loaded by a heavy rolling sphere.
    /// </summary>
    /// <remarks>
    /// The spheres are only acting as truss joints and collision proxies. They are not meant to represent the steel members. A truss bridge should ideally use:
    /// Rigid capsule or box bodies for steel members.
    /// Pin-like constraints at member endpoints.
    /// Separate rigid deck panels for the rolling surface.
    /// </remarks>
    public class TrussBridgeDemo : DemoBase
    {
        CollidableProperty<SubgroupCollisionFilter> collisionFilters;

        const int SegmentCount = 28;
        const int DeckWidthNodeCount = 5;

        const float SegmentLength = 1.4f;
        const float BridgeWidth = 8;
        const float TrussHeight = 4;
        const float DeckHeight = 4;

        BodyHandle[,] deckNodes;
        BodyHandle[,] topNodes;

        public override void Initialize(ContentArchive content, Camera camera)
        {
            camera.Position = new Vector3(0, 19, 47);
            camera.Yaw = 0;
            camera.Pitch = -0.23f;

            collisionFilters = new CollidableProperty<SubgroupCollisionFilter>();

            PairMaterialProperties material = new(frictionCoefficient: 1.2f, maximumRecoveryVelocity: 2, springSettings: new SpringSettings(30, 1));
            Simulation = Simulation.Create(BufferPool, new SubgroupFilteredCallbacks(collisionFilters, material), new DemoPoseIntegratorCallbacks(new Vector3(0, -10, 0), 0.01f, 0.03f), new SolveDescription(12, 2));

            CreateBridge();
            CreateDeckPanels();
            CreateApproachRamps();
            CreateRollingBall();
        }

        public override void Render(Renderer renderer, Camera camera, Input input, TextBuilder text, Font font)
        {
            Int2 resolution = renderer.Surface.Resolution;

            renderer.TextBatcher.Write(text.Clear().Append("A heavy sphere progressively loads a deformable pin-jointed truss bridge."), new Vector2(16, resolution.Y - 32), 16, Vector3.One, font);
            renderer.TextBatcher.Write(text.Clear().Append("The side chords, alternating diagonals, deck lattice and lateral bracing are ordinary distance constraints."), new Vector2(16, resolution.Y - 16), 16, Vector3.One, font);

            base.Render(renderer, camera, input, text, font);
        }
        BodyHandle CreateNode(Vector3 position, TypedIndex shape, BodyInertia inertia, int segmentIndex, bool anchor)
        {
            BodyDescription description = BodyDescription.CreateDynamic(Quaternion.Identity, inertia, shape, 0.01f);
            description.Pose.Position = position;

            if (anchor)
                description.LocalInertia = default;

            BodyHandle handle = Simulation.Bodies.Add(description);

            //All bridge nodes use the same group with internal collision disabled.
            //The rolling ball uses another group, so it still collides with the bridge.
            collisionFilters.Allocate(handle) = new SubgroupCollisionFilter
            {
                GroupId = 0,
                SubgroupMembership = 1,
                CollidableSubgroups = 0
            };

            return handle;
        }
        void AddMember(BodyHandle aHandle, BodyHandle bHandle, SpringSettings springSettings)
        {
            BodyReference a = Simulation.Bodies[aHandle];
            BodyReference b = Simulation.Bodies[bHandle];

            //Constraints between two anchors have no dynamic degrees of freedom.
            if (a.LocalInertia.InverseMass == 0 && b.LocalInertia.InverseMass == 0)
                return;

            float targetLength = Vector3.Distance(a.Pose.Position, b.Pose.Position);
            Simulation.Solver.Add(aHandle, bHandle, new CenterDistanceConstraint(targetLength, springSettings));
        }
        void CreateBridge()
        {
            const float nodeRadius = 0.24f;
            const float nodeMass = 1.5f;

            Sphere nodeShape = new(nodeRadius);
            TypedIndex nodeShapeIndex = Simulation.Shapes.Add(nodeShape);
            BodyInertia nodeInertia = nodeShape.ComputeInertia(nodeMass);

            deckNodes = new BodyHandle[SegmentCount + 1, DeckWidthNodeCount];
            topNodes = new BodyHandle[SegmentCount + 1, 2];

            float bridgeLength = SegmentCount * SegmentLength;
            float startX = -bridgeLength * 0.5f;
            float deckWidthSpacing = BridgeWidth / (DeckWidthNodeCount - 1);

            for (int segmentIndex = 0; segmentIndex <= SegmentCount; ++segmentIndex)
            {
                float x = startX + segmentIndex * SegmentLength;
                bool anchor = IsAnchorNode(segmentIndex);

                for (int widthIndex = 0; widthIndex < DeckWidthNodeCount; ++widthIndex)
                {
                    float z = -BridgeWidth * 0.5f + widthIndex * deckWidthSpacing;
                    deckNodes[segmentIndex, widthIndex] = CreateNode(new Vector3(x, DeckHeight, z), nodeShapeIndex, nodeInertia, segmentIndex, anchor);
                }

                topNodes[segmentIndex, 0] = CreateNode(new Vector3(x, DeckHeight + TrussHeight, -BridgeWidth * 0.5f), nodeShapeIndex, nodeInertia, segmentIndex, anchor);
                topNodes[segmentIndex, 1] = CreateNode(new Vector3(x, DeckHeight + TrussHeight, BridgeWidth * 0.5f), nodeShapeIndex, nodeInertia, segmentIndex, anchor);
            }

            SpringSettings chordSpring = new(9, 1);
            SpringSettings braceSpring = new(7, 1);
            SpringSettings deckSpring = new(11, 1);
            SpringSettings lateralSpring = new(6, 1);

            for (int segmentIndex = 0; segmentIndex < SegmentCount; ++segmentIndex)
            {
                //Longitudinal deck members.
                for (int widthIndex = 0; widthIndex < DeckWidthNodeCount; ++widthIndex)
                {
                    AddMember(deckNodes[segmentIndex, widthIndex], deckNodes[segmentIndex + 1, widthIndex], deckSpring);
                }

                //Longitudinal upper chords.
                AddMember(topNodes[segmentIndex, 0], topNodes[segmentIndex + 1, 0], chordSpring);
                AddMember(topNodes[segmentIndex, 1], topNodes[segmentIndex + 1, 1], chordSpring);

                //Deck-plane diagonal bracing.
                for (int widthIndex = 0; widthIndex < DeckWidthNodeCount - 1; ++widthIndex)
                {
                    if ((segmentIndex & 1) == 0)
                    {
                        AddMember(deckNodes[segmentIndex, widthIndex], deckNodes[segmentIndex + 1, widthIndex + 1], lateralSpring);
                    }
                    else
                    {
                        AddMember(deckNodes[segmentIndex, widthIndex + 1], deckNodes[segmentIndex + 1, widthIndex], lateralSpring);
                    }
                }

                //Alternating side diagonals form the two trusses.
                for (int sideIndex = 0; sideIndex < 2; ++sideIndex)
                {
                    int deckSideIndex = sideIndex == 0 ? 0 : DeckWidthNodeCount - 1;

                    if ((segmentIndex & 1) == 0)
                    {
                        AddMember(deckNodes[segmentIndex, deckSideIndex], topNodes[segmentIndex + 1, sideIndex], braceSpring);
                    }
                    else
                    {
                        AddMember(topNodes[segmentIndex, sideIndex], deckNodes[segmentIndex + 1, deckSideIndex], braceSpring);
                    }
                }

                //Cross-bracing between the upper side chords.
                if ((segmentIndex & 1) == 0)
                {
                    AddMember(topNodes[segmentIndex, 0], topNodes[segmentIndex + 1, 1], lateralSpring);
                    AddMember(topNodes[segmentIndex, 1], topNodes[segmentIndex + 1, 0], lateralSpring);
                }
            }

            for (int segmentIndex = 0; segmentIndex <= SegmentCount; ++segmentIndex)
            {
                //Transverse deck members.
                for (int widthIndex = 0; widthIndex < DeckWidthNodeCount - 1; ++widthIndex)
                {
                    AddMember(deckNodes[segmentIndex, widthIndex], deckNodes[segmentIndex, widthIndex + 1], deckSpring);
                }

                //Side-truss vertical members.
                AddMember(deckNodes[segmentIndex, 0], topNodes[segmentIndex, 0], braceSpring);
                AddMember(deckNodes[segmentIndex, DeckWidthNodeCount - 1], topNodes[segmentIndex, 1], braceSpring);

                //Upper transverse member.
                AddMember(topNodes[segmentIndex, 0], topNodes[segmentIndex, 1], lateralSpring);

                //Triangulate the transverse bridge section.
                AddMember(topNodes[segmentIndex, 0], deckNodes[segmentIndex, DeckWidthNodeCount - 1], lateralSpring);
                AddMember(topNodes[segmentIndex, 1], deckNodes[segmentIndex, 0], lateralSpring);
            }
        }
        void CreateDeckPanels()
        {
            const float panelThickness = 0.3f;
            const float panelMass = 3f;

            Box panelShape = new(SegmentLength * 0.96f, panelThickness, BridgeWidth * 0.96f);
            TypedIndex panelShapeIndex = Simulation.Shapes.Add(panelShape);
            BodyInertia panelInertia = panelShape.ComputeInertia(panelMass);

            int centerWidthIndex = DeckWidthNodeCount / 2;

            for (int segmentIndex = 0; segmentIndex < SegmentCount; ++segmentIndex)
            {
                Vector3 leftCenter = Simulation.Bodies[deckNodes[segmentIndex, centerWidthIndex]].Pose.Position;
                Vector3 rightCenter = Simulation.Bodies[deckNodes[segmentIndex + 1, centerWidthIndex]].Pose.Position;
                Vector3 panelPosition = (leftCenter + rightCenter) * 0.5f + new Vector3(0, 0.25f, 0);

                BodyDescription panelDescription = BodyDescription.CreateDynamic(panelPosition, panelInertia, panelShapeIndex, 0.01f);
                BodyHandle panelHandle = Simulation.Bodies.Add(panelDescription);

                collisionFilters.Allocate(panelHandle) = new SubgroupCollisionFilter
                {
                    GroupId = 0,
                    SubgroupMembership = 2,
                    CollidableSubgroups = 0
                };

                ConnectPanelToNode(panelHandle, deckNodes[segmentIndex, 0]);
                ConnectPanelToNode(panelHandle, deckNodes[segmentIndex, DeckWidthNodeCount - 1]);
                ConnectPanelToNode(panelHandle, deckNodes[segmentIndex + 1, 0]);
                ConnectPanelToNode(panelHandle, deckNodes[segmentIndex + 1, DeckWidthNodeCount - 1]);
            }
        }
        void CreateApproachRamps()
        {
            float bridgeLength = SegmentCount * SegmentLength;
            float bridgeStartX = -bridgeLength * 0.5f;
            float bridgeEndX = bridgeLength * 0.5f;

            const float rampHorizontalLength = 16;
            const float rampRise = 5;
            const float rampWidth = BridgeWidth + 2;
            const float rampThickness = 0.8f;

            float rampLength = MathF.Sqrt(rampHorizontalLength * rampHorizontalLength + rampRise * rampRise);
            float rampAngle = MathF.Atan2(rampRise, rampHorizontalLength);

            //The left ramp descends toward the bridge.
            Simulation.Statics.Add(new StaticDescription(new Vector3(bridgeStartX - rampHorizontalLength * 0.5f, DeckHeight + rampRise * 0.5f - rampThickness * 0.25f, 0), QuaternionEx.CreateFromAxisAngle(Vector3.UnitZ, -rampAngle), Simulation.Shapes.Add(new Box(rampLength, rampThickness, rampWidth))));

            //The right ramp descends away from the bridge.
            Simulation.Statics.Add(new StaticDescription(new Vector3(bridgeEndX + rampHorizontalLength * 0.5f, DeckHeight - rampRise * 0.5f - rampThickness * 0.25f, 0), QuaternionEx.CreateFromAxisAngle(Vector3.UnitZ, -rampAngle), Simulation.Shapes.Add(new Box(rampLength, rampThickness, rampWidth))));

            //Ground under the exit.
            Simulation.Statics.Add(new StaticDescription(new Vector3(bridgeEndX + rampHorizontalLength + 20, -1, 0), Simulation.Shapes.Add(new Box(40, 2, 30))));
        }
        void CreateRollingBall()
        {
            const float bridgeLength = SegmentCount * SegmentLength;
            const float ballRadius = 1.6f;
            const float ballMass = 140;

            Sphere ballShape = new(ballRadius);
            TypedIndex ballShapeIndex = Simulation.Shapes.Add(ballShape);
            BodyInertia ballInertia = ballShape.ComputeInertia(ballMass);

            BodyDescription description = BodyDescription.CreateDynamic(Quaternion.Identity, ballInertia, ballShapeIndex, 0.01f);
            description.Pose.Position = new Vector3(-bridgeLength * 0.5f - 12, DeckHeight + 8, 0);
            description.Velocity.Linear = new Vector3(3, 0, 0);
            description.Velocity.Angular = new Vector3(0, 0, -3f / ballRadius);

            BodyHandle ballHandle = Simulation.Bodies.Add(description);
            collisionFilters.Allocate(ballHandle) = new SubgroupCollisionFilter(1);
        }
        void ConnectPanelToNode(BodyHandle panelHandle, BodyHandle nodeHandle)
        {
            BodyReference panel = Simulation.Bodies[panelHandle];
            BodyReference node = Simulation.Bodies[nodeHandle];

            Vector3 worldAnchor = node.Pose.Position;

            RigidPose.TransformByInverse(worldAnchor, panel.Pose, out Vector3 panelLocalOffset);
            RigidPose.TransformByInverse(worldAnchor, node.Pose, out Vector3 nodeLocalOffset);

            Simulation.Solver.Add(panelHandle, nodeHandle, new BallSocket
            {
                LocalOffsetA = panelLocalOffset,
                LocalOffsetB = nodeLocalOffset,
                SpringSettings = new SpringSettings(20, 1)
            });
        }

        static bool IsAnchorNode(int segmentIndex)
        {
            return segmentIndex == 0 || segmentIndex == SegmentCount;
        }
    }
}
