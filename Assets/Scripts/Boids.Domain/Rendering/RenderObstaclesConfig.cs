using Dman.Utilities;
using UnityEngine;

namespace Boids.Domain.Rendering
{
    [UnitySingleton]
    public class RenderObstaclesConfig: MonoBehaviour
    {
        public RenderSdfSettings settings = new();
    }
}