using Boids.Domain.DebugFlags;
using Boids.Domain.Lifetime;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace Boids.Domain
{
    static class NewBoidHelper
    {
        public static void Initialize(
            Entity entity,
            in Boid boidShared,
            in BoidSpawnData boidSpawn,
            RefRW<PhysicsVelocity> velocity,
            ref Unity.Mathematics.Random rng, EntityCommandBuffer ecb, float time)
        {
            var cycleDir = new float2(math.sin(boidSpawn.spawnAngle), math.cos(boidSpawn.spawnAngle));
            var randDir = rng.NextFloat2Direction();
            var targetHeading = math.lerp(cycleDir, randDir, boidSpawn.randomMagnitude);

            velocity.ValueRW.Linear = new float3(targetHeading * boidSpawn.initialSpeed, 0) * boidShared.simSpeedMultiplier;

            var timeTillDeath = boidSpawn.lifetimeSeconds;
            timeTillDeath *= rng.NextFloat(0.9f, 1.1f);
            var deathTime = time + timeTillDeath;
            var boidState = new BoidState()
            {
            };
            var lifetime = new LifetimeComponent()
            {
                deathTime = deathTime
            };
            
            ecb.AddComponent(entity, boidState);
            ecb.AddComponent(entity, lifetime);
            //ecb.AddComponent(entity, new DebugFlagComponent());
            ecb.RemoveComponent<BoidSpawnData>(entity);
        }
    }
}