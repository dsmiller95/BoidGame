using System;
using UnityEngine;

public class BoidBehavior : MonoBehaviour
{
    [SerializeField] public BoidConfig config;
    
    private Rigidbody2D _rigidbody2D;
    private Collider2D _collider2D;
    
    [Serializable]
    public class BoidConfig
    {
        public float initialSpeed = 2f;
        
        public float maxSpeed = 5f;
        public float neighborRadius = 3f;
        public int maxNeighbors = 10;
        public float separationRadius = 1f;
        public float separationWeight = 1.5f;
        public float alignmentWeight = 1f;
        public float cohesionWeight = 1f;
    }
    
    public void Initialize()
    {
        _rigidbody2D.linearVelocity = UnityEngine.Random.insideUnitCircle.normalized * config.initialSpeed;
    }
    
    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _collider2D = GetComponent<Collider2D>();
    }
    
    private Collider2D[] _neighborQueryResults;

    private void FixedUpdate()
    {
        if(_neighborQueryResults?.Length != config.maxNeighbors)
        {
            _neighborQueryResults = new Collider2D[config.maxNeighbors];
        }
        
        Vector2 separation = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        Vector2 cohesion = Vector2.zero;
        int neighborCount = 0;

        var myLayer = _rigidbody2D.gameObject.layer;
        var contactFilter = new ContactFilter2D()
        {
            useLayerMask = true,
            layerMask = 1 << myLayer
        };
        
        Physics2D.OverlapCircle(transform.position, config.neighborRadius, contactFilter, _neighborQueryResults);
        foreach (Collider2D neighbor in _neighborQueryResults)
        {
            if (neighbor != _collider2D)
            {
                Vector2 toNeighbor = (Vector2)neighbor.transform.position - _rigidbody2D.position;
                float distance = toNeighbor.magnitude;

                // Separation
                if (distance < config.separationRadius)
                {
                    separation -= toNeighbor / distance;
                }

                // Alignment and Cohesion
                BoidBehavior otherBoid = neighbor.GetComponent<BoidBehavior>();
                if (otherBoid != null)
                {
                    alignment += otherBoid._rigidbody2D.linearVelocity;
                    cohesion += (Vector2)neighbor.transform.position;
                    neighborCount++;
                }
            }
        }

        if (neighborCount > 0)
        {
            alignment /= neighborCount;
            alignment = alignment.normalized * config.maxSpeed;

            cohesion /= neighborCount;
            cohesion = (cohesion - _rigidbody2D.position).normalized * config.maxSpeed;
        }

        Vector2 velocity = _rigidbody2D.linearVelocity;
        velocity += separation * config.separationWeight + alignment * config.alignmentWeight + cohesion * config.cohesionWeight;
        velocity = Vector2.ClampMagnitude(velocity, config.maxSpeed);

        _rigidbody2D.linearVelocity = velocity;
    }
}
