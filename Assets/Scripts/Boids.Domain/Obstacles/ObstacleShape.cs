using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Boids.Domain.Obstacles
{

    public enum ObstacleShapeVariant : int
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
        public ObstacleShapeVariant shapeVariant;
        
        [FieldOffset(4)]
        public float obstacleRadius;
        
        [FieldOffset(8)]
        public FixedBytes16 variantData;
        
        [FieldOffset(8)]
        public BeamVariant beamVariant;
        
        [FieldOffset(8)]
        public CircleVariant circleVariant;
        
        [FieldOffset(8)]
        public SquareVariant squareVariant;

        /// <summary>
        /// gets the maximum distance from the center which could be affected by this obstacle
        /// </summary>
        /// <remarks>
        /// The actual obstacle may be smaller, but will not be larger.
        /// </remarks>
        public float MaximumExtent()
        {
            switch (shapeVariant)
            {
                case ObstacleShapeVariant.Sphere:
                    return obstacleRadius;
                case ObstacleShapeVariant.Beam:
                    return obstacleRadius + math.length(beamVariant.beamRelativeEnd);
                case ObstacleShapeVariant.Square:
                    return obstacleRadius;
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
                ObstacleShapeVariant.Sphere => GetNormal(circleVariant, pos, epsilon),
                ObstacleShapeVariant.Beam => GetNormal(beamVariant, pos, epsilon),
                ObstacleShapeVariant.Square => GetNormal(squareVariant, pos, epsilon),
                _ => throw new NotImplementedException("Unknown obstacle shape")
            };
        }
        
        private readonly float2 GetNormal<T>(in T variant, in float2 pos, float epsilon) where T : ISdfDefinition
        {
            var dX = variant.GetDistance(pos + new float2(epsilon, 0)) 
                     - variant.GetDistance(pos - new float2(epsilon, 0));
            var dY = variant.GetDistance(pos + new float2(0, epsilon))
                        - variant.GetDistance(pos - new float2(0, epsilon));
            return math.normalizesafe(new float2(dX, dY));
        }

        private readonly float GetDistance(in float2 relativeToCenter)
        {
            switch (shapeVariant)
            {
                case ObstacleShapeVariant.Sphere:
                    return this.circleVariant.GetDistance(relativeToCenter);
                case ObstacleShapeVariant.Beam:
                    return beamVariant.GetDistance(relativeToCenter);
                case ObstacleShapeVariant.Square:
                    return squareVariant.GetDistance(relativeToCenter);
                default:
                    throw new NotImplementedException("Unknown obstacle shape");
            }
        }
    }
    
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct ObstacleShapeDataDefinition
    {
        [FieldOffset(0)]
        public ObstacleShapeVariant shapeVariant;
        
        [FieldOffset(4)]
        [Range(1f, 30f)]
        public float obstacleRadius;
        
        [FieldOffset(8)]
        public BeamVariant beamVariant;
        
        [FieldOffset(8)]
        public CircleVariant circleVariant;
        
        [FieldOffset(8)]
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
            };
            switch (shapeVariant)
            {
                case ObstacleShapeVariant.Sphere:
                    resultShape.circleVariant = this.circleVariant.AdjustForScale(linearScale, rotation);
                    break;
                case ObstacleShapeVariant.Beam:
                    resultShape.beamVariant = this.beamVariant.AdjustForScale(linearScale, rotation);
                    break;
                case ObstacleShapeVariant.Square:
                    resultShape.squareVariant = this.squareVariant.AdjustForScale(linearScale, rotation);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return resultShape;
        }
    }
}