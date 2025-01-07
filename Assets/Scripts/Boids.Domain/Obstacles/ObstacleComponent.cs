using System;
using Unity.Entities;

namespace Boids.Domain.Obstacles
{
    public enum ObstacleType
    {
        None = 0,
        SphereRepel,
        //SphereAttract,
    }
    
    public struct ObstacleDisabledFlag : IComponentData
    {
    }

    [Serializable]
    public struct Obstacle
    {
        public ObstacleType variant;
        public float obstacleRadius;
        public float obstacleHardSurfaceRadius;
        public float RadiusSq => obstacleRadius * obstacleRadius;
    }

    [Serializable]
    public struct ObstacleComponent : IComponentData
    {
        public ObstacleType variant;
        public float obstacleRadius;
        public float obstacleHardSurfaceRadius;

        public readonly Obstacle AdjustForScale(float linearScale)
        {
            return new Obstacle
            {
                variant = this.variant,
                obstacleRadius = this.obstacleRadius * linearScale,
                obstacleHardSurfaceRadius = this.obstacleHardSurfaceRadius * linearScale,
            };
        }
    }
}