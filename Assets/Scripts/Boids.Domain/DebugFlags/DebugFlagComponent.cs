using System;
using Unity.Entities;

namespace Boids.Domain.DebugFlags
{
    [Serializable]
    public struct DebugFlagComponent : IComponentData
    {
        public bool isFlagged;
    }
}