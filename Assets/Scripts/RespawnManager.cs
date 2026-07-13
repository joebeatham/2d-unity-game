using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RespawnManager
{
    // different script because static classes can easily hold data across scene changes
    public static Vector3? NextRespawnPosition = null; // Stores next respawn position across scenes
}