## AI Controller

This uses a **handcrafted reactive path-following controller**, specifically a **look-ahead waypoint/flow-field steering controller**. It is not machine learning, neural-network, reinforcement-learning, or search-based AI.

Each car repeatedly:

1. Projects a point ahead of itself based on its current speed: `predictedLocation = position + forward * (5 + forwardVelocity * 2)`
2. Queries the racetrack for the closest target point and desired traffic direction: `_raceTrack.GetClosestPoint(...)`
3. Computes steering from the angular error between its current orientation and that target using `Atan2`.
4. Reduces throttle as the steering angle increases.
5. Brakes on sufficiently sharp turns and stops accelerating when overturned.

In robotics/game-AI terminology, it is closest to a **pure-pursuit-style lateral controller** with simple rule-based longitudinal control. It also resembles a lightweight **proportional feedback controller**, since steering is driven directly by positional/angular error, but it is not a formal PID controller because it has no integral or derivative terms.

The `LaneOffset` gives each vehicle a different lateral path within the track, while the flow-direction check prevents it from intentionally driving against traffic. There is no awareness of other cars, overtaking, collision avoidance, planning, memory, or learning.
