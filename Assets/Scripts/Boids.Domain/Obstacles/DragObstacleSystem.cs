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
                float bestPriority = -1;
                var closestObstacleEntity = Entity.Null;
                float2 closestObstaclePosition = default;
                float4x4 closestParentTransform = float4x4.identity;
                
                foreach (var (shape, obstacleTransform, obstacleLocalToWorld, draggable, entity) in
                    SystemAPI.Query<RefRO<SdfShapeComponent>, RefRO<LocalTransform>, RefRO<LocalToWorld>, RefRO<DraggableSdf>>()
                        .WithNone<Dragging>()
                        .WithEntityAccess())
                {
                    float4x4 parentTransform = float4x4.identity;
                    if (SystemAPI.HasComponent<Parent>(entity))
                    {
                        var parentEntity = SystemAPI.GetComponent<Parent>(entity).Value;
                        parentTransform = SystemAPI.GetComponent<LocalToWorld>(parentEntity).Value;
                    }
                    
                    var obstaclePosition = obstacleLocalToWorld.ValueRO.Position.xy;
                    var relativeToObstacleCenter = dragBeginAt - obstaclePosition;
                    var normalizedDistance = shape.ValueRO.ReceivesDrag(
                        obstacleLocalToWorld.ValueRO, relativeToObstacleCenter);
                    if(normalizedDistance > 1) continue;
                    if(draggable.ValueRO.dragPriority < bestPriority) continue;
                    if(normalizedDistance > closestDistance) continue;
                    
                    bestPriority = draggable.ValueRO.dragPriority;
                    closestDistance = normalizedDistance;
                    closestObstacleEntity = entity;
                    closestObstaclePosition = obstaclePosition;
                    closestParentTransform = parentTransform;
                }
                if (closestDistance <= 1)
                {
                    var draggin = new Dragging
                    {
                        originalPostion = closestObstaclePosition,
                        spaceTransform = closestParentTransform,
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
                
                foreach (var (dragObstacleComponent, obstacleTransform, obstacleLocalToWorld) in
                         SystemAPI.Query<RefRO<Dragging>, RefRW<LocalTransform>, RefRO<LocalToWorld>>()
                             .WithAll<DraggableSdf>())
                {
                    if (dragObstacleComponent.ValueRO.dragId != dragId) continue;
                    
                    var mouseDelta = continueAt - dragObstacleComponent.ValueRO.originalClickPosition;
                    var newPosition = dragObstacleComponent.ValueRO.originalPostion + mouseDelta;
                    
                    var newPosTransformed = dragObstacleComponent.ValueRO.spaceTransform.InverseTransformPoint(new float3(newPosition, 0));
                    obstacleTransform.ValueRW = obstacleTransform.ValueRW.WithPosition(newPosTransformed);
                }
            }

            
            foreach (var dragEnd in 
                     SystemAPI.Query<RefRO<OnDragEndEvent>>())
            {
                var endAt = dragEnd.ValueRO.endedAt;
                var dragId = dragEnd.ValueRO.dragId;
                
                foreach (var (dragObstacleComponent, obstacleTransform, obstacleLocalToWorld, entity) in
                         SystemAPI.Query<RefRO<Dragging>, RefRW<LocalTransform>, RefRO<LocalToWorld>>()
                             .WithAll<DraggableSdf>()
                             .WithEntityAccess())
                {
                    if (dragObstacleComponent.ValueRO.dragId != dragId) continue;
                    
                    var mouseDelta = endAt - dragObstacleComponent.ValueRO.originalClickPosition;
                    var newPosition = dragObstacleComponent.ValueRO.originalPostion + mouseDelta;
                    
                    var newPosTransformed = dragObstacleComponent.ValueRO.spaceTransform.InverseTransformPoint(new float3(newPosition, 0));
                    obstacleTransform.ValueRW = obstacleTransform.ValueRW.WithPosition(newPosTransformed);
                    
                    ecb.RemoveComponent<Dragging>(entity);
                }
            }
            
            ecb.Playback(state.EntityManager);
        }
    }
    
    public struct DraggableSdf : IComponentData
    {
        public static DraggableSdf Default => new DraggableSdf
        {
            dragPriority = 1,
        };
        public static DraggableSdf HighPriority => new DraggableSdf
        {
            dragPriority = 100,
        };
        
        public float dragPriority;
        // local space
        //public float dragRadius;
    }
    
    public struct Dragging : IComponentData
    {
        public int dragId;
        public float2 originalPostion;
        public float2 originalClickPosition;
        public float4x4 spaceTransform;
    }
}