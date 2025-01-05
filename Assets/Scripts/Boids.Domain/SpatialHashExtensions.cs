using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Boids.Domain
{
    public static class SpatialHashExtensions
    {
        public static IEnumerable<T> GetNeighborBucketsEnumerator<T>(this SpatialHash<T> hash, Vector2 around, float overlappingSquareRadius)
        {
            return hash.GetNeighborBuckets(around, overlappingSquareRadius, null)
                .WhereHasValue()
                .Where(x => x.Contents != null).SelectMany(x => x.Contents);
        }
    }
}