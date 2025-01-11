using System;
using Boids.Domain.TrackAcceleration;
using Dman.Utilities;
using Dman.Utilities.Logger;
using EntityJobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Boids.Domain.Audio
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct EmitSoundWhenJerk : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var world = state.WorldUnmanaged;
            var hackedEntity = state.SystemHandle.GetEntityViaReflection();
            if (!hackedEntity.HasValue || hackedEntity.Value == Entity.Null)
            {
                Log.Error("Could not get entity from system handle. reflection may have failed.");
                return;
            }

            world.EntityManager.AddBuffer<EmittedSound>(hackedEntity.Value);
        }

        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var world = state.WorldUnmanaged;
            var buffer = world.EntityManager.GetBuffer<EmittedSound>(state.SystemHandle);
            buffer.Clear();
            
            var maxEmitted = 10;
            buffer.Capacity = math.max(buffer.Capacity, maxEmitted);
            
            foreach (var (trackedAcceleration, emitSoundComponent, localToWorld, entity) in 
                     SystemAPI.Query<RefRO<TrackedAccelerationComponent>, RefRO<EmitSoundWhenJerkComponent>, RefRO<LocalToWorld>>()
                         .WithEntityAccess())
            {
                if (buffer.Length > maxEmitted) return;
                
                var emittedType = emitSoundComponent.ValueRO.TryEmit(trackedAcceleration.ValueRO);
                if (!emittedType.HasValue) continue;
                
                var position = localToWorld.ValueRO.Position.xy;
                var emitted = new SoundEffectEmit
                {
                    emittedFrom = entity,
                    type = emittedType.Value,
                    position = position
                };

                buffer.Add(EmittedSound.Create(emitted));
            }
        }
        
        public static NativeArray<EmittedSound> GetSoundData(World world)
        {
            var system = world.GetExistingSystem<EmitSoundWhenJerk>();
            var buffer = world.EntityManager.GetBuffer<EmittedSound>(system, isReadOnly: false);
            var bufferArr = buffer.ToNativeArray(Allocator.Temp);
            buffer.Clear();
            return bufferArr;
        }
    }

    [Serializable]
    public struct EmitSoundWhenJerkComponent : IComponentData
    {
        public SoundEffectType soundType;
        public float jerkThreshold;

        public readonly SoundEffectType? TryEmit(TrackedAccelerationComponent trackedAccelerationComponent)
        {
            if (math.length(trackedAccelerationComponent.jerk) > jerkThreshold)
            {
                return soundType;
            }

            return null;
        }
    }
}