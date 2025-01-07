using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Boids.Domain.Goals
{
    
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct DecayGoalCountSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = (float)state.World.Time.DeltaTime;
            foreach (var (goal, goalCount) in 
                     SystemAPI.Query<RefRO<Goal>, RefRW<GoalCount>>())
            {
                var decayAmount = deltaTime * goal.ValueRO.decayPerSecond;
                var fullDecay = goalCount.ValueRW.partialCount - decayAmount;
                var intDecay = (int)math.floor(fullDecay);
                goalCount.ValueRW.count += intDecay;
                goalCount.ValueRW.partialCount = fullDecay - intDecay;
            }
        }
    }
}