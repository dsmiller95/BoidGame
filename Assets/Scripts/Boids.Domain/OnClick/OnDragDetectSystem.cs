using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Boids.Domain.OnClick
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(InputSystemGroup))]
    public partial class OnDragDetectSystem : SystemBase
    {
        private int _dragIdx = 1;
        private DragState _dragState = DragState.NotDragging;

        private enum DragState
        {
            Dragging,
            NotDragging
        }
        
        protected override void OnUpdate()
        {
            var beganDragging = Input.GetMouseButtonDown(0);
            var isDragging = Input.GetMouseButton(0);
            var endedDragging = Input.GetMouseButtonUp(0);
            
            if (!(beganDragging || isDragging || endedDragging)) return;
            if(Camera.main is not {} mainCamera) return;
            float2 worldPoint = new float3(mainCamera.ScreenToWorldPoint(Input.mousePosition)).xy;

            if (beganDragging && endedDragging)
            {
                // noop
                return;
            }

            var isBeginDrag = beganDragging;
            var isEndDrag = endedDragging;
            var isDragContinue = isDragging && !isBeginDrag && !isEndDrag;

            if (isBeginDrag && _dragState != DragState.NotDragging)
            {
                Debug.LogError("Beginning to drag, but am already dragging. aborting.");
                return;
            }
            if(isDragContinue && _dragState != DragState.Dragging)
            {
                Debug.LogError("Continuing to drag, but am not dragging. aborting.");
                return;
            }
            if(isEndDrag && _dragState != DragState.Dragging)
            {
                Debug.LogError("Ending to drag, but am not dragging. aborting.");
                return;
            }
            
            if (isBeginDrag)
            {
                _dragIdx++;
                var beginDragEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(beginDragEntity, new OnDragBeginEvent
                {
                    dragId = _dragIdx,
                    beginAt = worldPoint
                });
                var activeDragEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(activeDragEntity, new ActiveDragComponent
                {
                    dragId = _dragIdx,
                    continueAt = worldPoint
                });
                _dragState = DragState.Dragging;
            }
            if (isDragContinue)
            {
                if(!SystemAPI.TryGetSingletonRW(out RefRW<ActiveDragComponent> activeDrag))
                {
                    Debug.LogError("Failed to get active drag component when dragging");
                    return;
                }
                activeDrag.ValueRW.continueAt = worldPoint;
            }
            if (isEndDrag)
            { 
                if(!SystemAPI.TryGetSingletonEntity<ActiveDragComponent>(out Entity activeDragEntity))
                {
                    Debug.LogError("Failed to get active drag component when ending drag");
                    return;
                }
                EntityManager.DestroyEntity(activeDragEntity);
                
                var endDragEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(endDragEntity, new OnDragEndEvent
                {
                    dragId = _dragIdx,
                    endedAt = worldPoint
                });
                _dragState = DragState.NotDragging;
            }
        }
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(OnDragDetectSystem))]
    [BurstCompile]
    public partial struct OnDragRemoveSystem : ISystem
    {
        private EntityQuery _clickEventQuery;
        public void OnCreate(ref SystemState state)
        {
            _clickEventQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<OnDragBeginEvent, OnDragEndEvent>()
                .Build(ref state);
        }
        
        public void OnUpdate(ref SystemState state)
        {
            state.EntityManager.DestroyEntity(_clickEventQuery);
        }
    }
}