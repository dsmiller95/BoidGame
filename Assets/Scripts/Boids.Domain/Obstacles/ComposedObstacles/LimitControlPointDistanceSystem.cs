using System;
using Boids.Domain.GridSnap;
using Boids.Domain.OnClick;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Boids.Domain.Obstacles.ComposedObstacles
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MoveObstaclesSystemGroup))]
    [UpdateBefore(typeof(GridSnapSystem))]
    [BurstCompile]
    public partial struct LimitControlPointDistanceSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (transform, limitDistance) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRO<LimitDistanceComponent>>())
            {
                var localPos = transform.ValueRO.Position.xy;

                var lenSq = math.lengthsq(localPos);
                var maxLen = limitDistance.ValueRO.maximumDistance;
                if (lenSq <= maxLen * maxLen)
                {
                    continue;
                }

                var newPos = maxLen * localPos / math.sqrt(lenSq);
                transform.ValueRW = transform.ValueRW.WithPosition(new float3(newPos, 0));
            }
        }

    }

    [Serializable]
    public struct LimitDistanceComponent : IComponentData
    {
        public static LimitDistanceComponent Default => new LimitDistanceComponent
        {
            maximumDistance = 3f
        };
        
        public float maximumDistance;

        public void Validate()
        {
            maximumDistance = math.max(MathExtensions.Epsilon, maximumDistance);
        }
    }
}