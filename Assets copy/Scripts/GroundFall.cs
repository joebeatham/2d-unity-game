using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundFall : MonoBehaviour
{
    public float GroundFallDelay = 0.2f; // Makes the ground only fall after a little delay so the player definitely falls
    private bool GroundFallen = false; // activate when ground has fallen
    private Rigidbody2D RigidBody; // Variable for Rigidbody2D component

    void Start()
    {
        RigidBody = GetComponent<Rigidbody2D>(); // Set RigidBody
        if (RigidBody == null) // Safety net
        {
            RigidBody = gameObject.AddComponent<Rigidbody2D>();
        }
        RigidBody.bodyType = RigidbodyType2D.Kinematic; // Platform not affected by physics at start as kinematic
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Sets ground to fall when player touches it
        if (!GroundFallen && other.CompareTag("PlayerBody"))
        {
            GroundFallen = true;
            StartCoroutine(FallAfterDelay()); // Reference to the FallAfterDelay function
        }
    }

    IEnumerator FallAfterDelay()
    {
        yield return new WaitForSeconds(GroundFallDelay); // Wait for delay time before continuing with the coroutine
        RigidBody.bodyType = RigidbodyType2D.Dynamic; // Set ground to be affected by physics by changing to Dynamic

        yield return new WaitForSeconds(1f); // Wait 1 second before continuing coroutine and starting fade

        // Fade out
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float fadeDuration = 1f; // Duration of fade
            float timer = 0f; // Timer of fade
            Color originalColor = sr.color; // Store original color and alpha
            while (timer < fadeDuration) // While timer is not as much as the fade duration
            {
                // Increase timer and set new alpha based on timer progress
                timer += Time.deltaTime;
                float Alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, Alpha);
                yield return null;
            }
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        }
        Destroy(gameObject); // Destroy ground object after fade
    }
}