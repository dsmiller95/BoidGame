using System;
using Unity.Entities;
using UnityEngine;

namespace Boids.Domain.Obstacles
{
    public enum ObstacleType
    {
        None = 0,
        SphereRepel,
        //SphereAttract,
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
    
    public class ObstacleAuthoring : MonoBehaviour
    {
        [Range(1f, 30f)]
        public float obstacleRadius = 1f;
        public float hardSurfaceRadius = 0.8f;
        
        private class ObstacleBaker : Baker<ObstacleAuthoring>
        {
            public override void Bake(ObstacleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new ObstacleComponent()
                {
                    variant = ObstacleType.SphereRepel,
                    obstacleRadius = authoring.obstacleRadius,
                    obstacleHardSurfaceRadius = authoring.hardSurfaceRadius,
                });
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            var radius = obstacleRadius * this.transform.lossyScale.x;
            Gizmos.DrawWireSphere(transform.position, radius);
            var hardRadius = hardSurfaceRadius * this.transform.lossyScale.x;
            Gizmos.DrawWireSphere(transform.position, hardRadius);
        }
    }
}