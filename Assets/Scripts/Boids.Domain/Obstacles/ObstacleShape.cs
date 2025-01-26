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

    public interface ISdfDefinition<out T> where T: struct, ISdfDefinition<T>
    {
        public T AdjustForScale(float linearScale, float rotation);
        public float GetDistance(in float2 queryRelativeToCenter);
        public void ApplyControlPoint(int index, float2 controlPoint);
    }
    
    [Serializable]
    public struct CircleVariant : ISdfDefinition<CircleVariant>
    {
        public readonly CircleVariant AdjustForScale(float linearScale, float rotation)
        {
            return this;
        }
        public readonly float GetDistance(in float2 queryRelativeToCenter)
        {
            return math.length(queryRelativeToCenter);
        }

        public void ApplyControlPoint(int index, float2 controlPoint)
        {
            // noop;
        }
    }

    [Serializable]
    public struct BeamVariant : ISdfDefinition<BeamVariant>
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

        public void ApplyControlPoint(int index, float2 controlPoint)
        {
            if(index == 0) 
            {
                beamRelativeEnd = controlPoint;
            }
        }
    }


    [Serializable]
    public struct BoxVariant : ISdfDefinition<BoxVariant>
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
        public readonly float GetDistance(in float2 queryRelativeToCenter)
        {
            float2 p = queryRelativeToCenter;
            float2 b = corner;
            
            float2 d = math.abs(p) - b;
            return math.length(math.max(d, 0.0f)) + math.min(math.max(d.x, d.y), 0.0f);
        }

        public void ApplyControlPoint(int index, float2 controlPoint)
        {
            if(index == 0) 
            {
                corner = controlPoint;
            }
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

        // TODO: put into its own thing, along with shape variant. externalize radii.
        [FieldOffset(12)]
        public FixedBytes16 variantData;
        
        [FieldOffset(12)]
        public CircleVariant circleVariant;
        
        [FieldOffset(12)]
        public BeamVariant beamVariant;
        
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
        
        public bool IsInside(in float distance)
        {
            return distance < obstacleRadius;
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
        
        public readonly (float, float2) GetDistanceAndNormal(in float2 queryRelativeToCenter)
        {
            return shapeVariant switch
            {
                ShapeVariant.Sphere => GetDistanceAndNormal(circleVariant, queryRelativeToCenter),
                ShapeVariant.Beam => GetDistanceAndNormal(beamVariant, queryRelativeToCenter),
                ShapeVariant.Box => GetDistanceAndNormal(boxVariant, queryRelativeToCenter),
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
        
        public readonly float GetDistance(in float2 queryRelativeToCenter)
        {
            return shapeVariant switch
            {
                ShapeVariant.Sphere => GetDistance(circleVariant, queryRelativeToCenter),
                ShapeVariant.Beam => GetDistance(beamVariant, queryRelativeToCenter),
                ShapeVariant.Box => GetDistance(boxVariant, queryRelativeToCenter),
                _ => throw new NotImplementedException("Unknown obstacle shape")
            };
        }
        
        public void AdjustForScale(float linearScale, float rotation)
        {
            obstacleRadius *= linearScale;
            annularRadius *= linearScale;
            switch (shapeVariant)
            {
                case ShapeVariant.Sphere:
                    circleVariant = circleVariant.AdjustForScale(linearScale, rotation);
                    break;
                case ShapeVariant.Beam:
                    beamVariant = beamVariant.AdjustForScale(linearScale, rotation);
                    break;
                case ShapeVariant.Box:
                    boxVariant = boxVariant.AdjustForScale(linearScale, rotation);
                    break;
                default:
                    throw new NotImplementedException("Unknown obstacle shape");
            }
        }
        
        private readonly (float, float2) GetNormalizedDistanceAndNormal<T>(in T variant, in float2 relativeToCenter) where T : struct, ISdfDefinition<T>
        {
            const float epsilon = 0.01f;
            
            var dist = GetDistance(variant, relativeToCenter);
            var totalRadius = obstacleRadius;
            return (dist / totalRadius, GetNormal(variant, relativeToCenter, epsilon));
        }
        
        private readonly (float, float2) GetDistanceAndNormal<T>(in T variant, in float2 relativeToCenter) where T : struct, ISdfDefinition<T>
        {
            const float epsilon = 0.01f;
            
            var dist = GetDistance(variant, relativeToCenter);
            return (dist , GetNormal(variant, relativeToCenter, epsilon));
        }
        
        private readonly float GetNormalizedDistance<T>(in T variant, in float2 relativeToCenter) where T : struct, ISdfDefinition<T>
        {
            var dist = GetDistance(variant, relativeToCenter);
            var totalRadius = obstacleRadius;
            return dist / totalRadius;
        }
        
        private readonly float2 GetNormal<T>(in T variant, in float2 pos, float epsilon) where T : struct, ISdfDefinition<T>
        {
            var dX = GetDistance(variant, pos + new float2(epsilon, 0)) 
                     - GetDistance(variant,pos - new float2(epsilon, 0));
            var dY = GetDistance(variant,pos + new float2(0, epsilon))
                     - GetDistance(variant,pos - new float2(0, epsilon));
            return math.normalizesafe(new float2(dX, dY));
        }
        
        private readonly float GetDistance<T>(in T variant, in float2 relativeToCenter) where T : struct, ISdfDefinition<T>
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
        [Range(0f, 30f)]
        public float obstacleRadius;
        
        [FieldOffset(8)]
        public float annularRadius;
        
        [FieldOffset(12)]
        public FixedBytes16 variantData;
        
        [FieldOffset(12)]
        public CircleVariant circleVariant;
        
        [FieldOffset(12)]
        public BeamVariant beamVariant;
        
        [FieldOffset(12)]
        public BoxVariant boxVariant;
        
        public void ApplyControlPointToVariant(int index, in float2 controlPoint)
        {
            switch (shapeVariant)
            {
                case ShapeVariant.Sphere:
                    circleVariant.ApplyControlPoint(index, controlPoint);
                    break;
                case ShapeVariant.Beam:
                    beamVariant.ApplyControlPoint(index, controlPoint);
                    break;
                case ShapeVariant.Box:
                    boxVariant.ApplyControlPoint(index, controlPoint);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
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
                obstacleRadius = this.obstacleRadius,
                annularRadius = this.annularRadius,
                variantData = this.variantData,
            };
            resultShape.AdjustForScale(linearScale, rotation);
            return resultShape;
        }
    }
}