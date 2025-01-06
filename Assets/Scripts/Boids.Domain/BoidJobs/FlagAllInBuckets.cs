using Boids.Domain.DebugFlags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Boids.Domain.BoidJobs
{
    [BurstCompile]
    internal struct FlagAllInBuckets : IJob
    {
        public int2 MinBucket;
        public int2 MaxBucket;
            
        public ComponentLookup<DebugFlagComponent> DebugFlagLookup;
        [ReadOnly] public NativeParallelMultiHashMap<int2, OtherBoidData> SpatialMap;

        public void Execute()
        {
            for (int x = MinBucket.x; x <= MaxBucket.x; x++)
            {
                for (int y = MinBucket.y; y <= MaxBucket.y; y++)
                {
                    var bucket = new int2(x, y);
                    if (!SpatialMap.TryGetFirstValue(bucket, out var otherBoidData, out var it))
                    {
                        continue;
                    }

                    do
                    {
                        var refRw = DebugFlagLookup.GetRefRWOptional(otherBoidData.Entity);
                        if (refRw.IsValid) refRw.ValueRW.SetFlag(FlagType.Secondary);
                    } while (SpatialMap.TryGetNextValue(out otherBoidData, ref it));
                }
            }
        }
            
    }
}