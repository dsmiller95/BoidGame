using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Boids.Domain.Obstacles
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct DrawObstacleFieldSystem : ISystem
    {
        private bool _enabled;
        
        public void OnCreate(ref SystemState state)
        {
            _enabled = false;
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_enabled) return;
            var world = state.WorldUnmanaged;

            var obstacleFunction = ObstacleFunction.Default;
            var collectJob = CollectObstacleField.WithSize(obstacleFunction,
                -50, 50, 4000);
            
            var obstacleSet = new NativeParallelHashSet<float2>(collectJob.GetRequiredSpace(), world.UpdateAllocator.ToAllocator);
            collectJob.results = obstacleSet.AsParallelWriter();

            var jobDep = collectJob.Schedule(collectJob.InvokeSize, 64);

            var drawJob = new DrawObstacleField
            {
                Obstacles = obstacleSet
            };

            jobDep = drawJob.Schedule(jobDep);
            
            state.Dependency = JobHandle.CombineDependencies(state.Dependency, jobDep);
        }


        [BurstCompile]
        private struct DrawObstacleField : IJob
        {
            public NativeParallelHashSet<float2> Obstacles;
            public void Execute()
            {
                foreach (float2 obstacle in Obstacles)
                {
                    DrawObstacle(obstacle);
                }
            }

            private void DrawObstacle(float2 at)
            {
                Vector2 c1 = at + new float2(.2f, .2f);
                Vector2 c2 = at + new float2(.2f, -.2f);
                Vector2 c3 = at + new float2(-.2f, -.2f);
                Vector2 c4 = at + new float2(-.2f, .2f);
            
                Debug.DrawLine(c1, c2, Color.red);
                Debug.DrawLine(c2, c3, Color.red);
                Debug.DrawLine(c3, c4, Color.red);
                Debug.DrawLine(c4, c1, Color.red);
            }
        }
    }
    
    [BurstCompile]
    public struct CollectObstacleField : IJobParallelFor
    {
        public static CollectObstacleField WithSize(
            ObstacleFunction function,
            float2 min, float2 max, int2 resolution)
        {
            return new CollectObstacleField
            {
                ObstacleFunction = function,
                min = min,
                resolution = resolution,
                perPointSize = (max - min) / resolution,
            };
        }
            
        public int GetRequiredSpace()
        {
            var requiredByFunction = (int)math.ceil(ObstacleFunction.GetMaximumDensity() * Area);
            return math.min(requiredByFunction * 2, InvokeSize);
        }

        public int InvokeSize => resolution.x * resolution.y;
        public float Area
        {
            get
            {
                var size = perPointSize * resolution;
                var area = size.x * size.y;
                return area;
            }
        }

        public ObstacleFunction ObstacleFunction;
        public float2 min;
        public float2 perPointSize;
        public int2 resolution;

        public NativeParallelHashSet<float2>.ParallelWriter results;

        public void Execute(int index)
        {
            var pointIndex = new int2(index % resolution.y, index / resolution.y);
            var point = pointIndex * perPointSize + min;
            var obstacle = ObstacleFunction.GetObstacleFromField(point);
            if(math.any(math.isnan(obstacle))) throw new InvalidOperationException("Obstacle function returned NaN");
            results.Add(obstacle);
        }
    }
}