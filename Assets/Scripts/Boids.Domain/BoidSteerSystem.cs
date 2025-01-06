﻿using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using Random = Unity.Mathematics.Random;

namespace Boids.Domain
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(BoidSystemGroup))]
    public partial struct BoidSteerSystem : ISystem
    {
        private uint _seedOffset;
        
        public void OnCreate(ref SystemState state)
        {
            _seedOffset = 9724;
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _seedOffset += 1;
            var rng = Random.CreateFromIndex(_seedOffset);
            var time = (float)state.WorldUnmanaged.Time.ElapsedTime;
            var dt = state.WorldUnmanaged.Time.DeltaTime;
            
            var boidQuery = SystemAPI.QueryBuilder()
                .WithAll<Boid, BoidState>()
                .WithAllRW<PhysicsVelocity, LocalTransform>()
                .WithNone<BoidSpawnData>()
                .Build();
            
            var world = state.WorldUnmanaged;
            
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<Boid> uniqueBoidTypes, world.UpdateAllocator.ToAllocator);

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

                var spatialMap = new NativeParallelMultiHashMap<int2, OtherBoidData>(boidCount, world.UpdateAllocator.ToAllocator);
                var populateBoidHashMapJob = new PopulateBoidHashMap
                {
                    SpatialHashDefinition = spatialHashDefinition,
                    SpatialMapWriter = spatialMap.AsParallelWriter()
                };
                state.Dependency = populateBoidHashMapJob.ScheduleParallel(boidQuery, state.Dependency);
                
                var steerBoidsJob = new SteerBoids
                {
                    DeltaTime = dt,
                    BoidVariant = boidConfig,
                    SpatialHashDefinition = spatialHashDefinition,
                    SpatialMap = spatialMap
                };
                state.Dependency = steerBoidsJob.ScheduleParallel(boidQuery, state.Dependency);

                boidQuery.AddDependency(state.Dependency);
                boidQuery.ResetFilter();
            }
        }
        
        [BurstCompile]
        partial struct PopulateBoidHashMap : IJobEntity
        {
            public SpatialHashDefinition SpatialHashDefinition;
            
            public NativeParallelMultiHashMap<int2, OtherBoidData>.ParallelWriter SpatialMapWriter;
            
            private void Execute(in PhysicsVelocity velocity, in LocalTransform presumedWorldTransform)
            {
                var boidData = OtherBoidData.From(velocity, presumedWorldTransform);
                var cell = SpatialHashDefinition.GetCell(presumedWorldTransform.Position.xy);
                SpatialMapWriter.Add(cell, boidData);
            }
        }

        [BurstCompile]
        private partial struct SteerBoids : IJobEntity
        {
            public float DeltaTime;
            public Boid BoidVariant;
            public SpatialHashDefinition SpatialHashDefinition;
            
            [ReadOnly] public NativeParallelMultiHashMap<int2, OtherBoidData> SpatialMap;

            private void Execute(ref PhysicsVelocity velocity, ref LocalTransform presumedWorld, in BoidState boidState)
            {
                var myPos = presumedWorld.Position.xy;
                var myVelocity = velocity.Linear.xy;
                var maxRadius = BoidVariant.MaxNeighborDistance;
                SpatialHashDefinition.GetMinMaxBuckets(myPos, maxRadius, out var minBucket, out var maxBucket);

                var accumulator = new AccumulatedBoidSteering();
                var maxRadiusSq = maxRadius * maxRadius;
                for (int x = minBucket.x; x <= maxBucket.x; x++)
                {
                    for (int y = minBucket.y; y <= maxBucket.y; y++)
                    {
                        var bucket = new int2(x, y);
                        if (!SpatialMap.TryGetFirstValue(bucket, out var otherBoidData, out var it))
                        {
                            continue;
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
                        } while (SpatialMap.TryGetNextValue(out otherBoidData, ref it));
                    }
                }
                
                var nextHeading = accumulator.GetNextHeading(BoidVariant, myVelocity, myPos, DeltaTime);
                var rotation = math.atan2(nextHeading.y, nextHeading.x);
                
                velocity.Linear = new float3(nextHeading, 0);
                presumedWorld = LocalTransform.FromPositionRotation(
                    new float3(myPos, 0),
                    quaternion.Euler(0, 0, rotation)
                );
            }
        }

        private struct AccumulatedBoidSteering
        {
            private float2 _separation;
            private int _separationCount;
            
            private float2 _alignment;
            private int _alignmentCount;
            
            private float2 _cohesion;
            private int _cohesionCount;
            
            
            public void Accumulate(in Boid boidSettings, in OtherBoidData otherBoid, in float2 toNeighbor, in float distance)
            {
                if (distance < boidSettings.separationRadius)
                {
                    _separationCount++;
                    
                    var fromNeighbor = -toNeighbor;
                    var fromNeighborNormalized = fromNeighbor / distance;
                    var separationAdjustment = 1f - (distance / boidSettings.separationRadius);
                    //separationAdjustment = separationAdjustment * separationAdjustment;
                    //separationAdjustment = Mathf.Clamp01(separationAdjustment);
                    _separation += fromNeighborNormalized * (0.5f + separationAdjustment * 0.5f);
                }

                if (distance < boidSettings.alignmentRadius)
                {
                    _alignmentCount++;
                    _alignment += otherBoid.Velocity;
                }

                if (distance < boidSettings.cohesionRadius)
                {
                    _cohesionCount++;
                    _cohesion += toNeighbor;
                }
            }

            public float2 GetNextHeading(in Boid boidSettings, in float2 linearVelocity, in float2 position, in float deltaTime)
            {
                if(_separationCount > 0)
                {
                }
                else _separation = Vector2.zero;
            
                if(_alignmentCount > 0)
                {
                    _alignment = (_alignment / _alignmentCount) - linearVelocity;
                }
                else _alignment = Vector2.zero;

                if (_cohesionCount > 0)
                {
                    _cohesion /= _cohesionCount;
                    _cohesion -= position;
                }
                else _cohesion = Vector2.zero;


                var targetForward = _separation * boidSettings.separationWeight +
                                    _alignment * boidSettings.alignmentWeight +
                                    _cohesion * boidSettings.cohesionWeight;
                
                var nextHeading = linearVelocity + deltaTime * (targetForward - linearVelocity);
                var nextHeadingClamped = ClampMagnitude(nextHeading, boidSettings.minSpeed, boidSettings.maxSpeed);
                return nextHeadingClamped;
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
        
        struct OtherBoidData
        {
            public float2 Position;
            public float2 Velocity;
            
            public static OtherBoidData From(in PhysicsVelocity velocity, in LocalTransform presumedWorld)
            {
                return new OtherBoidData
                {
                    Position = presumedWorld.Position.xy,
                    Velocity = velocity.Linear.xy
                };
            }
        }
    }
}