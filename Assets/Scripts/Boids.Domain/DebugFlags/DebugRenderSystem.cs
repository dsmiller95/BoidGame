using Unity.Entities;
using UnityEngine;

namespace Boids.Domain.DebugFlags
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct DebugRenderSystem: ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder()
                .WithAll<SpriteRenderer, DebugFlagComponent>()
                .Build();
            var job = new WriteDebugColors();
            job.Run(query);
        }

        private partial struct WriteDebugColors : IJobEntity
        {
            private void Execute(in DebugFlagComponent flag, SpriteRenderer spriteRenderer)
            {
                if (flag.isFlagged)
                {
                    spriteRenderer.color = Color.red;
                }
                else
                {
                    spriteRenderer.color = Color.white;
                }
            }
        }
    }
}