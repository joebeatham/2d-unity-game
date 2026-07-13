using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    // Its own script as more neat and easier to manage this way as this script is pure data and PlayerSpawner handles the logic
    // static variable to hold the spawn point name across scenes
     public static string RoomSpawnPoint; // saves which game object (spawn point) to spawn at upon load into a new scene
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
