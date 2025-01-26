using Boids.Domain.Obstacles;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Boids.Domain.Rendering
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
    [BurstCompile]
    public partial struct RenderPlainSdfObjectsSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (objectData, plainObject, shapeComponent, localToWorld) in 
                     SystemAPI.Query<RefRW<SDFObjectRenderData>, RefRO<SdfPlainObject>, RefRO<SdfShapeComponent>, RefRO<LocalToWorld>>())
            {
                var shape = shapeComponent.ValueRO.shapeData.GetWorldSpace(localToWorld.ValueRO);
                
                objectData.ValueRW = SDFObjectRenderData.FromShape(
                    shape,
                    plainObject.ValueRO.hardRadius,
                    plainObject.ValueRO.color.ToFloat4(),
                    localToWorld.ValueRO.Position.xy
                );
            }
        }
    }
}