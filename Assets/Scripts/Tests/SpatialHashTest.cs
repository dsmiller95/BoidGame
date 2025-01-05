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
        var results = hash.GetNeighborBuckets(Vector2.zero, 1)
            .ToList();

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("test", results.Single());
    }
}
