using System;
using System.Collections.Generic;
using UnityEngine;

public class BoidSwarm : MonoBehaviour
{
    public Vector2 HashGridSize = new Vector2(10, 10);
    
    private float _maxNeighborDistance = 10f;
    private List<BoidBehavior> _allBoids;
    
    private void Awake()
    {
        _allBoids = new List<BoidBehavior>();
        _spatialHash = new SpatialHash<BoidBehavior>(HashGridSize);
    }
    
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
    }
    
    private SpatialHash<BoidBehavior> _spatialHash;
    private List<BoidBehavior>[] _neighborBuckets;
    
    private void FixedUpdate()
    {
        if(HashGridSize != _spatialHash.CellSize)
        {
            _spatialHash = new SpatialHash<BoidBehavior>(HashGridSize);
        }
        
        _spatialHash.Clear();
        
        foreach (BoidBehavior boid in _allBoids)
        {
            if (boid == null)
            {
                throw new InvalidOperationException("boids must deregister before being destroyed");
            }
            
            var position = boid.GetPosition();
            _spatialHash.Add(position, boid);
        }

        foreach (BoidBehavior boid in _allBoids)
        {
            _spatialHash.GetNeighborBuckets(boid.GetPosition(), _maxNeighborDistance, ref _neighborBuckets);
            
            boid.ManagedFixedUpdate(_neighborBuckets);
        }
    }
}