using System;
using Boids.Domain.GridSnap;
using Boids.Domain.Rendering;
using Dman.Utilities.Logger;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

namespace Boids.Domain.Obstacles.ComposedObstacles
{
    public class ObstacleControlPointAuthoring : MonoBehaviour
    {
        public ShapeDataDefinition shapeData = new ShapeDataDefinition()
        {
            shapeVariant = ShapeVariant.Sphere,
            obstacleRadius = 1f
        };

        public Color color;
        
        [FormerlySerializedAs("draggable")] public bool playerOwned = false;
        public bool snapToGrid = false;
        
        
        private class ComposedObstacleBaker : Baker<ObstacleControlPointAuthoring>
        {
            public override void Bake(ObstacleControlPointAuthoring authoring)
            {
                var parent = this.GetParent();
                var parentAuthoring = GetComponent<ObstacleAuthoring>(parent);
                if (parent == null || parentAuthoring == null)
                {
                    Log.Error("ComposedObstacle must be a child of an Obstacle");
                    return;
                }
                
                var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);
                
                AddComponent(entity, new SdfShapeComponent()
                {
                    shapeData = authoring.shapeData,
                });

                if (authoring.color != Color.clear)
                { 
                    var originalColor = authoring.color.ToFloat4();
                    AddComponent(entity, new OriginalColor
                    {
                        Color = originalColor,
                    });
                    AddComponent(entity, new ObstacleRender()
                    {
                        color = originalColor,
                    });
                    AddComponent(entity, new SDFObjectRenderData()
                    {
                        color = originalColor,
                    });
                }
                
                
                if (authoring.playerOwned)
                {
                    AddComponent(entity, DraggableSdf.HighPriority);
                    AddComponent(entity, new ObstacleMayDisableFlag());
                }
                if(authoring.snapToGrid || authoring.playerOwned)
                {
                    AddComponent(entity, new SnapMeToGridFlag());
                }
            }
        }
    }
}