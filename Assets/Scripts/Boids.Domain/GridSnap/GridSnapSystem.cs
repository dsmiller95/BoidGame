﻿using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Boids.Domain.GridSnap
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct GridSnapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var world = state.WorldUnmanaged;
            var gridDefinition = new GridDefinition
            {
                gridSize = 10f
            };
            world.EntityManager.AddComponentData(state.SystemHandle, gridDefinition);
        }

        public void OnUpdate(ref SystemState state)
        {
            var gridDefinition = state.WorldUnmanaged.EntityManager.GetComponentData<GridDefinition>(state.SystemHandle);
            
            foreach (var transform in 
                     SystemAPI.Query<RefRW<LocalTransform>>()
                         .WithAll<SnapMeToGridFlag>()
                         .WithChangeFilter<LocalTransform>())
            {
                var position = transform.ValueRO.Position.xy;
                position = gridDefinition.SnapToClosest(position);
                
                transform.ValueRW = transform.ValueRW.WithPosition(new float3(position, 0));
                Debug.Log("snapped thing to grid");
            }
        }
    }

    [Serializable]
    public struct GridDefinition : IComponentData
    {
        public float gridSize;
        
        public float2 SnapToClosest(float2 position)
        {
            return math.round(position / gridSize) * gridSize;
        }
    }

    public struct SnapMeToGridFlag : IComponentData { }
}