using BepuUtilities;
using DemoRenderer;
using BepuPhysics;
using BepuPhysics.Collidables;
using System.Numerics;
using DemoContentLoader;
using BepuPhysics.Constraints;
using Demos.Demos;
using System;

namespace Demos.SpecializedTests;

public class ConstraintTestDemo : Demo
{
    static float GetNextPosition(ref float x)
    {
        var toReturn = x;
        x += 3;
        return toReturn;
    }

    public override void Initialize(ContentArchive content, Camera camera)
    {
        camera.Position = new Vector3(25, 4, 40);
        camera.Yaw = 0;
        Simulation = Simulation.Create(BufferPool, new DemoNarrowPhaseCallbacks(new SpringSettings(30, 1), 2, 1), new DemoPoseIntegratorCallbacks(new Vector3(0, -10, 0)), new SolveDescription(8, 1));

        Box shapeA = new(.75f, 1, .5f);
        TypedIndex shapeIndexA = Simulation.Shapes.Add(shapeA);
        CollidableDescription collidableA = new(shapeIndexA);
        Box shapeB = new(.75f, 1, .5f);
        TypedIndex shapeIndexB = Simulation.Shapes.Add(shapeB);
        CollidableDescription collidableB = new(shapeIndexB);
        BodyActivityDescription activity = new(0.01f);
        BodyInertia inertiaA = shapeA.ComputeInertia(1);
        BodyInertia inertiaB = shapeB.ComputeInertia(1);
        var nextX = -10f;
        {
            var x = GetNextPosition(ref nextX);
            BodyHandle a = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 3, 0), inertiaA, collidableA, activity));
            BodyHandle b = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 5, 0), inertiaB, collidableB, activity));
            Simulation.Solver.Add(a, b, new BallSocket { LocalOffsetA = new Vector3(0, 1, 0), LocalOffsetB = new Vector3(0, -1, 0), SpringSettings = new SpringSettings(30, 1) });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyHandle a = Simulation.Bodies.Add(BodyDescription.CreateKinematic(new Vector3(x, 3, 0), collidableA, activity));
            BodyHandle b = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 5, 0), inertiaB, collidableB, activity));
            Simulation.Solver.Add(a, b, new BallSocket { LocalOffsetA = new Vector3(0, 1, 0), LocalOffsetB = new Vector3(0, -1, 0), SpringSettings = new SpringSettings(30, 1) });
            Simulation.Solver.Add(a, b, new AngularHinge { LocalHingeAxisA = new Vector3(0, 1, 0), LocalHingeAxisB = new Vector3(0, 1, 0), SpringSettings = new SpringSettings(30, 1) });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyHandle a = Simulation.Bodies.Add(BodyDescription.CreateKinematic(new Vector3(x, 3, 0), collidableA, activity));
            BodyHandle b = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 5, 0), inertiaB, collidableB, activity));
            Simulation.Solver.Add(a, b, new Hinge
            {
                LocalOffsetA = new Vector3(0, 1, 0),
                LocalHingeAxisA = new Vector3(0, 1, 0),
                LocalOffsetB = new Vector3(0, -1, 0),
                LocalHingeAxisB = new Vector3(0, 1, 0),
                SpringSettings = new SpringSettings(30, 1)
            });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyHandle a = Simulation.Bodies.Add(BodyDescription.CreateKinematic(new Vector3(x, 3, 0), collidableA, activity));
            BodyHandle b = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 5, 0), inertiaB, collidableB, activity));
            Simulation.Solver.Add(a, b, new BallSocket { LocalOffsetA = new Vector3(0, 1, 0), LocalOffsetB = new Vector3(0, -1, 0), SpringSettings = new SpringSettings(30, 1) });
            Simulation.Solver.Add(a, b, new AngularSwivelHinge { LocalSwivelAxisA = new Vector3(1, 0, 0), LocalHingeAxisB = new Vector3(0, 1, 0), SpringSettings = new SpringSettings(30, 1) });
            Simulation.Solver.Add(a, b, new SwingLimit { AxisLocalA = new Vector3(0, 1, 0), AxisLocalB = new Vector3(0, 1, 0), MaximumSwingAngle = MathHelper.PiOver2, SpringSettings = new SpringSettings(30, 1) });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyHandle a = Simulation.Bodies.Add(BodyDescription.CreateKinematic(new Vector3(x, 3, 0), collidableA, activity));
            BodyHandle b = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 5, 0), inertiaB, collidableB, activity));
            Simulation.Solver.Add(a, b, new SwivelHinge
            {
                LocalOffsetA = new Vector3(0, 1, 0),
                LocalSwivelAxisA = new Vector3(1, 0, 0),
                LocalOffsetB = new Vector3(0, -1, 0),
                LocalHingeAxisB = new Vector3(0, 1, 0),
                SpringSettings = new SpringSettings(30, 1)
            });
            Simulation.Solver.Add(a, b, new SwingLimit { AxisLocalA = new Vector3(0, 1, 0), AxisLocalB = new Vector3(0, 1, 0), MaximumSwingAngle = MathHelper.PiOver2, SpringSettings = new SpringSettings(30, 1) });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyHandle a = Simulation.Bodies.Add(BodyDescription.CreateKinematic(new Vector3(x, 3, 0), collidableA, activity));
            BodyHandle b = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 5, 0), inertiaB, collidableB, activity));
            Simulation.Solver.Add(a, b, new BallSocket { LocalOffsetA = new Vector3(0, 1, 0), LocalOffsetB = new Vector3(0, -1, 0), SpringSettings = new SpringSettings(30, 1) });
            Simulation.Solver.Add(a, b, new TwistServo
            {
                LocalBasisA = RagdollDemo.CreateBasis(new Vector3(0, 1, 0), new Vector3(1, 0, 0)),
                LocalBasisB = RagdollDemo.CreateBasis(new Vector3(0, 1, 0), new Vector3(1, 0, 0)),
                TargetAngle = MathHelper.PiOver4,
                SpringSettings = new SpringSettings(30, 1),
                ServoSettings = new ServoSettings(float.MaxValue, 0, float.MaxValue)
            });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyHandle a = Simulation.Bodies.Add(BodyDescription.CreateKinematic(new Vector3(x, 3, 0), collidableA, activity));
            BodyHandle b = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 5, 0), inertiaB, collidableB, activity));
            Simulation.Solver.Add(a, b, new BallSocket { LocalOffsetA = new Vector3(0, 1, 0), LocalOffsetB = new Vector3(0, -1, 0), SpringSettings = new SpringSettings(30, 1) });
            Simulation.Solver.Add(a, b, new TwistLimit
            {
                LocalBasisA = RagdollDemo.CreateBasis(new Vector3(0, 1, 0), new Vector3(1, 0, 0)),
                LocalBasisB = RagdollDemo.CreateBasis(new Vector3(0, 1, 0), new Vector3(1, 0, 0)),
                MinimumAngle = MathHelper.Pi * -0.5f,
                MaximumAngle = MathHelper.Pi * 0.95f,
                SpringSettings = new SpringSettings(30, 1),
            });
            Simulation.Solver.Add(a, b, new AngularHinge { LocalHingeAxisA = new Vector3(0, 1, 0), LocalHingeAxisB = new Vector3(0, 1, 0), SpringSettings = new SpringSettings(30, 1) });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyHandle a = Simulation.Bodies.Add(BodyDescription.CreateKinematic(new Vector3(x, 3, 0), collidableA, activity));
            BodyHandle b = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 5, 0), inertiaB, collidableB, activity));
            Simulation.Solver.Add(a, b, new BallSocket { LocalOffsetA = new Vector3(0, 1, 0), LocalOffsetB = new Vector3(0, -1, 0), SpringSettings = new SpringSettings(30, 1) });
            Simulation.Solver.Add(a, b, new TwistMotor
            {
                LocalAxisA = new Vector3(0, 1, 0),
                LocalAxisB = new Vector3(0, 1, 0),
                TargetVelocity = MathHelper.Pi * 2,
                Settings = new MotorSettings(float.MaxValue, 0.1f)
            });
            Simulation.Solver.Add(a, b, new AngularHinge { LocalHingeAxisA = new Vector3(0, 1, 0), LocalHingeAxisB = new Vector3(0, 1, 0), SpringSettings = new SpringSettings(30, 1) });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyHandle a = Simulation.Bodies.Add(BodyDescription.CreateKinematic(new Vector3(x, 3, 0), collidableA, activity));
            BodyHandle b = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 5, 0), inertiaB, collidableB, activity));
            Simulation.Solver.Add(a, b, new BallSocket { LocalOffsetA = new Vector3(0, 1, 0), LocalOffsetB = new Vector3(0, -1, 0), SpringSettings = new SpringSettings(30, 1) });
            Simulation.Solver.Add(a, b, new AngularServo
            {
                TargetRelativeRotationLocalA = QuaternionEx.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.PiOver2),
                ServoSettings = new ServoSettings(float.MaxValue, 0, 12f),
                SpringSettings = new SpringSettings(30, 1)
            });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyHandle a = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 3, 0), inertiaA, collidableA, activity));
            BodyHandle b = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 5, 0), inertiaB, collidableB, activity));
            Simulation.Solver.Add(a, b, new BallSocket { LocalOffsetA = new Vector3(0, 1, 0), LocalOffsetB = new Vector3(0, -1, 0), SpringSettings = new SpringSettings(30, 1) });
            Simulation.Solver.Add(a, b, new AngularMotor { TargetVelocityLocalA = new Vector3(0, 1, 0), Settings = new MotorSettings(15, 0.0001f) });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyDescription aDescription = BodyDescription.CreateDynamic(new Vector3(x, 3, 0), inertiaA, collidableA, activity);
            BodyDescription bDescription = BodyDescription.CreateDynamic(new Vector3(x, 5, 0), inertiaB, collidableB, activity);
            //aDescription.Velocity.Angular = new Vector3(0, 0, 5);
            BodyHandle a = Simulation.Bodies.Add(aDescription);
            BodyHandle b = Simulation.Bodies.Add(bDescription);
            Simulation.Solver.Add(a, b, new Weld { LocalOffset = new Vector3(0, 2, 0), LocalOrientation = Quaternion.Identity, SpringSettings = new SpringSettings(30, 1) });
        }
        {
            var x = GetNextPosition(ref nextX);
            Sphere sphere = new(0.125f);
            //Treat each vertex as a point mass that cannot rotate.
            BodyInertia sphereInertia = new() { InverseMass = 1 };
            CollidableDescription sphereCollidable = new(Simulation.Shapes.Add(sphere));
            Vector3 a = new(x, 3, 0);
            Vector3 b = new(x, 4, 0);
            Vector3 c = new(x, 3, 1);
            Vector3 d = new(x + 1, 3, 0);
            BodyDescription aDescription = BodyDescription.CreateDynamic(a, sphereInertia, sphereCollidable, activity);
            BodyDescription bDescription = BodyDescription.CreateDynamic(b, sphereInertia, sphereCollidable, activity);
            BodyDescription cDescription = BodyDescription.CreateDynamic(c, sphereInertia, sphereCollidable, activity);
            BodyDescription dDescription = BodyDescription.CreateDynamic(d, sphereInertia, sphereCollidable, activity);
            BodyHandle aHandle = Simulation.Bodies.Add(aDescription);
            BodyHandle bHandle = Simulation.Bodies.Add(bDescription);
            BodyHandle cHandle = Simulation.Bodies.Add(cDescription);
            BodyHandle dHandle = Simulation.Bodies.Add(dDescription);
            SpringSettings distanceSpringiness = new(3f, 1);
            Simulation.Solver.Add(aHandle, bHandle, new CenterDistanceConstraint(Vector3.Distance(a, b), distanceSpringiness));
            Simulation.Solver.Add(aHandle, cHandle, new CenterDistanceConstraint(Vector3.Distance(a, c), distanceSpringiness));
            Simulation.Solver.Add(aHandle, dHandle, new CenterDistanceConstraint(Vector3.Distance(a, d), distanceSpringiness));
            Simulation.Solver.Add(bHandle, cHandle, new CenterDistanceConstraint(Vector3.Distance(b, c), distanceSpringiness));
            Simulation.Solver.Add(bHandle, dHandle, new CenterDistanceConstraint(Vector3.Distance(b, d), distanceSpringiness));
            Simulation.Solver.Add(cHandle, dHandle, new CenterDistanceConstraint(Vector3.Distance(c, d), distanceSpringiness));
            Simulation.Solver.Add(aHandle, bHandle, cHandle, dHandle, new VolumeConstraint(a, b, c, d, new SpringSettings(30, 1)));
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyDescription aDescription = BodyDescription.CreateDynamic(new Vector3(x, 3, 0), inertiaA, collidableA, activity);
            BodyDescription bDescription = BodyDescription.CreateDynamic(new Vector3(x, 6, 0), inertiaB, collidableB, activity);
            BodyHandle a = Simulation.Bodies.Add(aDescription);
            BodyHandle b = Simulation.Bodies.Add(bDescription);
            Simulation.Solver.Add(a, b, new DistanceServo(new Vector3(0, 0.55f, 0), new Vector3(0, -0.55f, 0), 1.9f, new SpringSettings(30, 1), ServoSettings.Default));
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyDescription aDescription = BodyDescription.CreateDynamic(new Vector3(x, 3, 0), inertiaA, collidableA, activity);
            BodyDescription bDescription = BodyDescription.CreateDynamic(new Vector3(x, 6, 0), inertiaB, collidableB, activity);
            BodyHandle a = Simulation.Bodies.Add(aDescription);
            BodyHandle b = Simulation.Bodies.Add(bDescription);
            Simulation.Solver.Add(a, b, new DistanceLimit(new Vector3(0, 0.55f, 0), new Vector3(0, -0.55f, 0), 1f, 3, new SpringSettings(30, 1)));
        }
        {
            var x = GetNextPosition(ref nextX);
            Sphere sphere = new(0.125f);
            //Treat each vertex as a point mass that cannot rotate.
            BodyInertia sphereInertia = new() { InverseMass = 1 };
            CollidableDescription sphereCollidable = new(Simulation.Shapes.Add(sphere));
            Vector3 a = new(x, 3, 0);
            Vector3 b = new(x, 4, 0);
            Vector3 c = new(x + 1, 3, 0);
            BodyDescription aDescription = BodyDescription.CreateDynamic(a, sphereInertia, sphereCollidable, activity);
            BodyDescription bDescription = BodyDescription.CreateDynamic(b, sphereInertia, sphereCollidable, activity);
            BodyDescription cDescription = BodyDescription.CreateDynamic(c, sphereInertia, sphereCollidable, activity);
            BodyHandle aHandle = Simulation.Bodies.Add(aDescription);
            BodyHandle bHandle = Simulation.Bodies.Add(bDescription);
            BodyHandle cHandle = Simulation.Bodies.Add(cDescription);
            SpringSettings distanceSpringiness = new(3f, 1);
            Simulation.Solver.Add(aHandle, bHandle, new CenterDistanceConstraint(Vector3.Distance(a, b), distanceSpringiness));
            Simulation.Solver.Add(aHandle, cHandle, new CenterDistanceConstraint(Vector3.Distance(a, c), distanceSpringiness));
            Simulation.Solver.Add(bHandle, cHandle, new CenterDistanceConstraint(Vector3.Distance(b, c), distanceSpringiness));
            Simulation.Solver.Add(aHandle, bHandle, cHandle, new AreaConstraint(a, b, c, new SpringSettings(30, 1)));
        }
        {
            var x = GetNextPosition(ref nextX);
            Sphere sphere = new(0.125f);
            //Treat each vertex as a point mass that cannot rotate.
            BodyInertia sphereInertia = new() { InverseMass = 1 };
            CollidableDescription sphereCollidable = new(Simulation.Shapes.Add(sphere));
            Vector3 a = new(x, 3, 0);
            Vector3 b = new(x, 4, 0);
            Vector3 c = new(x + 1, 3, 0);
            BodyDescription aDescription = BodyDescription.CreateDynamic(a, sphereInertia, sphereCollidable, activity);
            BodyDescription bDescription = BodyDescription.CreateDynamic(b, sphereInertia, sphereCollidable, activity);
            BodyDescription cDescription = BodyDescription.CreateDynamic(c, sphereInertia, sphereCollidable, activity);
            BodyHandle aHandle = Simulation.Bodies.Add(aDescription);
            BodyHandle bHandle = Simulation.Bodies.Add(bDescription);
            BodyHandle cHandle = Simulation.Bodies.Add(cDescription);
            SpringSettings distanceSpringiness = new(3f, 1);
            var distanceAB = Vector3.Distance(a, b);
            var distanceBC = Vector3.Distance(b, c);
            var distanceCA = Vector3.Distance(c, a);
            Simulation.Solver.Add(aHandle, bHandle, new CenterDistanceLimit(distanceAB * 0.15f, distanceAB, distanceSpringiness));
            Simulation.Solver.Add(aHandle, cHandle, new CenterDistanceLimit(distanceBC * 0.15f, distanceBC, distanceSpringiness));
            Simulation.Solver.Add(bHandle, cHandle, new CenterDistanceLimit(distanceCA * 0.15f, distanceCA, distanceSpringiness));
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyDescription aDescription = BodyDescription.CreateDynamic(new Vector3(x, 3, 0), default, collidableA, activity);
            BodyDescription bDescription = BodyDescription.CreateDynamic(new Vector3(x, 6, 0), inertiaB, collidableB, activity);
            BodyHandle a = Simulation.Bodies.Add(aDescription);
            BodyHandle b = Simulation.Bodies.Add(bDescription);
            Simulation.Solver.Add(a, b, new PointOnLineServo
            {
                LocalOffsetA = new Vector3(0, 0.5f, 0),
                LocalOffsetB = new Vector3(0, -0.5f, 0),
                LocalDirection = new Vector3(0, 1, 0),
                SpringSettings = new SpringSettings(30, 1),
                ServoSettings = ServoSettings.Default
            });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyDescription aDescription = BodyDescription.CreateDynamic(new Vector3(x, 3, 0), inertiaA, collidableA, activity);
            BodyDescription bDescription = BodyDescription.CreateDynamic(new Vector3(x, 6, 0), inertiaB, collidableB, activity);
            BodyHandle a = Simulation.Bodies.Add(aDescription);
            BodyHandle b = Simulation.Bodies.Add(bDescription);
            Simulation.Solver.Add(a, b, new LinearAxisServo
            {
                LocalOffsetA = new Vector3(0, 0.5f, 0),
                LocalOffsetB = new Vector3(0, -0.5f, 0),
                LocalPlaneNormal = new Vector3(0, 1, 0),
                TargetOffset = 2,
                SpringSettings = new SpringSettings(30, 1),
                ServoSettings = ServoSettings.Default
            });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyDescription aDescription = BodyDescription.CreateDynamic(new Vector3(x, 3, 0), inertiaA, collidableA, activity);
            BodyDescription bDescription = BodyDescription.CreateDynamic(new Vector3(x, 6, 0), inertiaB, collidableB, activity);
            BodyHandle a = Simulation.Bodies.Add(aDescription);
            BodyHandle b = Simulation.Bodies.Add(bDescription);
            Simulation.Solver.Add(a, b, new PointOnLineServo
            {
                LocalOffsetA = new Vector3(0, 0.5f, 0),
                LocalOffsetB = new Vector3(0, -0.5f, 0),
                LocalDirection = new Vector3(0, 1, 0),
                SpringSettings = new SpringSettings(30, 1),
                ServoSettings = ServoSettings.Default
            });
            Simulation.Solver.Add(a, b, new LinearAxisMotor
            {
                LocalOffsetA = new Vector3(0, 0.5f, 0),
                LocalOffsetB = new Vector3(0, -0.5f, 0),
                LocalAxis = new Vector3(0, 1, 0),
                TargetVelocity = -2,
                Settings = new MotorSettings(15, 0.01f)
            });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyDescription aDescription = BodyDescription.CreateDynamic(new Vector3(x, 3, 0), inertiaA, collidableA, activity);
            BodyDescription bDescription = BodyDescription.CreateDynamic(new Vector3(x, 6, 0), inertiaB, collidableB, activity);
            BodyHandle a = Simulation.Bodies.Add(aDescription);
            BodyHandle b = Simulation.Bodies.Add(bDescription);
            Simulation.Solver.Add(a, b, new PointOnLineServo
            {
                LocalOffsetA = new Vector3(0, 0.5f, 0),
                LocalOffsetB = new Vector3(0, -0.5f, 0),
                LocalDirection = new Vector3(0, 1, 0),
                SpringSettings = new SpringSettings(30, 1),
                ServoSettings = ServoSettings.Default
            });
            Simulation.Solver.Add(a, b, new LinearAxisLimit
            {
                LocalOffsetA = new Vector3(0, 0.5f, 0),
                LocalOffsetB = new Vector3(0, -0.5f, 0),
                LocalAxis = new Vector3(0, 1, 0),
                MinimumOffset = 1,
                MaximumOffset = 2,
                SpringSettings = new SpringSettings(30, 1)
            });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyHandle a = Simulation.Bodies.Add(BodyDescription.CreateKinematic(new Vector3(x, 3, 0), collidableA, activity));
            BodyHandle b = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 5, 0), inertiaB, collidableB, activity));
            Simulation.Solver.Add(a, b, new BallSocket { LocalOffsetA = new Vector3(0, 1, 0), LocalOffsetB = new Vector3(0, -1, 0), SpringSettings = new SpringSettings(30, 1) });
            Simulation.Solver.Add(b, a, new AngularAxisMotor
            {
                LocalAxisA = new Vector3(0, 1, 0),
                TargetVelocity = MathHelper.Pi * 5,
                Settings = new MotorSettings(float.MaxValue, 0.1f)
            });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyHandle a = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 3, 0), inertiaA, collidableA, activity));
            Simulation.Solver.Add(a, new OneBodyLinearServo
            {
                LocalOffset = new Vector3(0, 1, 0),
                Target = new Vector3(x, 3, 0),
                ServoSettings = new ServoSettings(2, 0, float.MaxValue),
                SpringSettings = new SpringSettings(5, 1)
            });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyHandle a = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 3, 0), inertiaA, collidableA, activity));
            BodyHandle b = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 5, 0), inertiaB, collidableB, activity));
            Simulation.Solver.Add(a, b, new BallSocket { LocalOffsetA = new Vector3(0, 1, 0), LocalOffsetB = new Vector3(0, -1, 0), SpringSettings = new SpringSettings(30, 1) });
            Simulation.Solver.Add(a, new OneBodyLinearMotor
            {
                LocalOffset = new Vector3(0, 1, 0),
                TargetVelocity = new Vector3(0, -1, 0),
                Settings = new MotorSettings(float.MaxValue, 1e-2f),
            });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyHandle a = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 3, 0), inertiaA, collidableA, activity));
            BodyHandle b = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 5, 0), inertiaB, collidableB, activity));
            Simulation.Solver.Add(a, b, new BallSocket { LocalOffsetA = new Vector3(0, 1, 0), LocalOffsetB = new Vector3(0, -1, 0), SpringSettings = new SpringSettings(30, 1) });
            Simulation.Solver.Add(a, new OneBodyAngularServo
            {
                TargetOrientation = Quaternion.Identity,
                ServoSettings = ServoSettings.Default,
                SpringSettings = new SpringSettings(30f, 1f)
            });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyHandle a = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 3, 0), inertiaA, collidableA, activity));
            BodyHandle b = Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(x, 5, 0), inertiaB, collidableB, activity));
            Simulation.Solver.Add(a, b, new BallSocket { LocalOffsetA = new Vector3(0, 1, 0), LocalOffsetB = new Vector3(0, -1, 0), SpringSettings = new SpringSettings(30, 1) });
            Simulation.Solver.Add(a, new OneBodyAngularMotor
            {
                TargetVelocity = new Vector3(1, 0, 0),
                Settings = new MotorSettings(float.MaxValue, 0.001f),
            });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyDescription aDescription = BodyDescription.CreateDynamic(new Vector3(x, 3, 0), inertiaA, collidableA, activity);
            BodyDescription bDescription = BodyDescription.CreateDynamic(new Vector3(x, 6, 0), inertiaB, collidableB, activity);
            BodyHandle a = Simulation.Bodies.Add(aDescription);
            BodyHandle b = Simulation.Bodies.Add(bDescription);
            Simulation.Solver.Add(a, b, new BallSocketMotor
            {
                LocalOffsetB = new Vector3(0, -1, 0),
                TargetVelocityLocalA = new Vector3(0, -0.25f, 0),
                Settings = new MotorSettings(10, 1e-4f)
            });
        }
        {
            var x = GetNextPosition(ref nextX);
            BodyDescription aDescription = BodyDescription.CreateDynamic(new Vector3(x, 3, 0), inertiaA, collidableA, activity);
            BodyDescription bDescription = BodyDescription.CreateDynamic(new Vector3(x, 6, 0), inertiaB, collidableB, activity);
            BodyHandle a = Simulation.Bodies.Add(aDescription);
            BodyHandle b = Simulation.Bodies.Add(bDescription);
            Simulation.Solver.Add(a, b, new BallSocketServo
            {
                LocalOffsetA = new Vector3(0, 1, 0),
                LocalOffsetB = new Vector3(0, -1, 0),
                SpringSettings = new SpringSettings(30, 1),
                ServoSettings = new ServoSettings(100, 1, 100)
            });
        }
        {
            var x = GetNextPosition(ref nextX);
            CollidableDescription wheelShape = new(Simulation.Shapes.Add(new Cylinder(1, 0.1f)));
            Quaternion wheelOrientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), MathF.PI * 0.5f);
            BodyDescription aDescription = BodyDescription.CreateDynamic((new Vector3(x, 3, 0), wheelOrientation), inertiaA, wheelShape, activity);
            BodyDescription bDescription = BodyDescription.CreateDynamic((new Vector3(x, 6, 0), wheelOrientation), inertiaB, wheelShape, activity);
            BodyDescription cDescription = BodyDescription.CreateKinematic(new Vector3(x, 4.5f, -1), Simulation.Shapes.Add(new Box(3, 6, 1)), activity);
            BodyHandle a = Simulation.Bodies.Add(aDescription);
            BodyHandle b = Simulation.Bodies.Add(bDescription);
            BodyHandle c = Simulation.Bodies.Add(cDescription);
            Simulation.Solver.Add(a, b, new AngularAxisGearMotor
            {
                LocalAxisA = new Vector3(0, 1, 0),
                VelocityScale = -4,
                Settings = new MotorSettings(float.MaxValue, 0.0001f)
            });
            Simulation.Solver.Add(c, a, new Hinge
            {
                LocalOffsetA = new Vector3(0, -1.5f, 1),
                LocalHingeAxisA = new Vector3(0, 0, 1),
                LocalOffsetB = new Vector3(0, 0, 0),
                LocalHingeAxisB = new Vector3(0, 1, 0),
                SpringSettings = new SpringSettings(30, 1)
            });
            Simulation.Solver.Add(c, b, new Hinge
            {
                LocalOffsetA = new Vector3(0, 1.5f, 1),
                LocalHingeAxisA = new Vector3(0, 0, 1),
                LocalOffsetB = new Vector3(0, 0, 0),
                LocalHingeAxisB = new Vector3(0, 1, 0),
                SpringSettings = new SpringSettings(30, 1)
            });
        }

        Simulation.Statics.Add(new StaticDescription(new Vector3(), Simulation.Shapes.Add(new Box(256, 1, 256))));
    }
}


