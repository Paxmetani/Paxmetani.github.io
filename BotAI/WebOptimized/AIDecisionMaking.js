/**
 * AIDecisionMaking.js - Web-optimized AI Decision System
 * Prevents clustering and creates unpredictable tactical decisions
 * Features: Spatial awareness, dynamic tactics, personality-driven decisions
 */

class AIDecisionMaking {
    constructor(entity, config = {}) {
        this.entity = entity;
        
        // Configuration
        this.decisionInterval = config.decisionInterval || 100; // ms
        this.detectionRange = config.detectionRange || 30.0;
        this.clusterAvoidanceRange = config.clusterAvoidanceRange || 15.0;
        this.maxBotsInArea = config.maxBotsInArea || 2;
        
        // State
        this.currentDecision = 'patrol';
        this.lastKnownPlayerPos = null;
        this.nearbyBots = [];
        
        // Personality for unpredictability
        this.personalityFactor = Math.random();
        this.aggressionTrait = Math.random();
        this.cautionTrait = Math.random();
        this.curiosityTrait = Math.random();
        
        // Performance optimization
        this.lastDecisionTime = 0;
        this.lastSpatialUpdate = 0;
        this.spatialUpdateInterval = 500; // ms
        
        // Decision weights (for unpredictability)
        this.decisionWeights = {
            patrol: 0.3,
            hunt: 0.2,
            engage: 0.2,
            flank: 0.15,
            retreat: 0.1,
            reposition: 0.05
        };
    }
    
    /**
     * Main decision update loop
     */
    update(deltaTime, allBots = [], playerTarget = null) {
        const now = performance.now();
        
        // Frame-rate limited decisions
        if (now - this.lastDecisionTime < this.decisionInterval) {
            return;
        }
        
        this.lastDecisionTime = now;
        
        // Update spatial awareness
        if (now - this.lastSpatialUpdate > this.spatialUpdateInterval) {
            this._updateSpatialAwareness(allBots);
            this.lastSpatialUpdate = now;
        }
        
        // Make decision based on situation
        this._makeDecision(playerTarget);
    }
    
    /**
     * Make tactical decision with unpredictability
     */
    _makeDecision(playerTarget) {
        // Priority 1: Avoid clustering
        if (this._isInCluster()) {
            this.currentDecision = 'reposition';
            this._executeReposition();
            return;
        }
        
        // Priority 2: Engage nearby player
        if (playerTarget) {
            const dist = this._getDistance(this.entity.position, playerTarget.position || playerTarget);
            
            if (dist <= this.detectionRange) {
                this._decidePlayerEngagement(playerTarget, dist);
                return;
            }
        }
        
        // Priority 3: Continue hunting last known position
        if (this.lastKnownPlayerPos && this.currentDecision === 'hunt') {
            const distToLastKnown = this._getDistance(this.entity.position, this.lastKnownPlayerPos);
            
            if (distToLastKnown < 2) {
                this.lastKnownPlayerPos = null;
                this.currentDecision = 'patrol';
            } else {
                return; // Continue hunting
            }
        }
        
        // Default: Patrol with randomized behavior
        this._decidePatrol();
    }
    
    /**
     * Decide how to engage player with tactical variety
     */
    _decidePlayerEngagement(player, distance) {
        const playerPos = player.position || player;
        
        // Check if too many bots already engaging
        if (this._tooManyBotsEngaging(playerPos)) {
            this.currentDecision = 'flank';
            this._executeFlank(playerPos);
            return;
        }
        
        // Tactical decision based on distance and personality
        const rand = Math.random();
        const aggression = this.aggressionTrait;
        
        if (distance < 10) {
            // Close range - engage or flank
            if (rand < 0.6 + aggression * 0.3) {
                this.currentDecision = 'engage';
                this._executeEngage(player);
            } else {
                this.currentDecision = 'flank';
                this._executeFlank(playerPos);
            }
        } else {
            // Medium/long range - hunt, flank, or engage
            const tacticRoll = rand + aggression * 0.2;
            
            if (tacticRoll < 0.4) {
                this.currentDecision = 'hunt';
                this._executeHunt(playerPos);
            } else if (tacticRoll < 0.7) {
                this.currentDecision = 'flank';
                this._executeFlank(playerPos);
            } else {
                this.currentDecision = 'engage';
                this._executeEngage(player);
            }
        }
        
        this.lastKnownPlayerPos = { ...playerPos };
    }
    
    /**
     * Execute patrol behavior
     */
    _decidePatrol() {
        this.currentDecision = 'patrol';
        
        if (!this.entity.movement) return;
        
        // Random patrol point with variation
        if (!this.entity.movement.hasDestination) {
            const angle = Math.random() * Math.PI * 2;
            const distance = 10 + Math.random() * 15;
            
            const patrolPos = {
                x: this.entity.position.x + Math.cos(angle) * distance,
                z: this.entity.position.z + Math.sin(angle) * distance
            };
            
            this.entity.movement.setDestination(patrolPos, 'patrol');
        }
    }
    
    /**
     * Execute hunt behavior
     */
    _executeHunt(targetPos) {
        if (!this.entity.movement) return;
        
        this.entity.movement.setDestination(targetPos, 'hunt');
    }
    
    /**
     * Execute engage behavior
     */
    _executeEngage(target) {
        if (this.entity.combat) {
            this.entity.combat.setTarget(target);
        }
    }
    
    /**
     * Execute flank maneuver
     */
    _executeFlank(targetPos) {
        if (!this.entity.movement) return;
        
        // Calculate flank position with randomness
        const toTarget = {
            x: targetPos.x - this.entity.position.x,
            z: targetPos.z - this.entity.position.z
        };
        
        const dist = Math.sqrt(toTarget.x * toTarget.x + toTarget.z * toTarget.z);
        if (dist < 0.1) return;
        
        toTarget.x /= dist;
        toTarget.z /= dist;
        
        // Perpendicular direction
        const perp = { x: -toTarget.z, z: toTarget.x };
        const side = Math.random() > 0.5 ? 1 : -1;
        
        const flankPos = {
            x: targetPos.x + perp.x * 12 * side + (Math.random() - 0.5) * 4,
            z: targetPos.z + perp.z * 12 * side + (Math.random() - 0.5) * 4
        };
        
        this.entity.movement.setDestination(flankPos, 'flank');
    }
    
    /**
     * Execute repositioning to avoid clustering
     */
    _executeReposition() {
        if (!this.entity.movement) return;
        
        // Calculate direction away from nearby bots
        let avoidX = 0;
        let avoidZ = 0;
        
        for (const bot of this.nearbyBots) {
            const dx = this.entity.position.x - bot.position.x;
            const dz = this.entity.position.z - bot.position.z;
            const dist = Math.sqrt(dx * dx + dz * dz);
            
            if (dist > 0.1) {
                avoidX += dx / dist;
                avoidZ += dz / dist;
            }
        }
        
        const avoidMag = Math.sqrt(avoidX * avoidX + avoidZ * avoidZ);
        if (avoidMag > 0.1) {
            avoidX /= avoidMag;
            avoidZ /= avoidMag;
        }
        
        // Add randomness to reposition target
        const randomAngle = (Math.random() - 0.5) * Math.PI;
        const cos = Math.cos(randomAngle);
        const sin = Math.sin(randomAngle);
        
        const finalX = avoidX * cos - avoidZ * sin;
        const finalZ = avoidX * sin + avoidZ * cos;
        
        const repositionPos = {
            x: this.entity.position.x + finalX * 10,
            z: this.entity.position.z + finalZ * 10
        };
        
        this.entity.movement.setDestination(repositionPos, 'reposition');
    }
    
    /**
     * Update spatial awareness (nearby bots)
     */
    _updateSpatialAwareness(allBots) {
        this.nearbyBots = [];
        
        for (const bot of allBots) {
            if (bot === this.entity) continue;
            
            const dist = this._getDistance(this.entity.position, bot.position);
            
            if (dist <= this.clusterAvoidanceRange) {
                this.nearbyBots.push(bot);
            }
        }
    }
    
    /**
     * Check if in a cluster
     */
    _isInCluster() {
        return this.nearbyBots.length >= this.maxBotsInArea;
    }
    
    /**
     * Check if too many bots engaging target
     */
    _tooManyBotsEngaging(targetPos) {
        let engagingCount = 0;
        
        for (const bot of this.nearbyBots) {
            if (bot.decisionMaking) {
                const decision = bot.decisionMaking.currentDecision;
                if (decision === 'engage' || decision === 'hunt') {
                    engagingCount++;
                }
            }
        }
        
        return engagingCount >= 2; // Max 2 bots engaging simultaneously
    }
    
    /**
     * Utility: Calculate distance between two positions
     */
    _getDistance(pos1, pos2) {
        const dx = pos1.x - pos2.x;
        const dz = pos1.z - pos2.z;
        return Math.sqrt(dx * dx + dz * dz);
    }
    
    /**
     * Get current decision state
     */
    getCurrentDecision() {
        return this.currentDecision;
    }
    
    /**
     * Get personality factor
     */
    getPersonalityFactor() {
        return this.personalityFactor;
    }
    
    /**
     * Get nearby bot count
     */
    getNearbyBotCount() {
        return this.nearbyBots.length;
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AIDecisionMaking;
}
