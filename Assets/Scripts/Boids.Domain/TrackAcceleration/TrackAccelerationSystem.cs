using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Boids.Domain.TrackAcceleration
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct RotateForeverSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = (float)state.World.Time.DeltaTime;
            foreach (var (trackedAcceleration, velocity) in 
                    SystemAPI.Query<RefRW<TrackedAccelerationComponent>, RefRO<PhysicsVelocity>>())
            {
                var currentVelocity = velocity.ValueRO.Linear;
                var previousVelocity = trackedAcceleration.ValueRW.lastVelocity;
                
                // var acceleration = (currentVelocity - trackedAcceleration.ValueRW.lastVelocity) / deltaTime;
                // var jerk = (acceleration - trackedAcceleration.ValueRW.acceleration) / deltaTime;
                //
                // trackedAcceleration.ValueRW.jerk = jerk;
                // trackedAcceleration.ValueRW.acceleration = acceleration;
                // trackedAcceleration.ValueRW.lastVelocity = currentVelocity;
                
                
                
                // 1. Compute raw acceleration
                float3 rawAcceleration = (currentVelocity - previousVelocity) / deltaTime;
            
                // 2. Smooth acceleration (for example, via EMA)
                float3 smoothedAcceleration = ComputeEMA(
                    trackedAcceleration.ValueRO.acceleration,  // previous smoothed acceleration
                    rawAcceleration,
                    0.1f // alpha
                );

                // 3. Compute raw jerk: difference in acceleration / dt
                float3 rawJerk = (smoothedAcceleration - trackedAcceleration.ValueRO.acceleration) / deltaTime;
            
                // 4. Smooth jerk
                float3 smoothedJerk = ComputeEMA(
                    trackedAcceleration.ValueRO.jerk,  // previous smoothed jerk
                    rawJerk,
                    0.1f // alpha
                );

                // 5. Save back to components
                trackedAcceleration.ValueRW.acceleration = smoothedAcceleration;
                trackedAcceleration.ValueRW.jerk = smoothedJerk;

                // 6. Update previous velocity
                //prevVelocityData.ValueRW.Value = currentVelocity;
                
                
                // trackedAcceleration.ValueRW.jerk = jerk;
                // trackedAcceleration.ValueRW.acceleration = acceleration;
                trackedAcceleration.ValueRW.lastVelocity = currentVelocity;
            }
        }
        
        private static float3 ComputeEMA(float3 previousEma, float3 currentValue, float alpha)
        {
            return alpha * currentValue + (1f - alpha) * previousEma;
        }
    }
    
    [Serializable]
    public struct TrackedAccelerationComponent : IComponentData
    {
        public static TrackedAccelerationComponent Default => new TrackedAccelerationComponent
        {
            lastVelocity = float3.zero,
            acceleration = float3.zero,
            jerk = float3.zero
        };
        
        public float3 lastVelocity;
        public float3 acceleration;
        public float3 jerk;
    }
}