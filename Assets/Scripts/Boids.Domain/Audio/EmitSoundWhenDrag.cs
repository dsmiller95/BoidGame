using System;
using Boids.Domain.Obstacles;
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
    public partial struct EmitSoundWhenDrag : ISystem
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
            var buffer = world.EntityManager.GetBuffer<EmittedSound>(state.SystemHandle, isReadOnly: false);
            
            var maxEmitted = 10;
            buffer.Capacity = math.max(buffer.Capacity, maxEmitted);
            
            foreach (var (wasDragging, isDragging, emitSoundComponent, localToWorld) in 
                     SystemAPI.Query<RefRW<WasDragging>, RefRO<IsDragging>, RefRO<EmitSoundWhenDragComponent>, RefRO<LocalToWorld>>()
                         .WithChangeFilter<IsDragging>())
            {
                if (buffer.Length > maxEmitted) return;
                
                if(isDragging.ValueRO.value == wasDragging.ValueRO.value) continue;
                wasDragging.ValueRW.value = isDragging.ValueRO.value;
                
                var emittedType = emitSoundComponent.ValueRO.TryEmit(becameDragging: isDragging.ValueRO.value);
                if (!emittedType.HasValue) continue;
                
                var position = localToWorld.ValueRO.Position.xy;
                var emitted = new SoundEffectEmit
                {
                    type = emittedType.Value,
                    position = position
                };

                buffer.Add(EmittedSound.Create(emitted));
            }
        }
        
        public static NativeArray<EmittedSound> GetSoundData(World world)
        {
            var system = world.GetExistingSystem<EmitSoundWhenDrag>();
            var buffer = world.EntityManager.GetBuffer<EmittedSound>(system, isReadOnly: false);
            var bufferArr = buffer.ToNativeArray(Allocator.Temp);
            buffer.Clear();
            return bufferArr;
        }
    }

    [Serializable]
    public struct WasDragging : IComponentData
    {
        public static WasDragging Default => NotDragging;
        public static WasDragging Dragging => new() {value = true};
        public static WasDragging NotDragging => new() {value = false};
        
        public bool value;
    }
    
    [Serializable]
    public struct EmitSoundWhenDragComponent : IComponentData
    {
        public SoundEffectType pickupSoundType;
        public SoundEffectType dropSoundType;

        public readonly SoundEffectType? TryEmit(bool becameDragging)
        {
            return becameDragging ? pickupSoundType : dropSoundType;
        }
    }
}