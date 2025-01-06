using Boids.Domain.DebugFlags;
using Boids.Domain.Lifetime;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace Boids.Domain
{
    readonly partial struct NewBoidAspect : IAspect
    {
        private readonly Entity _entity;
        
        private readonly Boid _boidShared;
        private readonly BoidSpawnData _boidSpawn;
        private readonly RefRW<PhysicsVelocity> _velocity;
        
        public void Initialize(ref Unity.Mathematics.Random rng, EntityCommandBuffer ecb, float time)
        {
            var cycleDir = new float2(math.sin(time), math.cos(time));
            var randDir = rng.NextFloat2Direction();
            var targetHeading = math.lerp(cycleDir, randDir, _boidSpawn.randomMagnitude);

            _velocity.ValueRW.Linear = new float3(targetHeading * _boidSpawn.initialSpeed, 0);
            
            var deathTime = Time.fixedTime + _boidSpawn.lifetimeSeconds;
            deathTime *= rng.NextFloat(0.9f, 1.1f);
            var boidState = new BoidState()
            {
            };
            var lifetime = new LifetimeComponent()
            {
                deathTime = deathTime
            };
            
            ecb.AddComponent(_entity, boidState);
            ecb.AddComponent(_entity, lifetime);
            //ecb.AddComponent(_entity, new DebugFlagComponent());
            ecb.RemoveComponent<BoidSpawnData>(_entity);
        }
    }
}