using Boids.Domain.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Boids.Domain.Obstacles
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ObstacleDisableSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);
            // TODO: should I be doing this? are there examples of this working? I wonder if this is gonna break
            var parallelWriter = ecb.AsParallelWriter();
            
            var world = state.WorldUnmanaged;
            var nullZoneQuery = SystemAPI.QueryBuilder()
                .WithAll<ZoneComponent, LocalToWorld, ZoneTypeNullTag>()
                .Build();
            var zoneCount = nullZoneQuery.CalculateEntityCount();

            var enabledObstacleQueryWithChildren = SystemAPI.QueryBuilder()
                .WithAll<LocalToWorld, ObstacleMayDisableFlag, Child>()
                .WithNone<ObstacleDisabledFlag>()
                .Build();
            var enabledObstacleQueryWithoutChildren = SystemAPI.QueryBuilder()
                .WithAll<LocalToWorld, ObstacleMayDisableFlag>()
                .WithNone<ObstacleDisabledFlag, Child>()
                .Build();
            var disabledObstacleQueryWithChildren = SystemAPI.QueryBuilder()
                .WithAll<LocalToWorld, ObstacleDisabledFlag, ObstacleMayDisableFlag, Child>()
                .Build();
            var disabledObstacleQueryWithoutChildren = SystemAPI.QueryBuilder()
                .WithAll<LocalToWorld, ObstacleDisabledFlag, ObstacleMayDisableFlag>()
                .WithNone<Child>()
                .Build();
            
            var copyZones = CollectionHelper.CreateNativeArray<Zone, RewindableAllocator>(zoneCount, ref world.UpdateAllocator);
            
            var copyZoneJob = new CopyZoneJob
            {
                Zones = copyZones
            };
            state.Dependency = copyZoneJob.ScheduleParallel(nullZoneQuery, state.Dependency);
            nullZoneQuery.AddDependency(state.Dependency);
            
            var enableObstaclesWithoutChildrenJob = new EnableObstacleJobWithoutChildren()
            {
                Zones = copyZones,
                CommandBuffer = parallelWriter,
                SetEnable = true
            };
            state.Dependency = enableObstaclesWithoutChildrenJob.ScheduleParallel(disabledObstacleQueryWithoutChildren, state.Dependency);
            disabledObstacleQueryWithoutChildren.AddDependency(state.Dependency);
            
            
            var disableObstaclesWithoutChildrenJob = new EnableObstacleJobWithoutChildren()
            {
                Zones = copyZones,
                CommandBuffer = parallelWriter,
                SetEnable = false
            };
            state.Dependency = disableObstaclesWithoutChildrenJob.ScheduleParallel(enabledObstacleQueryWithoutChildren, state.Dependency);
            enabledObstacleQueryWithoutChildren.AddDependency(state.Dependency);
            
            
            var disabledComponentLookup = SystemAPI.GetComponentLookup<ObstacleDisabledFlag>(true);
            var enableObstaclesWithChildrenJob = new EnableObstacleJobWithChildren()
            {
                DisabledObstacles = disabledComponentLookup,
                Zones = copyZones,
                CommandBuffer = parallelWriter,
                SetEnable = true
            };
            state.Dependency = enableObstaclesWithChildrenJob.ScheduleParallel(disabledObstacleQueryWithChildren, state.Dependency);
            disabledObstacleQueryWithChildren.AddDependency(state.Dependency);
            
            
            var disableObstaclesWithChildrenJob = new EnableObstacleJobWithChildren()
            {
                DisabledObstacles = disabledComponentLookup,
                Zones = copyZones,
                CommandBuffer = parallelWriter,
                SetEnable = false
            };
            state.Dependency = disableObstaclesWithChildrenJob.ScheduleParallel(enabledObstacleQueryWithChildren, state.Dependency);
            enabledObstacleQueryWithChildren.AddDependency(state.Dependency);
            
            
        }
        
        [BurstCompile]
        private partial struct CopyZoneJob : IJobEntity
        {
            public NativeArray<Zone> Zones;
            
            private void Execute([EntityIndexInQuery] int entityIndexInQuery, in ZoneComponent zone, in LocalToWorld localToWorld)
            {
                Zones[entityIndexInQuery] = zone.GetRelative(localToWorld);
            }
        }
        
        [BurstCompile]
        private partial struct EnableObstacleJobWithChildren : IJobEntity
        {
            [ReadOnly] public NativeArray<Zone> Zones;
            [ReadOnly] public ComponentLookup<ObstacleDisabledFlag> DisabledObstacles;
            
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            /// <summary>
            /// Set to true if the job is iterating over a query of disabled obstacles,
            /// and should set them to enabled.
            /// </summary>
            public bool SetEnable;
            
            public void Execute(
                [EntityIndexInQuery] int index,
                Entity entity,
                in LocalToWorld localToWorld,
                in DynamicBuffer<Child> children)
            {
                var position = localToWorld.Position.xy;
                var shouldDisable = false;
                foreach (Zone zone in Zones)
                {
                    shouldDisable |= zone.Contains(position);
                }

                if (!shouldDisable)
                {
                    foreach (var child in children)
                    {
                        if (DisabledObstacles.HasComponent(child.Value))
                        {
                            shouldDisable = true;
                        }
                    }
                }
                
                if (SetEnable && !shouldDisable)
                {
                    CommandBuffer.RemoveComponent<ObstacleDisabledFlag>(index, entity);
                }

                if (!SetEnable && shouldDisable)
                {
                    CommandBuffer.AddComponent<ObstacleDisabledFlag>(-index, entity);
                }
            }
        }
        
        [BurstCompile]
        private partial struct EnableObstacleJobWithoutChildren : IJobEntity
        {
            [ReadOnly] public NativeArray<Zone> Zones;
            
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            /// <summary>
            /// Set to true if the job is iterating over a query of disabled obstacles,
            /// and should set them to enabled.
            /// </summary>
            public bool SetEnable;
            
            public void Execute(
                [EntityIndexInQuery] int index,
                Entity entity,
                in LocalToWorld localToWorld)
            {
                var position = localToWorld.Position.xy;
                var shouldDisable = false;
                foreach (Zone zone in Zones)
                {
                    shouldDisable |= zone.Contains(position);
                }
                
                if (SetEnable && !shouldDisable)
                {
                    CommandBuffer.RemoveComponent<ObstacleDisabledFlag>(index, entity);
                }

                if (!SetEnable && shouldDisable)
                {
                    CommandBuffer.AddComponent<ObstacleDisabledFlag>(-index, entity);
                }
            }
        }
    }
}