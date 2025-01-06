using Unity.Entities;

namespace Boids.Domain.Goals
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct GoalCountSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // get count somehow
            foreach (var (goal, goalCount) in 
                     SystemAPI.Query<RefRO<Goal>, RefRW<GoalCount>>())
            {
                var newCount = goalCount.ValueRO.count + 1;
                if (newCount > goal.ValueRO.required * 2)
                {
                    newCount = 0;
                }
                goalCount.ValueRW.count = newCount;
            }
        }
    }
}