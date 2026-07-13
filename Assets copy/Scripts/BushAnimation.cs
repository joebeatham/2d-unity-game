
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

public class BushAnimation : MonoBehaviour
{
    public SkeletonAnimation SkeletonAnimation; // Spine2D game object can be set in inspector
    // Sets all animation variables to the Spine2D animation names
    public string Idle = "Idle Loop"; 
    public string WalkPastL2R = "Walk Past L2R";
    public string WalkPastR2L = "Walk Past R2L";

    void Start()
    {
        // Set default animation to idle unless told otherwise
        SkeletonAnimation.AnimationState.SetAnimation(0, Idle, true);
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBody")) // if bush hitbox is touched by player hitbox
        {
            // Get both X positions of the player and the bush
            float BushHorizontal = transform.position.x;
            float PlayerHorizontal = other.transform.position.x;
            string WalkAnimation = PlayerHorizontal < BushHorizontal ? WalkPastL2R : WalkPastR2L; // Determine player position relative to bush to know which bush sway animation to play
            SkeletonAnimation.AnimationState.SetAnimation(0, WalkAnimation, false); // Plays the correct sway animation once
            SkeletonAnimation.AnimationState.AddAnimation(0, Idle, true, 0f); // Return to idle animation loop after sway animation
        }
    }
}
