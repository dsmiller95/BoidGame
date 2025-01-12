using System;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Boids.Domain.Goals
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct GoalScoringSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var world = state.WorldUnmanaged;
            world.EntityManager.AddComponentData(state.SystemHandle, new ScoredGoalsData());
        }

        public void OnUpdate(ref SystemState state)
        {
            var completionAllGoals = 0f;
            var unclampedCompletionAllGoals = 0f;
            var totalGoals = 0;

            foreach (var (goal, goalCount) in SystemAPI.Query<RefRO<Goal>, RefRO<GoalCount>>())
            {
                unclampedCompletionAllGoals += goal.ValueRO.GetUnclampedCompletionPercent(goalCount.ValueRO);
                completionAllGoals += goal.ValueRO.GetCompletionPercent(goalCount.ValueRO);
                totalGoals++;
            }
            
            var scoredData = new ScoredGoalsData()
            {
                scoreCompletionPercent = totalGoals == 0 ? 0 : completionAllGoals / totalGoals,
                unclampedScoreCompletionPercent = totalGoals == 0 ? 0 : unclampedCompletionAllGoals / totalGoals
            };
            state.WorldUnmanaged.EntityManager.SetComponentData(state.SystemHandle, scoredData);
        }
        
        public static ScoredGoalsData GetScoringData(World world)
        {
            var scoringSystem = world.GetExistingSystem<GoalScoringSystem>();
            return world.EntityManager.GetComponentData<ScoredGoalsData>(scoringSystem);
        }
    }
    
    [Serializable]
    public struct ScoredGoalsData : IComponentData
    {
        [Range(0, 1)]
        public float scoreCompletionPercent;
        public float unclampedScoreCompletionPercent;
        
        public bool IsCompleted => scoreCompletionPercent >= 1;
    }
}