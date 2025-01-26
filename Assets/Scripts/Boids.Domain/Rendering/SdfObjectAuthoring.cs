using System;
using Boids.Domain.Obstacles;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Boids.Domain.Rendering
{
    [Serializable]
    public struct SdfPlainObject : IComponentData
    {
        public float hardRadiusFraction;
        public float hardRadius;
        public Color color;
    }
    
    public class SdfObjectAuthoring : MonoBehaviour
    {
        public ShapeDataDefinition shape;
        public SdfPlainObject plainObject;

        public static bool Migrate(SdfObjectAuthoring component)
        {
            var hardRadius = component.plainObject.hardRadiusFraction * component.shape.obstacleRadius;
            if(!Mathf.Approximately(component.plainObject.hardRadius, hardRadius))
            {
                component.plainObject.hardRadius = hardRadius;
                return true;
            }
            return false;
        }

        private class SdfObjectBaker : Baker<SdfObjectAuthoring>
        {
            public override void Bake(SdfObjectAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, authoring.plainObject);
                AddComponent(entity, new SdfShapeComponent
                {
                    shapeData = authoring.shape,
                });
                AddComponent(entity, new SDFObjectRenderData());
            }
        }
    }
}