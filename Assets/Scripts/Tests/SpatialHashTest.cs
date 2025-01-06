using System.Collections;
using System.Linq;
using Boids.Domain;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

public class SpatialHashTest
{
    // A Test behaves as an ordinary method
    [Test]
    public void SpatialHash_AddOneItem_GetsOneItem()
    {
        var hash = new SpatialHash<string>(Vector2Int.one);
        
        hash.Add(Vector2.zero, "test");
        var results = hash.GetNeighborBucketsEnumerator(Vector2.zero, 1)
            .ToList();

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("test", results.Single());
    }
    
    
    [Test]
    public void SpatialHash_GetOverlapping_Returns3x3Square()
    {
        var hash = new SpatialHash<string>(Vector2Int.one);

        var bucketResult = hash.GetNeighborBuckets(Vector2.zero, 0.6f);

        Assert.AreEqual(9, bucketResult.Count);
        var bucketPoints = bucketResult.Select(x => x.Center).ToList();
        Assert.Contains(V(-1, -1), bucketPoints);
        Assert.Contains(V(-1,  0), bucketPoints);
        Assert.Contains(V(-1,  1), bucketPoints);
        Assert.Contains(V( 0, -1), bucketPoints);
        Assert.Contains(V( 0,  0), bucketPoints);
        Assert.Contains(V( 0,  1), bucketPoints);
        Assert.Contains(V( 1, -1), bucketPoints);
        Assert.Contains(V( 1,  0), bucketPoints);
        Assert.Contains(V( 1,  1), bucketPoints);
    }
    
    [Test]
    public void SpatialHash_GetOverlapping_Returns2x2Square()
    {
        var hash = new SpatialHash<string>(Vector2Int.one * 2);

        var bucketResult = hash.GetNeighborBuckets(Vector2.one * 0.1f, 1f);

        Assert.AreEqual(4, bucketResult.Count);
        var bucketPoints = bucketResult
            .Select(x => x.Center).ToList();
        Assert.Contains(V(0,  0), bucketPoints);
        Assert.Contains(V(0,  2), bucketPoints);
        Assert.Contains(V(2,  0), bucketPoints);
        Assert.Contains(V(2,  2), bucketPoints);
    }

    [Test]
    public void SpatialHashDefinition_GetOverlapping_Returns2x2Square()
    {
        var hashDefinition = new SpatialHashDefinition(2f);

        hashDefinition.GetMinMaxBuckets(
            new float2(0.5f),
            1.4f,
            out var minBucket, out var maxBucket);
        
        Assert.AreEqual(Vi(-1, -1), minBucket);
        Assert.AreEqual(Vi(0, 0), maxBucket);
    }
    
    [Test]
    public void SpatialHashDefinition_GetOverlapping_Returns3x3Square()
    {
        var hashDefinition = new SpatialHashDefinition(1f);

        hashDefinition.GetMinMaxBuckets(
            new float2(.5f),
            0.6f,
            out var minBucket, out var maxBucket);
        
        Assert.AreEqual(Vi(-1, -1), minBucket);
        Assert.AreEqual(Vi(1, 1), maxBucket);
    }


    
    private Vector2 V(float x, float y)
    {
        return new Vector2(x, y);
    }
    private int2 Vi(int x, int y)
    {
        return new int2(x, y);
    }
}
