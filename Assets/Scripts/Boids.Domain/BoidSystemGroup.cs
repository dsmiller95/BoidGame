using Unity.Entities;

namespace Boids.Domain
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class BoidSystemGroup : ComponentSystemGroup
    {
        
    }
}