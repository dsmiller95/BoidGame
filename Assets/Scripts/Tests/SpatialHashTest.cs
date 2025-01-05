using System.Collections;
using System.Linq;
using Boids.Domain;
using NUnit.Framework;
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

        var bucketResult =
            hash.GetNeighborBuckets(Vector2.zero, 0.6f, null);

        Assert.AreEqual(9, bucketResult.Length);
        var bucketPoints = bucketResult.WhereHasValue()
            .Select(x => x.Center).ToList();
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

        var bucketResult =
            hash.GetNeighborBuckets(Vector2.one * 0.1f, 1f, null);

        Assert.AreEqual(4, bucketResult.Length);
        var bucketPoints = bucketResult.WhereHasValue()
            .Select(x => x.Center).ToList();
        Assert.Contains(V(0,  0), bucketPoints);
        Assert.Contains(V(0,  2), bucketPoints);
        Assert.Contains(V(2,  0), bucketPoints);
        Assert.Contains(V(2,  2), bucketPoints);
    }
    
    
    [Test]
    public void SpatialHash_GetOverlapping_DoesNotReallocate_ToSmallerBuffer()
    {
        var hash = new SpatialHash<string>(Vector2Int.one * 2);

        var existingBuckets = new Bucket<string>?[9];
        
        var bucketResult = hash.GetNeighborBuckets(Vector2.one * 0.1f, 0.1f, existingBuckets);
        
        Assert.AreEqual(9, bucketResult.Length);
        Assert.AreEqual(existingBuckets, bucketResult);
    }

    private Vector2 V(float x, float y)
    {
        return new Vector2(x, y);
    }
}
