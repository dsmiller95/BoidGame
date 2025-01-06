using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Boids.Domain.DebugFlags
{
    public enum FlagType
    {
        None = 0,
        Secondary = 1,
        Primary = 2,
    }
    
    [Serializable]
    public struct DebugFlagComponent : IComponentData
    {
        public FlagType flag;
        
        public void SetFlag(FlagType flagType)
        {
            flag = (FlagType)math.max((int)flagType, (int)flag);
        }
    }
}