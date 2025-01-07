using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Boids.Domain.OnClick
{
    [Serializable]
    public struct OnClickEventComponent : IComponentData
    {
        
    }
    
    [Serializable]
    public struct OnDragBeginEvent : IComponentData
    {
        public int dragId;
        public float2 beginAt;
    }
    
    public struct ActiveDragComponent : IComponentData
    {
        public int dragId;
        public float2 continueAt;
    }
    
    [Serializable]
    public struct OnDragEndEvent : IComponentData
    {
        public int dragId;
        public float2 endedAt;
    }
    
}