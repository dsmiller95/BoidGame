using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Boids.Domain
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(BoidSystemGroup))]
    public partial struct BoidInitializeSystem : ISystem
    {
        private uint _seedOffset;
        
        public void OnCreate(ref SystemState state)
        {
            _seedOffset = 2899;
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _seedOffset += 1;
            var rng = Random.CreateFromIndex(_seedOffset);
            var time = (float)state.WorldUnmanaged.Time.ElapsedTime;
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (boidShared, boidSpawn, velocity, entity) in SystemAPI
                         .Query<Boid, BoidSpawnData, RefRW<PhysicsVelocity>>()
                         .WithEntityAccess())
            {
                NewBoidHelper.Initialize(entity, boidShared, boidSpawn, velocity, ref rng, ecb, time);
            }
            ecb.Playback(state.EntityManager);
        }
    }
}