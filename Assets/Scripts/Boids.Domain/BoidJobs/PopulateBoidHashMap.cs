using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Boids.Domain.BoidJobs
{
    internal struct OtherBoidData
    {
        public float2 Position;
        public float2 Velocity;
        public Entity Entity;
            
        public static OtherBoidData From(
            in PhysicsVelocity velocity,
            in LocalTransform presumedWorld,
            in Entity entity,
            in Boid boidVariant)
        {
            return new OtherBoidData
            {
                Position = presumedWorld.Position.xy,
                Velocity = velocity.Linear.xy / boidVariant.simSpeedMultiplier,
                Entity = entity
            };
        }
    }
    
    [BurstCompile]
    internal partial struct PopulateBoidHashMap : IJobEntity
    {
        public SpatialHashDefinition SpatialHashDefinition;
        public Boid BoidVariant;
            
        public NativeParallelMultiHashMap<int2, OtherBoidData>.ParallelWriter SpatialMapWriter;
            
        private void Execute(in PhysicsVelocity velocity, in LocalTransform presumedWorldTransform, in Entity entity)
        {
            var boidData = OtherBoidData.From(velocity, presumedWorldTransform, entity, BoidVariant);
            var cell = SpatialHashDefinition.GetCell(presumedWorldTransform.Position.xy);
            SpatialMapWriter.Add(cell, boidData);
        }
    }
}