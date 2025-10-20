/**
 * AICombat.js - Web-optimized AI Combat System
 * Highly unpredictable tactical combat with performance optimization
 * Features: Burst firing, predictive aiming, dynamic tactics, efficient raycasting
 */

class AICombat {
    constructor(entity, config = {}) {
        this.entity = entity;
        
        // Combat configuration
        this.attackRange = config.attackRange || 10.0;
        this.minAttackRange = config.minAttackRange || 3.0;
        this.attackCooldown = config.attackCooldown || 1200; // ms
        this.weaponDamage = config.weaponDamage || 25;
        this.burstFireCount = config.burstFireCount || 3;
        
        // Tactical behavior
        this.aggressionLevel = config.aggressionLevel || 0.7;
        this.reactionTime = config.reactionTime || 300; // ms
        this.aimAccuracy = config.aimAccuracy || 0.75;
        this.usePredictiveAiming = config.usePredictiveAiming !== false;
        
        // Dynamic combat
        this.retreatThreshold = config.retreatThreshold || 0.3; // 30% health
        this.flankinChance = config.flankingChance || 0.3;
        
        // State
        this.currentTarget = null;
        this.targetPosition = null;
        this.lastAttackTime = 0;
        this.isInCombat = false;
        this.combatState = 'idle';
        
        // Health system
        this.maxHealth = config.maxHealth || 100;
        this.currentHealth = this.maxHealth;
        
        // Personalization for unpredictability
        this.personalAggression = this.aggressionLevel + (Math.random() - 0.5) * 0.4;
        this.personalAggression = Math.max(0, Math.min(1, this.personalAggression));
        this.personalAccuracy = this.aimAccuracy + (Math.random() - 0.5) * 0.2;
        this.personalAccuracy = Math.max(0, Math.min(1, this.personalAccuracy));
        
        // Performance optimization
        this.lastUpdate = performance.now();
        this.updateInterval = 50; // 20 updates per second
        this.predictedTargetPos = null;
        this.lastPredictionTime = 0;
        
        // Tactical timers
        this.tacticChangeTimer = 0;
        this.currentTactic = 'engage';
        this.burstFireActive = false;
        
        // Pooled objects for performance
        this._tempVec = { x: 0, y: 0, z: 0 };
    }
    
    /**
     * Main combat update loop
     */
    update(deltaTime, playerTarget = null) {
        const now = performance.now();
        
        // Frame-rate limiting for performance
        if (now - this.lastUpdate < this.updateInterval) {
            return;
        }
        
        const dt = (now - this.lastUpdate) / 1000;
        this.lastUpdate = now;
        
        // Update combat state
        this._updateCombatState(playerTarget);
        
        // Execute combat behavior
        if (this.isInCombat && this.currentTarget) {
            this._executeCombatBehavior(dt);
        }
        
        // Update tactic timer
        this.tacticChangeTimer += dt;
    }
    
    /**
     * Set combat target
     */
    setTarget(target) {
        this.currentTarget = target;
        if (target) {
            this.targetPosition = target.position || target;
            this.isInCombat = true;
        }
    }
    
    /**
     * Clear current target
     */
    clearTarget() {
        this.currentTarget = null;
        this.targetPosition = null;
        this.isInCombat = false;
        this.combatState = 'idle';
    }
    
    /**
     * Update combat state based on situation
     */
    _updateCombatState(playerTarget) {
        if (!playerTarget) {
            this.isInCombat = false;
            this.combatState = 'idle';
            return;
        }
        
        const dist = this._getDistanceToTarget(playerTarget);
        
        if (dist <= this.attackRange * 1.5) {
            this.isInCombat = true;
            this.setTarget(playerTarget);
        } else {
            this.isInCombat = false;
        }
    }
    
    /**
     * Execute combat behavior with tactical unpredictability
     */
    _executeCombatBehavior(dt) {
        // Update target prediction
        this._updateTargetPrediction();
        
        // Determine combat tactic dynamically
        const tactic = this._determineCombatTactic();
        
        // Execute tactic
        switch (tactic) {
            case 'engage':
                this._executeDirectEngagement();
                break;
            case 'flank':
                this._executeFlankingManeuver();
                break;
            case 'advance':
                this._executeAggressiveAdvance();
                break;
            case 'retreat':
                this._executeTacticalRetreat();
                break;
            case 'suppress':
                this._executeSuppressionFire();
                break;
            case 'ambush':
                this._executeAmbushTactic();
                break;
        }
        
        // Try to attack if cooldown ready
        if (this._canAttack()) {
            this._performAttack();
        }
    }
    
    /**
     * Determine combat tactic with unpredictability
     */
    _determineCombatTactic() {
        const dist = this._getDistanceToTarget(this.currentTarget);
        const healthPercent = this.currentHealth / this.maxHealth;
        
        // Change tactics periodically for unpredictability
        if (this.tacticChangeTimer > 3 + Math.random() * 4) {
            this.tacticChangeTimer = 0;
            
            // Low health - retreat or desperate aggression
            if (healthPercent < this.retreatThreshold) {
                return this.personalAggression > 0.8 ? 'advance' : 'retreat';
            }
            
            // Close range - engage or flank
            if (dist < this.minAttackRange) {
                return Math.random() < 0.7 ? 'engage' : 'flank';
            }
            
            // Medium range - multiple tactical options
            if (dist < this.attackRange) {
                const rand = Math.random();
                if (rand < 0.35) return 'advance';
                if (rand < 0.65) return 'flank';
                if (rand < 0.85) return 'engage';
                return 'suppress';
            }
            
            // Long range - approach with variety
            const rand = Math.random();
            if (rand < 0.4) return 'advance';
            if (rand < 0.6) return 'flank';
            if (rand < 0.8) return 'suppress';
            return 'ambush';
        }
        
        return this.currentTactic;
    }
    
    /**
     * Update predicted target position for accurate shooting
     */
    _updateTargetPrediction() {
        if (!this.usePredictiveAiming || !this.currentTarget) {
            this.predictedTargetPos = this.targetPosition;
            return;
        }
        
        const now = performance.now();
        if (now - this.lastPredictionTime < 100) return; // Cache for 100ms
        
        this.lastPredictionTime = now;
        
        // Predict based on target velocity if available
        if (this.currentTarget.velocity) {
            const timeToTarget = this._getDistanceToTarget(this.currentTarget) / 20; // Assume projectile speed of 20
            
            this.predictedTargetPos = {
                x: this.targetPosition.x + this.currentTarget.velocity.x * timeToTarget,
                y: this.targetPosition.y || 0,
                z: this.targetPosition.z + this.currentTarget.velocity.z * timeToTarget
            };
        } else {
            this.predictedTargetPos = this.targetPosition;
        }
    }
    
    /**
     * Execute direct engagement
     */
    _executeDirectEngagement() {
        this.currentTactic = 'engage';
        
        // Face target with some inaccuracy for realism
        const inaccuracy = (1 - this.personalAccuracy) * 0.1;
        const angle = this._getAngleToTarget(this.predictedTargetPos);
        this.entity.rotation = angle + (Math.random() - 0.5) * inaccuracy;
    }
    
    /**
     * Execute flanking maneuver
     */
    _executeFlankingManeuver() {
        this.currentTactic = 'flank';
        
        if (!this.entity.movement) return;
        
        // Calculate flank position
        const toTarget = this._getDirectionToTarget(this.currentTarget);
        const perpendicular = { x: -toTarget.z, z: toTarget.x };
        
        // Choose random flank side
        const side = Math.random() > 0.5 ? 1 : -1;
        const flankPos = {
            x: this.targetPosition.x + perpendicular.x * this.attackRange * 0.7 * side,
            z: this.targetPosition.z + perpendicular.z * this.attackRange * 0.7 * side
        };
        
        this.entity.movement.setDestination(flankPos, 'flank');
    }
    
    /**
     * Execute aggressive advance
     */
    _executeAggressiveAdvance() {
        this.currentTactic = 'advance';
        
        if (!this.entity.movement) return;
        
        // Move towards target aggressively
        const toTarget = this._getDirectionToTarget(this.currentTarget);
        const advancePos = {
            x: this.targetPosition.x + toTarget.x * 5,
            z: this.targetPosition.z + toTarget.z * 5
        };
        
        this.entity.movement.setDestination(advancePos, 'engage');
    }
    
    /**
     * Execute tactical retreat
     */
    _executeTacticalRetreat() {
        this.currentTactic = 'retreat';
        
        if (!this.entity.movement) return;
        
        // Move away from target
        const awayDir = this._getDirectionToTarget(this.currentTarget);
        const retreatPos = {
            x: this.entity.position.x - awayDir.x * 10,
            z: this.entity.position.z - awayDir.z * 10
        };
        
        this.entity.movement.setDestination(retreatPos, 'retreat');
    }
    
    /**
     * Execute suppression fire
     */
    _executeSuppressionFire() {
        this.currentTactic = 'suppress';
        
        // Face target area
        const angle = this._getAngleToTarget(this.targetPosition);
        this.entity.rotation = angle;
        
        // Fire rapidly with lower accuracy
        if (this._canAttack()) {
            this._performSuppressionBurst();
        }
    }
    
    /**
     * Execute ambush tactic (unpredictable positioning)
     */
    _executeAmbushTactic() {
        this.currentTactic = 'ambush';
        
        if (!this.entity.movement) return;
        
        // Find position with cover angle
        const angle = Math.random() * Math.PI * 2;
        const dist = this.attackRange * (0.6 + Math.random() * 0.3);
        
        const ambushPos = {
            x: this.targetPosition.x + Math.cos(angle) * dist,
            z: this.targetPosition.z + Math.sin(angle) * dist
        };
        
        this.entity.movement.setDestination(ambushPos, 'reposition');
    }
    
    /**
     * Check if can attack
     */
    _canAttack() {
        const now = performance.now();
        return now - this.lastAttackTime >= this.attackCooldown;
    }
    
    /**
     * Perform attack with accuracy calculation
     */
    _performAttack() {
        this.lastAttackTime = performance.now();
        
        // Calculate hit chance with dynamic accuracy
        const dist = this._getDistanceToTarget(this.currentTarget);
        const distanceFalloff = Math.max(0, 1 - dist / this.attackRange);
        const accuracy = this.personalAccuracy * distanceFalloff;
        
        const isHit = Math.random() < accuracy;
        
        if (isHit) {
            this._dealDamage(this.weaponDamage);
        }
        
        // Burst fire for realism
        if (this.burstFireCount > 1 && !this.burstFireActive) {
            this._triggerBurstFire();
        }
        
        // Callback for visual/audio feedback
        if (this.entity.onAttack) {
            this.entity.onAttack(isHit);
        }
    }
    
    /**
     * Trigger burst fire
     */
    _triggerBurstFire() {
        this.burstFireActive = true;
        let shotsFired = 0;
        
        const burstInterval = setInterval(() => {
            shotsFired++;
            
            if (shotsFired >= this.burstFireCount || !this.isInCombat) {
                clearInterval(burstInterval);
                this.burstFireActive = false;
                return;
            }
            
            // Burst shots have slightly lower accuracy
            const accuracy = this.personalAccuracy * 0.8;
            const isHit = Math.random() < accuracy;
            
            if (isHit) {
                this._dealDamage(this.weaponDamage * 0.8);
            }
            
            if (this.entity.onAttack) {
                this.entity.onAttack(isHit);
            }
        }, 100);
    }
    
    /**
     * Perform suppression burst (faster, less accurate)
     */
    _performSuppressionBurst() {
        const burstCount = 3 + Math.floor(Math.random() * 3); // 3-5 shots
        let shotsFired = 0;
        
        const suppressInterval = setInterval(() => {
            shotsFired++;
            
            if (shotsFired >= burstCount || !this.isInCombat) {
                clearInterval(suppressInterval);
                return;
            }
            
            const accuracy = this.personalAccuracy * 0.5; // Low accuracy for suppression
            const isHit = Math.random() < accuracy;
            
            if (isHit) {
                this._dealDamage(this.weaponDamage * 0.6);
            }
            
            if (this.entity.onAttack) {
                this.entity.onAttack(isHit);
            }
        }, 150);
    }
    
    /**
     * Deal damage to target
     */
    _dealDamage(damage) {
        if (this.currentTarget && this.currentTarget.takeDamage) {
            this.currentTarget.takeDamage(damage);
        }
    }
    
    /**
     * Take damage from external source
     */
    takeDamage(damage) {
        this.currentHealth -= damage;
        this.currentHealth = Math.max(0, this.currentHealth);
        
        // Increase aggression when hit
        this.personalAggression = Math.min(1, this.personalAggression + 0.1);
        
        if (this.currentHealth <= 0) {
            this._die();
        }
        
        // Callback for damage feedback
        if (this.entity.onDamaged) {
            this.entity.onDamaged(damage, this.currentHealth / this.maxHealth);
        }
    }
    
    /**
     * Handle death
     */
    _die() {
        this.isInCombat = false;
        this.combatState = 'dead';
        
        if (this.entity.onDeath) {
            this.entity.onDeath();
        }
    }
    
    /**
     * Utility: Get distance to target
     */
    _getDistanceToTarget(target) {
        const targetPos = target.position || target;
        const dx = this.entity.position.x - targetPos.x;
        const dz = this.entity.position.z - targetPos.z;
        return Math.sqrt(dx * dx + dz * dz);
    }
    
    /**
     * Utility: Get direction to target (normalized)
     */
    _getDirectionToTarget(target) {
        const targetPos = target.position || target;
        const dx = targetPos.x - this.entity.position.x;
        const dz = targetPos.z - this.entity.position.z;
        const dist = Math.sqrt(dx * dx + dz * dz);
        
        if (dist < 0.01) return { x: 0, z: 0 };
        
        return { x: dx / dist, z: dz / dist };
    }
    
    /**
     * Utility: Get angle to target
     */
    _getAngleToTarget(targetPos) {
        const dx = targetPos.x - this.entity.position.x;
        const dz = targetPos.z - this.entity.position.z;
        return Math.atan2(dx, dz);
    }
    
    /**
     * Get health percentage
     */
    getHealthPercentage() {
        return this.currentHealth / this.maxHealth;
    }
    
    /**
     * Get current combat state
     */
    getCombatState() {
        return this.currentTactic;
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AICombat;
}
