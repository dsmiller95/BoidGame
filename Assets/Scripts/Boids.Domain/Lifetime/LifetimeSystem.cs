using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Boids.Domain.Lifetime
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct LifetimeSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            var time = state.WorldUnmanaged.Time.ElapsedTime;
            foreach (var (lifetime, entity) in 
                     SystemAPI.Query<RefRO<LifetimeComponent>>().WithEntityAccess())
            {
                if (lifetime.ValueRO.deathTime < time)
                {
                    ecb.DestroyEntity(entity);
                }
            }
            ecb.Playback(state.EntityManager);
        }
    }
}