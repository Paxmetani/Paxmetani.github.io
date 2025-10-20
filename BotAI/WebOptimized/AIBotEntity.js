/**
 * AIBotEntity.js - Complete AI Bot Entity
 * Combines all AI systems into one entity with web optimization
 */

class AIBotEntity {
    constructor(position, config = {}) {
        this.active = true;
        this.position = position || { x: 0, y: 0, z: 0 };
        this.rotation = 0;
        this.velocity = { x: 0, y: 0, z: 0 };
        
        // Initialize AI components
        this.movement = new AIMovement(this, config.movement || {});
        this.combat = new AICombat(this, config.combat || {});
        this.decisionMaking = new AIDecisionMaking(this, config.decisionMaking || {});
        
        // Unique identifier
        this.id = `bot_${Math.random().toString(36).substr(2, 9)}`;
        
        // Callbacks for events
        this.onAttack = null;
        this.onDamaged = null;
        this.onDeath = null;
        this.onDestinationReached = null;
    }
    
    /**
     * Main update loop
     */
    update(deltaTime, allBots = [], playerTarget = null) {
        if (!this.active) return;
        
        // Update decision making
        this.decisionMaking.update(deltaTime, allBots, playerTarget);
        
        // Get nearby bots for movement avoidance
        const nearbyBots = allBots.filter(bot => {
            if (bot === this || !bot.active) return false;
            const dist = this._getDistance(this.position, bot.position);
            return dist < 15;
        });
        
        // Update movement
        this.movement.update(deltaTime, nearbyBots);
        
        // Update combat strafing if in combat
        if (this.combat.isInCombat && playerTarget) {
            this.movement.updateStrafing(deltaTime / 1000);
        }
        
        // Update combat
        this.combat.update(deltaTime, playerTarget);
    }
    
    /**
     * Take damage
     */
    takeDamage(damage) {
        this.combat.takeDamage(damage);
    }
    
    /**
     * Get distance to position
     */
    _getDistance(pos1, pos2) {
        const dx = pos1.x - pos2.x;
        const dz = pos1.z - pos2.z;
        return Math.sqrt(dx * dx + dz * dz);
    }
    
    /**
     * Destroy this bot
     */
    destroy() {
        this.active = false;
    }
    
    /**
     * Get bot state for debugging
     */
    getState() {
        return {
            id: this.id,
            position: this.position,
            rotation: this.rotation,
            decision: this.decisionMaking.getCurrentDecision(),
            combatState: this.combat.getCombatState(),
            health: this.combat.getHealthPercentage(),
            isInCombat: this.combat.isInCombat,
            hasDestination: this.movement.hasDestination
        };
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AIBotEntity;
}
