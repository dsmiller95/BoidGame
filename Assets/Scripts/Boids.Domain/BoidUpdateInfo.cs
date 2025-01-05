using System;
using UnityEngine.Serialization;

namespace Boids.Domain
{
    [Serializable]
    public struct BoidUpdateInfo
    {
        public int totalIterations;
        public int totalBoids;
        public TimeSpan totalElapsed;

        public override string ToString()
        {
            var iterationsPerBoid = totalBoids == 0 ? 0 : totalIterations / (float)totalBoids;
            return $"BoidUpdateInfo. Iters:\t{totalIterations,-10}\t" +
                   $"Boids:\t{totalBoids,-10}\t" +
                   $"ItersPerBoid:\t{iterationsPerBoid,-10:F1}\t" +
                   $"MicrosecondsPerBoid:\t{(1_000 * totalElapsed.TotalMilliseconds / totalBoids),-10:F0}\t" +
                   $"NonosecondsPerIter\t{(1_000_000 * totalElapsed.TotalMilliseconds / totalIterations),-10:F0}\t";
        }
    }

    public enum BoidUpdateResult
    {
        KeepAlive,
        Destroy,
    }
}