using Unity.Burst;
using Unity.Entities;

namespace Boids.Domain.Audio
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct EmitSoundSystem : ISystem
    {
        
    }
}