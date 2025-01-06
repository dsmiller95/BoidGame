using Boids.Domain.Obstacles;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Tests
{
    public class ObstacleFunctionTest
    {
        
        [Theory]
        [TestCase("010_reg", 10, 100, 10f, 16f)]
        [TestCase("020_reg",20, 200, 10f, 16f)]
        [TestCase("030_reg",30, 300, 10f, 16f)]
        [TestCase("050_reg",50, 500, 10f, 16f)]
        [TestCase("100_reg",100, 1000, 10f, 16f)]
        [TestCase("200_reg",200, 2000, 10f, 16f)]
        [TestCase("400_reg",400, 4000, 10f, 16f)]
        [TestCase("050_denseRad",400, 4000, 2f, 16f)]
        [TestCase("050_denseAngle",400, 4000, 10f, 64f)]
        public void ObstacleFunction_EstimatesDensity_WhenDifferentSizes(
            string name, float size, int resolution, float spiralSpacing, float angleDensityMult)
        {
            var obstacleFunction = ObstacleFunction.Default;
            obstacleFunction.spiralSpacing = spiralSpacing;
            obstacleFunction.angleDensityMultiplier = angleDensityMult / math.PI2;
            
            var collectJob = CollectObstacleField.WithSize(
                obstacleFunction, -size, size, resolution);
        
            using var obstacleSet = new NativeParallelHashSet<float2>(collectJob.InvokeSize, Allocator.TempJob);
            collectJob.results = obstacleSet.AsParallelWriter();
        
            collectJob.Schedule(collectJob.InvokeSize, 64).Complete();
        
            var precalculatedDensity = obstacleFunction.GetMaximumDensity();
            var actualDensity = obstacleSet.Count() / collectJob.Area;
            var undershot = precalculatedDensity - actualDensity;
            var errorPercent = undershot / actualDensity;

            var msg =
                $"Underestimated by {-errorPercent:P1}. expected to overshoot. " +
                $"{obstacleSet.Count()} obstacles found, " +
                $"at most {precalculatedDensity * collectJob.Area} obstacles expected";
            Debug.Log(msg);
            
            Assert.Greater(precalculatedDensity, actualDensity, msg);
        }
    }
}
