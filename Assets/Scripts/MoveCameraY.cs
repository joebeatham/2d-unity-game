using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCameraY : MonoBehaviour
{
    public Transform player;
    public float yThreshold = 2f; // Threshold for Y-axis movement
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (player != null)
        {
            // Get the current camera position
            Vector3 cameraPosition = transform.position;

            // Check if the player's Y position is above the threshold
            if (player.position.y > cameraPosition.y + yThreshold)
            {
                // Move the camera up to follow the player
                cameraPosition.y = player.position.y - yThreshold;
            }
            else if (player.position.y < cameraPosition.y - yThreshold)
            {
                // Move the camera down to follow the player
                cameraPosition.y = player.position.y + yThreshold;
            }

            // Update the camera's position
            transform.position = new Vector3(player.position.x, cameraPosition.y, -10);
        }
    }
}
