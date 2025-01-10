using Boids.Domain.OnClick;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Boids.Domain.Obstacles.ComposedObstacles
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MoveObstaclesSystemGroup), OrderLast = true)]
    [BurstCompile]
    public partial struct ComposedObstacleDataCopySystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (composedObstacle, children) in
                SystemAPI.Query<RefRW<SdfShapeComponent>, DynamicBuffer<Child>>()
                    .WithAll<CompositeObstacleFlag>())
            {
                if(children.Length <= 0) continue;
                
                
                var obstacleShape = composedObstacle.ValueRO;

                for (var i = 0; i < children.Length; i++)
                {
                    //Debug.Log("Copying data to child " + i);
                    Child child = children[i];
                    var childTransform = SystemAPI.GetComponent<LocalTransform>(child.Value);
                    var controlPoint = childTransform.Position.xy;
                    obstacleShape.ApplyControlPointToVariant(i, controlPoint);
                }

                composedObstacle.ValueRW = obstacleShape;
            }
        }

    }
}