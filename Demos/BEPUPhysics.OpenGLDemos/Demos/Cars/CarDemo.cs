using System;
using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities;
using BepuUtilities.Memory;
using DemoContentLoader;
using DemoRenderer;
using DemoRenderer.UI;
using BEPUPhysics.OpenGLDemos.Helpers;
using BEPUPhysics.OpenGLDemos.Interaction;
using DemoUtilities;
using OpenTK.Windowing.GraphicsLibraryFramework;
using BEPUPhysics.OpenGLDemos.Types;

namespace BEPUPhysics.OpenGLDemos.Demos.Cars;

public class CarDemo : DemoBase
{
    #region Configs
    static Keys Forward = Keys.W;
    static Keys Backward = Keys.S;
    static Keys Right = Keys.D;
    static Keys Left = Keys.A;
    static Keys Zoom = Keys.LeftShift;
    static Keys Brake = Keys.Space;
    static Keys BrakeAlternate = Keys.Backspace; //I have a weird keyboard.
    static Keys ToggleCar = Keys.C;

    const int _aiCount = 384;
    const int _planeWidth = 257;
    const float _terrainScale = 3;

    const float _x = 0.9f;
    const float _y = -0.1f;
    const float _frontZ = 1.7f;
    const float _backZ = -1.7f;
    const float _wheelBaseWidth = _x * 2;
    const float _wheelBaseLength = _frontZ - _backZ;
    #endregion

    #region Subtypes
    struct CarAIController
    {
        public SimpleCarController Controller;
        public float LaneOffset;
    }
    #endregion

    #region Components
    private SimpleCarController playerController;
    private Buffer<CarAIController> _aiControllers;
    private RaceTrack _raceTrack;
    private bool playerControlActive = true;
    #endregion

    #region States
    Random _random = new(5);
    #endregion

    #region Framework
    public override void Initialize(ContentArchive content, Camera camera)
    {
        camera.Position = new Vector3(0, 5, 10);
        camera.Yaw = 0;
        camera.Pitch = 0;

        CollidableProperty<CarBodyProperties> carBodyProperties = new(); // A reference-type lookup table indexed by collidable handles

        Simulation = Simulation.Create(BufferPool, new CarCallbacks() { Properties = carBodyProperties }, new DemoPoseIntegratorCallbacks(new Vector3(0, -10, 0)), new SolveDescription(6, 1));

        CompoundBuilder carBodyBuilder = new(BufferPool, Simulation.Shapes, 2);
        carBodyBuilder.Add(new Box(1.85f, 0.7f, 4.73f), RigidPose.Identity, 10);
        carBodyBuilder.Add(new Box(1.85f, 0.6f, 2.5f), new Vector3(0, 0.65f, -0.35f), 0.5f);
        carBodyBuilder.BuildDynamicCompound(out Buffer<CompoundChild> children, out BodyInertia bodyInertia, out _);
        carBodyBuilder.Dispose();
        Compound bodyShape = new(children);
        TypedIndex bodyShapeIndex = Simulation.Shapes.Add(bodyShape);
        Cylinder wheelShape = new(0.4f, .18f);
        BodyInertia wheelInertia = wheelShape.ComputeInertia(0.25f);
        TypedIndex wheelShapeIndex = Simulation.Shapes.Add(wheelShape);

        // Player controller
        playerController = new SimpleCarController(SimpleCar.Create(Simulation, carBodyProperties, new Vector3(0, 10, 0), bodyShapeIndex, bodyInertia, 0.5f, wheelShapeIndex, wheelInertia, 2f,
            new Vector3(-_x, _y, _frontZ), new Vector3(_x, _y, _frontZ), new Vector3(-_x, _y, _backZ), new Vector3(_x, _y, _backZ), new Vector3(0, -1, 0), 0.25f,
            new SpringSettings(5f, 0.7f), QuaternionEx.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI * 0.5f)),
            forwardSpeed: 75, forwardForce: 6, zoomMultiplier: 2, backwardSpeed: 30, backwardForce: 4, idleForce: 0.25f, brakeForce: 7, steeringSpeed: 1.5f, maximumSteeringAngle: MathF.PI * 0.23f,
            wheelBaseLength: _wheelBaseLength, wheelBaseWidth: _wheelBaseWidth, ackermanSteering: 1);

        // Race track
        CreateRaceTrack(_random);

        // AI
        CreateAIControllers(carBodyProperties, bodyInertia, bodyShapeIndex, wheelInertia, wheelShapeIndex);

        // Terrain
        CreateCollisionTerrain();
    }
    public override void Update(DemoUtilities.Window window, Camera camera, Input input, float dt)
    {
        // Input control
        if (input != null)
        {
            if (input.WasPushed(ToggleCar))
                playerControlActive = !playerControlActive;
            if (playerControlActive)
            {
                // Steering
                float steeringSum = 0;
                if (input.IsDown(Left))
                    steeringSum += 1;
                if (input.IsDown(Right))
                    steeringSum -= 1;

                float targetSpeedFraction = input.IsDown(Forward) ? 1f : input.IsDown(Backward) ? -1f : 0;
                bool zoom = input.IsDown(Zoom);

                // For control purposes, we'll match the fixed update rate of the simulation. Could decouple it- this dt isn't vulnerable to the same instabilities as the simulation itself with variable durations.
                playerController.Update(Simulation, TimestepDuration, steeringSum, targetSpeedFraction, zoom, input.IsDown(Brake) || input.IsDown(BrakeAlternate));
            }
        }

        // AI ticking
        // Remark: a reactive path-following controller (look-ahead waypoint/flow-field steering controller)
        for (int i = 0; i < _aiControllers.Length; ++i)
        {
            ref CarAIController ai = ref _aiControllers[i];
            BodyReference body = Simulation.Bodies[ai.Controller.Car.Body];
            ref RigidPose pose = ref body.Pose;
            Matrix3x3.CreateFromQuaternion(pose.Orientation, out Matrix3x3 orientation);
            float forwardVelocity = Vector3.Dot(orientation.Z, body.Velocity.Linear);
            Vector2 predictedLocation = new Vector2(pose.Position.X, pose.Position.Z) + new Vector2(orientation.Z.X, orientation.Z.Z) * (5 + forwardVelocity * 2);

            // Steering
            _raceTrack.GetClosestPoint(predictedLocation, ai.LaneOffset, out Vector2 closestPoint, out Vector2 flowDirection);
            float steeringAngle;
            if (flowDirection.X * orientation.Z.X + flowDirection.Y * orientation.Z.Z < 0)
            {
                // Don't drive against traffic!
                steeringAngle = ai.Controller.MaximumSteeringAngle;
            }
            else
            {
                Vector2 toClosestPoint = closestPoint - new Vector2(pose.Position.X, pose.Position.Z);
                float horizontalOffset = orientation.X.X * toClosestPoint.X + orientation.X.Z * toClosestPoint.Y;
                float forwardOffset = orientation.Z.X * toClosestPoint.X + orientation.Z.Z * toClosestPoint.Y;
                steeringAngle = MathF.Atan2(horizontalOffset, forwardOffset);
            }
            float speedFraction = 0.25f + MathF.Min(0.75f, MathF.Max(0, 0.75f * (MathF.Abs(steeringAngle) - 0.2f) / -0.4f));
            if (orientation.Y.Y < 0.4f)
                speedFraction = 0;

            ai.Controller.Update(Simulation, TimestepDuration, steeringAngle, speedFraction, steeringAngle < 0.05f, steeringAngle > MathF.PI * 0.2f && forwardVelocity > ai.Controller.ForwardSpeed * 0.6f);
        }

        base.Update(window, camera, input, dt);
    }
    public override void Render(Renderer renderer, Camera camera, Input input, TextBuilder text, Font font)
    {
        if (playerControlActive)
        {
            BodyReference carBody = new(playerController.Car.Body, Simulation.Bodies);
            QuaternionEx.TransformUnitY(carBody.Pose.Orientation, out Vector3 carUp);
            camera.Position = carBody.Pose.Position + carUp * 1.3f + camera.Backward * 8;
        }

        int textHeight = 16;
        Vector2 position = new(32, renderer.Surface.Resolution.Y - 128);
        RenderControl(ref position, textHeight, nameof(Forward), ControlStringsCache.GetName(Forward), text, renderer.TextBatcher, font);
        RenderControl(ref position, textHeight, nameof(Backward), ControlStringsCache.GetName(Backward), text, renderer.TextBatcher, font);
        RenderControl(ref position, textHeight, nameof(Right), ControlStringsCache.GetName(Right), text, renderer.TextBatcher, font);
        RenderControl(ref position, textHeight, nameof(Left), ControlStringsCache.GetName(Left), text, renderer.TextBatcher, font);
        RenderControl(ref position, textHeight, nameof(Zoom), ControlStringsCache.GetName(Zoom), text, renderer.TextBatcher, font);
        RenderControl(ref position, textHeight, nameof(Brake), ControlStringsCache.GetName(Brake), text, renderer.TextBatcher, font);
        RenderControl(ref position, textHeight, nameof(ToggleCar), ControlStringsCache.GetName(ToggleCar), text, renderer.TextBatcher, font);
        base.Render(renderer, camera, input, text, font);
    }
    #endregion

    #region Routines
    private void CreateRaceTrack(Random random)
    {
        _raceTrack = new RaceTrack { QuadrantRadius = (_planeWidth - 32) * _terrainScale * 0.25f, Center = default };

        // Add some building-ish landmarks in the middle of each of the four racetrack quadrants.
        for (int i = 0; i < 4; ++i)
        {
            Vector3 landmarkCenter = new((i & 1) * _raceTrack.QuadrantRadius * 2 - _raceTrack.QuadrantRadius, -20, (i & 2) * _raceTrack.QuadrantRadius - _raceTrack.QuadrantRadius);
            Vector3 landmarkMin = landmarkCenter - new Vector3(_raceTrack.QuadrantRadius * 0.5f, 0, _raceTrack.QuadrantRadius * 0.5f);
            Vector3 landmarkSpan = new(_raceTrack.QuadrantRadius, 0, _raceTrack.QuadrantRadius);

            // Random buildings
            for (int j = 0; j < 25; ++j)
            {
                Box buildingShape = new(10 + random.NextSingle() * 10, 20 + random.NextSingle() * 20, 10 + random.NextSingle() * 10);
                Vector3 position = new Vector3(0, buildingShape.HalfHeight, 0) + landmarkMin + landmarkSpan * new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle());
                Quaternion rotation = QuaternionEx.CreateFromAxisAngle(Vector3.UnitY, random.NextSingle() * MathF.PI);
                Simulation.Statics.Add(new StaticDescription(position, rotation, Simulation.Shapes.Add(buildingShape)));
            }
        }
    }
    private void CreateAIControllers(CollidableProperty<CarBodyProperties> carBodyProperties, BodyInertia bodyInertia, TypedIndex bodyShapeIndex, BodyInertia wheelInertia, TypedIndex wheelShapeIndex)
    {
        // Create a bunch of AI cars to race against.
        BufferPool.Take(_aiCount, out _aiControllers);

        // Get terrain bound
        Vector3 min = new(-_planeWidth * _terrainScale * 0.45f, 10, -_planeWidth * _terrainScale * 0.45f);
        Vector3 span = new(_planeWidth * _terrainScale * 0.9f, 15, _planeWidth * _terrainScale * 0.9f);

        // Create AI controllers
        for (int i = 0; i < _aiCount; ++i)
        {
            // The AI cars are very similar, except... we handicap them a little to make the player feel good about themselves.
            Vector3 position = min + span * new Vector3(_random.NextSingle(), _random.NextSingle(), _random.NextSingle());
            Quaternion orientation = QuaternionEx.CreateFromAxisAngle(new Vector3(0, 1, 0), _random.NextSingle() * MathF.PI * 2);
            SimpleCar car = SimpleCar.Create(Simulation, carBodyProperties, (position, orientation), bodyShapeIndex, bodyInertia, 0.5f, wheelShapeIndex, wheelInertia, 2f, new Vector3(-_x, _y, _frontZ), new Vector3(_x, _y, _frontZ), new Vector3(-_x, _y, _backZ), new Vector3(_x, _y, _backZ), new Vector3(0, -1, 0), 0.25f, new SpringSettings(5, 0.7f), QuaternionEx.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI * 0.5f));

            _aiControllers[i].Controller = new SimpleCarController(car, forwardSpeed: 50, forwardForce: 5, zoomMultiplier: 2, backwardSpeed: 10, backwardForce: 4, idleForce: 0.25f, brakeForce: 7, steeringSpeed: 1.5f, maximumSteeringAngle: MathF.PI * 0.23f, wheelBaseLength: _wheelBaseLength, wheelBaseWidth: _wheelBaseWidth, ackermanSteering: 1);
            _aiControllers[i].LaneOffset = _random.NextSingle() * 20 - 10;
        }
    }
    private void CreateCollisionTerrain()
    {
        Vector2 terrainPosition = new Vector2(1 - _planeWidth, 1 - _planeWidth) * _terrainScale * 0.5f;
        Mesh planeMesh = DemoMeshHelper.CreateDeformedPlane(_planeWidth, _planeWidth,
            (int vX, int vY) =>
            {
                float octave0 = (MathF.Sin((vX + 5f) * 0.05f) + MathF.Sin((vY + 11) * 0.05f)) * 1.8f;
                float octave1 = (MathF.Sin((vX + 17) * 0.15f) + MathF.Sin((vY + 19) * 0.15f)) * 0.9f;
                float octave2 = (MathF.Sin((vX + 37) * 0.35f) + MathF.Sin((vY + 93) * 0.35f)) * 0.4f;
                float octave3 = (MathF.Sin((vX + 53) * 0.65f) + MathF.Sin((vY + 47) * 0.65f)) * 0.2f;
                float octave4 = (MathF.Sin((vX + 67) * 1.50f) + MathF.Sin((vY + 13) * 1.5f)) * 0.125f;
                int distanceToEdge = _planeWidth / 2 - Math.Max(Math.Abs(vX - _planeWidth / 2), Math.Abs(vY - _planeWidth / 2));
                float edgeRamp = 25f / (distanceToEdge + 1);
                float terrainHeight = octave0 + octave1 + octave2 + octave3 + octave4;
                Vector2 vertexPosition = new Vector2(vX * _terrainScale, vY * _terrainScale) + terrainPosition;
                float distanceToTrack = _raceTrack.GetDistance(vertexPosition);
                float trackWeight = MathF.Min(1f, 3f / (distanceToTrack * 0.1f + 1f));
                float height = trackWeight * -10f + terrainHeight * (1 - trackWeight);
                return new Vector3(vertexPosition.X, height + edgeRamp, vertexPosition.Y);

            }, new Vector3(1, 1, 1), BufferPool, ThreadDispatcher);
        Simulation.Statics.Add(new StaticDescription(new Vector3(0, -15, 0), QuaternionEx.CreateFromAxisAngle(new Vector3(0, 1, 0), MathF.PI / 2), Simulation.Shapes.Add(planeMesh)));
    }
    #endregion

    #region Render Routines
    void RenderControl(ref Vector2 position, float textHeight, string controlName, string controlValue, TextBuilder text, TextBatcher textBatcher, Font font)
    {
        text.Clear().Append(controlName).Append(": ").Append(controlValue);
        textBatcher.Write(text, position, textHeight, new Vector3(1), font);
        position.Y += textHeight * 1.1f;
    }
    #endregion
}