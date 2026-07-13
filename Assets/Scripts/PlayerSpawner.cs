using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (!string.IsNullOrEmpty(SpawnPointManager.RoomSpawnPoint)) // Check if a spawn point is set
        {
            GameObject Spawn = GameObject.Find(SpawnPointManager.RoomSpawnPoint); // Set spawn point from static variable in SpawnPointManager
            if (Spawn != null)
            {
                GameObject Player = GameObject.FindGameObjectWithTag("PlayerBody"); // Find the player by tag PlayerBody on the player
                if (Player != null)
                {
                    // Move player to spawn point
                    Player.transform.position = Spawn.transform.position;

                    // Reset parallax backgrounds
                    foreach (var parallax in FindObjectsOfType<ParallaxBackground>()) // find parallax background class
                    {
                        parallax.ResetParallaxOrigin(); // use ResetParallaxOrigin function in that script
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
