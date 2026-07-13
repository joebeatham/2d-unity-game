using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    public GameObject TopPart;          // The part that breaks off and flies away
    public GameObject BottomPart;       // The part that stays still
    public float VerticalForce = 0.6f; // Upward force
    public float HorizontalForce = 0.5f; // Horizontal force
    public float BreakForce = 5f;       // Force applied to the top part when broken off
    private bool Broken = false;        // To prevent multiple breaks in one object

    public void Break(Vector2 BreakDirection)
    {
        if (Broken) return; // Stops multiple breaks
        Broken = true;

        // Detach top part and add Rigidbody2D to it
        TopPart.transform.parent = null; 
        var rb = TopPart.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = TopPart.AddComponent<Rigidbody2D>(); // Add Rigidbody2D for physics but only when hit

        rb.AddForce(BreakDirection * BreakForce, ForceMode2D.Impulse); // Apply a force to the top part so it can fly off
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerAttack")) // Triggers the break when hit by the player
        {
            float xDirection; // Direction to apply horizontal force

            // Try to get the player's Move script
            var player = other.GetComponentInParent<Move>();
            if (player != null)
            {
                // Use the player's facing direction to decide force direction and make it look natural
                xDirection = player.PlayerAnimation.skeleton.ScaleX > 0 ? 1f : -1f;
            }
            else
            {
                // Fallback if direction not found which is rare
                float xDiff = TopPart.transform.position.x - other.transform.position.x;
                xDirection = xDiff > 0 ? 1f : -1f;
            }

            xDirection *= HorizontalForce; // change direction of the horizontal force based on player direction (1 or -1)
            Vector2 HitDirection = new Vector2(xDirection, VerticalForce).normalized; // Normalized to make sure vector has a length of 1 to control angle and trajectory
            Break(HitDirection); // calls Break function
        }
    }
}