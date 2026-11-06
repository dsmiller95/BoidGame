using System;
using Unity.Entities;
using UnityEngine;

namespace Boids.Domain.DebugFlags
{
    public struct DebugSpriteRenderer : IComponentData
    {
        public UnityObjectRef<SpriteRenderer> value;
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct DebugRenderSystem: ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (flag, spriteRenderer) in SystemAPI
                         .Query<RefRO<DebugFlagComponent>, RefRO<DebugSpriteRenderer>>())
            {
                var color = flag.ValueRO.flag switch
                {
                    FlagType.None => Color.white,
                    FlagType.Secondary => Color.red,
                    FlagType.Primary => Color.magenta,
                    _ => throw new ArgumentOutOfRangeException()
                };
                spriteRenderer.ValueRO.value.Value.color = color;
            }
        }
    }
}
