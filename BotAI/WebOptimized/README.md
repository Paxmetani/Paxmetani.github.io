# Web-Optimized AI Combat and Movement System

## 🎯 Overview

This is a high-performance, web-optimized AI system designed for browser-based tactical games. The system features unpredictable combat behavior, intelligent movement patterns, and extensive performance optimizations for smooth gameplay.

## 🌐 Live Demo

Open `example.html` in your browser to see the AI system in action!

**Controls:**
- Move mouse to control player position
- Click "Add Bot" to spawn new AI bots
- Watch bots engage in tactical combat with unpredictable behavior

## ✨ Key Features

### 🎲 Tactical Unpredictability
- **Randomized Decision Making**: Each bot has unique personality traits affecting behavior
- **Dynamic Combat Tactics**: 6 different combat states (engage, flank, advance, retreat, suppress, ambush)
- **Unpredictable Movement**: Randomized paths, variable speeds, context-sensitive maneuvers
- **Personality System**: Individual aggression, caution, and curiosity traits per bot

### ⚡ Web Performance Optimization
- **Spatial Hashing**: O(1) nearby bot queries using grid-based spatial partitioning
- **LOD System**: Distance-based update frequency (high/medium/low/minimal detail levels)
- **Frame-Rate Limiting**: Configurable max bots updated per frame
- **Object Pooling**: Reusable vector objects to reduce garbage collection
- **Update Batching**: Spreads AI processing across multiple frames
- **Efficient Raycasting**: Cached calculations and early-exit optimizations

### 🤖 Advanced AI Behaviors
- **Anti-Clustering**: Automatic bot redistribution to prevent grouping
- **Predictive Aiming**: Leads moving targets for realistic shooting
- **Combat Strafing**: Dynamic lateral movement during engagements
- **Stuck Detection**: Automatic recovery from blocked paths
- **Burst Firing**: Realistic weapon behavior with multiple shots
- **Tactical Flanking**: Perpendicular positioning for strategic advantage

## 📁 File Structure

```
WebOptimized/
├── AIMovement.js              # Movement system with anti-clustering
├── AICombat.js                # Combat system with tactical behaviors
├── AIDecisionMaking.js        # Decision-making and spatial awareness
├── AIPerformanceManager.js    # Performance optimization and LOD
├── AIBotEntity.js             # Complete bot entity combining all systems
├── example.html               # Interactive demo with visualization
└── README.md                  # This file
```

## 🚀 Quick Start

### Basic Usage

```html
<!DOCTYPE html>
<html>
<head>
    <title>AI Bot Demo</title>
</head>
<body>
    <canvas id="gameCanvas" width="800" height="600"></canvas>

    <!-- Load AI Scripts -->
    <script src="AIMovement.js"></script>
    <script src="AICombat.js"></script>
    <script src="AIDecisionMaking.js"></script>
    <script src="AIPerformanceManager.js"></script>
    <script src="AIBotEntity.js"></script>
    
    <script>
        // Create AI manager
        const aiManager = new AIPerformanceManager({
            maxBotsPerFrame: 5,
            spatialGridSize: 10,
            maxBotsInCluster: 2
        });
        
        // Spawn a bot
        const bot = new AIBotEntity({ x: 100, y: 0, z: 100 }, {
            movement: { baseSpeed: 3.5 },
            combat: { attackRange: 10, aggressionLevel: 0.7 },
            decisionMaking: { detectionRange: 30 }
        });
        
        // Register with manager
        aiManager.registerBot(bot);
        
        // Game loop
        function update() {
            const deltaTime = 16; // ~60fps
            const playerPos = { x: 400, y: 0, z: 300 };
            
            aiManager.update(deltaTime, playerPos);
            requestAnimationFrame(update);
        }
        update();
    </script>
</body>
</html>
```

## 📚 API Documentation

### AIBotEntity

Main bot entity combining all AI systems.

```javascript
const bot = new AIBotEntity(position, config);
```

**Parameters:**
- `position` - `{x, y, z}` - Initial position
- `config` - Configuration object with:
  - `movement` - Movement system config
  - `combat` - Combat system config
  - `decisionMaking` - Decision system config

**Methods:**
- `update(deltaTime, allBots, playerTarget)` - Main update loop
- `takeDamage(damage)` - Apply damage to bot
- `destroy()` - Deactivate bot
- `getState()` - Get current bot state for debugging

**Properties:**
- `position` - Current position `{x, y, z}`
- `rotation` - Current rotation in radians
- `active` - Whether bot is active
- `movement` - AIMovement instance
- `combat` - AICombat instance
- `decisionMaking` - AIDecisionMaking instance

### AIMovement

Handles bot movement with anti-clustering.

**Configuration:**
```javascript
{
    baseSpeed: 3.5,              // Base movement speed
    runSpeed: 6.0,               // Running speed
    personalSpaceRadius: 3.0,    // Avoidance radius
    avoidanceForce: 2.0,         // Separation force strength
    stoppingDistance: 1.0,       // Stop distance from destination
    pathRecalcInterval: 2000     // Path recalculation time (ms)
}
```

**Key Methods:**
- `setDestination(dest, behavior)` - Set movement target
- `enableCombatStrafing(target)` - Enable strafing around target
- `stop()` - Stop all movement
- `getCurrentSpeed()` - Get current speed

### AICombat

Tactical combat system with unpredictable behavior.

**Configuration:**
```javascript
{
    attackRange: 10.0,           // Maximum attack distance
    minAttackRange: 3.0,         // Minimum preferred distance
    attackCooldown: 1200,        // Time between attacks (ms)
    weaponDamage: 25,            // Damage per hit
    burstFireCount: 3,           // Shots per burst
    aggressionLevel: 0.7,        // Base aggression (0-1)
    reactionTime: 300,           // Reaction delay (ms)
    aimAccuracy: 0.75,           // Accuracy (0-1)
    usePredictiveAiming: true    // Lead moving targets
}
```

**Combat States:**
- `engage` - Direct engagement
- `flank` - Flanking maneuver
- `advance` - Aggressive approach
- `retreat` - Tactical withdrawal
- `suppress` - Suppression fire
- `ambush` - Unpredictable positioning

**Key Methods:**
- `setTarget(target)` - Set combat target
- `clearTarget()` - Clear target
- `takeDamage(damage)` - Receive damage
- `getHealthPercentage()` - Get health (0-1)
- `getCombatState()` - Get current tactic

### AIDecisionMaking

High-level tactical decision making.

**Configuration:**
```javascript
{
    decisionInterval: 100,       // Decision update rate (ms)
    detectionRange: 30.0,        // Player detection distance
    clusterAvoidanceRange: 15.0, // Anti-cluster range
    maxBotsInArea: 2             // Max bots per area
}
```

**Decision States:**
- `patrol` - Random patrolling
- `hunt` - Pursuing known position
- `engage` - Active combat
- `flank` - Flanking maneuver
- `retreat` - Withdrawing
- `reposition` - Avoiding clusters

**Key Methods:**
- `getCurrentDecision()` - Get current state
- `getPersonalityFactor()` - Get personality value
- `getNearbyBotCount()` - Get nearby bot count

### AIPerformanceManager

Central performance optimization system.

**Configuration:**
```javascript
{
    maxBotsPerFrame: 5,          // Max bot updates per frame
    spatialGridSize: 10,         // Grid cell size for spatial hashing
    maxDetailDistance: 25,       // High detail distance
    lowFrequencyDistance: 50,    // Low update frequency distance
    maxBotsInCluster: 2          // Max bots in grid cell
}
```

**Key Methods:**
- `registerBot(bot)` - Add bot to system
- `unregisterBot(bot)` - Remove bot
- `update(deltaTime, playerPos)` - Update all bots
- `getStats()` - Get performance statistics
- `clearAll()` - Remove all bots

## 🎨 Customization Examples

### Create Aggressive Bot

```javascript
const aggressiveBot = new AIBotEntity(position, {
    combat: {
        aggressionLevel: 0.9,
        attackRange: 12,
        reactionTime: 200,
        aimAccuracy: 0.85
    },
    movement: {
        baseSpeed: 5.0
    }
});
```

### Create Cautious Sniper Bot

```javascript
const sniperBot = new AIBotEntity(position, {
    combat: {
        aggressionLevel: 0.4,
        attackRange: 20,
        reactionTime: 500,
        aimAccuracy: 0.95,
        weaponDamage: 50
    },
    movement: {
        baseSpeed: 2.5
    },
    decisionMaking: {
        detectionRange: 40
    }
});
```

### Create Fast Flanker Bot

```javascript
const flankerBot = new AIBotEntity(position, {
    combat: {
        aggressionLevel: 0.75,
        flankingChance: 0.8
    },
    movement: {
        baseSpeed: 6.0,
        runSpeed: 8.0
    }
});
```

## 📊 Performance Metrics

The system is optimized for:
- **20+ concurrent bots** at 60 FPS
- **< 5ms average update time** for AI processing
- **Minimal memory footprint** through object pooling
- **Scalable performance** with LOD system

Performance tuning options:
- Reduce `maxBotsPerFrame` for lower-end devices
- Increase `spatialGridSize` for larger maps
- Adjust LOD distances for detail vs. performance trade-off

## 🔧 Optimization Techniques Used

1. **Spatial Hashing**: O(1) nearby entity queries instead of O(n²)
2. **Frame-Rate Limiting**: Process limited bots per frame
3. **LOD System**: Reduce update frequency for distant bots
4. **Object Pooling**: Reuse vector objects to reduce GC
5. **Cached Calculations**: Store expensive computations
6. **Early Exit Patterns**: Skip unnecessary calculations
7. **Update Batching**: Spread processing across frames
8. **Distance-Squared**: Avoid sqrt when possible

## 🎮 Integration Tips

### With Three.js

```javascript
// Create 3D representation
const geometry = new THREE.SphereGeometry(0.5, 16, 16);
const material = new THREE.MeshBasicMaterial({ color: 0xff0000 });
const mesh = new THREE.Mesh(geometry, material);
scene.add(mesh);

// Update mesh from AI
function update() {
    bot.update(deltaTime, allBots, player);
    mesh.position.set(bot.position.x, bot.position.y, bot.position.z);
    mesh.rotation.y = bot.rotation;
}
```

### With Canvas 2D

```javascript
function render(ctx) {
    for (const bot of bots) {
        ctx.save();
        ctx.translate(bot.position.x, bot.position.z);
        ctx.rotate(bot.rotation);
        
        // Draw bot
        ctx.fillStyle = bot.combat.isInCombat ? 'red' : 'blue';
        ctx.fillRect(-5, -5, 10, 10);
        
        ctx.restore();
    }
}
```

### With Babylon.js

```javascript
const mesh = BABYLON.MeshBuilder.CreateBox("bot", {size: 1}, scene);

function update() {
    bot.update(deltaTime, allBots, player);
    mesh.position = new BABYLON.Vector3(
        bot.position.x, 
        bot.position.y, 
        bot.position.z
    );
    mesh.rotation.y = bot.rotation;
}
```

## 🐛 Debugging

Enable debug visualization:

```javascript
// Get detailed stats
const stats = aiManager.getStats();
console.log('FPS:', stats.fps);
console.log('Bots in combat:', stats.botsInCombat);
console.log('Clustered areas:', stats.clusteredAreas);

// Check individual bot state
const state = bot.getState();
console.log('Decision:', state.decision);
console.log('Combat state:', state.combatState);
console.log('Health:', state.health);
```

## 🚧 Known Limitations

- No built-in pathfinding (A* or NavMesh) - uses direct navigation
- 2D movement only (X, Z plane) - Y coordinate unused
- No obstacle avoidance beyond bots
- Single weapon type per bot

## 🔜 Future Enhancements

- A* pathfinding integration
- Cover system for tactical positioning
- Team coordination behaviors
- Dynamic difficulty adjustment
- Machine learning integration
- Multi-weapon support

## 📝 License

Free to use for any purpose. No attribution required.

## 🔗 Links

- **AIMovement.js**: `/BotAI/WebOptimized/AIMovement.js`
- **AICombat.js**: `/BotAI/WebOptimized/AICombat.js`
- **AIDecisionMaking.js**: `/BotAI/WebOptimized/AIDecisionMaking.js`
- **AIPerformanceManager.js**: `/BotAI/WebOptimized/AIPerformanceManager.js`
- **AIBotEntity.js**: `/BotAI/WebOptimized/AIBotEntity.js`
- **Example Demo**: `/BotAI/WebOptimized/example.html`

## 💡 Tips for Best Results

1. **Tune aggression levels** per bot for variety (0.4 - 0.9 range)
2. **Vary detection ranges** to create different bot archetypes
3. **Adjust maxBotsPerFrame** based on target device performance
4. **Use callbacks** (`onAttack`, `onDamaged`) for visual/audio feedback
5. **Monitor performance stats** to identify bottlenecks
6. **Keep spatial grid size** appropriate for map scale (typically 10-20 units)

## ❓ FAQ

**Q: How many bots can run smoothly?**
A: 20-30 bots on average hardware, 50+ on high-end systems.

**Q: Can I use this with Unity/Unreal exports?**
A: Yes, but you may need to adapt the API for WebGL exports.

**Q: How do I make bots more/less aggressive?**
A: Adjust `aggressionLevel` in combat config (0-1 range).

**Q: Can bots work in teams?**
A: Currently individual behaviors only. Team AI is a planned feature.

**Q: Performance is slow, what should I do?**
A: Reduce `maxBotsPerFrame`, increase LOD distances, or reduce total bot count.

---

**Built with ❤️ for high-performance web games**
