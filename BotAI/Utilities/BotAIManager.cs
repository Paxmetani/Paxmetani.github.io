using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manager class for optimizing bot AI performance and preventing clustering across all bots
/// </summary>
public class BotAIManager : MonoBehaviour
{
    [Header("Manager Configuration")]
    [SerializeField] private BotAIConfiguration configuration;
    [SerializeField] private int maxActiveBots = 20;
    [SerializeField] private float managerUpdateFrequency = 0.5f;
    
    // Singleton instance
    public static BotAIManager Instance { get; private set; }
    
    // Bot management
    private List<ADecisionMaking> allBots = new List<ADecisionMaking>();
    private Queue<ADecisionMaking> updateQueue = new Queue<ADecisionMaking>();
    private float lastManagerUpdate;
    
    // Performance optimization
    private int botsUpdatedThisFrame = 0;
    private Camera playerCamera;
    private Transform playerTransform;
    
    // Clustering prevention
    private Dictionary<Vector3Int, List<ADecisionMaking>> spatialGrid = new Dictionary<Vector3Int, List<ADecisionMaking>>();
    private const int GRID_SIZE = 10;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        FindPlayerReferences();
    }
    
    private void Update()
    {
        UpdateBotPerformanceOptimization();
        
        if (Time.time - lastManagerUpdate >= managerUpdateFrequency)
        {
            UpdateSpatialGrid();
            OptimizeBotDistribution();
            lastManagerUpdate = Time.time;
        }
    }
    
    /// <summary>
    /// Initialize the bot AI manager
    /// </summary>
    private void InitializeManager()
    {
        if (configuration == null)
        {
            Debug.LogWarning("BotAIManager: No configuration assigned. Using default settings.");
            configuration = ScriptableObject.CreateInstance<BotAIConfiguration>();
        }
    }
    
    /// <summary>
    /// Find player references for optimization calculations
    /// </summary>
    private void FindPlayerReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = FindObjectOfType<Camera>();
        }
    }
    
    /// <summary>
    /// Register a bot with the manager
    /// </summary>
    public void RegisterBot(ADecisionMaking bot)
    {
        if (!allBots.Contains(bot))
        {
            allBots.Add(bot);
            updateQueue.Enqueue(bot);
            
            Debug.Log($"BotAIManager: Registered bot {bot.name}. Total bots: {allBots.Count}");
        }
    }
    
    /// <summary>
    /// Unregister a bot from the manager
    /// </summary>
    public void UnregisterBot(ADecisionMaking bot)
    {
        if (allBots.Contains(bot))
        {
            allBots.Remove(bot);
            
            // Remove from spatial grid
            Vector3Int gridPos = WorldToGridPosition(bot.transform.position);
            if (spatialGrid.ContainsKey(gridPos))
            {
                spatialGrid[gridPos].Remove(bot);
                if (spatialGrid[gridPos].Count == 0)
                {
                    spatialGrid.Remove(gridPos);
                }
            }
            
            Debug.Log($"BotAIManager: Unregistered bot {bot.name}. Total bots: {allBots.Count}");
        }
    }
    
    /// <summary>
    /// Update performance optimization for bots
    /// </summary>
    private void UpdateBotPerformanceOptimization()
    {
        botsUpdatedThisFrame = 0;
        
        // Process bots in queue with frame-rate limiting
        while (updateQueue.Count > 0 && botsUpdatedThisFrame < configuration.maxBotsPerFrameUpdate)
        {
            ADecisionMaking bot = updateQueue.Dequeue();
            
            if (bot != null && bot.gameObject.activeInHierarchy)
            {
                UpdateBotOptimization(bot);
                updateQueue.Enqueue(bot); // Re-add to end of queue
                botsUpdatedThisFrame++;
            }
        }
    }
    
    /// <summary>
    /// Update optimization settings for individual bot
    /// </summary>
    private void UpdateBotOptimization(ADecisionMaking bot)
    {
        if (playerTransform == null) return;
        
        float distanceToPlayer = Vector3.Distance(bot.transform.position, playerTransform.position);
        
        // Adjust update frequency based on distance
        if (distanceToPlayer > configuration.lowFrequencyUpdateDistance)
        {
            // Far bots get lower update frequency
            bot.enabled = Time.frameCount % 4 == 0; // Update every 4th frame
        }
        else
        {
            bot.enabled = true; // Normal update rate
        }
        
        // LOD system for detailed behaviors
        if (configuration.useLODSystem)
        {
            ABehavior behavior = bot.GetComponent<ABehavior>();
            if (behavior != null)
            {
                if (distanceToPlayer > configuration.maxDetailDistance)
                {
                    // Disable detailed behaviors for distant bots
                    behavior.enabled = false;
                }
                else
                {
                    behavior.enabled = true;
                }
            }
        }
    }
    
    /// <summary>
    /// Update spatial grid for clustering prevention
    /// </summary>
    private void UpdateSpatialGrid()
    {
        // Clear previous grid
        spatialGrid.Clear();
        
        // Add all active bots to grid
        foreach (var bot in allBots.Where(b => b != null && b.gameObject.activeInHierarchy))
        {
            Vector3Int gridPos = WorldToGridPosition(bot.transform.position);
            
            if (!spatialGrid.ContainsKey(gridPos))
            {
                spatialGrid[gridPos] = new List<ADecisionMaking>();
            }
            
            spatialGrid[gridPos].Add(bot);
        }
    }
    
    /// <summary>
    /// Optimize bot distribution to prevent clustering
    /// </summary>
    private void OptimizeBotDistribution()
    {
        foreach (var kvp in spatialGrid)
        {
            var botsInCell = kvp.Value;
            
            if (botsInCell.Count > configuration.maxBotsInCluster)
            {
                // Too many bots in this area - force some to reposition
                HandleOvercrowdedArea(botsInCell, kvp.Key);
            }
        }
    }
    
    /// <summary>
    /// Handle areas with too many bots
    /// </summary>
    private void HandleOvercrowdedArea(List<ADecisionMaking> bots, Vector3Int gridPosition)
    {
        // Sort bots by priority (combat bots have higher priority)
        bots.Sort((a, b) => {
            ACombat combatA = a.GetComponent<ACombat>();
            ACombat combatB = b.GetComponent<ACombat>();
            
            bool aInCombat = combatA != null && combatA.IsInCombat();
            bool bInCombat = combatB != null && combatB.IsInCombat();
            
            if (aInCombat && !bInCombat) return -1;
            if (!aInCombat && bInCombat) return 1;
            
            return 0;
        });
        
        // Force lower priority bots to reposition
        for (int i = configuration.maxBotsInCluster; i < bots.Count; i++)
        {
            if (bots[i] != null)
            {
                ForceRepositioning(bots[i]);
            }
        }
    }
    
    /// <summary>
    /// Force a bot to reposition to reduce clustering
    /// </summary>
    private void ForceRepositioning(ADecisionMaking bot)
    {
        // Find a less crowded area
        Vector3 currentPos = bot.transform.position;
        Vector3 repositionTarget = FindLessCrowdedPosition(currentPos);
        
        AMovement movement = bot.GetComponent<AMovement>();
        if (movement != null)
        {
            movement.SetDestination(repositionTarget);
        }
        
        // Set behavior to repositioning
        ABehavior behavior = bot.GetComponent<ABehavior>();
        if (behavior != null)
        {
            behavior.SetBehaviorState(ABehavior.BehaviorState.Repositioning);
        }
    }
    
    /// <summary>
    /// Find a position with fewer bots nearby
    /// </summary>
    private Vector3 FindLessCrowdedPosition(Vector3 currentPosition)
    {
        float searchRadius = GRID_SIZE * 2f;
        int attempts = 8;
        
        for (int i = 0; i < attempts; i++)
        {
            float angle = (360f / attempts) * i;
            Vector3 direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector3 testPosition = currentPosition + direction * searchRadius;
            
            Vector3Int testGridPos = WorldToGridPosition(testPosition);
            
            if (!spatialGrid.ContainsKey(testGridPos) || spatialGrid[testGridPos].Count < configuration.maxBotsInCluster)
            {
                return testPosition;
            }
        }
        
        // If no good position found, just move away from current position
        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f), 
            0, 
            Random.Range(-1f, 1f)
        ).normalized;
        
        return currentPosition + randomDirection * searchRadius;
    }
    
    /// <summary>
    /// Convert world position to grid coordinates
    /// </summary>
    private Vector3Int WorldToGridPosition(Vector3 worldPos)
    {
        return new Vector3Int(
            Mathf.RoundToInt(worldPos.x / GRID_SIZE),
            0, // We only care about X-Z plane for bot positioning
            Mathf.RoundToInt(worldPos.z / GRID_SIZE)
        );
    }
    
    /// <summary>
    /// Get nearby bots to a specific position
    /// </summary>
    public List<ADecisionMaking> GetNearbyBots(Vector3 position, float radius)
    {
        List<ADecisionMaking> nearbyBots = new List<ADecisionMaking>();
        Vector3Int centerGrid = WorldToGridPosition(position);
        
        int gridRadius = Mathf.CeilToInt(radius / GRID_SIZE);
        
        for (int x = -gridRadius; x <= gridRadius; x++)
        {
            for (int z = -gridRadius; z <= gridRadius; z++)
            {
                Vector3Int gridPos = centerGrid + new Vector3Int(x, 0, z);
                
                if (spatialGrid.ContainsKey(gridPos))
                {
                    foreach (var bot in spatialGrid[gridPos])
                    {
                        if (bot != null && Vector3.Distance(bot.transform.position, position) <= radius)
                        {
                            nearbyBots.Add(bot);
                        }
                    }
                }
            }
        }
        
        return nearbyBots;
    }
    
    /// <summary>
    /// Get current bot statistics
    /// </summary>
    public BotStatistics GetBotStatistics()
    {
        var stats = new BotStatistics();
        stats.totalBots = allBots.Count;
        stats.activeBots = allBots.Count(b => b != null && b.gameObject.activeInHierarchy);
        
        int combatBots = 0;
        int patrollingBots = 0;
        int repositioningBots = 0;
        
        foreach (var bot in allBots.Where(b => b != null))
        {
            switch (bot.GetCurrentDecision())
            {
                case ADecisionMaking.BotDecisionState.Engage:
                    combatBots++;
                    break;
                case ADecisionMaking.BotDecisionState.Patrol:
                    patrollingBots++;
                    break;
                case ADecisionMaking.BotDecisionState.Reposition:
                    repositioningBots++;
                    break;
            }
        }
        
        stats.botsInCombat = combatBots;
        stats.botsPatrolling = patrollingBots;
        stats.botsRepositioning = repositioningBots;
        stats.clusteredAreas = spatialGrid.Count(kvp => kvp.Value.Count > configuration.maxBotsInCluster);
        
        return stats;
    }
    
    // Public accessors
    public BotAIConfiguration GetConfiguration() => configuration;
    public int GetActiveBotCount() => allBots.Count(b => b != null && b.gameObject.activeInHierarchy);
    
    /// <summary>
    /// Debug visualization
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!configuration.showClusteringDebug) return;
        
        // Draw spatial grid
        Gizmos.color = Color.yellow;
        foreach (var kvp in spatialGrid)
        {
            Vector3 worldPos = new Vector3(kvp.Key.x * GRID_SIZE, 0, kvp.Key.z * GRID_SIZE);
            
            if (kvp.Value.Count > configuration.maxBotsInCluster)
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = Color.green;
            }
            
            Gizmos.DrawWireCube(worldPos, Vector3.one * GRID_SIZE);
            
            // Draw bot count
            UnityEditor.Handles.Label(worldPos + Vector3.up * 2, kvp.Value.Count.ToString());
        }
    }
}

/// <summary>
/// Structure for bot statistics
/// </summary>
[System.Serializable]
public struct BotStatistics
{
    public int totalBots;
    public int activeBots;
    public int botsInCombat;
    public int botsPatrolling;
    public int botsRepositioning;
    public int clusteredAreas;
}