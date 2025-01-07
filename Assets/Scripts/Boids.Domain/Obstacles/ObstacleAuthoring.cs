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
    public struct Obstacle : IComponentData
    {
        public ObstacleType variant;
        public float obstacleRadius;
        public float obstacleHardSurfaceRadius;

        public readonly Obstacle AdjustForScale(float linearScale)
        {
            var res = this;
            res.obstacleRadius *= linearScale;
            res.obstacleHardSurfaceRadius *= linearScale;
            return res;
        }
        
        public float RadiusSq => obstacleRadius * obstacleRadius;
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
                AddComponent(entity, new Obstacle
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