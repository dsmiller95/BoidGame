using Unity.Entities;

namespace Boids.Domain
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class BoidSystemGroup : ComponentSystemGroup
    {
        protected override void OnUpdate()
        {
            // May want to push time up like this?
            //World.PushTime();
            base.OnUpdate();
        }
    }
}