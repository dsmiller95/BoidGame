using System;
using Boids.Domain.TrackAcceleration;
using Dman.Utilities;
using Dman.Utilities.Logger;
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
            var hackedEntity = GetEntityFromHandleReflection(state.SystemHandle);
            if (!hackedEntity.HasValue || hackedEntity.Value == Entity.Null)
            {
                Log.Error("Could not get entity from system handle. reflection may have failed.");
                return;
            }

            world.EntityManager.AddBuffer<EmittedSound>(hackedEntity.Value);
            //var buffer = world.EntityManager.GetBuffer<EmittedSound>(state.SystemHandle);
        }
        
        private Entity? GetEntityFromHandleReflection(SystemHandle systemHandle)
        {
            var systemHandleEntityField = typeof(SystemHandle)
                .GetField("m_Entity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (systemHandleEntityField == null) return null;
            
            var entity = (Entity)systemHandleEntityField.GetValue(systemHandle);
            return entity;
        }

        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var world = state.WorldUnmanaged;
            var buffer = world.EntityManager.GetBuffer<EmittedSound>(state.SystemHandle);
            buffer.Clear();
            
            // var emitter = SingletonLocator<IEmitSoundEffects>.Instance;
            // if (emitter == null)
            // {
            //     return;
            // }
            // var maxEmitted = emitter.MaxEvents;
            var maxEmitted = 10;
            buffer.Capacity = math.max(buffer.Capacity, maxEmitted);

            var totalEmitted = 0;
            
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
            var buffer = world.EntityManager.GetBuffer<EmittedSound>(system, isReadOnly: true);
            return buffer.ToNativeArray(Allocator.Temp);
        }
    }

    [InternalBufferCapacity(16)]
    [Serializable]
    public struct EmittedSound : IBufferElementData
    {
        public SoundEffectEmit soundEffectEmit;
        
        public static EmittedSound Create(SoundEffectEmit soundEffectEmit)
        {
            return new EmittedSound
            {
                soundEffectEmit = soundEffectEmit
            };
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