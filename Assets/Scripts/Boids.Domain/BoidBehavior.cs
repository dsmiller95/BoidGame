using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Boids.Domain
{
    [Serializable]
    public struct Boid : ISharedComponentData
    {
        public float maxSpeed;
        public float minSpeed;
        public float separationRadius;
        public float separationWeight;
        public float alignmentRadius;
        public float alignmentWeight;
        public float cohesionRadius;
        public float cohesionWeight;

        public float obstacleAvoidanceWeight;
        public float obstacleAvoidanceConstantRepellent;
        
        
        
        public float2 bakedObstacle;

        // TODO: expose in authoring
        public float cellRadius => MaxNeighborDistance / 2f;
        
        public float MaxNeighborDistance => math.max(math.max(separationRadius, alignmentRadius), cohesionRadius);
    }

    [Serializable]
    public struct BoidSpawnData : ISharedComponentData
    {
        public float initialSpeed;
        public float randomMagnitude;
        public float lifetimeSeconds;
    }
    
    [Serializable]
    public struct BoidState : IComponentData
    {
    }

    public class BoidBehavior : MonoBehaviour
    {
        [SerializeField] public BoidConfig config;
    
        private Rigidbody2D _rigidbody2D;
        private BoidSwarm _mySwarm;
        private float _deathTime = 0f;
    
        [Serializable]
        public class BoidConfig
        {
            public float initialSpeed = 5f;
        
            public float maxSpeed = 2f;
            public float maxForce = 3f;
            public float minSpeed = 1f;
            public int maxNeighbors = 50;

            public float SeparationRadiusSquared => separationRadius * separationRadius;
            public float separationRadius = 3;
            public float separationWeight = 1.5f;
        
            public float AlignmentRadiusSquared => alignmentRadius * alignmentRadius;
            public float alignmentRadius = 5f;
            public float alignmentWeight = 1f;
        
            public float CohesionRadiusSquared => cohesionRadius * cohesionRadius;
            public float cohesionRadius = 6f;
            public float cohesionWeight = 1f;
            
            public float obstacleAvoidanceWeight = 2f;
            public float obstacleAvoidanceConstantRepellent = 2f;
            // [ra]
            // public float obstacleAvoidanceMaxDepth = f;
        
            public float lifetimeSeconds = 10f;
        
            [Range(0, 1)]
            public float randomMagnitude = 0.1f;

        }
    
        public void Initialize(BoidSwarm swarm)
        {
            var cycleDir = new Vector2(Mathf.Sin(Time.time), Mathf.Cos(Time.time));
            var randDir = UnityEngine.Random.insideUnitCircle.normalized;
            var targetHeading = Vector2.Lerp(cycleDir, randDir, config.randomMagnitude);
            _rigidbody2D.linearVelocity = targetHeading * config.initialSpeed; 
            _deathTime = Time.fixedTime + config.lifetimeSeconds;
            _deathTime *= Random.Range(0.9f, 1.1f);
        
            _mySwarm = swarm;
            _mySwarm.RegisterBoid(this);
        }

        public Vector2 GetPosition()
        {
            return _rigidbody2D.position;
        }
    
        public float GetMaxNeighborDistance()
        {
            return Mathf.Max(config.separationRadius, config.alignmentRadius, config.cohesionRadius);
        }

        private class Baker : Baker<BoidBehavior>
        {
            public override void Bake(BoidBehavior authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.Dynamic);
                AddSharedComponent(entity, new Boid
                {
                    maxSpeed = authoring.config.maxSpeed,
                    minSpeed = authoring.config.minSpeed,
                    separationRadius = authoring.config.separationRadius,
                    separationWeight = authoring.config.separationWeight,
                    alignmentRadius = authoring.config.alignmentRadius,
                    alignmentWeight = authoring.config.alignmentWeight,
                    cohesionRadius = authoring.config.cohesionRadius,
                    cohesionWeight = authoring.config.cohesionWeight,
                    //obstacleAvoidanceRadius = authoring.config.obstacleAvoidanceRadius,
                    obstacleAvoidanceWeight = authoring.config.obstacleAvoidanceWeight,
                    obstacleAvoidanceConstantRepellent = authoring.config.obstacleAvoidanceConstantRepellent,
                    
                    bakedObstacle = new float2(2, 4),
                });
                AddSharedComponent(entity, new BoidSpawnData
                {
                    initialSpeed = authoring.config.initialSpeed,
                    randomMagnitude = authoring.config.randomMagnitude,
                    lifetimeSeconds = authoring.config.lifetimeSeconds
                });
            }
        }
    
        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            Debug.Assert(_mySwarm != null, "boid must be initialized with a swarm", this);
        }
    
        public BoidUpdateResult ManagedFixedUpdate(
            List<Bucket<BoidBehavior>> sharedBucketBoids,
            SwarmConfig swarmConfig,
            float deltaTime,
            ref BoidUpdateInfo updateInfo)
        {
            if(_deathTime < Time.fixedTime)
            {
                return BoidUpdateResult.Destroy;
            }

            updateInfo.totalBoids++;
        
            Vector2 separation = Vector2.zero;
            Vector2 alignment = Vector2.zero;
            Vector2 cohesion = Vector2.zero;
            int separationNeighborCount = 0;
            int alignmentNeighborCount = 0;
            int cohesionNeighborCount = 0;

            var maxDistSq = GetMaxNeighborDistance();
            maxDistSq *= maxDistSq;

            Profiler.BeginSample("Boid.IterateBuckets");
            foreach (var bucket in sharedBucketBoids)
            {
                if (bucket.Contents == null) continue;
                updateInfo.totalIterations += bucket.Contents.Count;
                foreach (var otherBoid in bucket.Contents)
                {
                    if (otherBoid == this)
                    {
                        continue;
                    }
                
                    var neighborPosition = otherBoid._rigidbody2D.position;
                    var myPosition = _rigidbody2D.position;
                    Vector2 toNeighbor = neighborPosition - myPosition;
                    float distanceSq = toNeighbor.sqrMagnitude;
                    if (distanceSq > maxDistSq)
                    {
                        updateInfo.totalSkipped++;
                        continue;
                    }

                    float distance = Mathf.Sqrt(distanceSq);

                    // Separation
                    if (distance < config.separationRadius)
                    {
                        separationNeighborCount++;
                        var fromNeighbor = -toNeighbor;
                        var fromNeighborNormalized = fromNeighbor / distance;
                        var separationAdjustment = 1f - (distance / config.separationRadius);
                        //separationAdjustment = separationAdjustment * separationAdjustment;
                        //separationAdjustment = Mathf.Clamp01(separationAdjustment);
                        separation += fromNeighborNormalized * (0.5f + separationAdjustment * 0.5f);
                    }

                    // Alignment and Cohesion
                    if (distance < config.alignmentRadius)
                    {
                        alignmentNeighborCount++;
                        alignment += otherBoid._rigidbody2D.linearVelocity;
                    }

                    if (distance < config.cohesionRadius)
                    {
                        cohesionNeighborCount++;
                        cohesion += neighborPosition;
                    }
                }
            }
            Profiler.EndSample();
        
            Profiler.BeginSample($"Boid.ApplyResults");
            if(separationNeighborCount > 0)
            {
                //separation =  (currentPosition * separationNeighborCount) - separation;
                //separation = separation.normalized;
                //if(separation.magnitude > config.maxForce) separation = separation.normalized * config.maxForce;
                if(swarmConfig.drawDebugRays) DrawSteeringForce(separation, Color.blue);
            }
            else separation = Vector2.zero;
        
            if(alignmentNeighborCount > 0)
            {
                alignment = (alignment / alignmentNeighborCount) - _rigidbody2D.linearVelocity;
                //alignment = alignment.normalized;
                //if(alignment.magnitude > config.maxForce) alignment = alignment.normalized * config.maxForce;
                if(swarmConfig.drawDebugRays) DrawSteeringForce(alignment, Color.green);
            }
            else alignment = Vector2.zero;

            if (cohesionNeighborCount > 0)
            {
                cohesion /= cohesionNeighborCount;
                cohesion -= _rigidbody2D.position;
                //cohesion = cohesion.normalized;
                //if(cohesion.magnitude > config.maxForce) cohesion = cohesion.normalized * config.maxForce;
                if(swarmConfig.drawDebugRays) DrawSteeringForce(cohesion, Color.red);
            }
            else cohesion = Vector2.zero;

            if (swarmConfig.drawDebugRays)
            {
                var steerFrom = _rigidbody2D.position + _rigidbody2D.linearVelocity * _velocityTimeAheadDraw;
                Debug.DrawLine(_rigidbody2D.position, steerFrom, Color.magenta);
            }

            var targetForward = separation * config.separationWeight + alignment * config.alignmentWeight + cohesion * config.cohesionWeight;
            var nextHeading = _rigidbody2D.linearVelocity + deltaTime * (targetForward - _rigidbody2D.linearVelocity);
            nextHeading = ClampMagnitude(nextHeading, config.minSpeed, config.maxSpeed);
            //nextHeading = nextHeading.normalized;

            _rigidbody2D.linearVelocity = nextHeading; //* config.maxSpeed;
            _rigidbody2D.rotation = Mathf.Atan2(nextHeading.y, nextHeading.x) * Mathf.Rad2Deg;
        
            Profiler.EndSample();
        
            //_currentSteering = Vector2.Lerp(_currentSteering, steerForce, 0.1f);
            // Vector2 velocity = _rigidbody2D.linearVelocity;
            // velocity += _currentSteering;
            // velocity = Vector2.ClampMagnitude(velocity, config.maxSpeed);
            // if (velocity.magnitude < config.minSpeed)
            // {
            //     velocity = velocity.normalized * config.minSpeed;
            // }

            //_rigidbody2D.linearVelocity = velocity;

            return BoidUpdateResult.KeepAlive;
        }

        private Vector2 ClampMagnitude(Vector2 heading, float min, float max)
        {
            var mag = heading.magnitude;
            if (mag < min) return heading.normalized * min;
            if (mag > max) return heading.normalized * max;
            return heading;
        }

        private readonly float _velocityTimeAheadDraw = 0.5f;
        private void DrawSteeringForce(Vector2 steering, Color color)
        {
            var velocity = _rigidbody2D.linearVelocity;
            var position = _rigidbody2D.position;
            var steerFrom = position;// + velocity * _velocityTimeAheadDraw;
        
            Debug.DrawLine(steerFrom, steerFrom + steering, color);
        }

        private void OnDrawGizmosSelected()
        {
            // draw circles for each radius in their color
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, config.separationRadius);
        
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, config.alignmentRadius);
        
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, config.cohesionRadius);
        }
    }
}