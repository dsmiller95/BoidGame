using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Boids.Domain.Misc
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct RotateForeverSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = (float)state.World.Time.DeltaTime;
            foreach (var (localTransform, rotateForever) in 
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotateForeverComponent>>())
            {
                var local = localTransform.ValueRW;
                local = local.RotateZ(rotateForever.ValueRO.rotationSpeed * deltaTime);
                localTransform.ValueRW = local;
            }
        }
    }

    [Serializable]
    public struct RotateForeverComponent : IComponentData
    {
        [Tooltip("Radians per second")]
        public float rotationSpeed;
    }
}