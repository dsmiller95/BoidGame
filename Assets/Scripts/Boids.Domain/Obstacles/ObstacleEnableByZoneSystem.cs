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
    public partial struct ObstacleEnableByZoneSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);
            
            var world = state.WorldUnmanaged;
            var nullZoneQuery = SystemAPI.QueryBuilder()
                .WithAll<ZoneComponent, LocalToWorld, ZoneTypeNullTag>()
                .Build();
            var zoneCount = nullZoneQuery.CalculateEntityCount();

            var enabledObstacleQuery = SystemAPI.QueryBuilder()
                .WithAll<ObstacleComponent, LocalToWorld>()
                .WithNone<ObstacleDisabledFlag>()
                .Build();
            var disabledObstacleQuery = SystemAPI.QueryBuilder()
                .WithAll<ObstacleComponent, LocalToWorld, ObstacleDisabledFlag>()
                .Build();
            
            var copyZones = CollectionHelper.CreateNativeArray<Zone, RewindableAllocator>(zoneCount, ref world.UpdateAllocator);
            
            var copyZoneJob = new CopyZoneJob
            {
                Zones = copyZones
            };
            state.Dependency = copyZoneJob.ScheduleParallel(nullZoneQuery, state.Dependency);
            nullZoneQuery.AddDependency(state.Dependency);

            var parallelWriter = ecb.AsParallelWriter();
            var enableObstaclesJob = new EnableObstacleJob()
            {
                Zones = copyZones,
                CommandBuffer = parallelWriter,
                SetEnable = true
            };
            var enableObstacleJob = enableObstaclesJob.ScheduleParallel(disabledObstacleQuery, state.Dependency);
            disabledObstacleQuery.AddDependency(enableObstacleJob);
            
            var disableObstaclesJob = new EnableObstacleJob()
            {
                Zones = copyZones,
                CommandBuffer = parallelWriter,
                SetEnable = false
            };
            var disableObstacleJob = disableObstaclesJob.ScheduleParallel(enabledObstacleQuery, enableObstacleJob);
            enabledObstacleQuery.AddDependency(disableObstacleJob);
            
            state.Dependency = JobHandle.CombineDependencies(enableObstacleJob, disableObstacleJob);
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
        private partial struct EnableObstacleJob : IJobEntity
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
                foreach (Zone zone in Zones)
                {
                    var containsPosition = zone.Contains(position);
                    if (SetEnable && !containsPosition)
                    {
                        CommandBuffer.RemoveComponent<ObstacleDisabledFlag>(index, entity);
                    }

                    if (!SetEnable && containsPosition)
                    {
                        CommandBuffer.AddComponent<ObstacleDisabledFlag>(-index, entity);
                    }
                }
            }
        }
    }
}