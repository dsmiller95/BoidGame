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
    public float TimePerSpawnGroup;
    public int MaxBoids;
    public int GroupSize;

    public BoidSpawnData SpawnData;

    [Pure]
    public float MaxTimeAccumulate(float deltaTime)
    {
        return math.max(TimePerSpawnGroup, deltaTime);
    }

    [Pure]
    public float2 GetRelativeSpawn(ref Random rng)
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
    public int boidGroupSize = 50;
    public int targetBoidCount = 1000;
    
    [Range(0, 360)]
    public float spawnAngle;
    
    public float initialSpeed = 5;
    [Range(0, 1)]
    public float randomMagnitude = 0;
    public float lifetimeSeconds = 30;
    
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
                TimePerSpawnGroup = Mathf.Max(0.001f, authoring.timePerSpawn),
                MaxBoids = authoring.targetBoidCount,
                GroupSize = authoring.boidGroupSize,
                SpawnData = new BoidSpawnData()
                { 
                    spawnAngle = authoring.spawnAngle * Mathf.Deg2Rad,
                    initialSpeed = authoring.initialSpeed,
                    randomMagnitude = authoring.randomMagnitude,
                    lifetimeSeconds = authoring.lifetimeSeconds
                }
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector2 myCenter = transform.position;
        Gizmos.DrawWireCube(myCenter, spawnSize);
        
        var dirFromAngle = new Vector2(Mathf.Sin(spawnAngle * Mathf.Deg2Rad), Mathf.Cos(spawnAngle * Mathf.Deg2Rad));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(myCenter, myCenter + dirFromAngle * 5);
    }
}