using System;
using Unity.Burst;
using Unity.Entities;

namespace Boids.Domain.Obstacles
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ObstacleScoringSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var world = state.WorldUnmanaged;
            world.EntityManager.AddComponentData(state.SystemHandle, new ScoredObstaclesData());
        }

        public void OnUpdate(ref SystemState state)
        {
            var scoringObstacleQuery = SystemAPI.QueryBuilder()
                .WithAll<ScoringObstacleFlag>()
                .WithNone<ObstacleDisabledFlag>()
                .Build();

            var scoringObstacleData = new ScoredObstaclesData()
            {
                totalScoringObstacles = scoringObstacleQuery.CalculateEntityCount()
            };
            state.WorldUnmanaged.EntityManager.SetComponentData(state.SystemHandle, scoringObstacleData);
        }
        
        public static ScoredObstaclesData GetScoringObstacleData(World world)
        {
            var scoringSystem = world.GetExistingSystem<ObstacleScoringSystem>();
            return world.EntityManager.GetComponentData<ScoredObstaclesData>(scoringSystem);
        }
    }

    public struct ScoringObstacleFlag : IComponentData
    {
        
    }
    
    [Serializable]
    public struct ScoredObstaclesData : IComponentData
    {
        public int totalScoringObstacles;
    }
}