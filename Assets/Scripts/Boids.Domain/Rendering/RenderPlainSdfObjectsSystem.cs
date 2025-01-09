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
            foreach (var (objectData, plainObject, localToWorld) in 
                     SystemAPI.Query<RefRW<SDFObjectData>, RefRO<SdfPlainObject>, RefRO<LocalToWorld>>())
            {
                var shape = plainObject.ValueRO.shape.GetWorldSpace(localToWorld.ValueRO);
                objectData.ValueRW = new SDFObjectData
                {
                    radius = shape.obstacleRadius,
                    hardRadiusFraction = plainObject.ValueRO.hardRadiusFraction,
                    color = plainObject.ValueRO.color.ToFloat4(),
                    center = localToWorld.ValueRO.Position.xy,
                    shapeVariant = SdfVariantData.FromShape(shape)
                };
            }
        }
    }
}