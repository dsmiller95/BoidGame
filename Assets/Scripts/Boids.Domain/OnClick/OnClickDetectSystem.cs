using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Boids.Domain.OnClick
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class InputSystemGroup : ComponentSystemGroup
    {
        
    }
    
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(InputSystemGroup))]
    public partial class OnClickDetectSystem : SystemBase 
    {
        
        
        protected override void OnUpdate()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if(Camera.main is not {} mainCamera) return;
            
            var worldPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            worldPoint.z = 0;

            var newEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(newEntity, new OnClickEventComponent());
            EntityManager.AddComponentData(newEntity, LocalTransform.FromPosition(worldPoint));
        }
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(OnClickDetectSystem))]
    [BurstCompile]
    public partial struct OnClickRemoveSystem : ISystem
    {
        private EntityQuery _clickEventQuery;
        public void OnCreate(ref SystemState state)
        {
            _clickEventQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<OnClickEventComponent>()
                .Build(ref state);
        }
        
        public void OnUpdate(ref SystemState state)
        {
            state.EntityManager.DestroyEntity(_clickEventQuery);
        }
    }
}