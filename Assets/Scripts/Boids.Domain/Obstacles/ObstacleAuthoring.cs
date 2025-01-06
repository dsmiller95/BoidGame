using System;
using Unity.Entities;
using UnityEngine;

namespace Boids.Domain.Obstacles
{
    public enum ObstacleType
    {
        SphereRepel,
        //SphereAttract,
    }
    
    [Serializable]
    public struct Obstacle : IComponentData
    {
        public ObstacleType variant;
    }
    
    public class ObstacleAuthoring : MonoBehaviour
    {
        private class ObstacleBaker : Baker<ObstacleAuthoring>
        {
            public override void Bake(ObstacleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new Obstacle
                {
                    variant = ObstacleType.SphereRepel
                });
            }
        }
    }
}