using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Boids.Domain
{
    public static class SpatialHashExtensions
    {
        public static IEnumerable<T> GetNeighborBuckets<T>(this SpatialHash<T> hash, Vector2 around, float overlappingSquareRadius)
        {
            return hash.GetNeighborBuckets(around, overlappingSquareRadius, null)
                .Where(x => x != null).SelectMany(x => x);
        }
    }
}