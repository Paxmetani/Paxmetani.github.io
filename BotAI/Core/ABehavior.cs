using System.Collections;
using UnityEngine;

/// <summary>
/// Enhanced behavior system for bot AI with natural and varied behavior patterns
/// </summary>
public class ABehavior : MonoBehaviour
{
    [Header("Behavior Configuration")]
    [SerializeField] private float behaviorUpdateInterval = 0.5f;
    [SerializeField] private float alertnessLevel = 0.8f;
    [SerializeField] private float curiosityFactor = 0.6f;
    [SerializeField] private bool enableEmotionalStates = true;
    
    [Header("Animation and Visual Feedback")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool useHeadTracking = true;
    [SerializeField] private Transform headTransform;
    [SerializeField] private float headTrackingSpeed = 2f;
    
    [Header("Audio Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] alertSounds;
    [SerializeField] private AudioClip[] combatSounds;
    [SerializeField] private AudioClip[] movementSounds;
    
    // Core components
    private ADecisionMaking decisionMaking;
    private AMovement movement;
    private ACombat combat;
    
    // Behavior state
    private BehaviorState currentState;
    private BehaviorState previousState;
    private float lastBehaviorUpdate;
    private float stateTimer;
    
    // Emotional and personality system
    private EmotionalState currentEmotion;
    private float personalityTrait_Aggressiveness;
    private float personalityTrait_Caution;
    private float personalityTrait_Curiosity;
    private System.Random behaviorRandomizer;
    
    // Environmental awareness
    private Transform lookAtTarget;
    private float lastLookAroundTime;
    private Vector3 investigationPoint;
    private bool isInvestigating;
    
    public enum BehaviorState
    {
        Idle,
        Patrolling,
        Investigating,
        Alert,
        Hunting,
        Engaging,
        Flanking,
        Repositioning,
        Retreating,
        Suppressing
    }
    
    public enum EmotionalState
    {
        Neutral,
        Alert,
        Aggressive,
        Cautious,
        Confident,
        Desperate,
        Focused
    }
    
    // Events for behavior changes
    public System.Action<BehaviorState> OnBehaviorStateChanged;
    public System.Action<EmotionalState> OnEmotionalStateChanged;
    
    private void Awake()
    {
        decisionMaking = GetComponent<ADecisionMaking>();
        movement = GetComponent<AMovement>();
        combat = GetComponent<ACombat>();
        
        // Initialize personality traits with randomization
        behaviorRandomizer = new System.Random(GetInstanceID());
        InitializePersonality();
        
        currentState = BehaviorState.Idle;
        currentEmotion = EmotionalState.Neutral;
    }
    
    private void Start()
    {
        // Subscribe to combat events for behavior changes
        if (combat != null)
        {
            combat.OnCombatStart += OnCombatStarted;
            combat.OnCombatEnd += OnCombatEnded;
            combat.OnHealthChanged += OnHealthChanged;
        }
        
        StartCoroutine(BehaviorUpdateLoop());
        StartCoroutine(LookAroundBehavior());
    }
    
    private void Update()
    {
        UpdateAnimations();
        HandleHeadTracking();
        UpdateStateTimer();
    }
    
    /// <summary>
    /// Initialize bot personality traits
    /// </summary>
    private void InitializePersonality()
    {
        personalityTrait_Aggressiveness = (float)behaviorRandomizer.NextDouble();
        personalityTrait_Caution = (float)behaviorRandomizer.NextDouble();
        personalityTrait_Curiosity = (float)behaviorRandomizer.NextDouble();
        
        // Adjust traits based on decision making component if available
        if (decisionMaking != null)
        {
            float personalityFactor = decisionMaking.GetPersonalityFactor();
            personalityTrait_Aggressiveness = Mathf.Lerp(personalityTrait_Aggressiveness, personalityFactor, 0.5f);
        }
    }
    
    /// <summary>
    /// Main behavior update loop
    /// </summary>
    private IEnumerator BehaviorUpdateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(behaviorUpdateInterval);
            UpdateBehaviorState();
        }
    }
    
    /// <summary>
    /// Update current behavior state based on situation
    /// </summary>
    private void UpdateBehaviorState()
    {
        BehaviorState newState = DetermineBehaviorState();
        
        if (newState != currentState)
        {
            ChangeBehaviorState(newState);
        }
        
        // Update emotional state based on behavior and situation
        if (enableEmotionalStates)
        {
            UpdateEmotionalState();
        }
    }
    
    /// <summary>
    /// Determine appropriate behavior state based on current situation
    /// </summary>
    private BehaviorState DetermineBehaviorState()
    {
        // Priority 1: Combat situations
        if (combat != null && combat.IsInCombat())
        {
            var combatState = combat.GetCombatState();
            switch (combatState)
            {
                case ACombat.CombatState.Engaging:
                    return BehaviorState.Engaging;
                case ACombat.CombatState.Suppressing:
                    return BehaviorState.Suppressing;
                case ACombat.CombatState.Advancing:
                    return BehaviorState.Engaging;
                case ACombat.CombatState.Retreating:
                    return BehaviorState.Retreating;
                case ACombat.CombatState.Flanking:
                    return BehaviorState.Flanking;
            }
        }
        
        // Priority 2: Decision making states
        if (decisionMaking != null)
        {
            var decision = decisionMaking.GetCurrentDecision();
            switch (decision)
            {
                case ADecisionMaking.BotDecisionState.Hunt:
                    return BehaviorState.Hunting;
                case ADecisionMaking.BotDecisionState.Engage:
                    return BehaviorState.Engaging;
                case ADecisionMaking.BotDecisionState.Flank:
                    return BehaviorState.Flanking;
                case ADecisionMaking.BotDecisionState.Retreat:
                    return BehaviorState.Retreating;
                case ADecisionMaking.BotDecisionState.Reposition:
                    return BehaviorState.Repositioning;
                case ADecisionMaking.BotDecisionState.Patrol:
                    return movement != null && movement.HasDestination() ? BehaviorState.Patrolling : BehaviorState.Idle;
            }
        }
        
        // Priority 3: Investigation behavior
        if (isInvestigating)
        {
            return BehaviorState.Investigating;
        }
        
        // Default states
        if (movement != null && movement.HasDestination())
        {
            return BehaviorState.Patrolling;
        }
        
        return BehaviorState.Idle;
    }
    
    /// <summary>
    /// Change to new behavior state
    /// </summary>
    private void ChangeBehaviorState(BehaviorState newState)
    {
        previousState = currentState;
        currentState = newState;
        stateTimer = 0f;
        
        OnBehaviorStateChanged?.Invoke(currentState);
        
        // Handle state-specific initialization
        HandleStateEnter(newState);
        
        // Play appropriate audio feedback
        PlayStateAudio(newState);
        
        Debug.Log($"Bot {name} behavior: {previousState} -> {currentState}");
    }
    
    /// <summary>
    /// Handle entering a new state
    /// </summary>
    private void HandleStateEnter(BehaviorState state)
    {
        switch (state)
        {
            case BehaviorState.Alert:
                StartCoroutine(AlertBehavior());
                break;
            case BehaviorState.Investigating:
                StartInvestigation();
                break;
            case BehaviorState.Idle:
                StartCoroutine(IdleBehavior());
                break;
        }
    }
    
    /// <summary>
    /// Update emotional state based on current situation
    /// </summary>
    private void UpdateEmotionalState()
    {
        EmotionalState newEmotion = DetermineEmotionalState();
        
        if (newEmotion != currentEmotion)
        {
            currentEmotion = newEmotion;
            OnEmotionalStateChanged?.Invoke(currentEmotion);
            ApplyEmotionalEffects();
        }
    }
    
    /// <summary>
    /// Determine emotional state based on situation and personality
    /// </summary>
    private EmotionalState DetermineEmotionalState()
    {
        // Combat situations
        if (combat != null && combat.IsInCombat())
        {
            float healthPercentage = combat.GetHealthPercentage();
            
            if (healthPercentage < 0.3f)
            {
                return personalityTrait_Caution > 0.6f ? EmotionalState.Desperate : EmotionalState.Aggressive;
            }
            else if (healthPercentage > 0.8f && personalityTrait_Aggressiveness > 0.7f)
            {
                return EmotionalState.Confident;
            }
            else
            {
                return EmotionalState.Focused;
            }
        }
        
        // Alert situations
        if (currentState == BehaviorState.Alert || currentState == BehaviorState.Investigating)
        {
            return EmotionalState.Alert;
        }
        
        // Hunting behavior
        if (currentState == BehaviorState.Hunting)
        {
            return personalityTrait_Aggressiveness > 0.5f ? EmotionalState.Aggressive : EmotionalState.Cautious;
        }
        
        return EmotionalState.Neutral;
    }
    
    /// <summary>
    /// Apply effects based on current emotional state
    /// </summary>
    private void ApplyEmotionalEffects()
    {
        switch (currentEmotion)
        {
            case EmotionalState.Aggressive:
                // Increase movement speed and attack rate
                if (movement != null) movement.SetSpeedOverride(1.2f);
                break;
                
            case EmotionalState.Cautious:
                // Decrease movement speed, increase alertness
                if (movement != null) movement.SetSpeedOverride(0.8f);
                break;
                
            case EmotionalState.Confident:
                // Normal speed but more aggressive behavior
                if (movement != null) movement.SetSpeedOverride(1.1f);
                break;
                
            case EmotionalState.Desperate:
                // Faster movement, more erratic behavior
                if (movement != null) movement.SetSpeedOverride(1.3f);
                break;
                
            case EmotionalState.Focused:
                // Optimal performance
                if (movement != null) movement.SetSpeedOverride(1.0f);
                break;
        }
    }
    
    /// <summary>
    /// Alert behavior coroutine
    /// </summary>
    private IEnumerator AlertBehavior()
    {
        float alertDuration = 3f + (float)behaviorRandomizer.NextDouble() * 2f; // 3-5 seconds
        float alertStartTime = Time.time;
        
        while (Time.time - alertStartTime < alertDuration && currentState == BehaviorState.Alert)
        {
            // Look around alertly
            if (behaviorRandomizer.NextDouble() < 0.3f)
            {
                Vector3 lookDirection = GetRandomLookDirection();
                SetLookAtTarget(transform.position + lookDirection * 10f);
            }
            
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    /// <summary>
    /// Idle behavior with randomized actions
    /// </summary>
    private IEnumerator IdleBehavior()
    {
        while (currentState == BehaviorState.Idle)
        {
            float waitTime = (float)behaviorRandomizer.NextDouble() * 3f + 1f; // 1-4 seconds
            
            // Random idle actions based on personality
            if (personalityTrait_Curiosity > 0.5f && behaviorRandomizer.NextDouble() < 0.2f)
            {
                TriggerInvestigation();
            }
            
            yield return new WaitForSeconds(waitTime);
        }
    }
    
    /// <summary>
    /// Look around behavior for environmental awareness
    /// </summary>
    private IEnumerator LookAroundBehavior()
    {
        while (true)
        {
            float waitTime = (float)behaviorRandomizer.NextDouble() * 2f + 1f; // 1-3 seconds
            yield return new WaitForSeconds(waitTime);
            
            // Only look around when not in combat or focused states
            if (currentState != BehaviorState.Engaging && currentState != BehaviorState.Suppressing)
            {
                Vector3 lookDirection = GetRandomLookDirection();
                SetLookAtTarget(transform.position + lookDirection * 15f);
            }
        }
    }
    
    /// <summary>
    /// Trigger investigation of nearby point of interest
    /// </summary>
    private void TriggerInvestigation()
    {
        if (isInvestigating) return;
        
        Vector3 investigationDirection = GetRandomLookDirection();
        investigationPoint = transform.position + investigationDirection * (5f + (float)behaviorRandomizer.NextDouble() * 10f);
        isInvestigating = true;
        
        // This will trigger behavior state change in next update
    }
    
    /// <summary>
    /// Start investigation behavior
    /// </summary>
    private void StartInvestigation()
    {
        if (movement != null)
        {
            movement.SetDestination(investigationPoint);
            movement.SetSpeedOverride(0.9f); // Slower, more cautious movement
        }
        
        StartCoroutine(InvestigationBehavior());
    }
    
    /// <summary>
    /// Investigation behavior coroutine
    /// </summary>
    private IEnumerator InvestigationBehavior()
    {
        float investigationTime = 2f + (float)behaviorRandomizer.NextDouble() * 3f; // 2-5 seconds
        float startTime = Time.time;
        
        while (Time.time - startTime < investigationTime && isInvestigating)
        {
            // Look around the investigation area
            Vector3 lookDirection = GetRandomLookDirection();
            SetLookAtTarget(investigationPoint + lookDirection * 3f);
            
            yield return new WaitForSeconds(0.8f);
        }
        
        isInvestigating = false;
    }
    
    /// <summary>
    /// Update animations based on current state
    /// </summary>
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Set animator parameters based on state
        animator.SetBool("IsMoving", movement != null && movement.GetCurrentSpeed() > 0.1f);
        animator.SetBool("IsInCombat", combat != null && combat.IsInCombat());
        animator.SetBool("IsAlert", currentState == BehaviorState.Alert || currentState == BehaviorState.Investigating);
        
        // Set speed parameter for blend trees
        float speedNormalized = movement != null ? movement.GetCurrentSpeed() / 6f : 0f;
        animator.SetFloat("Speed", speedNormalized);
        
        // Emotional state parameters
        animator.SetInteger("EmotionState", (int)currentEmotion);
    }
    
    /// <summary>
    /// Handle head tracking for more natural behavior
    /// </summary>
    private void HandleHeadTracking()
    {
        if (!useHeadTracking || headTransform == null || lookAtTarget == null) return;
        
        Vector3 targetDirection = (lookAtTarget.position - headTransform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        
        headTransform.rotation = Quaternion.Slerp(headTransform.rotation, targetRotation, Time.deltaTime * headTrackingSpeed);
        
        // Clear look target after some time
        if (Time.time - lastLookAroundTime > 3f)
        {
            lookAtTarget = null;
        }
    }
    
    /// <summary>
    /// Get random look direction
    /// </summary>
    private Vector3 GetRandomLookDirection()
    {
        float angle = (float)behaviorRandomizer.NextDouble() * 360f;
        return new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad));
    }
    
    /// <summary>
    /// Set look at target for head tracking
    /// </summary>
    private void SetLookAtTarget(Vector3 worldPosition)
    {
        if (lookAtTarget == null)
        {
            lookAtTarget = new GameObject("LookAtTarget").transform;
        }
        
        lookAtTarget.position = worldPosition;
        lastLookAroundTime = Time.time;
    }
    
    /// <summary>
    /// Play audio based on behavior state
    /// </summary>
    private void PlayStateAudio(BehaviorState state)
    {
        if (audioSource == null) return;
        
        AudioClip clipToPlay = null;
        
        switch (state)
        {
            case BehaviorState.Alert:
            case BehaviorState.Investigating:
                if (alertSounds.Length > 0)
                    clipToPlay = alertSounds[behaviorRandomizer.Next(alertSounds.Length)];
                break;
                
            case BehaviorState.Engaging:
            case BehaviorState.Suppressing:
                if (combatSounds.Length > 0)
                    clipToPlay = combatSounds[behaviorRandomizer.Next(combatSounds.Length)];
                break;
                
            case BehaviorState.Patrolling:
                if (movementSounds.Length > 0 && behaviorRandomizer.NextDouble() < 0.1f) // 10% chance
                    clipToPlay = movementSounds[behaviorRandomizer.Next(movementSounds.Length)];
                break;
        }
        
        if (clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay, 0.7f);
        }
    }
    
    /// <summary>
    /// Update state timer for behavior tracking
    /// </summary>
    private void UpdateStateTimer()
    {
        stateTimer += Time.deltaTime;
    }
    
    // Event handlers
    private void OnCombatStarted()
    {
        if (currentEmotion == EmotionalState.Neutral)
        {
            currentEmotion = EmotionalState.Alert;
        }
    }
    
    private void OnCombatEnded()
    {
        StartCoroutine(ReturnToNeutralEmotion(3f));
    }
    
    private void OnHealthChanged(float healthPercentage)
    {
        if (healthPercentage < 0.3f && personalityTrait_Caution > 0.5f)
        {
            currentEmotion = EmotionalState.Desperate;
        }
    }
    
    /// <summary>
    /// Gradually return to neutral emotional state
    /// </summary>
    private IEnumerator ReturnToNeutralEmotion(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (!combat.IsInCombat())
        {
            currentEmotion = EmotionalState.Neutral;
            OnEmotionalStateChanged?.Invoke(currentEmotion);
        }
    }
    
    // Public accessors and methods
    public BehaviorState GetCurrentState() => currentState;
    public BehaviorState GetPreviousState() => previousState;
    public EmotionalState GetCurrentEmotion() => currentEmotion;
    public float GetStateTimer() => stateTimer;
    
    /// <summary>
    /// Force set behavior state (used by other components)
    /// </summary>
    public void SetBehaviorState(BehaviorState newState)
    {
        if (newState != currentState)
        {
            ChangeBehaviorState(newState);
        }
    }
    
    /// <summary>
    /// Get personality trait values
    /// </summary>
    public Vector3 GetPersonalityTraits()
    {
        return new Vector3(personalityTrait_Aggressiveness, personalityTrait_Caution, personalityTrait_Curiosity);
    }
    
    private void OnDestroy()
    {
        if (lookAtTarget != null && lookAtTarget.gameObject != null)
        {
            Destroy(lookAtTarget.gameObject);
        }
    }
}