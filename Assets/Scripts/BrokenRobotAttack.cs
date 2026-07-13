using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrokenRobotAttack : MonoBehaviour
{
    private bool CanDealDamage = false; // Add this flag

    void OnEnable()
    {
        // When hitbox becomes active, allow damage dealing
        CanDealDamage = true;
        Debug.Log("Enemy hitbox enabled - can deal damage");
    }

    void OnDisable()
    {
        // When hitbox becomes inactive, prevent damage dealing
        CanDealDamage = false;
        Debug.Log("Enemy hitbox disabled - cannot deal damage");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("=== ENEMY ATTACK HITBOX === detected: " + other.name + " | Tag: " + other.tag + " | Layer: " + LayerMask.LayerToName(other.gameObject.layer));
        Debug.Log("Enemy attack hitbox hit: " + other.name + " with tag: " + other.tag);
        
        // Only process collisions when the hitbox is supposed to be dealing damage
        if (!CanDealDamage)
        {
            Debug.Log("Hitbox collision ignored - not in damage window");
            return;
        }

        // ADD THIS - Check if we hit the player
        if (other.CompareTag("PlayerBody")) // Change to whatever tag your player has
        {
            Debug.Log("Enemy attack hit player!");
            
            var playerHealth = other.GetComponent<Health>();
            if (playerHealth != null)
            {
                var dummyAI = GetComponentInParent<DummyAI>();
                if (dummyAI != null)
                {
                    Debug.Log("Dealing attack damage: " + dummyAI.AttackDamage);
                    playerHealth.TakeDamage((int)dummyAI.AttackDamage);
                    
                    // Disable damage dealing after first hit to prevent multiple hits
                    CanDealDamage = false;
                    // Optional: Add knockback
                    var playerRigidbody = other.GetComponent<Rigidbody2D>();
                    if (playerRigidbody != null)
                    {
                        float knockbackDirection = other.transform.position.x > transform.position.x ? 1f : -1f;
                        float knockbackForce = 300f;
                        Vector2 knockbackVector = new Vector2(knockbackDirection, 0.2f);
                        playerRigidbody.AddForce(knockbackVector * knockbackForce);
                    }
                }
            }
            else
            {
                Debug.Log("Player found but no Health component!");
            }
        }
        else
        {
            Debug.Log("Hit something else: " + other.tag);
        }
    }
}
