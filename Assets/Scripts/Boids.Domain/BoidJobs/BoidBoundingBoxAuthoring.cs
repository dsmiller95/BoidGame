using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Boids.Domain.BoidJobs
{
    [Serializable]
    public struct BoidBoundingBox : IComponentData
    {
        public float2 min;
        public float2 max;
    }

    public class BoidBoundingBoxAuthoring : MonoBehaviour
    {
        public Vector2 center;
        public Vector2 extents;

        private class BoidBoundingBoxBaker : Baker<BoidBoundingBoxAuthoring>
        {
            public override void Bake(BoidBoundingBoxAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new BoidBoundingBox
                {
                    min = authoring.center - authoring.extents,
                    max = authoring.center + authoring.extents
                });
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center,extents * 2);
        }
    }
}