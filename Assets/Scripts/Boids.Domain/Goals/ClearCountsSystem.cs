using Unity.Burst;
using Unity.Entities;

namespace Boids.Domain.Goals
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(BoidSystemGroup))]
    [BurstCompile]
    internal partial struct ClearCountsSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var goalCount in 
                     SystemAPI.Query<RefRW<GoalCount>>())
            {
                goalCount.ValueRW.count = 0;
            }
        }
    }
}