using System;
using System.Runtime.InteropServices;
using Boids.Domain.Obstacles;
using Dman.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Boids.Domain.Rendering
{
    
    [Serializable]
    public struct SDFObjectRenderData : IComponentData
    {
        public float radius;
        public float hardRadiusFraction;
        public float annularRadius;
        public float2 center;
        public float4 color;
        
        public SdfVariantData shapeVariant;

        public static SDFObjectRenderData FromShape(ObstacleShape shape, float hardSurface, float4 color, float2 worldCenter)
        {
            return new SDFObjectRenderData
            {
                radius = shape.obstacleRadius,
                hardRadiusFraction = hardSurface,
                annularRadius = shape.annularRadius,
                color = color,
                center = worldCenter,
                shapeVariant = SdfVariantData.FromShape(shape)
            };
        }
    }

        
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct SdfVariantData
    {
        [FieldOffset(0)]
        public int shapeType;
            
        [FieldOffset(4)]
        public FixedBytes16 variantData;
        [FieldOffset(4)]
        public CircleVariant circleVariant;
        [FieldOffset(4)]
        public BeamVariant beamVariant;
        [FieldOffset(4)]
        public BoxVariant boxVariant;

        public static SdfVariantData FromShape(ObstacleShape shape)
        {
            return new SdfVariantData()
            {
                shapeType = (int)shape.shapeVariant,
                variantData = shape.variantData
            };
        }
    }
    
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
    public partial class RenderSdfObjectsSystem : SystemBase
    {
        private RenderSdfSettings _settings = new();
        private GraphicsBuffer? _graphicsBuffer;
        private EntityQuery _sdfObjectQuery;

        protected override void OnCreate()
        {
            _settings = RenderSdfSettingsSingleton.Instance;
            _settings.SetSdfObjects(ref _graphicsBuffer, 0);
            
            _sdfObjectQuery = new EntityQueryBuilder(this.WorldUpdateAllocator)
                .WithAll<SDFObjectRenderData>()
                .Build(this);
            
            _sdfObjectQuery.SetChangedVersionFilter(typeof(SDFObjectRenderData));
            
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            var count = _sdfObjectQuery.CalculateEntityCount();
            if (count == 0) return;
            
            var sdfData = _sdfObjectQuery.ToComponentDataListAsync<SDFObjectRenderData>(
                World.UpdateAllocator.ToAllocator,
                this.Dependency,
                out var sdfObjectsDependency);
            
            _settings.SetSdfObjects(ref _graphicsBuffer, count);
            
            sdfObjectsDependency.Complete();
            var sdfDataArr = sdfData.AsArray();
            _graphicsBuffer?.SetData(sdfDataArr);
        }
    }
}