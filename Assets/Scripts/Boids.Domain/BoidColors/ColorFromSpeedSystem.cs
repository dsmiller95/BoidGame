using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using UnityEngine;

namespace Boids.Domain.BoidColors
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    public partial struct ColorFromSpeedSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var world = state.WorldUnmanaged;
            
            var queryWithoutOverride = SystemAPI.QueryBuilder()
                .WithAll<PhysicsVelocity, SpeedToColor>()
                .WithNone<URPMaterialPropertyBaseColor>()
                .Build();
            if (!queryWithoutOverride.IsEmpty)
            {
                var ecb = new EntityCommandBuffer(world.UpdateAllocator.ToAllocator);
                ecb.AddComponent(queryWithoutOverride, typeof(URPMaterialPropertyBaseColor), EntityQueryCaptureMode.AtPlayback);
                ecb.Playback(state.EntityManager);
            }
            
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<Boid> uniqueBoidTypes, world.UpdateAllocator.ToAllocator);

            foreach (Boid boid in uniqueBoidTypes)
            {
                foreach (var (myColor, velocity, speedToColor) in 
                         SystemAPI.Query<RefRW<URPMaterialPropertyBaseColor>, RefRO<PhysicsVelocity>, RefRO<SpeedToColor>>()
                             .WithSharedComponentFilter(boid))
                {
                    var deltaTModifier = boid.simSpeedMultiplier;
                    var speed = math.length(velocity.ValueRO.Linear) / deltaTModifier;
                    
                    var newColor = speedToColor.ValueRO.GetColor(speed);
                    myColor.ValueRW.Value = newColor;
                }
            }
        }
    }
    
    [Serializable]
    public struct SpeedToColor : IComponentData
    {
        public float minSpeed;
        public float4 minColor;
        
        public float maxSpeed;
        public float4 maxColor;
        
        public readonly float4 GetColor(float speed)
        {
            var t = math.unlerp(minSpeed, maxSpeed, speed);
            t = math.clamp(t, 0, 1);
            return math.lerp(minColor, maxColor, t);
        }
    }
}