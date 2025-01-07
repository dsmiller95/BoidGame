using Boids.Domain.Obstacles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Boids.Domain.BoidJobs
{
    [BurstCompile]
    internal partial struct SteerBoids : IJobEntity
    {
        public float DeltaTime;
        public Boid BoidVariant;
        public SpatialHashDefinition SpatialHashDefinition;
        public BoidBoundingBox BoidBounds;

        public bool DrawDebug;
            
        [ReadOnly] public NativeParallelMultiHashMap<int2, OtherBoidData> SpatialBoids;
        [ReadOnly] public NativeParallelHashMap<int2, ObstacleCellData> SpatialObstacles;

        private void Execute(
            ref PhysicsVelocity velocity, 
            ref LocalTransform presumedWorld,
            in BoidState boidState,
            in Entity entity)
        {
            var myPos = presumedWorld.Position.xy;
            var myVelocity = velocity.Linear.xy;
            var maxRadius = BoidVariant.MaxNeighborDistance;
            SpatialHashDefinition.GetMinMaxBuckets(myPos, maxRadius, out var minBucket, out var maxBucket);

            var accumulator = AccumulatedBoidSteering.Empty;
            var maxRadiusSq = maxRadius * maxRadius;
            for (int x = minBucket.x; x <= maxBucket.x; x++)
            {
                for (int y = minBucket.y; y <= maxBucket.y; y++)
                {
                    var bucket = new int2(x, y);
                    AccumulateBucketBoids(bucket, myPos, maxRadiusSq, ref accumulator);
                    AccumulateBucketObstacles(bucket, myPos, ref accumulator);
                }
            }
                
            accumulator.AccumulateBounds(BoidBounds, myPos);
            // var obstacle = ObstacleFunction.Default.GetObstacleFromField(myPos);
            // if (DrawDebug)
            // {
            //     Debug.DrawLine(new float3(myPos, 0), new float3(obstacle, 0), Color.red);
            // }
            // var relativeObstaclePosition = obstacle - myPos;
            // accumulator.AccumulateObstacle(relativeObstaclePosition);
                
            var (targetForward, hardSurface) = accumulator.GetTargetForward(BoidVariant, myVelocity, myPos);
            var targetForwardNormalized = math.normalizesafe(targetForward);
            var extraForce = targetForwardNormalized * BoidVariant.acceleration;
                
            var nextHeadingUnclamped = myVelocity + DeltaTime * (targetForward - myVelocity);
            nextHeadingUnclamped += math.normalizesafe(nextHeadingUnclamped) * DeltaTime * BoidVariant.acceleration;
            
            nextHeadingUnclamped = math.select(
                nextHeadingUnclamped,
                targetForwardNormalized * math.length(myVelocity),
                hardSurface);
            var nextHeading = ClampMagnitude(nextHeadingUnclamped, BoidVariant.minSpeed, BoidVariant.maxSpeed);
                
            var rotation = math.atan2(nextHeading.y, nextHeading.x);
            velocity.Linear = new float3(nextHeading, 0);
            presumedWorld = presumedWorld.WithRotation(quaternion.Euler(0, 0, rotation));
        }

        private void AccumulateBucketBoids(
            in int2 bucket, in float2 myPos, in float maxRadiusSq, ref AccumulatedBoidSteering accumulator)
        {
            if (!SpatialBoids.TryGetFirstValue(bucket, out var otherBoidData, out var it))
            {
                return;
            }

            do
            {
                float2 toNeighbor = otherBoidData.Position - myPos;
                var distanceSq = math.lengthsq(toNeighbor);
                if (distanceSq > maxRadiusSq || distanceSq <= 0.0001f) // ignore self at almost 0-dist
                {
                    continue;
                }

                var distance = math.sqrt(distanceSq);

                accumulator.Accumulate(BoidVariant, otherBoidData, toNeighbor, distance);
            } while (SpatialBoids.TryGetNextValue(out otherBoidData, ref it));
        }
        
        private void AccumulateBucketObstacles(
            in int2 bucket, in float2 myPos, ref AccumulatedBoidSteering accumulator)
        {
            if(!SpatialObstacles.TryGetValue(bucket, out var obstacleData))
            {
                return;
            }
            
            accumulator.AccumulateObstacleCell(obstacleData, myPos);
        }
        
        private float2 ClampMagnitude(float2 heading, float min, float max)
        {
            var mag = math.length(heading);
            if (mag < 0.0001f) return heading;
            if (mag < min) return (heading / mag) * min;
            if (mag > max) return (heading / mag) * max;
            return heading;
        }
    }
}