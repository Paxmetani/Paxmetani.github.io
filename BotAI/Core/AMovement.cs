using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enhanced movement system for bot AI that prevents clustering and creates natural movement patterns
/// </summary>
public class AMovement : MonoBehaviour
{
    [Header("Movement Configuration")]
    [SerializeField] private float baseSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float accelerationMultiplier = 2f;
    
    [Header("Anti-Clustering Movement")]
    [SerializeField] private float personalSpaceRadius = 3f;
    [SerializeField] private float avoidanceForce = 2f;
    [SerializeField] private LayerMask obstacleLayer = 1;
    [SerializeField] private LayerMask botLayer = 1 << 8;
    
    [Header("Natural Movement")]
    [SerializeField] private float stoppingDistance = 1f;
    [SerializeField] private float pathRecalculationTime = 2f;
    [SerializeField] private bool useVariableSpeed = true;
    
    // Core components
    private NavMeshAgent navAgent;
    private Rigidbody rb;
    private ADecisionMaking decisionMaking;
    
    // Movement state
    private Vector3 currentDestination;
    private bool hasDestination = false;
    private float lastPathCalculation = 0f;
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    
    // Dynamic movement properties
    private float personalizedSpeed;
    private float currentSpeedModifier = 1f;
    private System.Random movementRandomizer;
    
    // Natural movement patterns
    private bool isStrafing = false;
    private float strafeTimer = 0f;
    private float strafeDirection = 1f;
    
    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        decisionMaking = GetComponent<ADecisionMaking>();
        
        // Initialize with unique movement characteristics
        movementRandomizer = new System.Random(GetInstanceID());
        personalizedSpeed = baseSpeed + (float)(movementRandomizer.NextDouble() - 0.5) * 1f;
        
        SetupNavMeshAgent();
    }
    
    private void Start()
    {
        lastPosition = transform.position;
    }
    
    private void Update()
    {
        HandleMovementUpdate();
        CheckForStuckState();
        HandleNaturalMovementPatterns();
    }
    
    private void FixedUpdate()
    {
        ApplyClusteringAvoidance();
    }
    
    /// <summary>
    /// Set up the NavMeshAgent with personalized settings
    /// </summary>
    private void SetupNavMeshAgent()
    {
        if (navAgent != null)
        {
            navAgent.speed = personalizedSpeed;
            navAgent.acceleration = personalizedSpeed * accelerationMultiplier;
            navAgent.angularSpeed = rotationSpeed;
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.autoBraking = true;
            navAgent.autoRepath = true;
        }
    }
    
    /// <summary>
    /// Set destination with improved pathfinding
    /// </summary>
    public void SetDestination(Vector3 destination)
    {
        if (navAgent != null && navAgent.enabled)
        {
            currentDestination = destination;
            hasDestination = true;
            
            // Add slight randomization to prevent identical paths
            Vector3 randomizedDestination = destination + GetRandomOffset();
            
            if (NavMesh.SamplePosition(randomizedDestination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                navAgent.SetDestination(hit.position);
                lastPathCalculation = Time.time;
                
                // Adjust speed based on situation
                AdjustSpeedForSituation();
            }
        }
    }
    
    /// <summary>
    /// Handle regular movement updates
    /// </summary>
    private void HandleMovementUpdate()
    {
        if (!hasDestination || navAgent == null || !navAgent.enabled)
            return;
            
        // Recalculate path periodically for dynamic avoidance
        if (Time.time - lastPathCalculation > pathRecalculationTime)
        {
            if (hasDestination)
            {
                SetDestination(currentDestination);
            }
        }
        
        // Check if destination reached
        if (navAgent.remainingDistance < stoppingDistance && !navAgent.pathPending)
        {
            hasDestination = false;
            OnDestinationReached();
        }
    }
    
    /// <summary>
    /// Apply forces to avoid clustering with other bots
    /// </summary>
    private void ApplyClusteringAvoidance()
    {
        if (rb == null) return;
        
        Collider[] nearbyBots = Physics.OverlapSphere(transform.position, personalSpaceRadius, botLayer);
        Vector3 avoidanceForceVector = Vector3.zero;
        
        foreach (Collider botCollider in nearbyBots)
        {
            if (botCollider.transform == transform) continue;
            
            Vector3 directionAway = transform.position - botCollider.transform.position;
            float distance = directionAway.magnitude;
            
            if (distance < personalSpaceRadius && distance > 0)
            {
                // Stronger avoidance when closer
                float forceMultiplier = (personalSpaceRadius - distance) / personalSpaceRadius;
                avoidanceForceVector += directionAway.normalized * avoidanceForce * forceMultiplier;
            }
        }
        
        // Apply the avoidance force
        if (avoidanceForceVector.magnitude > 0.1f)
        {
            rb.AddForce(avoidanceForceVector, ForceMode.VelocityChange);
            
            // Also adjust NavMesh destination slightly to maintain path
            if (hasDestination && navAgent.enabled)
            {
                Vector3 adjustedDestination = currentDestination + avoidanceForceVector.normalized * 2f;
                if (NavMesh.SamplePosition(adjustedDestination, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    navAgent.SetDestination(hit.position);
                }
            }
        }
    }
    
    /// <summary>
    /// Check if bot is stuck and handle accordingly
    /// </summary>
    private void CheckForStuckState()
    {
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        
        if (hasDestination && distanceMoved < 0.1f)
        {
            stuckTimer += Time.deltaTime;
            
            if (stuckTimer > 2f) // Stuck for 2 seconds
            {
                HandleStuckState();
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
        
        lastPosition = transform.position;
    }
    
    /// <summary>
    /// Handle when bot gets stuck
    /// </summary>
    private void HandleStuckState()
    {
        // Try to find alternative path
        Vector3 randomDirection = GetRandomDirection();
        Vector3 unstuckDestination = transform.position + randomDirection * 5f;
        
        if (NavMesh.SamplePosition(unstuckDestination, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);
            
            // Temporarily increase speed to get unstuck faster
            StartCoroutine(TemporarySpeedBoost(1.5f, 3f));
        }
    }
    
    /// <summary>
    /// Handle natural movement patterns like strafing
    /// </summary>
    private void HandleNaturalMovementPatterns()
    {
        if (decisionMaking == null) return;
        
        var currentDecision = decisionMaking.GetCurrentDecision();
        
        // Add strafing during combat engagement
        if (currentDecision == ADecisionMaking.BotDecisionState.Engage)
        {
            HandleCombatStrafing();
        }
        else
        {
            isStrafing = false;
        }
    }
    
    /// <summary>
    /// Handle strafing movement during combat
    /// </summary>
    private void HandleCombatStrafing()
    {
        strafeTimer += Time.deltaTime;
        
        if (strafeTimer > movementRandomizer.Next(2, 5)) // Strafe for 2-5 seconds
        {
            strafeDirection *= -1; // Change direction
            strafeTimer = 0f;
        }
        
        Vector3 strafeMovement = transform.right * strafeDirection * personalizedSpeed * 0.5f * Time.deltaTime;
        transform.Translate(strafeMovement, Space.World);
        isStrafing = true;
    }
    
    /// <summary>
    /// Adjust movement speed based on current situation
    /// </summary>
    private void AdjustSpeedForSituation()
    {
        if (!useVariableSpeed || decisionMaking == null) return;
        
        var currentDecision = decisionMaking.GetCurrentDecision();
        
        switch (currentDecision)
        {
            case ADecisionMaking.BotDecisionState.Hunt:
            case ADecisionMaking.BotDecisionState.Engage:
                currentSpeedModifier = 1.3f; // Faster when hunting/engaging
                break;
            case ADecisionMaking.BotDecisionState.Patrol:
                currentSpeedModifier = 0.8f; // Slower when patrolling
                break;
            case ADecisionMaking.BotDecisionState.Retreat:
                currentSpeedModifier = 1.5f; // Fastest when retreating
                break;
            case ADecisionMaking.BotDecisionState.Reposition:
                currentSpeedModifier = 1.2f; // Fast repositioning
                break;
            default:
                currentSpeedModifier = 1f;
                break;
        }
        
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.speed = personalizedSpeed * currentSpeedModifier;
        }
    }
    
    /// <summary>
    /// Get random offset for destination variation
    /// </summary>
    private Vector3 GetRandomOffset()
    {
        float range = 2f;
        return new Vector3(
            (float)(movementRandomizer.NextDouble() - 0.5) * range,
            0,
            (float)(movementRandomizer.NextDouble() - 0.5) * range
        );
    }
    
    /// <summary>
    /// Get random direction for unstuck movement
    /// </summary>
    private Vector3 GetRandomDirection()
    {
        float angle = (float)movementRandomizer.NextDouble() * 360f;
        return new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad));
    }
    
    /// <summary>
    /// Temporarily boost speed
    /// </summary>
    private IEnumerator TemporarySpeedBoost(float multiplier, float duration)
    {
        float originalSpeed = navAgent.speed;
        navAgent.speed = originalSpeed * multiplier;
        
        yield return new WaitForSeconds(duration);
        
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.speed = originalSpeed;
        }
    }
    
    /// <summary>
    /// Called when bot reaches destination
    /// </summary>
    private void OnDestinationReached()
    {
        // Add small pause with randomization to make behavior less robotic
        StartCoroutine(PauseBeforeNextAction());
    }
    
    /// <summary>
    /// Brief pause with randomization
    /// </summary>
    private IEnumerator PauseBeforeNextAction()
    {
        float pauseTime = (float)movementRandomizer.NextDouble() * 1f; // 0-1 second pause
        yield return new WaitForSeconds(pauseTime);
    }
    
    // Public accessors
    public bool HasDestination() => hasDestination;
    public bool ReachedDestination() => !hasDestination;
    public Vector3 GetCurrentDestination() => currentDestination;
    public float GetCurrentSpeed() => navAgent != null ? navAgent.velocity.magnitude : 0f;
    public bool IsStrafing() => isStrafing;
    
    /// <summary>
    /// Force stop movement
    /// </summary>
    public void Stop()
    {
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.ResetPath();
        }
        hasDestination = false;
        isStrafing = false;
    }
    
    /// <summary>
    /// Set movement speed override
    /// </summary>
    public void SetSpeedOverride(float speedMultiplier)
    {
        currentSpeedModifier = speedMultiplier;
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.speed = personalizedSpeed * currentSpeedModifier;
        }
    }
}