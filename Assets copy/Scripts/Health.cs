using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

public class Health : MonoBehaviour
{
    public int MaxHealth = 5; // Max health of player
    private int CurrentHealth; // Tracks current HP
    private float DamageTakenCooldown = 0.1f; // Cooldown time between taking damage so you don't take infinite damage from an enemy
    private float LastDamageTime = -1f; // Time when player last took damage
    public Vector2 RespawnPosition; // Where the player respawns after death (last mirror or start)
    public Vector3 LastSafePosition; // Last safe position saved by ground check
    public Text CurrentHPText; // UI text element to show current HP
    private bool IsAttacking = false; // Is player currently attacking?

    // Start is called before the first frame update
    void Start()
    {
        CurrentHealth = MaxHealth;
        UpdateHealthUI(); // UI function to update health display

        if (RespawnManager.NextRespawnPosition.HasValue) // Check if a respawn position is stored in RespawnManager
        {
            // If yes, move the player to the respawn position on start
            transform.position = RespawnManager.NextRespawnPosition.Value;
            RespawnManager.NextRespawnPosition = null;
        }
        else if (PlayerPrefs.HasKey("SavedX")) // Check for last saved position in PlayerPrefs (permanent save)
        {
            LoadSavedPosition();
        }
        else
        {
            RespawnPosition = transform.position; // Go to current position if no saved data (safety net)
        }

        // Reset parallax backgrounds after moving the player
        foreach (var parallax in FindObjectsOfType<ParallaxBackground>())
        {
            parallax.ResetParallaxOrigin(); // Use the function from ParallaxBackground script
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

public void SetAttackState(bool attacking)
{
    IsAttacking = attacking;
    Debug.Log("Player attack state set to: " + attacking);
}


    public void TakeDamage(int amount)
    {
        Debug.Log("=== TAKE DAMAGE CALLED ===");
        Debug.Log("Damage amount: " + amount);

        // Add cooldown check for ALL damage sources, not just spikes
    if (Time.time <= LastDamageTime + DamageTakenCooldown)
    {
        Debug.Log("TakeDamage blocked by cooldown. Time since last damage: " + (Time.time - LastDamageTime));
        return;
    }
    // SET LastDamageTime IMMEDIATELY after cooldown check passes
    LastDamageTime = Time.time;
        Debug.Log("TakeDamage called. Amount: " + amount + ", CurrentHealth before: " + CurrentHealth); // Debug log for easy future debugging
        CurrentHealth -= amount; // Subtract 1 from current HP when taking damage
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth); // Ensure health stays within 0 - MaxHealth
        UpdateHealthUI(); // Update UI
        if (CurrentHealth <= 0) // Check if player is dead (health 0 or below)
        {
            string SavedScene = PlayerPrefs.GetString("SavedScene", SceneManager.GetActiveScene().name); // Get saved scene
            // Get saved position from PlayerPrefs (permanent save)
            Vector3 SavedPosition = new Vector3(
            PlayerPrefs.GetFloat("SavedX", transform.position.x),
            PlayerPrefs.GetFloat("SavedY", transform.position.y),
            PlayerPrefs.GetFloat("SavedZ", transform.position.z));

            if (SceneManager.GetActiveScene().name != SavedScene) // If saved scene is different than the death one then load that scene
            {
                RespawnManager.NextRespawnPosition = SavedPosition;
                SceneManager.LoadScene(SavedScene);
            }
            else // Same scene respawn
            {
                transform.position = SavedPosition;

                // Reset parallax backgrounds after respawn if in the same scene so that they align correctly
                foreach (var parallax in FindObjectsOfType<ParallaxBackground>())
                {
                    parallax.ResetParallaxOrigin();
                }
            }
            
            CurrentHealth = MaxHealth; // Set health back to full after respawn
            UpdateHealthUI(); // set UI after respawn
        }
    }

    void UpdateHealthUI()
    {
        CurrentHPText.text = CurrentHealth.ToString(); // Update UI text to show value of current health variable
    }

    public void RestoreFullHealth()
    {
        CurrentHealth = MaxHealth; // Set health to full
        UpdateHealthUI(); // Update UI
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Health script detected collision with: " + other.name + " | Tag: " + other.tag + " | Layer: " + LayerMask.LayerToName(other.gameObject.layer));
        
        if (other.transform.IsChildOf(transform) || transform.IsChildOf(other.transform))
        {
        Debug.Log("Ignoring collision with child/parent object: " + other.name);
        return;
        }

        if (other.CompareTag("Spike")) // If player touches damaging surface eg spikes
        {
            if (Time.time > LastDamageTime + DamageTakenCooldown) // Check if enough time has passed since last damage
            {
                TakeDamage(1);
                Debug.Log("Player hit spike! Calling TakeDamage."); // Debug log for easy future debugging

                // Teleport to last saved safe position and put velocity at zero
                transform.position = LastSafePosition;
                GetComponent<Rigidbody2D>().velocity = Vector2.zero;

                // Temporarily disables player input until all movement keys are released so you don't immediately move again maybe into danger
                var Move = GetComponent<Move>();
                Move.LockMovement = true;
                Move.LoRInputReleased = false;
                Move.PreviousLoRInput = 0f; 
            }
        }
        // Handle enemy contact damage (Hollow Knight style)
    if (other.CompareTag("Enemy"))
    {
        // Don't take contact damage while attacking
        if (IsAttacking)
        {
            Debug.Log("Enemy contact ignored - player is attacking");
            return;
        }
        if (Time.time > LastDamageTime + DamageTakenCooldown)
        {
            TakeDamage(1); // Adjust damage as needed
            Debug.Log("Player touched enemy! Taking contact damage.");
            
            // Optional: Add knockback when touching enemy
            var playerRigidbody = GetComponent<Rigidbody2D>();
            if (playerRigidbody != null)
            {
                // Calculate knockback direction (away from enemy)
                float knockbackDirection = transform.position.x > other.transform.position.x ? 1f : -1f;
                Vector2 knockbackForce = new Vector2(knockbackDirection * 200f, 100f); // Adjust force as needed
                playerRigidbody.AddForce(knockbackForce);
            }
        }
    }
   if (other.CompareTag("EnemyAttack") || (other.CompareTag("Enemy") && other.gameObject.layer == LayerMask.NameToLayer("EnemyAttack")))
{
    Debug.Log("Enemy attack damage detected!");
    if (Time.time > LastDamageTime + DamageTakenCooldown)
    {
        TakeDamage(2); // Higher damage for attacks
        Debug.Log("Player hit by enemy attack!");
        
        var playerRigidbody = GetComponent<Rigidbody2D>();
        if (playerRigidbody != null)
        {
            float knockbackDirection = transform.position.x > other.transform.position.x ? 1f : -1f;
            Vector2 knockbackForce = new Vector2(knockbackDirection * 300f, 100f);
            playerRigidbody.AddForce(knockbackForce);
        }
    }
}
    }

    public void LoadSavedPosition()
    {
        if (PlayerPrefs.HasKey("SavedX")) // Check if saved position exists in PlayerPrefs
        {
            // If so then  make respawn position the saved position
            float x = PlayerPrefs.GetFloat("SavedX");
            float y = PlayerPrefs.GetFloat("SavedY");
            float z = PlayerPrefs.GetFloat("SavedZ");
            RespawnPosition = new Vector3(x, y, z);
        }
    }
}
