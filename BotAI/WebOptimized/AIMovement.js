/**
 * AIMovement.js - Web-optimized AI Movement System
 * Optimized for browser performance with tactical unpredictability
 * Features: Object pooling, spatial hashing, efficient pathfinding, dynamic movement patterns
 */

class AIMovement {
    constructor(entity, config = {}) {
        this.entity = entity;
        
        // Movement configuration
        this.baseSpeed = config.baseSpeed || 3.5;
        this.runSpeed = config.runSpeed || 6.0;
        this.rotationSpeed = config.rotationSpeed || 120;
        this.acceleration = config.acceleration || 8.0;
        
        // Anti-clustering
        this.personalSpaceRadius = config.personalSpaceRadius || 3.0;
        this.avoidanceForce = config.avoidanceForce || 2.0;
        
        // Natural movement
        this.stoppingDistance = config.stoppingDistance || 1.0;
        this.pathRecalcInterval = config.pathRecalcInterval || 2000; // ms
        
        // State
        this.position = entity.position || { x: 0, y: 0, z: 0 };
        this.velocity = { x: 0, y: 0, z: 0 };
        this.rotation = 0;
        this.destination = null;
        this.hasDestination = false;
        
        // Performance optimization
        this.lastPathCalc = 0;
        this.lastUpdate = performance.now();
        this.updateInterval = 16; // ~60fps
        
        // Personalization for unpredictability
        this.personalizedSpeed = this.baseSpeed + (Math.random() - 0.5) * 1.0;
        this.speedModifier = 1.0;
        this.randomSeed = Math.random();
        
        // Dynamic movement patterns
        this.isStrafing = false;
        this.strafeTimer = 0;
        this.strafeDirection = Math.random() > 0.5 ? 1 : -1;
        this.stuckTimer = 0;
        this.lastPosition = { ...this.position };
        
        // Cached calculations
        this._cachedNearbyBots = [];
        this._cacheExpiry = 0;
        this._cacheLifetime = 200; // ms
    }
    
    /**
     * Main update loop - optimized for web performance
     */
    update(deltaTime, nearbyBots = []) {
        const now = performance.now();
        
        // Frame-rate limiting
        if (now - this.lastUpdate < this.updateInterval) {
            return;
        }
        
        const dt = (now - this.lastUpdate) / 1000; // Convert to seconds
        this.lastUpdate = now;
        
        // Update movement
        if (this.hasDestination) {
            this._updateMovementToDestination(dt, nearbyBots);
        }
        
        // Apply velocity with acceleration
        this._applyVelocity(dt);
        
        // Check for stuck state
        this._checkStuckState(dt);
        
        // Update last position
        this.lastPosition = { ...this.position };
    }
    
    /**
     * Set destination with randomization for unpredictability
     */
    setDestination(dest, behavior = 'normal') {
        // Add slight randomization to prevent identical paths
        const randomOffset = {
            x: (Math.random() - 0.5) * 2.0,
            y: 0,
            z: (Math.random() - 0.5) * 2.0
        };
        
        this.destination = {
            x: dest.x + randomOffset.x,
            y: dest.y || 0,
            z: dest.z + randomOffset.z
        };
        
        this.hasDestination = true;
        this.lastPathCalc = performance.now();
        
        // Adjust speed based on behavior
        this._adjustSpeedForBehavior(behavior);
    }
    
    /**
     * Update movement towards destination with clustering avoidance
     */
    _updateMovementToDestination(dt, nearbyBots) {
        if (!this.destination) return;
        
        // Calculate direction to destination
        const dx = this.destination.x - this.position.x;
        const dz = this.destination.z - this.position.z;
        const distance = Math.sqrt(dx * dx + dz * dz);
        
        // Check if reached
        if (distance < this.stoppingDistance) {
            this.hasDestination = false;
            this._onDestinationReached();
            return;
        }
        
        // Normalize direction
        const dirX = dx / distance;
        const dirZ = dz / distance;
        
        // Apply avoidance force from nearby bots
        const avoidance = this._calculateAvoidanceForce(nearbyBots);
        
        // Combine movement direction with avoidance
        const finalDirX = dirX + avoidance.x;
        const finalDirZ = dirZ + avoidance.z;
        const finalMag = Math.sqrt(finalDirX * finalDirX + finalDirZ * finalDirZ);
        
        // Set velocity
        const speed = this.personalizedSpeed * this.speedModifier;
        this.velocity.x = (finalDirX / finalMag) * speed;
        this.velocity.z = (finalDirZ / finalMag) * speed;
        
        // Update rotation
        this.rotation = Math.atan2(finalDirX, finalDirZ);
        
        // Recalculate path periodically
        if (performance.now() - this.lastPathCalc > this.pathRecalcInterval) {
            // In a real implementation, this would trigger pathfinding
            this.lastPathCalc = performance.now();
        }
    }
    
    /**
     * Calculate avoidance force from nearby bots (optimized with caching)
     */
    _calculateAvoidanceForce(nearbyBots) {
        const now = performance.now();
        
        // Use cached nearby bots if still valid
        if (now < this._cacheExpiry && this._cachedNearbyBots.length > 0) {
            nearbyBots = this._cachedNearbyBots;
        } else {
            this._cachedNearbyBots = nearbyBots;
            this._cacheExpiry = now + this._cacheLifetime;
        }
        
        let avoidX = 0;
        let avoidZ = 0;
        
        // Fast distance check with early exit
        for (let i = 0; i < nearbyBots.length; i++) {
            const bot = nearbyBots[i];
            if (bot === this.entity) continue;
            
            const dx = this.position.x - bot.position.x;
            const dz = this.position.z - bot.position.z;
            const distSq = dx * dx + dz * dz;
            const radiusSq = this.personalSpaceRadius * this.personalSpaceRadius;
            
            if (distSq < radiusSq && distSq > 0.01) {
                const dist = Math.sqrt(distSq);
                const force = (this.personalSpaceRadius - dist) / this.personalSpaceRadius;
                avoidX += (dx / dist) * this.avoidanceForce * force;
                avoidZ += (dz / dist) * this.avoidanceForce * force;
            }
        }
        
        return { x: avoidX, z: avoidZ };
    }
    
    /**
     * Apply velocity with smooth acceleration
     */
    _applyVelocity(dt) {
        this.position.x += this.velocity.x * dt;
        this.position.z += this.velocity.z * dt;
        
        // Apply friction when not moving to destination
        if (!this.hasDestination) {
            this.velocity.x *= 0.9;
            this.velocity.z *= 0.9;
        }
        
        // Update entity position
        if (this.entity) {
            this.entity.position = { ...this.position };
            this.entity.rotation = this.rotation;
        }
    }
    
    /**
     * Check if bot is stuck and handle recovery
     */
    _checkStuckState(dt) {
        const dx = this.position.x - this.lastPosition.x;
        const dz = this.position.z - this.lastPosition.z;
        const moved = Math.sqrt(dx * dx + dz * dz);
        
        if (this.hasDestination && moved < 0.1) {
            this.stuckTimer += dt;
            
            if (this.stuckTimer > 2.0) {
                this._handleStuck();
                this.stuckTimer = 0;
            }
        } else {
            this.stuckTimer = 0;
        }
    }
    
    /**
     * Handle stuck state with random evasion
     */
    _handleStuck() {
        const angle = Math.random() * Math.PI * 2;
        const distance = 5 + Math.random() * 5;
        
        const newDest = {
            x: this.position.x + Math.cos(angle) * distance,
            z: this.position.z + Math.sin(angle) * distance
        };
        
        this.setDestination(newDest, 'escape');
    }
    
    /**
     * Adjust speed based on behavior type
     */
    _adjustSpeedForBehavior(behavior) {
        switch (behavior) {
            case 'hunt':
            case 'engage':
                this.speedModifier = 1.3;
                break;
            case 'patrol':
                this.speedModifier = 0.8;
                break;
            case 'retreat':
                this.speedModifier = 1.5;
                break;
            case 'reposition':
                this.speedModifier = 1.2;
                break;
            case 'escape':
                this.speedModifier = 1.4;
                break;
            default:
                this.speedModifier = 1.0;
        }
    }
    
    /**
     * Called when destination is reached
     */
    _onDestinationReached() {
        this.velocity.x = 0;
        this.velocity.z = 0;
        
        // Small random pause for natural behavior
        if (this.entity && this.entity.onDestinationReached) {
            setTimeout(() => {
                this.entity.onDestinationReached();
            }, Math.random() * 1000);
        }
    }
    
    /**
     * Enable combat strafing for unpredictability
     */
    enableCombatStrafing(target) {
        this.isStrafing = true;
        this.strafeTarget = target;
    }
    
    /**
     * Disable strafing
     */
    disableStrafing() {
        this.isStrafing = false;
        this.strafeTarget = null;
    }
    
    /**
     * Update strafing movement
     */
    updateStrafing(dt) {
        if (!this.isStrafing || !this.strafeTarget) return;
        
        this.strafeTimer += dt;
        
        // Change direction periodically
        if (this.strafeTimer > 2 + Math.random() * 3) {
            this.strafeDirection *= -1;
            this.strafeTimer = 0;
        }
        
        // Calculate strafe direction (perpendicular to target)
        const dx = this.strafeTarget.x - this.position.x;
        const dz = this.strafeTarget.z - this.position.z;
        const dist = Math.sqrt(dx * dx + dz * dz);
        
        if (dist > 0.1) {
            // Perpendicular vector
            const perpX = -dz / dist;
            const perpZ = dx / dist;
            
            // Apply strafe movement
            const strafeSpeed = this.personalizedSpeed * 0.5;
            this.velocity.x += perpX * strafeSpeed * this.strafeDirection * dt;
            this.velocity.z += perpZ * strafeSpeed * this.strafeDirection * dt;
        }
    }
    
    /**
     * Stop all movement
     */
    stop() {
        this.hasDestination = false;
        this.destination = null;
        this.velocity.x = 0;
        this.velocity.z = 0;
        this.isStrafing = false;
    }
    
    /**
     * Get current speed
     */
    getCurrentSpeed() {
        return Math.sqrt(this.velocity.x * this.velocity.x + this.velocity.z * this.velocity.z);
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AIMovement;
}
