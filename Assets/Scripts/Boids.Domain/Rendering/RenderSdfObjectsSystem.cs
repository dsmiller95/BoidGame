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
    public struct SDFObjectData : IComponentData
    {
        public float radius;
        public float hardRadiusFraction;
        public float2 center;
        public float4 color;
            
        public SdfVariantData shapeVariant;
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
        public SquareVariant squareVariant;
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
            _settings = SingletonLocator<RenderObstaclesConfig>.Instance.settings;
            _settings.SetSdfObjects(ref _graphicsBuffer, 0);
            
            _sdfObjectQuery = new EntityQueryBuilder(this.WorldUpdateAllocator)
                .WithAll<SDFObjectData>()
                .Build(this);
            
            _sdfObjectQuery.SetChangedVersionFilter(typeof(SDFObjectData));
            
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            var count = _sdfObjectQuery.CalculateEntityCount();
            if (count == 0) return;
            
            var sdfData = _sdfObjectQuery.ToComponentDataListAsync<SDFObjectData>(
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