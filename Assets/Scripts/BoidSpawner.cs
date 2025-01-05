using System;
using Cysharp.Threading.Tasks;
using Dman.Utilities;
using UnityEngine;

public class BoidSpawner : MonoBehaviour
{
    public BoidSwarm swarmOwner;
    public BoidBehavior boidPrefab;
    
    [Range(0.001f, 10f)]
    public float timePerSpawn = 0.5f;
    public int targetBoidCount = 1000;
    
    public Vector2 spawnSize;

    private float _timeSinceLastSpawn = 0f;

    
    private void FixedUpdate()
    {
        timePerSpawn = MathF.Max(0.001f, timePerSpawn);
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