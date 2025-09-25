using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Enhanced decision making system for bot AI that prevents clustering and improves natural behavior
/// </summary>
public class ADecisionMaking : MonoBehaviour
{
    [Header("Decision Making Configuration")]
    [SerializeField] private float decisionInterval = 0.1f;
    [SerializeField] private float clusteringAvoidanceRange = 15f;
    [SerializeField] private float playerDetectionRange = 30f;
    [SerializeField] private float randomDecisionWeight = 0.3f;
    
    [Header("Anti-Clustering System")]
    [SerializeField] private int maxBotsInArea = 2;
    [SerializeField] private float spreadOutForce = 10f;
    [SerializeField] private LayerMask botLayer = 1 << 8;
    
    // Core components
    private AMovement movement;
    private ACombat combat;
    private ABehavior behavior;
    
    // Decision state
    private float lastDecisionTime;
    private Vector3 lastKnownPlayerPosition;
    private BotDecisionState currentDecision;
    private List<Transform> nearbyBots = new List<Transform>();
    
    // Randomization for natural behavior
    private System.Random randomizer;
    private float personalityFactor; // Unique per bot for varied behavior
    
    public enum BotDecisionState
    {
        Patrol,
        Hunt,
        Engage,
        Flank,
        Retreat,
        Reposition
    }
    
    private void Awake()
    {
        movement = GetComponent<AMovement>();
        combat = GetComponent<ACombat>();
        behavior = GetComponent<ABehavior>();
        
        // Initialize randomization with unique seed per bot
        randomizer = new System.Random(GetInstanceID());
        personalityFactor = (float)randomizer.NextDouble();
    }
    
    private void Start()
    {
        // Stagger decision making to prevent synchronized behavior
        decisionInterval += randomizer.Next(-20, 21) * 0.001f;
    }
    
    private void Update()
    {
        if (Time.time - lastDecisionTime >= decisionInterval)
        {
            MakeDecision();
            lastDecisionTime = Time.time;
        }
    }
    
    /// <summary>
    /// Main decision making logic - prevents clustering and creates dynamic behavior
    /// </summary>
    private void MakeDecision()
    {
        UpdateNearbyBots();
        Vector3 playerPosition = FindNearestPlayer();
        
        // Priority 1: Avoid clustering with other bots
        if (IsInCluster())
        {
            currentDecision = BotDecisionState.Reposition;
            HandleRepositioning();
            return;
        }
        
        // Priority 2: Engage if player is nearby and not too many bots are already engaging
        if (playerPosition != Vector3.zero)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerPosition);
            
            if (distanceToPlayer <= playerDetectionRange && !TooManyBotsEngagingPlayer(playerPosition))
            {
                // Add randomization to prevent predictable behavior
                float aggressionRoll = (float)randomizer.NextDouble();
                
                if (aggressionRoll < 0.6f + personalityFactor * 0.3f) // 60-90% chance to engage
                {
                    if (distanceToPlayer < 10f)
                    {
                        currentDecision = BotDecisionState.Engage;
                        combat.SetTarget(playerPosition);
                    }
                    else if (ShouldFlank())
                    {
                        currentDecision = BotDecisionState.Flank;
                        HandleFlanking(playerPosition);
                    }
                    else
                    {
                        currentDecision = BotDecisionState.Hunt;
                        movement.SetDestination(playerPosition);
                    }
                    lastKnownPlayerPosition = playerPosition;
                    return;
                }
            }
        }
        
        // Priority 3: Continue hunting last known player position
        if (lastKnownPlayerPosition != Vector3.zero && currentDecision == BotDecisionState.Hunt)
        {
            if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < 2f)
            {
                lastKnownPlayerPosition = Vector3.zero; // Clear when reached
                currentDecision = BotDecisionState.Patrol;
            }
        }
        
        // Default: Patrol with randomized behavior
        if (currentDecision != BotDecisionState.Hunt)
        {
            currentDecision = BotDecisionState.Patrol;
            HandlePatrolling();
        }
    }
    
    /// <summary>
    /// Check if this bot is in a cluster with other bots
    /// </summary>
    private bool IsInCluster()
    {
        return nearbyBots.Count >= maxBotsInArea;
    }
    
    /// <summary>
    /// Update list of nearby bots for clustering prevention
    /// </summary>
    private void UpdateNearbyBots()
    {
        nearbyBots.Clear();
        Collider[] botsInRange = Physics.OverlapSphere(transform.position, clusteringAvoidanceRange, botLayer);
        
        foreach (Collider botCollider in botsInRange)
        {
            if (botCollider.transform != transform) // Don't include self
            {
                nearbyBots.Add(botCollider.transform);
            }
        }
    }
    
    /// <summary>
    /// Handle repositioning to avoid clustering
    /// </summary>
    private void HandleRepositioning()
    {
        Vector3 avoidanceDirection = Vector3.zero;
        
        // Calculate direction away from nearby bots
        foreach (Transform nearbyBot in nearbyBots)
        {
            Vector3 directionAway = (transform.position - nearbyBot.position).normalized;
            avoidanceDirection += directionAway;
        }
        
        avoidanceDirection = avoidanceDirection.normalized;
        Vector3 repositionTarget = transform.position + avoidanceDirection * spreadOutForce;
        
        // Add some randomization to make repositioning less predictable
        Vector3 randomOffset = new Vector3(
            (float)(randomizer.NextDouble() - 0.5) * 5f,
            0,
            (float)(randomizer.NextDouble() - 0.5) * 5f
        );
        
        repositionTarget += randomOffset;
        movement.SetDestination(repositionTarget);
        behavior.SetBehaviorState(ABehavior.BehaviorState.Repositioning);
    }
    
    /// <summary>
    /// Handle flanking maneuvers for more tactical gameplay
    /// </summary>
    private void HandleFlanking(Vector3 playerPosition)
    {
        Vector3 directionToPlayer = (playerPosition - transform.position).normalized;
        Vector3 rightFlank = Vector3.Cross(directionToPlayer, Vector3.up).normalized;
        
        // Randomly choose left or right flank
        Vector3 flankDirection = randomizer.NextDouble() > 0.5 ? rightFlank : -rightFlank;
        Vector3 flankPosition = playerPosition + flankDirection * 12f;
        
        movement.SetDestination(flankPosition);
        behavior.SetBehaviorState(ABehavior.BehaviorState.Flanking);
    }
    
    /// <summary>
    /// Handle patrol behavior with randomization
    /// </summary>
    private void HandlePatrolling()
    {
        if (!movement.HasDestination() || movement.ReachedDestination())
        {
            // Generate random patrol point
            Vector3 randomDirection = new Vector3(
                (float)(randomizer.NextDouble() - 0.5) * 2f,
                0,
                (float)(randomizer.NextDouble() - 0.5) * 2f
            ).normalized;
            
            Vector3 patrolTarget = transform.position + randomDirection * UnityEngine.Random.Range(10f, 25f);
            movement.SetDestination(patrolTarget);
        }
        
        behavior.SetBehaviorState(ABehavior.BehaviorState.Patrolling);
    }
    
    /// <summary>
    /// Find the nearest player position
    /// </summary>
    private Vector3 FindNearestPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= playerDetectionRange)
            {
                return player.transform.position;
            }
        }
        return Vector3.zero;
    }
    
    /// <summary>
    /// Check if too many bots are already engaging the player
    /// </summary>
    private bool TooManyBotsEngagingPlayer(Vector3 playerPosition)
    {
        int engagingBots = 0;
        foreach (Transform bot in nearbyBots)
        {
            ADecisionMaking botDecision = bot.GetComponent<ADecisionMaking>();
            if (botDecision != null && (botDecision.currentDecision == BotDecisionState.Engage || 
                                       botDecision.currentDecision == BotDecisionState.Hunt))
            {
                engagingBots++;
            }
        }
        
        return engagingBots >= 2; // Max 2 bots engaging simultaneously
    }
    
    /// <summary>
    /// Determine if bot should attempt flanking based on personality and situation
    /// </summary>
    private bool ShouldFlank()
    {
        return personalityFactor > 0.4f && randomizer.NextDouble() < 0.4f;
    }
    
    // Public accessors for other components
    public BotDecisionState GetCurrentDecision() => currentDecision;
    public float GetPersonalityFactor() => personalityFactor;
    public int GetNearbyBotCount() => nearbyBots.Count;
}