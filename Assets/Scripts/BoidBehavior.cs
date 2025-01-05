using System;
using UnityEngine;

public class BoidBehavior : MonoBehaviour
{
    [SerializeField] public BoidConfig config;
    
    private Rigidbody2D _rigidbody2D;
    private Collider2D _collider2D;
    private float _spawnedAtTime = 0f;
    private Vector2 _currentSteering = Vector2.zero;
    
    [Serializable]
    public class BoidConfig
    {
        public float initialSpeed = 5f;
        
        public float maxSpeed = 2f;
        public float maxForce = 3f;
        public float minSpeed = 1f;
        public int maxNeighbors = 50;
        
        public float separationRadius = 25f;
        public float separationWeight = 1.5f;
        public float alignmentRadius = 50f;
        public float alignmentWeight = 1f;
        public float cohesionRadius = 50f;
        public float cohesionWeight = 1f;
        
        public float lifetimeSeconds = 10f;
        
        [Range(0, 1)]
        public float randomMagnitude = 0.1f;

    }
    
    public void Initialize()
    {
        var cycleDir = new Vector2(Mathf.Sin(Time.time), Mathf.Cos(Time.time));
        var randDir = UnityEngine.Random.insideUnitCircle.normalized;
        var targetHeading = Vector2.Lerp(cycleDir, randDir, config.randomMagnitude);
        _rigidbody2D.linearVelocity = targetHeading * config.initialSpeed; 
        _spawnedAtTime = Time.fixedTime;
    }
    
    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _collider2D = GetComponent<Collider2D>();
    }
    
    private Collider2D[] _neighborQueryResults;

    private void FixedUpdate()
    {
        if(_spawnedAtTime + config.lifetimeSeconds < Time.fixedTime)
        {
            Destroy(gameObject);
            return;
        }
        
        if(_neighborQueryResults?.Length != config.maxNeighbors)
        {
            _neighborQueryResults = new Collider2D[config.maxNeighbors];
        }
        
        Vector2 separation = Vector2.zero;
        Vector2 alignment = Vector2.zero;
        Vector2 cohesion = Vector2.zero;
        int separationNeighborCount = 0;
        int alignmentNeighborCount = 0;
        int cohesionNeighborCount = 0;

        var myLayer = _rigidbody2D.gameObject.layer;
        var contactFilter = new ContactFilter2D()
        {
            useLayerMask = true,
            layerMask = 1 << myLayer
        };

        var maxRadius = Mathf.Max(config.separationRadius, config.alignmentRadius, config.cohesionRadius);
        Physics2D.OverlapCircle(transform.position, maxRadius, contactFilter, _neighborQueryResults);
        foreach (Collider2D neighbor in _neighborQueryResults)
        {
            // assume that the results are contiguous
            if (neighbor == null) break;
            if (neighbor == _collider2D) continue;
            BoidBehavior otherBoid = neighbor.GetComponent<BoidBehavior>();
            if (otherBoid == null) continue;

            var neighborPosition = otherBoid._rigidbody2D.position;
            var myPosition = _rigidbody2D.position;
            Vector2 toNeighbor = neighborPosition - myPosition;
            float distance = toNeighbor.magnitude;

            // Separation
            if (distance < config.separationRadius)
            {
                separationNeighborCount++;
                var fromNeighbor = -toNeighbor;
                var separationAdjustment = 1f - (distance / config.separationRadius);
                //separationAdjustment = separationAdjustment * separationAdjustment;
                //separationAdjustment = Mathf.Clamp01(separationAdjustment);
                separation += fromNeighbor.normalized * (0.5f + separationAdjustment * 0.5f);
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

        
        var forward = _rigidbody2D.linearVelocity.normalized;
        var currentPosition = _rigidbody2D.position;
        if(separationNeighborCount > 0)
        {
            //separation =  (currentPosition * separationNeighborCount) - separation;
            //separation = separation.normalized;
            //if(separation.magnitude > config.maxForce) separation = separation.normalized * config.maxForce;
            DrawSteeringForce(separation, Color.blue);
        }
        else separation = Vector2.zero;
        
        if(alignmentNeighborCount > 0)
        {
            alignment = (alignment / alignmentNeighborCount) - _rigidbody2D.linearVelocity;
            //alignment = alignment.normalized;
            //if(alignment.magnitude > config.maxForce) alignment = alignment.normalized * config.maxForce;
            DrawSteeringForce(alignment, Color.green);
        }
        else alignment = Vector2.zero;

        if (cohesionNeighborCount > 0)
        {
            cohesion /= cohesionNeighborCount;
            cohesion -= _rigidbody2D.position;
            //cohesion = cohesion.normalized;
            //if(cohesion.magnitude > config.maxForce) cohesion = cohesion.normalized * config.maxForce;
            DrawSteeringForce(cohesion, Color.red);
        }
        else cohesion = Vector2.zero;
        
        var steerFrom = _rigidbody2D.position + _rigidbody2D.linearVelocity * _velocityTimeAheadDraw;
        Debug.DrawLine(_rigidbody2D.position, steerFrom, Color.magenta);

        var targetForward = separation * config.separationWeight + alignment * config.alignmentWeight + cohesion * config.cohesionWeight;
        var nextHeading = _rigidbody2D.linearVelocity + Time.fixedDeltaTime * (targetForward - _rigidbody2D.linearVelocity);
        nextHeading = ClampMagnitude(nextHeading, config.minSpeed, config.maxSpeed);
        //nextHeading = nextHeading.normalized;

        _rigidbody2D.linearVelocity = nextHeading; //* config.maxSpeed;
        _rigidbody2D.rotation = Mathf.Atan2(nextHeading.y, nextHeading.x) * Mathf.Rad2Deg;
        
        //_currentSteering = Vector2.Lerp(_currentSteering, steerForce, 0.1f);
        // Vector2 velocity = _rigidbody2D.linearVelocity;
        // velocity += _currentSteering;
        // velocity = Vector2.ClampMagnitude(velocity, config.maxSpeed);
        // if (velocity.magnitude < config.minSpeed)
        // {
        //     velocity = velocity.normalized * config.minSpeed;
        // }

        //_rigidbody2D.linearVelocity = velocity;
    }

    private Vector2 ClampMagnitude(Vector2 heading, float min, float max)
    {
        var mag = heading.magnitude;
        if (mag < min) return heading.normalized * min;
        if (mag > max) return heading.normalized * max;
        return heading;
    }

    private float _velocityTimeAheadDraw = 0.5f;
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
