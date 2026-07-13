using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMoveAll : MonoBehaviour
{
    public Transform player; // Variable of the player's Transform (location in the scene)
    public float YPositionOffset = 2f; // Vertical offset to keep the player below the center of the screen
    public float FollowSmoothness = 5f; // The speed that the camera follows the player after a delay

    // Start is called before the first frame update
    void Start()
    {

    }

    // LateUpdate is called after all Update methods every frame update
    // We use this because the player might have Update methods and LateUpdate ensures the camera moves after the player has moved
    void LateUpdate()
    {
        if (player != null) // Make sure player reference is set
        {
            Vector3 CameraPosition = transform.position; // Current camera position
            float targetY = player.position.y + YPositionOffset; // Calculate where the camera should be
            CameraPosition.y = Mathf.Lerp(CameraPosition.y, targetY, FollowSmoothness * Time.deltaTime); // Smoothly move towards target Y with a little delay
            transform.position = new Vector3(player.position.x, CameraPosition.y, -10); // Update camera position keeping a fixed X and Z
        }
    }
}
