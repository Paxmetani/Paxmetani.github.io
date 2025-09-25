using UnityEngine;

/// <summary>
/// Configuration scriptable object for bot AI settings - allows easy tweaking without code changes
/// </summary>
[CreateAssetMenu(fileName = "BotAIConfiguration", menuName = "Bot AI/Configuration")]
public class BotAIConfiguration : ScriptableObject
{
    [Header("Performance Settings")]
    [Tooltip("Maximum number of bots that can be processed per frame")]
    public int maxBotsPerFrameUpdate = 5;
    
    [Tooltip("Update frequency for AI decisions (in seconds)")]
    public float aiUpdateFrequency = 0.1f;
    
    [Tooltip("Distance beyond which bots are put into low-frequency update mode")]
    public float lowFrequencyUpdateDistance = 50f;
    
    [Header("Anti-Clustering Configuration")]
    [Tooltip("Minimum distance bots try to maintain from each other")]
    public float minimumBotSeparation = 8f;
    
    [Tooltip("Maximum number of bots allowed in a small area")]
    public int maxBotsInCluster = 2;
    
    [Tooltip("Force applied to separate clustered bots")]
    public float separationForce = 15f;
    
    [Header("Combat Configuration")]
    [Tooltip("Base reaction time for bots (in seconds)")]
    public float baseReactionTime = 0.4f;
    
    [Tooltip("Variation in reaction time (±seconds)")]
    public float reactionTimeVariation = 0.2f;
    
    [Tooltip("Base accuracy for bot shooting (0-1)")]
    public float baseAccuracy = 0.7f;
    
    [Tooltip("Accuracy variation between bots (±)")]
    public float accuracyVariation = 0.2f;
    
    [Header("Movement Configuration")]
    [Tooltip("Base movement speed for bots")]
    public float baseMovementSpeed = 4f;
    
    [Tooltip("Speed variation between bots (±)")]
    public float speedVariation = 1f;
    
    [Tooltip("Enable advanced pathfinding features")]
    public bool useAdvancedPathfinding = true;
    
    [Header("Behavioral Configuration")]
    [Tooltip("How often bots change their patrol patterns (seconds)")]
    public float patrolChangeFrequency = 30f;
    
    [Tooltip("Enable personality-based behavior differences")]
    public bool enablePersonalitySystem = true;
    
    [Tooltip("Enable emotional state system")]
    public bool enableEmotionalStates = true;
    
    [Header("Optimization Settings")]
    [Tooltip("Use object pooling for bot creation/destruction")]
    public bool useObjectPooling = true;
    
    [Tooltip("Maximum draw distance for bot details")]
    public float maxDetailDistance = 25f;
    
    [Tooltip("LOD (Level of Detail) system enabled")]
    public bool useLODSystem = true;
    
    [Header("Debug Settings")]
    [Tooltip("Show debug information for bot AI")]
    public bool showDebugInfo = false;
    
    [Tooltip("Show clustering prevention visualization")]
    public bool showClusteringDebug = false;
    
    [Tooltip("Show pathfinding debug lines")]
    public bool showPathfindingDebug = false;
}