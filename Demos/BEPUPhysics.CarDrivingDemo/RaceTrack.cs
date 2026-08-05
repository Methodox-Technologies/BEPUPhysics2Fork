using System.Numerics;
using System.Runtime.CompilerServices;

namespace BEPUPhysics.CarDrivingDemo
{
    /// <summary>
    /// Represents a planar four-quadrant race track composed of four connected circular arcs.
    /// </summary>
    /// <remarks>
    /// The track is defined in the XZ plane using <see cref="Vector2"/>, where each component corresponds to a horizontal world-space axis. 
    /// Four circle centers are positioned diagonally around <see cref="Center"/>, each separated from it by <see cref="QuadrantRadius"/>.
    ///
    /// The structure provides geometric queries only. It does not create collision geometry, render the track, or maintain vehicle state. 
    /// The demo uses it to shape terrain around the track and to provide AI vehicles with lane targets and travel directions.
    /// </remarks>
    internal struct RaceTrack
    {
        #region Properties
        /// <summary>
        /// Gets or sets the radius of each circular track segment and the horizontal offset
        /// from the track center to each quadrant's circle center.
        /// </summary>
        public float QuadrantRadius;
        /// <summary>
        /// Gets or sets the center of the complete track in the horizontal plane.
        /// </summary>
        public Vector2 Center;
        #endregion

        #region Methods
        /// <summary>
        /// Finds the closest target point on a track lane and the track's travel direction
        /// at that point.
        /// </summary>
        /// <param name="point">The position to query in track-local horizontal coordinates.</param>
        /// <param name="laneOffset">The signed radial offset from the track centerline. Positive and negative values select parallel lanes on opposite sides of the centerline.</param>
        /// <param name="closestPoint">Receives the closest point on the requested lane.</param>
        /// <param name="flowDirection">Receives the normalized tangent direction representing the intended direction of travel at <paramref name="closestPoint"/>.</param>
        /// <remarks>
        /// The quadrant containing <paramref name="point"/> determines which circular segment is queried. The lane offset and tangent orientation are adjusted between quadrants so that the resulting path and travel direction remain continuous around the track.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void GetClosestPoint(in Vector2 point, float laneOffset, out Vector2 closestPoint, out Vector2 flowDirection)
        {
            Vector2 localPoint = point - Center;
            Vector2 quadrantCenter = new(localPoint.X < 0 ? -QuadrantRadius : QuadrantRadius, localPoint.Y < 0 ? -QuadrantRadius : QuadrantRadius);
            Vector2 quadrantCenterToPoint = new Vector2(localPoint.X, localPoint.Y) - quadrantCenter;
            float distanceToQuadrantCenter = quadrantCenterToPoint.Length();
            bool on01Or10 = localPoint.X * localPoint.Y < 0;
            float signedLaneOffset = on01Or10 ? -laneOffset : laneOffset;
            Vector2 toCircleEdgeDirection = distanceToQuadrantCenter > 0 ? quadrantCenterToPoint * (1f / distanceToQuadrantCenter) : new Vector2(QuadrantRadius + signedLaneOffset, 0);
            Vector2 offsetFromQuadrantCircle = (QuadrantRadius + signedLaneOffset) * toCircleEdgeDirection;
            closestPoint = quadrantCenter + offsetFromQuadrantCircle;
            Vector2 perpendicular = new(toCircleEdgeDirection.Y, -toCircleEdgeDirection.X);
            flowDirection = on01Or10 ? perpendicular : -perpendicular;
        }

        /// <summary>
        /// Computes the planar distance from a point to the track centerline.
        /// </summary>
        /// <param name="point">The position to query in track-local horizontal coordinates.</param>
        /// <returns>The Euclidean distance from <paramref name="point"/> to the closest point on the track centerline.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float GetDistance(in Vector2 point)
        {
            GetClosestPoint(point, 0, out Vector2 closest, out _);
            return Vector2.Distance(closest, point);
        }
        #endregion
    }
}