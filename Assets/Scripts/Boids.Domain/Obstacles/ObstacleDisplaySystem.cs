using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Boids.Domain.Obstacles
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct ObstacleDisplaySystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // consider adding change filtering on ObstacleDisabledFlag if this is a perf problem later
            var enabledObstacles = SystemAPI.QueryBuilder()
                .WithAll<SpriteRenderer, ObstacleComponent, OriginalColor>()
                .WithNone<ObstacleDisabledFlag>()
                .Build();
            var disabledObstacles = SystemAPI.QueryBuilder()
                .WithAll<SpriteRenderer, ObstacleComponent, OriginalColor, ObstacleDisabledFlag>()
                .Build();

            var disabledColor = Color.gray;
            
            var enabledJob = new ApplyColorChange
            {
                LerpToColor = disabledColor.ToFloat4(),
                Amount = 0f,
            };
            var disabledJob = new ApplyColorChange
            {
                LerpToColor = disabledColor.ToFloat4(),
                Amount = 0.7f,
            };
            
            enabledJob.Run(enabledObstacles);
            disabledJob.Run(disabledObstacles);
        }

        private partial struct ApplyColorChange : IJobEntity
        {
            public float4 LerpToColor;
            public float Amount;
            private void Execute(SpriteRenderer spriteRenderer, in OriginalColor originalColor)
            {
                var color = math.lerp(originalColor.Color, LerpToColor, Amount);
                spriteRenderer.color = new Color(color.x, color.y, color.z, color.w);
            }
        }
    }
}