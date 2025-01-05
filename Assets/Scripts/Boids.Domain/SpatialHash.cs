using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Profiling;

namespace Boids.Domain
{
    public struct Bucket<T>
    {
        public Vector2 Center { get; }
        public List<T>? Contents { get; private set; }

        public static Bucket<T> Empty(Vector2 center) => new Bucket<T>(center)
        {
            Contents = null
        };
        public static Bucket<T> Fillable(Vector2 center) => new Bucket<T>(center)
        {
            Contents = new List<T>()
        };
        
        private Bucket(Vector2 center)
        {
            Contents = null;
            Center = center;
        }
        
        public void Clear() => Contents?.Clear();
        public void Add(T item)
        {
            Contents ??= new List<T>();
            Contents.Add(item);
        }
    }
    
    public class SpatialHash<T>
    {
        public Vector2 CellSize => _cellSize;
        private readonly Vector2 _cellSize;
        private readonly Dictionary<Vector2Int, Bucket<T>> _cellContents = new();

        public SpatialHash(Vector2 cellSize)
        {
            _cellSize = cellSize;
        }

        public void Clear()
        {
            foreach (var boidBucket in _cellContents.Values)
            {
                boidBucket.Clear();
            }
        }
    
        public void Add(Vector2 position, T item)
        {
            var cell = GetCell(position);
            if(!_cellContents.TryGetValue(cell, out var bucket))
            {
                bucket = Bucket<T>.Fillable(cell);
                _cellContents.Add(cell, bucket);
            }
            bucket.Add(item);
        }
    
        public Bucket<T>?[] GetNeighborBuckets(
            Vector2 around,
            float overlappingSquareRadius,
            Bucket<T>?[]? existingBuckets)
        {
            Profiler.BeginSample("SpatialHash.GetBuckets");
            var overlapExtents = new Vector2(overlappingSquareRadius, overlappingSquareRadius);
            var minCell = GetCell(around - overlapExtents);
            var maxCell = GetCell(around + overlapExtents);
        
            var outLen = GetIndex(maxCell) + 1;
            if(existingBuckets == null || existingBuckets.Length < outLen)
            {
                Profiler.BeginSample("SpatialHash.GetBuckets.AllocBucket");
                existingBuckets = new Bucket<T>?[outLen];
                Profiler.EndSample();
            }
            for (var x = minCell.x; x <= maxCell.x; x++)
            {
                for (var y = minCell.y; y <= maxCell.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    var index = GetIndex(cell);
                    if (_cellContents.TryGetValue(cell, out var bucket))
                    {
                        existingBuckets[index] = bucket;
                    }
                    else
                    {
                        existingBuckets[index] = Bucket<T>.Empty(VectorUtil.MultComponents(cell, _cellSize));
                    }
                }
            }
            Profiler.EndSample();

            return existingBuckets;
        
            int GetIndex(Vector2Int atPos)
            {
                var relativePos = atPos - minCell;
                var height = maxCell.y - minCell.y + 1; // +1 because it's inclusive
                return relativePos.x + relativePos.y * height;
            }
        }
    
        private Vector2Int GetCell(Vector2 position)
        {
            return Vector2Int.RoundToInt(position / _cellSize);
        }
    }
}