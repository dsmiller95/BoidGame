using Unity.Entities;

namespace Boids.Domain.DebugFlags
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(BoidSystemGroup))]
    [UpdateBefore(typeof(BoidSteerSystem))]
    public partial struct DebugUnflagSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var flag in SystemAPI.Query<RefRW<DebugFlagComponent>>())
            {
                flag.ValueRW.flag = FlagType.None;
            }
        }
    }
}