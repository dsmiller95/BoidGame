using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Unity.Mathematics;
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

        public List<Bucket<T>> GetNeighborBuckets(
            Vector2 around,
            float overlappingSquareRadius)
        {
            var newList = new List<Bucket<T>>();
            GetNeighborBuckets(around, overlappingSquareRadius, newList);
            return newList;
        }
        
        public void GetNeighborBuckets(
            Vector2 around,
            float overlappingSquareRadius,
            List<Bucket<T>> existingBuckets)
        {
            Profiler.BeginSample("SpatialHash.GetBuckets");
            var overlapExtents = new Vector2(overlappingSquareRadius, overlappingSquareRadius);
            var minCell = GetCell(around - overlapExtents);
            var maxCell = GetCell(around + overlapExtents);
        
            existingBuckets.Clear();
            for (var x = minCell.x; x <= maxCell.x; x++)
            {
                for (var y = minCell.y; y <= maxCell.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!_cellContents.TryGetValue(cell, out var bucket))
                    {
                        bucket = Bucket<T>.Empty(VectorUtil.MultComponents(cell, _cellSize));
                    }
                    existingBuckets.Add(bucket);
                }
            }

            Profiler.EndSample();
        }
    
        private Vector2Int GetCell(Vector2 position)
        {
            return Vector2Int.RoundToInt(position / _cellSize);
        }
    }

    public struct SpatialHashDefinition
    {
        public float CellSize;
        public float InverseCellSize;
        
        public SpatialHashDefinition(float cellSize)
        {
            CellSize = cellSize;
            InverseCellSize = 1f / cellSize;
        }
        public void GetMinMaxBuckets(
            float2 around,
            float overlappingSquareRadius,
            out int2 minCell, out int2 maxCell)
        {
            var overlapExtents = new float2(overlappingSquareRadius, overlappingSquareRadius);
            minCell = GetCell(around - overlapExtents);
            maxCell = GetCell(around + overlapExtents);
        }
        
        public int2 GetCell(float2 position)
        {
            return new int2(math.floor(position * InverseCellSize));
        }
        
        public float2 GetCenterOfCell(int2 cell)
        {
            return (new float2(cell) + new float2(0.5f)) * CellSize;
        }
    }
}