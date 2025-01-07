using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Boids.Domain.Goals
{
    [Serializable]
    public struct Goal : IComponentData
    {
        public float radius;
        public float consumptionRadius => radius;
        public int required;
        public float decayPerSecond;
    }
    
    [Serializable]
    public struct GoalRender : IComponentData
    {
        public Entity scaleForProgress;
    }

    [Serializable]
    public struct GoalCount : IComponentData
    {
        public int count;
        public float partialCount;
    }
    
    public class GoalAuthoring : MonoBehaviour
    {
        [Range(1f, 30f)]
        public float radius = 1f;
        public int required = 100;
        public int decayPerSecond = 10;
        public GameObject scaleForProgress = null!;

        private void Awake()
        {
            if(scaleForProgress == null) throw new Exception("countText is null");
        }

        private class GoalBaker : Baker<GoalAuthoring>
        {
            public override void Bake(GoalAuthoring authoring)
            {
                DependsOn(authoring.scaleForProgress);

                if (authoring.scaleForProgress == null) return;
                    
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new Goal
                {
                    radius = authoring.radius,
                    required = authoring.required,
                    decayPerSecond = authoring.decayPerSecond,
                });
                AddComponent(entity, new GoalCount());
                var scaleChildEntity = GetEntity(authoring.scaleForProgress, TransformUsageFlags.Dynamic);
                if(scaleChildEntity == Entity.Null)
                {
                    throw new Exception("scaleForProgress entity is null");
                }
                var render = new GoalRender()
                {
                    scaleForProgress = scaleChildEntity,
                };
                AddComponent(entity, render);
                
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            var scaledRadius = this.radius * this.transform.lossyScale.x;
            Gizmos.DrawWireSphere(transform.position, scaledRadius);
        }
    }
}