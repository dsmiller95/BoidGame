using Boids.Domain.Obstacles;
using Unity.Burst;
using Unity.Entities;

namespace Boids.Domain
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ResetCompletionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            // detect if any obstacles are being dragged
            var draggingQuery = SystemAPI.QueryBuilder()
                .WithAll<Dragging>()
                .Build();
            if (draggingQuery.IsEmpty) return;
            
            // if they are, clear out all gameplay values

            var boidQuery = SystemAPI.QueryBuilder()
                .WithAll<Boid>()
                .Build();
            
            var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);
            
            ecb.DestroyEntity(boidQuery, EntityQueryCaptureMode.AtPlayback);
        }
    }
}