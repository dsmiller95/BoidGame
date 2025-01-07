using Boids.Domain;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(BoidSystemGroup))]
[BurstCompile]
public partial struct BoidSpawnSystem : ISystem
{
    private EntityQuery _boidQuery;
    private uint _seedOffset;
    
    public void OnCreate(ref SystemState state)
    {
        _boidQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Boid>()
            .Build(ref state);
    }
    
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        _seedOffset += 1;
        
        var rng = Random.CreateFromIndex(_seedOffset);

        var deltaTime = state.World.Time.DeltaTime;

        var totalBoidCount = _boidQuery.CalculateEntityCount();

        foreach (var (boidSpawner, boidSpawnerLocalToWorld, boidSpawnerState) in 
                 SystemAPI.Query<RefRO<BoidSpawner>, RefRO<LocalToWorld>, RefRW<BoidSpawnerState>>())
        {
            var timeSinceLast = boidSpawnerState.ValueRO.TimeSinceLastSpawn;
            timeSinceLast += deltaTime;
            timeSinceLast = math.min(timeSinceLast, boidSpawner.ValueRO.MaxTimeAccumulate(deltaTime));
            boidSpawnerState.ValueRW.TimeSinceLastSpawn = timeSinceLast;

            var headroom = boidSpawner.ValueRO.MaxBoids - totalBoidCount;
            var toSpawn = math.floor(boidSpawnerState.ValueRO.TimeSinceLastSpawn / boidSpawner.ValueRO.TimePerSpawnGroup);
            toSpawn = math.min(toSpawn, headroom);
            if(toSpawn <= 0) continue;
            
            boidSpawnerState.ValueRW.TimeSinceLastSpawn -= toSpawn * boidSpawner.ValueRO.TimePerSpawnGroup;
            
            var spawnCenter = boidSpawnerLocalToWorld.ValueRO.Position;
            toSpawn *= boidSpawner.ValueRO.GroupSize;
            for (int i = 0; i < toSpawn; i++)
            {
                var spawned = ecb.Instantiate(boidSpawner.ValueRO.Prefab);
                var spawnPoint = spawnCenter + new float3(boidSpawner.ValueRO.GetRelativeSpawn(ref rng), 0);
                var localToWorld = LocalTransform.FromPosition(spawnPoint);
                ecb.SetComponent(spawned, localToWorld);
                //ecb.RemoveComponent<LocalTransform>(spawned);
            }
        }

        ecb.Playback(state.EntityManager);
    }
}