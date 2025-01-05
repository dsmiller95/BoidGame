using System;
using Cysharp.Threading.Tasks;
using Dman.Utilities;
using UnityEngine;

public class BoidSpawner : MonoBehaviour
{
    public BoidBehavior boidPrefab;
    
    public float timePerSpawn = 0.5f;
    
    public Vector2 spawnSize;

    private float _timeSinceLastSpawn = 0f;

    
    private void FixedUpdate()
    {
        if(_timeSinceLastSpawn < timePerSpawn)
        {
            _timeSinceLastSpawn += Time.fixedDeltaTime;
            return;
        }
        _timeSinceLastSpawn -= Time.fixedDeltaTime;
        
        SpawnBoid();
    }

    private void SpawnBoid()
    {
        Vector2 myCenter = transform.position;
        var spawnPosition = myCenter + new Vector2(
            UnityEngine.Random.Range(-spawnSize.x / 2, spawnSize.x / 2),
            UnityEngine.Random.Range(-spawnSize.y / 2, spawnSize.y / 2)
        );
        var instantiated = Instantiate(boidPrefab, spawnPosition, Quaternion.identity);
        instantiated.Initialize();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector2 myCenter = transform.position;
        Gizmos.DrawWireCube(myCenter, spawnSize);
    }
}