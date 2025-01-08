using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Boids.Domain.Obstacles
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct ObstacleDisplaySystem : ISystem
    {
        private EntityQuery _enabledObstacles;
        private EntityQuery _disabledObstacles;
        
        public void OnCreate(ref SystemState state)
        {
            _enabledObstacles = new EntityQueryBuilder(state.WorldUpdateAllocator)
                .WithAll<SpriteRenderer, ObstacleComponent, OriginalColor>()
                .WithEnabledObstacles()
                .Build(ref state);
            
            _disabledObstacles = new EntityQueryBuilder(state.WorldUpdateAllocator)
                .WithAll<SpriteRenderer, ObstacleComponent, OriginalColor>()
                .WithDisabledObstacles()
                .Build(ref state);
        }
        
        public void OnUpdate(ref SystemState state)
        {
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
            
            enabledJob.Run(_enabledObstacles);
            disabledJob.Run(_disabledObstacles);
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