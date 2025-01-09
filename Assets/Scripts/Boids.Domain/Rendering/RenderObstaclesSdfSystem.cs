using Boids.Domain.Obstacles;
using Dman.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Boids.Domain.Rendering
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
    [BurstCompile]
    public partial struct RenderObstaclesSdfSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (objectData, localToWorld, obstacleComponent, obstacleRender) in 
                     SystemAPI.Query<RefRW<SDFObjectData>, RefRO<LocalToWorld>, RefRO<ObstacleComponent>, RefRO<ObstacleRender>>())
            {
                var obstacle = obstacleComponent.ValueRO.GetWorldSpace(localToWorld.ValueRO);
                objectData.ValueRW = new SDFObjectData
                {
                    radius = obstacle.shape.obstacleRadius,
                    hardRadiusFraction = obstacle.obstacleHardSurfaceRadiusFraction,
                    color = obstacleRender.ValueRO.color,
                    center = localToWorld.ValueRO.Position.xy,
                    shapeVariant = new SdfVariantData()
                    {
                        shapeType = (int)obstacle.shape.shapeVariant,
                        variantData = obstacle.shape.variantData
                    }
                };
            }
        }
    }
}