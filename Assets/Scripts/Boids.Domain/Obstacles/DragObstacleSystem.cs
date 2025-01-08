using Boids.Domain.OnClick;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Boids.Domain.Obstacles
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InputSystemGroup))]
    [BurstCompile]
    public partial struct DragObstacleSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            
        }

        public void OnUpdate(ref SystemState state)
        {
            var world = state.WorldUnmanaged;
            var ecb = new EntityCommandBuffer(world.UpdateAllocator.ToAllocator);
            
            // pick the closest draggable obstacle and begin dragging it.
            foreach (var dragBegin in 
                SystemAPI.Query<RefRO<OnDragBeginEvent>>())
            {
                var dragBeginAt = dragBegin.ValueRO.beginAt;
                var closestDistance = float.MaxValue;
                var closestObstacleEntity = Entity.Null;
                Obstacle closestObstacle = default;
                float2 closestObstaclePosition = default;
                
                foreach (var (dragObstacleComponent, obstacleLocalToWorld, entity) in
                    SystemAPI.Query<RefRO<ObstacleComponent>, RefRO<LocalToWorld>>()
                        .WithAll<DraggableObstacle>()
                        .WithNone<Dragging>()
                        .WithEntityAccess())
                {
                    var obstaclePosition = obstacleLocalToWorld.ValueRO.Position.xy;
                    var obstacle = dragObstacleComponent.ValueRO.GetWorldSpace(obstacleLocalToWorld.ValueRO);
                    var relativeToObstacleCenter = dragBeginAt - obstaclePosition;
                    var normalizedDistance = obstacle.GetNormalizedDistance(relativeToObstacleCenter);
                    if(normalizedDistance > closestDistance) continue;
                    closestDistance = normalizedDistance;
                    closestObstacleEntity = entity;
                    closestObstacle = obstacle;
                    closestObstaclePosition = obstaclePosition;
                }
                if (closestDistance < float.MaxValue && closestObstacle.IsInsideHardSurface(closestDistance))
                {
                    var draggin = new Dragging
                    {
                        originalPostion = closestObstaclePosition,
                        originalClickPosition = dragBeginAt,
                        dragId = dragBegin.ValueRO.dragId,
                    };
                    ecb.AddComponent(closestObstacleEntity, draggin);
                }
            }

            
            foreach (var activeDrag in 
                SystemAPI.Query<RefRO<ActiveDragComponent>>())
            {
                var continueAt = activeDrag.ValueRO.continueAt;
                var dragId = activeDrag.ValueRO.dragId;
                
                foreach (var (dragObstacleComponent, obstacleTransform) in
                         SystemAPI.Query<RefRO<Dragging>, RefRW<LocalTransform>>()
                             .WithAll<DraggableObstacle>())
                {
                    if (dragObstacleComponent.ValueRO.dragId != dragId) continue;
                    
                    var mouseDelta = continueAt - dragObstacleComponent.ValueRO.originalClickPosition;
                    var newPosition = dragObstacleComponent.ValueRO.originalPostion + mouseDelta;
                    obstacleTransform.ValueRW = obstacleTransform.ValueRW.WithPosition(new float3(newPosition, 0));
                }
            }

            
            foreach (var dragEnd in 
                     SystemAPI.Query<RefRO<OnDragEndEvent>>())
            {
                var endAt = dragEnd.ValueRO.endedAt;
                var dragId = dragEnd.ValueRO.dragId;
                
                foreach (var (dragObstacleComponent, obstacleTransform, entity) in
                         SystemAPI.Query<RefRO<Dragging>, RefRW<LocalTransform>>()
                             .WithAll<DraggableObstacle>()
                             .WithEntityAccess())
                {
                    if (dragObstacleComponent.ValueRO.dragId != dragId) continue;
                    
                    var mouseDelta = endAt - dragObstacleComponent.ValueRO.originalClickPosition;
                    var newPosition = dragObstacleComponent.ValueRO.originalPostion + mouseDelta;
                    obstacleTransform.ValueRW = obstacleTransform.ValueRW.WithPosition(new float3(newPosition, 0));
                    ecb.RemoveComponent<Dragging>(entity);
                }
            }
            
            ecb.Playback(state.EntityManager);
        }
    }
    
    public struct DraggableObstacle : IComponentData
    {
        
    }
    
    public struct Dragging : IComponentData
    {
        public int dragId;
        public float2 originalPostion;
        public float2 originalClickPosition;
    }
}