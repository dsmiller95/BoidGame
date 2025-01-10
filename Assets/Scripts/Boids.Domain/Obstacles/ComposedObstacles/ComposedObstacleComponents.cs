using System;
using Unity.Entities;

namespace Boids.Domain.Obstacles.ComposedObstacles
{
    /*
     * Composed obstacle entity layout
     *
     * composite:
     * - everything a normal obstacle has
     * - transform children are iterated
     * ComposedObstacleLink entity:
     * - LocalToWorld, LocalTransform, Parent
     * - child of the composite
     * - ObstacleComposedComponent
     * - SdfShapeComponent
     * - ObstacleMayDisableFlag
     * - ObstacleDisabledFlag (computed, optional)
     * - DraggableSdf
     * - SnapMeToGridFlag
     * - missing:
     *  - ScoringObstacleFlag
     *  - ObstacleComponent
     *  - OriginalColor, ObstacleRender, SDFObjectRenderData
     *
     * updates:
     * - ObstacleDisable system now reads composedObstacleLinks
     *  - the system must order entities? if we want it perfect. but, maybe its fine if we just run every frame.
     *  - enable an obstacle only if it is outside all zones and no children are disabled
     *  - disable an obstacle if it is inside any zone or any children are disabled
     * - ObstacleCompositeSystem exists
     *  - iterates through all composed obstacle links and copies data from them into the shape variant
     *  - copy the relative position of the link into BeamVariant.beamRelativeEnd
     * - ObstacleDrag system will work OK as long as these entities are children of the composite
     *
     */

    public struct CompositeObstacleFlag : IComponentData
    {
        
    }

[Serializable]
    public struct ObstacleControlPoint : IComponentData
    {
    }
    
    
    
}