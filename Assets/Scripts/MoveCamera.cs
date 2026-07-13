using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform player;
    public float fixedY = 1f;
    public float yFollowSmoothness = 5f; // Higher = less delay, lower = more delay
    public float minY = -10f; // Set these in the Inspector
    public float maxY = 10f;
    private float currentY;

    void Start()
    {
        if (player != null)
            currentY = player.position.y; // Start exactly at the player's Y
    }

    void FixedUpdate()
{
    if (player != null)
    {
        float distance = Mathf.Abs(currentY - player.position.y);
        // The further the player is from the camera, the faster the camera moves
        float dynamicSmoothness = yFollowSmoothness + distance * 2f; // 3f is a multiplier you can tweak

        currentY = Mathf.Lerp(currentY, player.position.y, Time.fixedDeltaTime * dynamicSmoothness);
        currentY = Mathf.Clamp(currentY, minY, maxY); // Clamp the y position
        transform.position = new Vector3(player.position.x, currentY, -10);
    }
}
}
