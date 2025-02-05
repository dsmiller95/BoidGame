﻿using Boids.Domain.Obstacles;
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
            foreach (var (objectData, localToWorld,
                         obstacleComponent, shapeComponent, obstacleRender) in 
                     SystemAPI.Query<RefRW<SDFObjectRenderData>, RefRO<LocalToWorld>,
                         RefRO<ObstacleComponent>, RefRO<SdfShapeComponent>, RefRO<ObstacleRender>>())
            {
                var obstacle = shapeComponent.ValueRO.GetWorldSpace(localToWorld.ValueRO, obstacleComponent.ValueRO);
                var shape = obstacle.shape;
                objectData.ValueRW = SDFObjectRenderData.FromShape(
                    shape,
                    obstacle.hardRadius,
                    obstacleRender.ValueRO.color,
                    localToWorld.ValueRO.Position.xy
                );
            }
            
            foreach (var (objectData, localToWorld,
                         shapeComponent, obstacleRender) in 
                     SystemAPI.Query<RefRW<SDFObjectRenderData>, RefRO<LocalToWorld>,
                         RefRO<SdfShapeComponent>, RefRO<ObstacleRender>>()
                         .WithNone<ObstacleComponent>())
            {
                var shape = shapeComponent.ValueRO.shapeData.GetWorldSpace(localToWorld.ValueRO);
                objectData.ValueRW = SDFObjectRenderData.FromShape(
                    shape,
                    0,
                    obstacleRender.ValueRO.color,
                    localToWorld.ValueRO.Position.xy
                );
            }
        }
    }
}