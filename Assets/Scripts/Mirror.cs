using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Mirror : MonoBehaviour
{
    private Transform Player; // Location of player
    public KeyCode InteractKey = KeyCode.E; // E key to interact with mirror
    public float InteractRadius = 1.5f; // Radius you can be from mirror to interact with it

    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("PlayerBody").transform; // Find player on start by tag
    }

    void Update()
    {
        if (Vector2.Distance(transform.position, Player.position) <= InteractRadius) // in player is in radius
        {
            if (Input.GetKeyDown(InteractKey)) // If player presses E
            {
                // Save position and scene to PlayerPrefs (permanent save)
                PlayerPrefs.SetFloat("SavedX", Player.position.x);
                PlayerPrefs.SetFloat("SavedY", Player.position.y);
                PlayerPrefs.SetFloat("SavedZ", Player.position.z);
                PlayerPrefs.SetString("SavedScene", SceneManager.GetActiveScene().name);

                Health Health = Player.GetComponent<Health>(); // Get health script
                if (Health != null) // After getting script set respawn position and restore health
                {
                    Health.RespawnPosition = Player.position;
                    Health.RestoreFullHealth();
                }

                Debug.Log("Game Saved! Health Restored."); // Debug log for easy future debugging
            }
        }
    }
}