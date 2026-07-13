using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity; // Ensure you have the Spine Unity runtime package installed

public class WaterAnimation : MonoBehaviour
{
    public SkeletonAnimation spineAnimation; // Assign Spine2D game object in Inspector

    void Start()
    {
        if (spineAnimation == null) // Safety net in case not assigned in Inspector
        {
            spineAnimation = GetComponentInChildren<SkeletonAnimation>();
        }
        if (spineAnimation != null) // Set default and only animation as the Idle loop
        {
            spineAnimation.AnimationState.SetAnimation(0, "Idle", true);
        }
        else // Debug log for easy debugging
        {
            Debug.LogError("No SkeletonAnimation found on water or its children!");
        }
    }
}
