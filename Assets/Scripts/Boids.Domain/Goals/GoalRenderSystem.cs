using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Boids.Domain.Goals
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [BurstCompile]
    public partial struct GoalRenderSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (goal, goalCount, render) in 
                     SystemAPI.Query<RefRO<Goal>, RefRO<GoalCount>, RefRO<GoalRender>>())
            {
                var completionPercent = goal.ValueRO.GetCompletionPercent(goalCount.ValueRO);

                var volume = completionPercent;
                var scale = math.sqrt(volume);
                var newLocalTransform = LocalTransform.FromScale(scale);
                ecb.SetComponent(render.ValueRO.scaleForProgress, newLocalTransform);
            }
            ecb.Playback(state.EntityManager);
        }
    }
}