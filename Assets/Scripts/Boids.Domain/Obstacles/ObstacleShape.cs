using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Boids.Domain.Obstacles
{

    public enum ShapeVariant : int
    {
        Sphere = 0,
        Beam = 1,
        Box = 2,
    }

    public interface ISdfDefinition
    {
        public float GetDistance(in float2 queryRelativeToCenter, in float radius);
    }
    
    [Serializable]
    public struct CircleVariant : ISdfDefinition
    {
        public readonly CircleVariant AdjustForScale(float linearScale, float rotation)
        {
            return this;
        }
        public readonly float GetDistance(in float2 queryRelativeToCenter, in float radius)
        {
            return math.length(queryRelativeToCenter);
        }
    }

    [Serializable]
    public struct BeamVariant : ISdfDefinition
    {
        public float2 beamRelativeEnd;

        public readonly BeamVariant AdjustForScale(float linearScale, float rotation)
        {
            var rotatedRelative = math.mul(float2x2.Rotate(rotation), beamRelativeEnd);
            return new BeamVariant
            {
                beamRelativeEnd = rotatedRelative * linearScale,
            };
        }

        public readonly float GetDistance(in float2 queryRelativeToCenter, in float radius)
        {
            var a = new float2(0, 0);
            var b = beamRelativeEnd;
            var p = queryRelativeToCenter;
            
            var ba = b - a;
            var pa = p - a;
            var h = math.clamp(math.dot(pa, ba) / math.dot(ba, ba), 0, 1);
            return math.length(pa - h * ba);
        }
    }


    [Serializable]
    public struct BoxVariant : ISdfDefinition
    {
        /// <summary>
        /// corner is normalized
        /// </summary>
        public float2 corner;
        public readonly BoxVariant AdjustForScale(float linearScale, float rotation)
        {
            var rotatedCorner = math.mul(float2x2.Rotate(rotation), corner);
            return new BoxVariant
            {
                corner = rotatedCorner * linearScale,
            };
        }
        public readonly float GetDistance(in float2 queryRelativeToCenter, in float radius)
        {
            float2 p = queryRelativeToCenter;
            float2 b = corner;
            
            float2 d = math.abs(p) - b;
            return math.length(math.max(d, 0.0f)) + math.min(math.max(d.x, d.y), 0.0f);
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct ObstacleShape
    {
        [FieldOffset(0)]
        public ShapeVariant shapeVariant;
        
        [FieldOffset(4)]
        public float obstacleRadius;

        [FieldOffset(8)]
        public float annularRadius;

        [FieldOffset(12)]
        public FixedBytes16 variantData;
        
        [FieldOffset(12)]
        public BeamVariant beamVariant;
        
        [FieldOffset(12)]
        public CircleVariant circleVariant;
        
        [FieldOffset(12)]
        public BoxVariant boxVariant;

        /// <summary>
        /// gets the maximum distance from the center which could be affected by this obstacle
        /// </summary>
        /// <remarks>
        /// The actual obstacle may be smaller, but will not be larger. Currently used only for util rendering.
        /// </remarks>
        public float MaximumExtent()
        {
            var baseExtent = obstacleRadius + annularRadius;
            switch (shapeVariant)
            {
                case ShapeVariant.Sphere:
                    return baseExtent;
                case ShapeVariant.Beam:
                    return baseExtent + math.length(beamVariant.beamRelativeEnd);
                case ShapeVariant.Box:
                    return baseExtent + math.length(boxVariant.corner);
                default:
                    throw new NotImplementedException("Unknown obstacle shape");
            }
        }
        
        
        public readonly (float, float2) GetNormalizedDistanceAndNormal(in float2 queryRelativeToCenter)
        {
            return shapeVariant switch
            {
                ShapeVariant.Sphere => GetNormalizedDistanceAndNormal(circleVariant, queryRelativeToCenter),
                ShapeVariant.Beam => GetNormalizedDistanceAndNormal(beamVariant, queryRelativeToCenter),
                ShapeVariant.Box => GetNormalizedDistanceAndNormal(boxVariant, queryRelativeToCenter),
                _ => throw new NotImplementedException("Unknown obstacle shape")
            };
        }
        
        /// <summary>
        /// Get the distance from the center of the obstacle.
        /// </summary>
        /// <param name="queryRelativeToCenter">the query point relative to the center of this obstacle</param>
        /// <returns>A value in [0..1) if inside the obstacle radius, or [1..) if outside</returns>
        /// <exception cref="NotImplementedException"></exception>
        public readonly float GetNormalizedDistance(in float2 queryRelativeToCenter)
        {
            return shapeVariant switch
            {
                ShapeVariant.Sphere => GetNormalizedDistance(circleVariant, queryRelativeToCenter),
                ShapeVariant.Beam => GetNormalizedDistance(beamVariant, queryRelativeToCenter),
                ShapeVariant.Box => GetNormalizedDistance(boxVariant, queryRelativeToCenter),
                _ => throw new NotImplementedException("Unknown obstacle shape")
            };
        }
        
        private readonly (float, float2) GetNormalizedDistanceAndNormal<T>(in T variant, in float2 relativeToCenter) where T : ISdfDefinition
        {
            const float epsilon = 0.01f;
            
            var dist = GetDistance(variant, relativeToCenter);
            var totalRadius = obstacleRadius;
            return (dist / totalRadius, GetNormal(variant, relativeToCenter, epsilon));
        }
        
        private readonly float GetNormalizedDistance<T>(in T variant, in float2 relativeToCenter) where T : ISdfDefinition
        {
            var dist = GetDistance(variant, relativeToCenter);
            var totalRadius = obstacleRadius;
            return dist / totalRadius;
        }
        
        private readonly float2 GetNormal<T>(in T variant, in float2 pos, float epsilon) where T : ISdfDefinition
        {
            var dX = GetDistance(variant, pos + new float2(epsilon, 0)) 
                     - GetDistance(variant,pos - new float2(epsilon, 0));
            var dY = GetDistance(variant,pos + new float2(0, epsilon))
                     - GetDistance(variant,pos - new float2(0, epsilon));
            return math.normalizesafe(new float2(dX, dY));
        }
        
        private readonly float GetDistance<T>(in T variant, in float2 relativeToCenter) where T : ISdfDefinition
        {
            var dist = variant.GetDistance(relativeToCenter, obstacleRadius);
            if (annularRadius > 0)
            {
                dist = dist - obstacleRadius;
                dist = math.abs(dist) - annularRadius;
                dist = dist + obstacleRadius;
            }

            return dist;
        }
    }
    
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct ShapeDataDefinition
    {
        [FieldOffset(0)]
        public ShapeVariant shapeVariant;
        
        [FieldOffset(4)]
        [Range(0f, 30f)]
        public float obstacleRadius;
        
        [FieldOffset(8)]
        public float annularRadius;
        
        [FieldOffset(12)]
        public BeamVariant beamVariant;
        
        [FieldOffset(12)]
        public CircleVariant circleVariant;
        
        [FieldOffset(12)]
        public BoxVariant boxVariant;
        
        public readonly ObstacleShape GetWorldSpace(in LocalToWorld localToWorld)
        {
            var presumedLinearScale = localToWorld.Value.GetPresumedLinearScale();
            var rotation = math.Euler(localToWorld.Value.Rotation()).z;
            return AdjustForScale(presumedLinearScale, rotation);
        }
        private readonly ObstacleShape AdjustForScale(float linearScale, float rotation)
        {
            var resultShape = new ObstacleShape
            {
                shapeVariant = this.shapeVariant,
                obstacleRadius = this.obstacleRadius * linearScale,
                annularRadius = this.annularRadius * linearScale,
            };
            switch (shapeVariant)
            {
                case ShapeVariant.Sphere:
                    resultShape.circleVariant = this.circleVariant.AdjustForScale(linearScale, rotation);
                    break;
                case ShapeVariant.Beam:
                    resultShape.beamVariant = this.beamVariant.AdjustForScale(linearScale, rotation);
                    break;
                case ShapeVariant.Box:
                    resultShape.boxVariant = this.boxVariant.AdjustForScale(linearScale, rotation);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return resultShape;
        }
    }
}