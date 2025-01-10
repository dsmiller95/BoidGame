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
        public ShapeDataDefinition shape;
        [Range(0,1)]
        public float hardRadiusFraction;
        public Color color;
    }
    
    public class SdfObjectAuthoring : MonoBehaviour
    {
        public SdfPlainObject plainObject;

        private class SdfObjectBaker : Baker<SdfObjectAuthoring>
        {
            public override void Bake(SdfObjectAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, authoring.plainObject);
                AddComponent(entity, new SDFObjectRenderData());
            }
        }
    }
}