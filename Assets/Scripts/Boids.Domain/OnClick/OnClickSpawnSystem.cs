using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Boids.Domain.OnClick
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InputSystemGroup))]
    [BurstCompile]
    public partial struct OnClickSpawnSystem : ISystem
    {
        private EntityQuery _clickEventQuery;
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (clickEvent, localToWorld) in 
                     SystemAPI.Query<RefRO<OnClickEventComponent>, RefRO<LocalTransform>>())
            {
                foreach (var onClickSpawn in
                         SystemAPI.Query<RefRO<OnClickSpawn>>())
                {
                    var spawned = ecb.Instantiate(onClickSpawn.ValueRO.Prefab);
                    var transform = onClickSpawn.ValueRO.defaultTransform;
                    transform = transform.WithPosition(localToWorld.ValueRO.Position);
                    ecb.SetComponent(spawned, transform);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}