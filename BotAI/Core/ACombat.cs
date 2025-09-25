using System.Collections;
using UnityEngine;

/// <summary>
/// Enhanced combat system for bot AI with aggressive and dynamic behavior
/// </summary>
public class ACombat : MonoBehaviour
{
    [Header("Combat Configuration")]
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float minAttackRange = 3f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float weaponDamage = 25f;
    [SerializeField] private int burstFireCount = 3;
    
    [Header("Aggressive Combat Settings")]
    [SerializeField] private float aggressionLevel = 0.7f;
    [SerializeField] private float reactionTime = 0.3f;
    [SerializeField] private float aimAccuracy = 0.75f;
    [SerializeField] private bool usePredictiveAiming = true;
    
    [Header("Dynamic Combat Behavior")]
    [SerializeField] private float combatMovementSpeed = 1.2f;
    [SerializeField] private float retreatThreshold = 0.3f; // 30% health
    [SerializeField] private float aggressiveAdvanceDistance = 5f;
    [SerializeField] private bool enableSuppressionFire = true;
    
    // Core components
    private ADecisionMaking decisionMaking;
    private AMovement movement;
    private ABehavior behavior;
    
    // Combat state
    private Transform currentTarget;
    private Vector3 targetPosition;
    private float lastAttackTime;
    private bool isInCombat = false;
    private float combatStartTime;
    
    // Dynamic combat variables
    private System.Random combatRandomizer;
    private float personalAggressionFactor;
    private CombatState currentCombatState;
    private float lastTargetUpdateTime;
    private Vector3 predictedTargetPosition;
    
    // Health and damage system
    [Header("Health System")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    
    public enum CombatState
    {
        Idle,
        Acquiring,
        Engaging,
        Suppressing,
        Advancing,
        Retreating,
        Flanking
    }
    
    // Events
    public System.Action<float> OnHealthChanged;
    public System.Action OnCombatStart;
    public System.Action OnCombatEnd;
    
    private void Awake()
    {
        decisionMaking = GetComponent<ADecisionMaking>();
        movement = GetComponent<AMovement>();
        behavior = GetComponent<ABehavior>();
        
        // Initialize combat randomizer and personal factors
        combatRandomizer = new System.Random(GetInstanceID());
        personalAggressionFactor = aggressionLevel + (float)(combatRandomizer.NextDouble() - 0.5) * 0.4f;
        personalAggressionFactor = Mathf.Clamp01(personalAggressionFactor);
        
        // Initialize health
        currentHealth = maxHealth;
    }
    
    private void Update()
    {
        UpdateCombatState();
        HandleCombatLogic();
    }
    
    /// <summary>
    /// Update combat state based on current situation
    /// </summary>
    private void UpdateCombatState()
    {
        bool hasTarget = currentTarget != null;
        bool wasInCombat = isInCombat;
        
        isInCombat = hasTarget && Vector3.Distance(transform.position, currentTarget.position) <= attackRange * 1.5f;
        
        if (isInCombat && !wasInCombat)
        {
            combatStartTime = Time.time;
            OnCombatStart?.Invoke();
        }
        else if (!isInCombat && wasInCombat)
        {
            OnCombatEnd?.Invoke();
        }
    }
    
    /// <summary>
    /// Main combat logic handler
    /// </summary>
    private void HandleCombatLogic()
    {
        if (!isInCombat)
        {
            currentCombatState = CombatState.Idle;
            return;
        }
        
        UpdateTargetPrediction();
        DetermineCombatState();
        ExecuteCombatBehavior();
    }
    
    /// <summary>
    /// Set combat target
    /// </summary>
    public void SetTarget(Vector3 position)
    {
        targetPosition = position;
        
        // Try to find actual target GameObject
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(position, player.transform.position);
            if (distance < 5f) // Close enough to be the same target
            {
                currentTarget = player.transform;
            }
        }
    }
    
    /// <summary>
    /// Set combat target directly
    /// </summary>
    public void SetTarget(Transform target)
    {
        currentTarget = target;
        if (target != null)
        {
            targetPosition = target.position;
        }
    }
    
    /// <summary>
    /// Update predicted target position for better aiming
    /// </summary>
    private void UpdateTargetPrediction()
    {
        if (currentTarget == null) return;
        
        if (usePredictiveAiming)
        {
            Rigidbody targetRb = currentTarget.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                // Predict where target will be based on velocity
                float timeToTarget = Vector3.Distance(transform.position, currentTarget.position) / 20f; // Assume projectile speed
                predictedTargetPosition = currentTarget.position + targetRb.velocity * timeToTarget;
            }
            else
            {
                predictedTargetPosition = currentTarget.position;
            }
        }
        else
        {
            predictedTargetPosition = currentTarget.position;
        }
        
        lastTargetUpdateTime = Time.time;
    }
    
    /// <summary>
    /// Determine current combat state based on situation
    /// </summary>
    private void DetermineCombatState()
    {
        if (currentTarget == null)
        {
            currentCombatState = CombatState.Idle;
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        float healthPercentage = currentHealth / maxHealth;
        
        // Check if should retreat due to low health
        if (healthPercentage < retreatThreshold && personalAggressionFactor < 0.8f)
        {
            currentCombatState = CombatState.Retreating;
        }
        // Very close range - engage directly
        else if (distanceToTarget <= minAttackRange)
        {
            currentCombatState = CombatState.Engaging;
        }
        // Medium range with high aggression - advance
        else if (distanceToTarget <= attackRange && personalAggressionFactor > 0.6f)
        {
            // Randomly choose between advancing and flanking for dynamic behavior
            if (combatRandomizer.NextDouble() < 0.3f)
            {
                currentCombatState = CombatState.Flanking;
            }
            else
            {
                currentCombatState = CombatState.Advancing;
            }
        }
        // Long range - suppress or acquire
        else if (distanceToTarget <= attackRange * 1.2f)
        {
            if (enableSuppressionFire && combatRandomizer.NextDouble() < 0.4f)
            {
                currentCombatState = CombatState.Suppressing;
            }
            else
            {
                currentCombatState = CombatState.Engaging;
            }
        }
        else
        {
            currentCombatState = CombatState.Acquiring;
        }
    }
    
    /// <summary>
    /// Execute behavior based on current combat state
    /// </summary>
    private void ExecuteCombatBehavior()
    {
        switch (currentCombatState)
        {
            case CombatState.Acquiring:
                HandleTargetAcquisition();
                break;
            case CombatState.Engaging:
                HandleDirectEngagement();
                break;
            case CombatState.Suppressing:
                HandleSuppressionFire();
                break;
            case CombatState.Advancing:
                HandleAggressiveAdvance();
                break;
            case CombatState.Retreating:
                HandleTacticalRetreat();
                break;
            case CombatState.Flanking:
                HandleFlankingManeuver();
                break;
        }
    }
    
    /// <summary>
    /// Handle target acquisition phase
    /// </summary>
    private void HandleTargetAcquisition()
    {
        if (currentTarget != null)
        {
            // Move towards target while maintaining some distance
            Vector3 approachPosition = currentTarget.position + (transform.position - currentTarget.position).normalized * (attackRange * 0.8f);
            movement.SetDestination(approachPosition);
        }
    }
    
    /// <summary>
    /// Handle direct engagement with target
    /// </summary>
    private void HandleDirectEngagement()
    {
        if (currentTarget == null) return;
        
        // Face target
        Vector3 directionToTarget = (predictedTargetPosition - transform.position).normalized;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directionToTarget), Time.deltaTime * 5f);
        
        // Attack if in range and cooldown ready
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        if (distanceToTarget <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            PerformAttack();
        }
        
        // Dynamic movement during engagement - strafe
        if (movement != null)
        {
            movement.SetSpeedOverride(combatMovementSpeed);
        }
    }
    
    /// <summary>
    /// Handle suppression fire to pin down target
    /// </summary>
    private void HandleSuppressionFire()
    {
        if (currentTarget == null) return;
        
        // Face general target area
        Vector3 suppressionDirection = (currentTarget.position - transform.position).normalized;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(suppressionDirection), Time.deltaTime * 3f);
        
        // Fire in bursts with lower accuracy for suppression
        if (Time.time - lastAttackTime >= attackCooldown * 0.7f) // Faster fire rate
        {
            StartCoroutine(SuppressionBurst());
        }
    }
    
    /// <summary>
    /// Handle aggressive advance towards target
    /// </summary>
    private void HandleAggressiveAdvance()
    {
        if (currentTarget == null) return;
        
        // Move aggressively towards target
        Vector3 advancePosition = currentTarget.position + (transform.position - currentTarget.position).normalized * aggressiveAdvanceDistance;
        movement.SetDestination(advancePosition);
        movement.SetSpeedOverride(1.4f); // Faster advance
        
        // Fire while advancing if in range
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        if (distanceToTarget <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            PerformAttack();
        }
    }
    
    /// <summary>
    /// Handle tactical retreat when health is low
    /// </summary>
    private void HandleTacticalRetreat()
    {
        if (currentTarget == null) return;
        
        // Move away from target while occasionally firing
        Vector3 retreatDirection = (transform.position - currentTarget.position).normalized;
        Vector3 retreatPosition = transform.position + retreatDirection * 10f;
        movement.SetDestination(retreatPosition);
        movement.SetSpeedOverride(1.3f); // Fast retreat
        
        // Occasional return fire during retreat
        if (combatRandomizer.NextDouble() < 0.3f && Time.time - lastAttackTime >= attackCooldown * 1.5f)
        {
            PerformAttack();
        }
    }
    
    /// <summary>
    /// Handle flanking maneuver
    /// </summary>
    private void HandleFlankingManeuver()
    {
        if (currentTarget == null) return;
        
        // Calculate flank position
        Vector3 toTarget = (currentTarget.position - transform.position).normalized;
        Vector3 rightVector = Vector3.Cross(toTarget, Vector3.up).normalized;
        
        // Choose random flank side
        Vector3 flankDirection = combatRandomizer.NextDouble() > 0.5f ? rightVector : -rightVector;
        Vector3 flankPosition = currentTarget.position + flankDirection * attackRange * 0.7f;
        
        movement.SetDestination(flankPosition);
        movement.SetSpeedOverride(1.1f);
        
        // Fire when flanking if good angle
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        if (distanceToTarget <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            PerformAttack();
        }
    }
    
    /// <summary>
    /// Perform primary attack
    /// </summary>
    private void PerformAttack()
    {
        if (currentTarget == null) return;
        
        lastAttackTime = Time.time;
        
        // Calculate accuracy with some randomization
        float accuracy = aimAccuracy + (float)(combatRandomizer.NextDouble() - 0.5) * 0.2f;
        accuracy = Mathf.Clamp01(accuracy);
        
        // Determine if shot hits
        bool isHit = combatRandomizer.NextDouble() < accuracy;
        
        if (isHit)
        {
            // Deal damage to target
            IDamageable damageable = currentTarget.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(weaponDamage);
            }
            
            // Visual/audio feedback would go here
            Debug.Log($"Bot {name} hit target for {weaponDamage} damage");
        }
        
        // Start burst fire coroutine for more realistic combat
        if (burstFireCount > 1)
        {
            StartCoroutine(BurstFire(burstFireCount - 1));
        }
    }
    
    /// <summary>
    /// Burst fire coroutine
    /// </summary>
    private IEnumerator BurstFire(int remainingShots)
    {
        for (int i = 0; i < remainingShots; i++)
        {
            yield return new WaitForSeconds(0.1f);
            
            if (currentTarget != null)
            {
                float accuracy = aimAccuracy * 0.8f; // Slightly less accurate for burst
                bool isHit = combatRandomizer.NextDouble() < accuracy;
                
                if (isHit)
                {
                    IDamageable damageable = currentTarget.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(weaponDamage * 0.8f); // Reduced damage for burst shots
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Suppression fire burst
    /// </summary>
    private IEnumerator SuppressionBurst()
    {
        lastAttackTime = Time.time;
        int suppressionShots = combatRandomizer.Next(3, 6);
        
        for (int i = 0; i < suppressionShots; i++)
        {
            yield return new WaitForSeconds(0.15f);
            
            // Lower accuracy for suppression
            float suppressionAccuracy = aimAccuracy * 0.5f;
            bool isHit = combatRandomizer.NextDouble() < suppressionAccuracy;
            
            if (isHit && currentTarget != null)
            {
                IDamageable damageable = currentTarget.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(weaponDamage * 0.6f); // Lower damage for suppression
                }
            }
        }
    }
    
    /// <summary>
    /// Take damage from external source
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
        
        // Increase aggression when taking damage
        personalAggressionFactor = Mathf.Min(1f, personalAggressionFactor + 0.1f);
    }
    
    /// <summary>
    /// Handle bot death
    /// </summary>
    private void Die()
    {
        // Death logic would go here
        Debug.Log($"Bot {name} has been eliminated");
        
        // Disable bot or trigger death sequence
        gameObject.SetActive(false);
    }
    
    // Public accessors
    public bool IsInCombat() => isInCombat;
    public CombatState GetCombatState() => currentCombatState;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public Transform GetCurrentTarget() => currentTarget;
    public float GetAggressionLevel() => personalAggressionFactor;
    
    /// <summary>
    /// Clear current target
    /// </summary>
    public void ClearTarget()
    {
        currentTarget = null;
        targetPosition = Vector3.zero;
        isInCombat = false;
        currentCombatState = CombatState.Idle;
    }
}

/// <summary>
/// Interface for objects that can take damage
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage);
}