using System;
using System.ComponentModel;
using Boids.Domain.Rendering;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Boids.Domain.Zones
{
    public struct ZoneTypeNullTag : IComponentData
    {
    }
    
    public struct Zone
    {
        public float2 MinWorld;
        public float2 MaxWorld;
    }
    
    public struct ZoneComponent : IComponentData
    {
        public float2 Extents;
        
        
        public readonly Zone GetRelative(in LocalToWorld localToWorld)
        {
            var min = math.mul(localToWorld.Value, new float4(-Extents, 0, 1)).xy;
            var max = math.mul(localToWorld.Value, new float4(Extents, 0, 1)).xy;
            return new Zone
            {
                MinWorld = min,
                MaxWorld = max
            };
        }
    }

    public enum ZoneType
    {
        NullZone
    }
    
    public class ZoneAuthoring : MonoBehaviour
    {
        public ZoneType type;
        public Vector2 extents;

        private class ZoneBaker : Baker<ZoneAuthoring>
        {
            public override void Bake(ZoneAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                switch (authoring.type)
                {
                    case ZoneType.NullZone:
                        AddComponent(entity, new ZoneTypeNullTag());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                AddComponent(entity, new ZoneComponent
                {
                    Extents = authoring.extents,
                });
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Vector2 myCenter = transform.position;
            Gizmos.DrawWireCube(myCenter, extents * 2);
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            var childRenderer = GetComponentInChildren<SdfObjectAuthoring>();
            if (childRenderer != null)
            {
                childRenderer.shape.boxVariant.corner = extents;
                // MARK AS DIRTY
                UnityEditor.EditorUtility.SetDirty(childRenderer);
            }
        }
        #endif
    }
}