using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingPracticeDummy : MonoBehaviour
{
    public int maxHealth = 999; // Max health of enemy
    private int currentHealth; // Tracks current HP
    public Vector2 KnockbackForceVector = new Vector2(5f, 1f); // Vertical and horizontal force of knockback
    public float KnockbackTime = 0.2f; // Duration of the knockback force
    private float KnockbackTimer = 0f; // Time the enemy gets knocked back for
    private Vector3 KnockbackSpeed; // Speed of knockback each frame

    // Start is called before the first frame update 
    void Start()
{
    currentHealth = maxHealth;
}

void OnEnable()
{
    currentHealth = maxHealth;
}


void Update()
{
    // If timer is on then knock enemy back
    if (KnockbackTimer > 0f)
    {
        transform.position += KnockbackSpeed * Time.deltaTime;
        KnockbackTimer -= Time.deltaTime;
    }
}

public void ApplyKnockback(Vector2 force) // function to make the force of the knockback
{
    // Calculate knockback speed by using force and time
    KnockbackSpeed = new Vector3(force.x, force.y, 0) / KnockbackTime;
    KnockbackTimer = KnockbackTime; // Set knockback timer
}
   public void TakeDamage(int amount, int playerFacingDirection)
{
    currentHealth -= amount; // Damage health when enemy gets hit
    currentHealth = Mathf.Max(currentHealth, 0); // Prevent negative health
    Debug.Log("Dummy took " + amount + " damage. Remaining: " + currentHealth); // Debug log for easy debugging in future

    Vector2 appliedForce = new Vector2(KnockbackForceVector.x * playerFacingDirection, KnockbackForceVector.y); // Apply force based on player direction
    ApplyKnockback(appliedForce);

    if (currentHealth <= 0)
    {
        gameObject.SetActive(false); // Dummy dies if health reaches 0
    }
}
}