using BepuPhysics;

namespace BEPUPhysics.CarDrivingDemo
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