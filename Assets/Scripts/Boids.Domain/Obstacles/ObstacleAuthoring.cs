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
        public float RadiusSq => obstacleRadius * obstacleRadius;
    }
    
    public class ObstacleAuthoring : MonoBehaviour
    {
        [Range(1f, 30f)]
        public float obstacleRadius = 1f;
        
        private class ObstacleBaker : Baker<ObstacleAuthoring>
        {
            public override void Bake(ObstacleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new Obstacle
                {
                    variant = ObstacleType.SphereRepel,
                    obstacleRadius = authoring.obstacleRadius
                });
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, obstacleRadius);
        }
    }
}