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
        [Obsolete("use SdfShapecomponent instead")]
        public ShapeDataDefinition shape;
        [Range(0,1)]
        public float hardRadiusFraction;
        public Color color;
    }
    
    public class SdfObjectAuthoring : MonoBehaviour
    {
        public ShapeDataDefinition shape;
        public SdfPlainObject plainObject;

        public static bool Migrate(SdfObjectAuthoring component)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (component.shape.Equals(component.plainObject.shape)) return false;
            component.shape = component.plainObject.shape;
            return true;
#pragma warning restore CS0618 // Type or member is obsolete
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