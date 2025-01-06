using System;
using Unity.Entities;

namespace Boids.Domain.Lifetime
{
    [Serializable]
    public struct LifetimeComponent : IComponentData
    {
        public float deathTime;
    }
}