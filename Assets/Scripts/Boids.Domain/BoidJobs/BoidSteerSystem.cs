using Boids.Domain.DebugFlags;
using Boids.Domain.Lifetime;
using Boids.Domain.Obstacles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Boids.Domain.BoidJobs
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(BoidSystemGroup))]
    public partial struct BoidSteerSystem : ISystem
    {
        private uint _seedOffset;
        private NativeArray<Entity> _debugTrackingEntity;
        private NativeArray<float> _debugDeathTime;
        private bool _debugHashMap;
        private bool _debugObstacles;
        
        public void OnCreate(ref SystemState state)
        {
            _seedOffset = 9724;
            _debugTrackingEntity = new NativeArray<Entity>(1, Allocator.Persistent);
            _debugDeathTime = new NativeArray<float>(1, Allocator.Persistent);
            _debugHashMap = false;
            _debugObstacles = false;
        }
        
        public void OnDestroy(ref SystemState state)
        {
            _debugTrackingEntity.Dispose();
            _debugDeathTime.Dispose();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _seedOffset += 1;
            var rng = Random.CreateFromIndex(_seedOffset);
            var time = (float)state.WorldUnmanaged.Time.ElapsedTime;
            var dt = state.WorldUnmanaged.Time.DeltaTime;
            
            var boidQuery = SystemAPI.QueryBuilder()
                .WithAll<Boid, BoidState, LifetimeComponent>()
                .WithAllRW<PhysicsVelocity, LocalTransform>()
                //.WithAllRW<DebugFlagComponent>()
                .WithNone<BoidSpawnData>()
                .Build();

            var obstacleQuery = SystemAPI.QueryBuilder()
                .WithAll<Obstacle, LocalTransform>()
                .Build();
            
            var world = state.WorldUnmanaged;

            if (_debugHashMap)
            {
                // check if debug tracking entity is dead
                if (_debugTrackingEntity[0] != Entity.Null &&
                    (!state.EntityManager.Exists(_debugTrackingEntity[0]) ||
                    !state.EntityManager.HasComponent<LifetimeComponent>(_debugTrackingEntity[0])))
                {
                    _debugTrackingEntity[0] = Entity.Null;
                }

                if (_debugTrackingEntity[0] == Entity.Null)
                {
                    _debugDeathTime[0] = 0;
                    var pickYoungestJob = new PickYoungest()
                    {
                        YoungestEntity = _debugTrackingEntity,
                        MaxDeathTime = _debugDeathTime
                    };
                    
                    state.Dependency = pickYoungestJob.Schedule(boidQuery, state.Dependency);
                }
            }


            
            
            
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<Boid> uniqueBoidTypes, world.UpdateAllocator.ToAllocator);

            if (uniqueBoidTypes.Length > 2)
            {
                Debug.LogError("More than 2 boid variants. will be inneficient");
            }

            var obstacleCount = obstacleQuery.CalculateEntityCount();
            
            foreach (Boid boidConfig in uniqueBoidTypes)
            {
                boidQuery.AddSharedComponentFilter(boidConfig);

                var boidCount = boidQuery.CalculateEntityCount();
                if (boidCount == 0)
                {
                    boidQuery.ResetFilter();
                    continue;
                }

                var spatialHashDefinition = new SpatialHashDefinition(boidConfig.cellRadius);

                var spatialBoids = new NativeParallelMultiHashMap<int2, OtherBoidData>(boidCount, world.UpdateAllocator.ToAllocator);
                var populateBoidHashMapJob = new PopulateBoidHashMap
                {
                    SpatialHashDefinition = spatialHashDefinition,
                    SpatialMapWriter = spatialBoids.AsParallelWriter()
                };
                var boidMapDependency = populateBoidHashMapJob.ScheduleParallel(boidQuery, state.Dependency);

                var spatialObstacles = new NativeParallelMultiHashMap<int2, ObstacleData>(obstacleCount, world.UpdateAllocator.ToAllocator);
                var populateObstacleHashMapJob = new PopulateObstacleHashMap
                {
                    SpatialHashDefinition = spatialHashDefinition,
                    SpatialMapWriter = spatialObstacles.AsParallelWriter()
                };
                var obstacleMapDependency = populateObstacleHashMapJob.ScheduleParallel(obstacleQuery, state.Dependency);
                
                state.Dependency = JobHandle.CombineDependencies(boidMapDependency, obstacleMapDependency);
                
                var steerBoidsJob = new SteerBoids
                {
                    DeltaTime = dt,
                    BoidVariant = boidConfig,
                    SpatialHashDefinition = spatialHashDefinition,
                    SpatialBoids = spatialBoids,
                    SpatialObstacles = spatialObstacles,
                    DrawDebug = _debugObstacles,
                };
                if (_debugObstacles)
                {
                    state.Dependency.Complete();
                    steerBoidsJob.Run(boidQuery);
                }
                else
                {
                    state.Dependency = steerBoidsJob.ScheduleParallel(boidQuery, state.Dependency);
                }

                if (_debugHashMap)
                {
                    state.Dependency.Complete(); // required to query LocalTransform
                    var youngest = _debugTrackingEntity[0];
                    var debugCenter = SystemAPI.GetComponent<LocalTransform>(youngest).Position.xy;
                    spatialHashDefinition.GetMinMaxBuckets(debugCenter, boidConfig.MaxNeighborDistance, out var minBucket, out var maxBucket);

                    var debugFlagLookup = SystemAPI.GetComponentLookup<DebugFlagComponent>(isReadOnly: false);
                    
                    var job = new FlagAllInBuckets
                    {
                        DebugFlagLookup = debugFlagLookup,
                        MinBucket = minBucket,
                        MaxBucket = maxBucket,
                        SpatialMap = spatialBoids
                    };
                    
                    state.Dependency = job.Schedule(state.Dependency);
                }
                
                boidQuery.AddDependency(state.Dependency);
                boidQuery.ResetFilter();
            }
        }

        
        [BurstCompile]
        private partial struct PickYoungest : IJobEntity
        {
            public NativeArray<Entity> YoungestEntity;
            public NativeArray<float> MaxDeathTime;
            
            private void Execute(in LifetimeComponent lifetime, in Entity entity)
            {
                if (MaxDeathTime[0] < lifetime.deathTime)
                {
                    MaxDeathTime[0] = lifetime.deathTime;
                    YoungestEntity[0] = entity;
                }
            }
        }
    }
}
