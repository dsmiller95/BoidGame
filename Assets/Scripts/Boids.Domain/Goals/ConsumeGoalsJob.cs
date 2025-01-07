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
    internal partial struct ConsumeGoalsJob : IJobEntity
    {
        public SpatialHashDefinition SpatialHashDefinition;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        [ReadOnly] public NativeParallelMultiHashMap<int2, OtherBoidData> BoidBuckets;
        
        public void Execute(
            [EntityIndexInQuery] int entityIndexInQuery,
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
                    ConsumeCell(bucket, entityIndexInQuery, myPos, myRadiusSq, ref totalCount);
                }
            }
            
            count.count += totalCount;
        }
        
        private void ConsumeCell(in int2 bucket, in int sortKey,
            in float2 goalPos, in float myRadiusSq, ref int totalCount)
        {
            if (!BoidBuckets.TryGetFirstValue(bucket, out var otherBoidData, out var it))
            {
                return;
            }

            do
            {
                float2 toBoid = otherBoidData.Position - goalPos;
                var distanceSq = math.lengthsq(toBoid);
                if (distanceSq > myRadiusSq)
                {
                    continue;
                }

                CommandBuffer.DestroyEntity(sortKey, otherBoidData.Entity);
                totalCount++;
            } while (BoidBuckets.TryGetNextValue(out otherBoidData, ref it));
        }
    }
}