using System;
using UnityEngine.Serialization;

namespace Boids.Domain
{
    [Serializable]
    public struct BoidUpdateInfo
    {
        public int totalIterations;
        public int totalSkipped;
        public int totalBoids;
        public TimeSpan totalElapsed;

        public override string ToString()
        {
            var realIterations = totalIterations - totalSkipped;
            var iterationsPerBoid = totalBoids == 0 ? 0 : realIterations / (float)totalBoids;
            return $"BoidUpdateInfo. Iters:\t{realIterations,-10}\t" +
                   $"Ms:\t{totalElapsed.TotalMilliseconds,-10:F1}\t" +
                   $"Boids:\t{totalBoids,-10}\t" +
                   $"ItersPerBoid:\t{iterationsPerBoid,-10:F1}\t" +
                   $"MissRate:\t{totalSkipped/(float)totalIterations,-10:F1}\t" +
                   $"MicrosecondsPerBoid:\t{(1_000 * totalElapsed.TotalMilliseconds / totalBoids),-10:F0}\t" +
                   $"NanosecondsPerIter\t{(1_000_000 * totalElapsed.TotalMilliseconds / realIterations),-10:F0}\t";
        }
    }

    public enum BoidUpdateResult
    {
        KeepAlive,
        Destroy,
    }
}