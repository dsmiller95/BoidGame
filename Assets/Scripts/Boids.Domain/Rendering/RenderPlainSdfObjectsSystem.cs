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
                
                objectData.ValueRW = SDFObjectData.FromShape(
                    shape,
                    plainObject.ValueRO.hardRadiusFraction,
                    plainObject.ValueRO.color.ToFloat4(),
                    localToWorld.ValueRO.Position.xy
                );
            }
        }
    }
}