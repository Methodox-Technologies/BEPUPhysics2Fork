using BepuPhysics;

namespace BEPUPhysics.OpenGLDemos.Demos.Cars
{
    struct WheelHandles
    {
        public BodyHandle Wheel;
        public ConstraintHandle SuspensionSpring;
        public ConstraintHandle SuspensionTrack;
        public ConstraintHandle Hinge;
        public ConstraintHandle Motor;
    }
}