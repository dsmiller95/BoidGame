using System;
using Unity.Entities;
using UnityEngine;

namespace Boids.Domain.Obstacles
{
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
                var spriteRenderer = GetComponent<SpriteRenderer>();
                AddComponent(entity, new OriginalColor
                {
                    Color = spriteRenderer.color.ToFloat4()
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