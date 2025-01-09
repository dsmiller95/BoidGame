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
        Square = 2,
    }

    public interface ISdfDefinition
    {
        public float GetDistance(in float2 queryRelativeToCenter);
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

        public readonly float GetDistance(in float2 queryRelativeToCenter)
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
    public struct CircleVariant : ISdfDefinition
    {
        public readonly CircleVariant AdjustForScale(float linearScale, float rotation)
        {
            return this;
        }
        public readonly float GetDistance(in float2 queryRelativeToCenter)
        {
            return math.length(queryRelativeToCenter);
        }
    }


    [Serializable]
    public struct SquareVariant : ISdfDefinition
    {
        public float2 corner;
        public readonly SquareVariant AdjustForScale(float linearScale, float rotation)
        {
            return this;
        }
        public readonly float GetDistance(in float2 queryRelativeToCenter)
        {
            // TODO
            return math.length(queryRelativeToCenter);
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
        public SquareVariant squareVariant;

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
                case ShapeVariant.Square:
                    return baseExtent;
                default:
                    throw new NotImplementedException("Unknown obstacle shape");
            }
        }
        
        
        
        /// <summary>
        /// Get the distance from the center of the obstacle.
        /// </summary>
        /// <param name="queryRelativeToCenter">the query point relative to the center of this obstacle</param>
        /// <returns>A value in [0..1) if inside the obstacle radius, or [1..) if outside</returns>
        /// <exception cref="NotImplementedException"></exception>
        public readonly float GetNormalizedDistance(in float2 queryRelativeToCenter)
        {
            var dist = GetDistance(queryRelativeToCenter);
            return dist / obstacleRadius;
        }
        public readonly (float, float2) GetNormalizedDistanceAndNormal(in float2 queryRelativeToCenter)
        {
            var dist = GetDistance(queryRelativeToCenter);
            var normal = GetNormal(queryRelativeToCenter);
            return (dist / obstacleRadius, normal);
        }

        private readonly float2 GetNormal(in float2 pos)
        {
            const float epsilon = 0.01f;

            return shapeVariant switch
            {
                ShapeVariant.Sphere => GetNormal(circleVariant, pos, epsilon),
                ShapeVariant.Beam => GetNormal(beamVariant, pos, epsilon),
                ShapeVariant.Square => GetNormal(squareVariant, pos, epsilon),
                _ => throw new NotImplementedException("Unknown obstacle shape")
            };
        }
        
        private readonly float GetDistance(in float2 relativeToCenter)
        {
            return shapeVariant switch
            {
                ShapeVariant.Sphere => GetDistance(circleVariant, relativeToCenter),
                ShapeVariant.Beam => GetDistance(beamVariant, relativeToCenter),
                ShapeVariant.Square => GetDistance(squareVariant, relativeToCenter),
                _ => throw new NotImplementedException("Unknown obstacle shape")
            };
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
            var dist = variant.GetDistance(relativeToCenter);
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
        [Range(1f, 30f)]
        public float obstacleRadius;
        
        [FieldOffset(8)]
        public float annularRadius;
        
        [FieldOffset(12)]
        public BeamVariant beamVariant;
        
        [FieldOffset(12)]
        public CircleVariant circleVariant;
        
        [FieldOffset(12)]
        public SquareVariant squareVariant;
        
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
                case ShapeVariant.Square:
                    resultShape.squareVariant = this.squareVariant.AdjustForScale(linearScale, rotation);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return resultShape;
        }
    }
}