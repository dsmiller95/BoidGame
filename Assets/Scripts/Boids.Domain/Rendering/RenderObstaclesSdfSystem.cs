using System;
using System.Runtime.InteropServices;
using Boids.Domain.Obstacles;
using Dman.Utilities;
using Dman.Utilities.Logger;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Boids.Domain.Rendering
{
    [Serializable]
    public class RenderSdfSettings
    {
        public Material? sdfMaterial;
        
        public void SetSdfObjects(ref GraphicsBuffer? buffer, int count)
        {
            if (sdfMaterial == null)
            {
                Log.Error("SDF Material is null");
                return;
            }

            sdfMaterial.SetInt("_SDFObjectCount", count);
            if (count > 0)
            {
                if (buffer == null || buffer.count < count)
                {
                    buffer = CreateBuffer(count);
                }
                sdfMaterial.SetBuffer("_SDFObjects", buffer);
            }
        }

        private GraphicsBuffer CreateBuffer(int count)
        {
            return new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                count,
                System.Runtime.InteropServices.Marshal.SizeOf(typeof(SDFObjectData))
            );
        }
        
        [Serializable]
        public struct SDFObjectData
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
            public BeamVariant beamVariant;
            [FieldOffset(4)]
            public CircleVariant circleVariant;
            [FieldOffset(4)]
            public SquareVariant squareVariant;
        }
    }
    
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
    public partial class RenderObstaclesSdfSystem : SystemBase
    {

        private RenderSdfSettings _settings = new();
        private GraphicsBuffer? _graphicsBuffer;
        private EntityQuery _obstacleQuery;

        protected override void OnCreate()
        {
            _settings = SingletonLocator<RenderObstaclesConfig>.Instance.settings;
            _settings.SetSdfObjects(ref _graphicsBuffer, 0);
            
            _obstacleQuery = new EntityQueryBuilder(this.WorldUpdateAllocator)
                .WithAll<ObstacleComponent, ObstacleRender, LocalToWorld>()
                //.WithEnabledObstacles()
                .Build(this);
            
            // _obstacleQuery.AddChangedVersionFilter(typeof(LocalToWorld));
            // _obstacleQuery.AddChangedVersionFilter(typeof(ObstacleComponent));
            //_obstacleQuery.AddChangedVersionFilter(typeof(ObstacleRender));
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            var obstacleCount = _obstacleQuery.CalculateEntityCount();
            if (obstacleCount == 0) return;

            var sdfObjects = CollectionHelper.CreateNativeArray<RenderSdfSettings.SDFObjectData, RewindableAllocator>
                (obstacleCount, ref this.World.UpdateAllocator);
            
            var collectJob = new CollectObstacleSdfJob
            {
                OutSdfData = sdfObjects
            };
            
            var collectSDFDep = collectJob.Schedule(_obstacleQuery, this.Dependency);
            
            _settings.SetSdfObjects(ref _graphicsBuffer, obstacleCount);
            
            collectSDFDep.Complete();
            _graphicsBuffer?.SetData(sdfObjects);
        }

        [BurstCompile]
        private partial struct CollectObstacleSdfJob : IJobEntity
        {
            public NativeArray<RenderSdfSettings.SDFObjectData> OutSdfData;

            void Execute(
                [EntityIndexInQuery] int entityIndexInQuery,
                in LocalToWorld localToWorld,
                in ObstacleComponent obstacleComponent, in ObstacleRender obstacleRender)
            {
                var obstacle = obstacleComponent.GetWorldSpace(localToWorld);
                OutSdfData[entityIndexInQuery] = new RenderSdfSettings.SDFObjectData
                {
                    radius = obstacle.shape.obstacleRadius,
                    hardRadiusFraction = obstacle.obstacleHardSurfaceRadiusFraction,
                    color = obstacleRender.color,
                    center = localToWorld.Position.xy,
                    shapeVariant = new RenderSdfSettings.SdfVariantData()
                    {
                        shapeType = (int)obstacle.shape.shapeVariant,
                        variantData = obstacle.shape.variantData
                    }
                };
            }
        }
    }
}