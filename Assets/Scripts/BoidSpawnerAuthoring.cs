using System;
using System.Diagnostics.Contracts;
using Boids.Domain;
using Cysharp.Threading.Tasks;
using Dman.Utilities;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public struct BoidSpawner : IComponentData
{
    public Entity Prefab;
    public Vector2 SpawnSize;
    public float TimePerSpawn;
    public int MaxBoids;

    [Pure]
    public float MaxTimeAccumulate(float deltaTime)
    {
        return math.max(TimePerSpawn, deltaTime);
    }

    [Pure]
    public float2 GetRelativeSpawn(Unity.Mathematics.Random rng)
    {
        return rng.NextFloat2(-SpawnSize / 2, SpawnSize / 2);
    }
}

public struct BoidSpawnerState : IComponentData
{
    public float TimeSinceLastSpawn;
}

public class BoidSpawnerAuthoring : MonoBehaviour
{
    public BoidSwarm swarmOwner;
    public BoidBehavior boidPrefab;
    
    [Range(0.001f, 10f)]
    public float timePerSpawn = 0.5f;
    public int targetBoidCount = 1000;
    
    public Vector2 spawnSize;

    private float _timeSinceLastSpawn = 0f;

    private class BoidSpawnerBaker : Baker<BoidSpawnerAuthoring>
    {
        public override void Bake(BoidSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new BoidSpawner
            {
                Prefab = GetEntity(authoring.boidPrefab, TransformUsageFlags.Renderable),
                SpawnSize = authoring.spawnSize,
                TimePerSpawn = Mathf.Max(0.001f, authoring.timePerSpawn),
                MaxBoids = authoring.targetBoidCount
            });
            AddComponent(entity, new BoidSpawnerState
            {
                TimeSinceLastSpawn = 0f
            });
        }
    }
    
    private void FixedUpdate()
    {
        timePerSpawn = Mathf.Max(0.001f, timePerSpawn);
        _timeSinceLastSpawn += Time.fixedDeltaTime;
        
        while(_timeSinceLastSpawn >= timePerSpawn &&
              swarmOwner.BoidCount < targetBoidCount)
        {
            _timeSinceLastSpawn -= timePerSpawn;
            SpawnBoid();
        }
        
        // don't let _timeSinceLastSpawn accumulate across frames above the amount required to spawn 1 
        _timeSinceLastSpawn = Mathf.Min(_timeSinceLastSpawn, timePerSpawn);
    }

    private void SpawnBoid()
    {
        Vector2 myCenter = transform.position;
        var spawnPosition = myCenter + new Vector2(
            UnityEngine.Random.Range(-spawnSize.x / 2, spawnSize.x / 2),
            UnityEngine.Random.Range(-spawnSize.y / 2, spawnSize.y / 2)
        );
        var instantiated = Instantiate(boidPrefab, spawnPosition, Quaternion.identity, this.transform);
        instantiated.Initialize(swarmOwner);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector2 myCenter = transform.position;
        Gizmos.DrawWireCube(myCenter, spawnSize);
    }
}