using Unity.Entities;

namespace Boids.Domain
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default, childDefaultFlags: WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
    public partial class MoveObstaclesSystemGroup : ComponentSystemGroup
    {
        
    }
}