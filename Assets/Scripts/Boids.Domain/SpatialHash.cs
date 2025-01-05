using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Profiling;

namespace Boids.Domain
{
    public class SpatialHash<T>
    {
        public Vector2 CellSize => _cellSize;
        private readonly Vector2 _cellSize;
        private readonly Dictionary<Vector2Int, List<T>> _cellContents = new();

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
            if (!_cellContents.ContainsKey(cell))
            {
                _cellContents[cell] = new List<T>();
            }
            _cellContents[cell].Add(item);
        }
    
        public List<T>?[] GetNeighborBuckets(
            Vector2 around,
            float overlappingSquareRadius,
            List<T>?[]? existingBuckets)
        {
            Profiler.BeginSample("SpatialHash.GetBuckets");
            var overlapExtents = new Vector2(overlappingSquareRadius, overlappingSquareRadius);
            var minCell = GetCell(around - overlapExtents);
            var maxCell = GetCell(around + overlapExtents);
        
            var outLen = GetIndex(maxCell) + 1;
            if(existingBuckets == null || existingBuckets.Length != outLen)
            {
                Profiler.BeginSample("SpatialHash.GetBuckets.AllocBucket");
                existingBuckets = new List<T>[outLen];
                Profiler.EndSample();
            }
            for (var x = minCell.x; x <= maxCell.x; x++)
            {
                for (var y = minCell.y; y <= maxCell.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (_cellContents.TryGetValue(cell, out var bucket))
                    {
                        var index = GetIndex(cell);
                        existingBuckets[index] = bucket;
                    }
                }
            }
            Profiler.EndSample();

            return existingBuckets;
        
            int GetIndex(Vector2Int atPos)
            {
                var relativePos = atPos - minCell;
                var height = maxCell.y - minCell.y;
                return relativePos.x + relativePos.y * height;
            }
        }
    
        private Vector2Int GetCell(Vector2 position)
        {
            return Vector2Int.RoundToInt(position / _cellSize);
        }
    }
}