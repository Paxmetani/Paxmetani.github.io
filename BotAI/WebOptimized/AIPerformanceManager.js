/**
 * AIPerformanceManager.js - Web-optimized Performance Manager
 * Optimizes AI processing for browser performance
 * Features: Spatial hashing, LOD system, update batching, object pooling
 */

class AIPerformanceManager {
    constructor(config = {}) {
        // Configuration
        this.maxBotsPerFrame = config.maxBotsPerFrame || 5;
        this.spatialGridSize = config.spatialGridSize || 10;
        this.maxDetailDistance = config.maxDetailDistance || 25;
        this.lowFrequencyDistance = config.lowFrequencyDistance || 50;
        this.maxBotsInCluster = config.maxBotsInCluster || 2;
        
        // Bot management
        this.allBots = [];
        this.updateQueue = [];
        this.spatialGrid = new Map();
        
        // Performance tracking
        this.frameUpdateCount = 0;
        this.lastFrameReset = performance.now();
        this.performanceStats = {
            totalBots: 0,
            activeBots: 0,
            botsInCombat: 0,
            avgUpdateTime: 0,
            clusteredAreas: 0
        };
        
        // LOD (Level of Detail) settings
        this.lodLevels = {
            high: { distance: 15, updateInterval: 16 },
            medium: { distance: 30, updateInterval: 50 },
            low: { distance: 50, updateInterval: 100 },
            minimal: { distance: Infinity, updateInterval: 200 }
        };
        
        // Object pooling for performance
        this.vectorPool = [];
        this.maxPoolSize = 100;
    }
    
    /**
     * Register a bot with the manager
     */
    registerBot(bot) {
        if (!this.allBots.includes(bot)) {
            this.allBots.push(bot);
            this.updateQueue.push(bot);
            
            // Assign unique ID for tracking
            bot._managerId = this.allBots.length - 1;
            
            console.log(`[AIManager] Registered bot ${bot._managerId}. Total: ${this.allBots.length}`);
        }
    }
    
    /**
     * Unregister a bot
     */
    unregisterBot(bot) {
        const index = this.allBots.indexOf(bot);
        if (index > -1) {
            this.allBots.splice(index, 1);
            
            const queueIndex = this.updateQueue.indexOf(bot);
            if (queueIndex > -1) {
                this.updateQueue.splice(queueIndex, 1);
            }
            
            console.log(`[AIManager] Unregistered bot. Total: ${this.allBots.length}`);
        }
    }
    
    /**
     * Main update loop - optimized for web performance
     */
    update(deltaTime, playerPosition = null) {
        const frameStart = performance.now();
        
        // Reset frame counter
        if (frameStart - this.lastFrameReset > 1000) {
            this.frameUpdateCount = 0;
            this.lastFrameReset = frameStart;
        }
        
        // Update spatial grid
        this._updateSpatialGrid();
        
        // Process bots with frame-rate limiting
        this._updateBots(deltaTime, playerPosition);
        
        // Check for clustering and optimize distribution
        this._optimizeBotDistribution();
        
        // Update performance statistics
        const updateTime = performance.now() - frameStart;
        this._updateStats(updateTime);
    }
    
    /**
     * Update bots with performance optimization
     */
    _updateBots(deltaTime, playerPosition) {
        let botsUpdatedThisFrame = 0;
        const maxUpdates = this.maxBotsPerFrame;
        
        // Process update queue with batching
        while (this.updateQueue.length > 0 && botsUpdatedThisFrame < maxUpdates) {
            const bot = this.updateQueue.shift();
            
            if (!bot || !bot.active) continue;
            
            // Determine LOD level based on distance to player
            const lodLevel = this._getLODLevel(bot, playerPosition);
            
            // Apply LOD-based update frequency
            if (this._shouldUpdate(bot, lodLevel)) {
                // Get nearby bots efficiently using spatial grid
                const nearbyBots = this._getNearbyBots(bot.position, 15);
                
                // Update bot AI components
                if (bot.decisionMaking) {
                    bot.decisionMaking.update(deltaTime, this.allBots, playerPosition);
                }
                
                if (bot.movement) {
                    bot.movement.update(deltaTime, nearbyBots);
                }
                
                if (bot.combat) {
                    bot.combat.update(deltaTime, playerPosition);
                }
                
                bot._lastUpdate = performance.now();
                botsUpdatedThisFrame++;
            }
            
            // Re-add to end of queue for continuous updates
            this.updateQueue.push(bot);
        }
        
        this.frameUpdateCount = botsUpdatedThisFrame;
    }
    
    /**
     * Update spatial grid for efficient queries
     */
    _updateSpatialGrid() {
        this.spatialGrid.clear();
        
        for (const bot of this.allBots) {
            if (!bot || !bot.active) continue;
            
            const gridKey = this._getGridKey(bot.position);
            
            if (!this.spatialGrid.has(gridKey)) {
                this.spatialGrid.set(gridKey, []);
            }
            
            this.spatialGrid.get(gridKey).push(bot);
        }
    }
    
    /**
     * Get nearby bots efficiently using spatial grid
     */
    _getNearbyBots(position, radius) {
        const nearbyBots = [];
        const centerKey = this._getGridKey(position);
        
        // Check center cell and neighboring cells
        const gridRadius = Math.ceil(radius / this.spatialGridSize);
        const [cx, cz] = this._parseGridKey(centerKey);
        
        for (let x = cx - gridRadius; x <= cx + gridRadius; x++) {
            for (let z = cz - gridRadius; z <= cz + gridRadius; z++) {
                const key = `${x},${z}`;
                const botsInCell = this.spatialGrid.get(key);
                
                if (botsInCell) {
                    for (const bot of botsInCell) {
                        const dist = this._getDistance(position, bot.position);
                        if (dist <= radius) {
                            nearbyBots.push(bot);
                        }
                    }
                }
            }
        }
        
        return nearbyBots;
    }
    
    /**
     * Optimize bot distribution to prevent clustering
     */
    _optimizeBotDistribution() {
        let clusteredAreas = 0;
        
        for (const [gridKey, bots] of this.spatialGrid.entries()) {
            if (bots.length > this.maxBotsInCluster) {
                clusteredAreas++;
                this._handleClusteredArea(bots, gridKey);
            }
        }
        
        this.performanceStats.clusteredAreas = clusteredAreas;
    }
    
    /**
     * Handle overcrowded area
     */
    _handleClusteredArea(bots, gridKey) {
        // Sort by priority (combat bots have higher priority)
        bots.sort((a, b) => {
            const aCombat = a.combat && a.combat.isInCombat ? 1 : 0;
            const bCombat = b.combat && b.combat.isInCombat ? 1 : 0;
            return bCombat - aCombat;
        });
        
        // Force lower priority bots to reposition
        for (let i = this.maxBotsInCluster; i < bots.length; i++) {
            if (bots[i].decisionMaking) {
                bots[i].decisionMaking.currentDecision = 'reposition';
            }
        }
    }
    
    /**
     * Determine LOD level for bot based on distance to player
     */
    _getLODLevel(bot, playerPosition) {
        if (!playerPosition) return 'low';
        
        const dist = this._getDistance(bot.position, playerPosition);
        
        if (dist < this.lodLevels.high.distance) return 'high';
        if (dist < this.lodLevels.medium.distance) return 'medium';
        if (dist < this.lodLevels.low.distance) return 'low';
        
        return 'minimal';
    }
    
    /**
     * Check if bot should update based on LOD
     */
    _shouldUpdate(bot, lodLevel) {
        if (!bot._lastUpdate) {
            bot._lastUpdate = 0;
        }
        
        const now = performance.now();
        const interval = this.lodLevels[lodLevel].updateInterval;
        
        return now - bot._lastUpdate >= interval;
    }
    
    /**
     * Get grid key for position
     */
    _getGridKey(position) {
        const x = Math.floor(position.x / this.spatialGridSize);
        const z = Math.floor(position.z / this.spatialGridSize);
        return `${x},${z}`;
    }
    
    /**
     * Parse grid key
     */
    _parseGridKey(key) {
        const parts = key.split(',');
        return [parseInt(parts[0]), parseInt(parts[1])];
    }
    
    /**
     * Calculate distance between positions
     */
    _getDistance(pos1, pos2) {
        const dx = pos1.x - pos2.x;
        const dz = pos1.z - pos2.z;
        return Math.sqrt(dx * dx + dz * dz);
    }
    
    /**
     * Update performance statistics
     */
    _updateStats(updateTime) {
        this.performanceStats.totalBots = this.allBots.length;
        this.performanceStats.activeBots = this.allBots.filter(b => b && b.active).length;
        
        let combatBots = 0;
        for (const bot of this.allBots) {
            if (bot && bot.combat && bot.combat.isInCombat) {
                combatBots++;
            }
        }
        this.performanceStats.botsInCombat = combatBots;
        
        // Moving average for update time
        this.performanceStats.avgUpdateTime = 
            this.performanceStats.avgUpdateTime * 0.9 + updateTime * 0.1;
    }
    
    /**
     * Get performance statistics
     */
    getStats() {
        return {
            ...this.performanceStats,
            frameUpdateCount: this.frameUpdateCount,
            spatialGridCells: this.spatialGrid.size,
            fps: Math.round(1000 / Math.max(this.performanceStats.avgUpdateTime, 1))
        };
    }
    
    /**
     * Object pooling - get pooled vector
     */
    getPooledVector() {
        if (this.vectorPool.length > 0) {
            return this.vectorPool.pop();
        }
        return { x: 0, y: 0, z: 0 };
    }
    
    /**
     * Object pooling - return vector to pool
     */
    returnToPool(vector) {
        if (this.vectorPool.length < this.maxPoolSize) {
            vector.x = 0;
            vector.y = 0;
            vector.z = 0;
            this.vectorPool.push(vector);
        }
    }
    
    /**
     * Clear all bots
     */
    clearAll() {
        this.allBots = [];
        this.updateQueue = [];
        this.spatialGrid.clear();
        console.log('[AIManager] Cleared all bots');
    }
    
    /**
     * Get bot count
     */
    getBotCount() {
        return this.allBots.length;
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AIPerformanceManager;
}
