using System;
using System.Collections.Generic;
using Boids.Domain;
using UnityEngine;
using UnityEngine.Profiling;

[Serializable]
public struct SwarmConfig
{
    public static SwarmConfig Default => new()
    {
        drawDebugRays = false,
        hashGridSize = new Vector2(10, 10)
    };
    
    public bool drawDebugRays;
    public Vector2 hashGridSize;
}

public class BoidSwarm : MonoBehaviour
{
    public SwarmConfig config = SwarmConfig.Default;
    
    private float _maxNeighborDistance = 5f;
    private List<BoidBehavior> _allBoids = new();
    
    public int BoidCount => _allBoids.Count;
    
    public void RegisterBoid(BoidBehavior boid)
    {
        _maxNeighborDistance = Mathf.Max(_maxNeighborDistance, boid.GetMaxNeighborDistance());
        _allBoids.Add(boid);
    }
    
    public void DeregisterBoid(BoidBehavior boid)
    {
        RemoveBoidFromMaxDistance(boid);
        _allBoids.Remove(boid);
    }
    
    private void RemoveBoidFromMaxDistance(BoidBehavior boid)
    {
        if (!(boid.GetMaxNeighborDistance() >= _maxNeighborDistance)) return;
        
        Profiler.BeginSample("BoidSwarm.RemoveMaxDist", this);
        var newMax = 0f;
        foreach (BoidBehavior b in _allBoids)
        {
            newMax = Mathf.Max(newMax, b.GetMaxNeighborDistance());
            if(newMax >= _maxNeighborDistance)
            {
                // once we get up to the current max, we know we don't need to change the maxDist
                // in the case where all boids are the same config, this will exit after 1 iteration
                return;
            }
        }
        _maxNeighborDistance = newMax;
        Profiler.EndSample();
    }

    private SpatialHash<BoidBehavior> _spatialHash = new(new Vector2(10, 10));
    private List<BoidBehavior>?[]? _neighborBuckets;
    
    private void FixedUpdate()
    {
        Profiler.BeginSample("BoidSwarm.Setup", this);
        if(config.hashGridSize != _spatialHash.CellSize)
        {
            _spatialHash = new SpatialHash<BoidBehavior>(config.hashGridSize);
        }
        _spatialHash.Clear();
        Profiler.EndSample();
        
        Profiler.BeginSample("BoidSwarm.PopulateHash", this);
        foreach (BoidBehavior boid in _allBoids)
        {
            if (boid == null)
            {
                throw new InvalidOperationException("boids must deregister before being destroyed");
            }
            
            var position = boid.GetPosition();
            _spatialHash.Add(position, boid);
        }
        Profiler.EndSample();

        Profiler.BeginSample("BoidSwarm.UpdateBoids", this);
        foreach (BoidBehavior boid in _allBoids)
        {
            _neighborBuckets = _spatialHash.GetNeighborBuckets(
                boid.GetPosition(),
                _maxNeighborDistance, 
                _neighborBuckets);
            
            boid.ManagedFixedUpdate(_neighborBuckets, config);
        }
        Profiler.EndSample();
    }
}