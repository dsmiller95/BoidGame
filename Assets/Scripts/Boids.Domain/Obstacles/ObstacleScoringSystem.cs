using System;
using Unity.Burst;
using Unity.Entities;

namespace Boids.Domain.Obstacles
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ObstacleScoringSystem : ISystem
    {
        private EntityQuery _scoringObstacleQuery;
        public void OnCreate(ref SystemState state)
        {
            var world = state.WorldUnmanaged;
            world.EntityManager.AddComponentData(state.SystemHandle, new ScoredObstaclesData());
            
            _scoringObstacleQuery = new EntityQueryBuilder(state.WorldUpdateAllocator)
                .WithAll<ScoringObstacleFlag>()
                .WithEnabledObstacles()
                .Build(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            var scoringObstacleData = new ScoredObstaclesData()
            {
                totalScoringObstacles = _scoringObstacleQuery.CalculateEntityCount()
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