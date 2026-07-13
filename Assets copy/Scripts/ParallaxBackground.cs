using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    public Transform Player; // Position of the player
    public float HorizontalParallaxFactor = 0.2f; // Horizontal parallax factor
    public float VerticalParallaxFactor = 0.1f; // Vertical parallax factor

    private Vector3 BackgroundStartPosition; // Starting position of background
    private Vector3 PlayerStartPosition; // Starting position of player

    void Start()
    {
        BackgroundStartPosition = transform.position; // Set start position of background
        if (Player != null) // If player exists then set start position
        {
            PlayerStartPosition = Player.position;
        }
    }

    void Update()
    {
        if (Player != null) // If player assigned the parallax works
        {
            Vector3 PlayerMoved = Player.position - PlayerStartPosition; // How far has player moved from starting position
            transform.position = new Vector3 // Update background position based on player movement for a parallax effect
            (
                BackgroundStartPosition.x + PlayerMoved.x * HorizontalParallaxFactor, // Horizontal parallax
                BackgroundStartPosition.y + PlayerMoved.y * VerticalParallaxFactor, // Vertical parallax
                BackgroundStartPosition.z
            );
        }
    }

    public void ResetParallaxOrigin() // Call this when player respawns or scene changes
    {
        BackgroundStartPosition = transform.position;
        if (Player != null)
        {
            PlayerStartPosition = Player.position;
        }   
    }
}

