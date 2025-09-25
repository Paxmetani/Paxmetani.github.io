using UnityEngine;

/// <summary>
/// Example integration script showing how to set up and use the Bot AI system
/// </summary>
public class BotAIExample : MonoBehaviour
{
    [Header("Bot Prefab Setup")]
    [SerializeField] private GameObject botPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int botsToSpawn = 10;
    
    [Header("AI Configuration")]
    [SerializeField] private BotAIConfiguration aiConfiguration;
    
    private void Start()
    {
        SetupBotAIManager();
        SpawnBots();
    }
    
    /// <summary>
    /// Set up the bot AI manager in the scene
    /// </summary>
    private void SetupBotAIManager()
    {
        // Create manager if it doesn't exist
        if (BotAIManager.Instance == null)
        {
            GameObject managerObj = new GameObject("BotAIManager");
            BotAIManager manager = managerObj.AddComponent<BotAIManager>();
            
            // Assign configuration if available
            if (aiConfiguration != null)
            {
                // Configuration is handled through SerializeField in the manager
                Debug.Log("Bot AI Manager created with custom configuration");
            }
        }
    }
    
    /// <summary>
    /// Spawn bots at designated spawn points
    /// </summary>
    private void SpawnBots()
    {
        if (botPrefab == null || spawnPoints.Length == 0)
        {
            Debug.LogError("BotAIExample: Missing bot prefab or spawn points!");
            return;
        }
        
        for (int i = 0; i < botsToSpawn; i++)
        {
            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];
            Vector3 spawnPosition = spawnPoint.position + GetRandomOffset();
            
            GameObject bot = Instantiate(botPrefab, spawnPosition, spawnPoint.rotation);
            
            // Customize bot based on spawn index for variety
            CustomizeBot(bot, i);
            
            Debug.Log($"Spawned bot {i + 1} at {spawnPosition}");
        }
    }
    
    /// <summary>
    /// Customize individual bot characteristics
    /// </summary>
    private void CustomizeBot(GameObject bot, int botIndex)
    {
        // Get AI components
        ADecisionMaking decision = bot.GetComponent<ADecisionMaking>();
        ACombat combat = bot.GetComponent<ACombat>();
        AMovement movement = bot.GetComponent<AMovement>();
        ABehavior behavior = bot.GetComponent<ABehavior>();
        
        if (decision == null || combat == null || movement == null || behavior == null)
        {
            Debug.LogError($"Bot {bot.name} is missing required AI components!");
            return;
        }
        
        // Add variety based on bot index
        float variety = (botIndex % 3) / 3f; // 0, 0.33, 0.66
        
        // Customize combat aggressiveness
        switch (botIndex % 3)
        {
            case 0: // Aggressive bot
                // More aggressive bots configured through component settings
                bot.name = $"AggressiveBot_{botIndex}";
                break;
                
            case 1: // Balanced bot  
                bot.name = $"BalancedBot_{botIndex}";
                break;
                
            case 2: // Cautious bot
                bot.name = $"CautiousBot_{botIndex}";
                break;
        }
        
        // Register with manager (done automatically in Start())
        Debug.Log($"Customized {bot.name} with variety factor {variety}");
    }
    
    /// <summary>
    /// Get random spawn offset to prevent exact positioning overlap
    /// </summary>
    private Vector3 GetRandomOffset()
    {
        return new Vector3(
            Random.Range(-2f, 2f),
            0,
            Random.Range(-2f, 2f)
        );
    }
    
    /// <summary>
    /// Example of runtime bot management
    /// </summary>
    private void Update()
    {
        // Example: Show bot statistics in console every 10 seconds
        if (Time.frameCount % 600 == 0 && BotAIManager.Instance != null) // ~10 seconds at 60 FPS
        {
            BotStatistics stats = BotAIManager.Instance.GetBotStatistics();
            Debug.Log($"Bot Stats - Active: {stats.activeBots}, Combat: {stats.botsInCombat}, " +
                     $"Patrolling: {stats.botsPatrolling}, Clustered Areas: {stats.clusteredAreas}");
        }
        
        // Example: Spawn additional bot on key press
        if (Input.GetKeyDown(KeyCode.B))
        {
            SpawnAdditionalBot();
        }
    }
    
    /// <summary>
    /// Example of spawning additional bots during runtime
    /// </summary>
    private void SpawnAdditionalBot()
    {
        if (BotAIManager.Instance.GetActiveBotCount() >= 25)
        {
            Debug.Log("Maximum bot limit reached!");
            return;
        }
        
        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 spawnPosition = randomSpawnPoint.position + GetRandomOffset();
        
        GameObject newBot = Instantiate(botPrefab, spawnPosition, randomSpawnPoint.rotation);
        CustomizeBot(newBot, Random.Range(100, 200));
        
        Debug.Log($"Spawned additional bot at runtime: {newBot.name}");
    }
    
    /// <summary>
    /// Example of how to create bot prefab programmatically
    /// </summary>
    public static GameObject CreateBotPrefab()
    {
        // Create base GameObject
        GameObject botPrefab = new GameObject("BotAI");
        
        // Add required components
        botPrefab.AddComponent<Rigidbody>().freezeRotation = true;
        botPrefab.AddComponent<CapsuleCollider>();
        botPrefab.AddComponent<UnityEngine.AI.NavMeshAgent>();
        
        // Add AI components
        botPrefab.AddComponent<ADecisionMaking>();
        botPrefab.AddComponent<AMovement>();  
        botPrefab.AddComponent<ACombat>();
        botPrefab.AddComponent<ABehavior>();
        
        // Set up basic properties
        botPrefab.tag = "Bot";
        botPrefab.layer = 8; // Bot layer
        
        Debug.Log("Created bot prefab programmatically");
        return botPrefab;
    }
}

/// <summary>
/// Simple player controller for testing bot AI behavior
/// </summary>
public class SimplePlayerController : MonoBehaviour, IDamageable
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float health = 100f;
    
    private void Update()
    {
        // Simple movement for testing
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 movement = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
        transform.Translate(movement);
    }
    
    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log($"Player took {damage} damage. Health: {health}");
        
        if (health <= 0)
        {
            Debug.Log("Player eliminated!");
            // Handle player death
        }
    }
}