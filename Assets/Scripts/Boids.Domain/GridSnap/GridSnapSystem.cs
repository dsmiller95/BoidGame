using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Boids.Domain.GridSnap
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MoveObstaclesSystemGroup))]
    [BurstCompile]
    public partial struct GridSnapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var world = state.WorldUnmanaged;
            var gridDefinition = GridDefinition.Default;
            world.EntityManager.AddComponentData(state.SystemHandle, gridDefinition);
        }

        public void OnUpdate(ref SystemState state)
        {
            var gridDefinition = state.WorldUnmanaged.EntityManager.GetComponentData<GridDefinition>(state.SystemHandle);

            foreach (var transform in
                     SystemAPI.Query<RefRW<LocalTransform>>()
                         .WithAll<SnapMeToGridFlag>()
                         .WithNone<Parent>()
                         .WithChangeFilter<LocalTransform>()
                    )
            {
                var position = transform.ValueRO.Position.xy;
                position = gridDefinition.SnapToClosest(position);

                transform.ValueRW = transform.ValueRW.WithPosition(new float3(position, 0));
            }

            foreach (var (transform, parent) in 
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<Parent>>()
                         .WithAll<SnapMeToGridFlag>()
                         .WithChangeFilter<LocalTransform>()
                    )
            {
                var parentTransform = SystemAPI.GetComponent<LocalToWorld>(parent.ValueRO.Value);


                var worldPosition = parentTransform.Value.TransformPoint(transform.ValueRO.Position);
                var newWorldPosition = new float3(gridDefinition.SnapToClosest(worldPosition.xy), 0);

                var newLocalPosition = parentTransform.Value.InverseTransformPoint(newWorldPosition);
                
                
                transform.ValueRW = transform.ValueRW.WithPosition(newLocalPosition);
            }
        }
    }

    [Serializable]
    public struct GridDefinition : IComponentData
    {
        public static GridDefinition Default => new GridDefinition
        {
            gridSize = 10f/2f
        };
        
        public float gridSize;
        
        public float2 SnapToClosest(float2 position)
        {
            return Tiling.FindHexCenter(position, gridSize);
        }
    }

    public struct SnapMeToGridFlag : IComponentData { }
}