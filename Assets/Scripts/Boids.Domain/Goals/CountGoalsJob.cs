using Boids.Domain.BoidJobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Boids.Domain.Goals
{
    
    // invoked as part of the boid steer system. may want to expose the hashmap generated
    //  in the steering system to other systems?
    [BurstCompile]
    internal partial struct CountGoalsJob : IJobEntity
    {
        public SpatialHashDefinition SpatialHashDefinition;
        [ReadOnly] public NativeParallelMultiHashMap<int2, OtherBoidData> BoidBuckets;
        
        public void Execute(
            ref GoalCount count,
            in LocalToWorld localToWorld,
            in Goal goalConfig)
        {
            var myPos = localToWorld.Position.xy;
            var myRadius = goalConfig.radius * localToWorld.Value.GetPresumedLinearScale();
            var myRadiusSq = myRadius * myRadius;
            
            SpatialHashDefinition.GetMinMaxBuckets(myPos, myRadius, out var minBucket, out var maxBucket);

            int totalCount = 0;
            
            for (int x = minBucket.x; x <= maxBucket.x; x++)
            {
                for (int y = minBucket.y; y <= maxBucket.y; y++)
                {
                    var bucket = new int2(x, y);
                    var bucketCenter = SpatialHashDefinition.GetCenterOfCell(bucket);
                    var distanceSq = math.lengthsq(myPos - bucketCenter);
                    if (distanceSq > myRadiusSq)
                    {
                        continue;
                    }

                    totalCount += math.select(0, BoidBuckets.CountValuesForKey(bucket), distanceSq <= myRadiusSq);
                }
            }
            
            count.count += totalCount;
        }
    }
}