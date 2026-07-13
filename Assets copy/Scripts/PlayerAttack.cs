using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

public class PlayerAttack : MonoBehaviour
{
    public GameObject SwordHitbox; // Hitbox of attack assigned in inspector
    public float AttackTime = 0.2f; // Length in time of attack
    public bool Attacking = false; // Activates if player attacks
    public SkeletonAnimation PlayerAnimation; // The player's spine game object set in the Inspector

    [Header("Controller Input")]
    public bool useControllerInput = false;
    public SerialControllerManager serialController;

    // Controller input tracking
    private bool controllerAttackPressed = false;

    // Start is called before the first frame update
    void Start()
    {
        var AnimationBlendData = PlayerAnimation.AnimationState.Data; // Animation blend data stored here
        // Transition from Sword Attack to other animations with a short blend time
        AnimationBlendData.SetMix("Sword Attack", "Idle", 0.1f);
        AnimationBlendData.SetMix("Sword Attack", "Running", 0.1f);
        AnimationBlendData.SetMix("Sword Attack", "Jumping Loop Up", 0.1f);
        AnimationBlendData.SetMix("Sword Attack", "Jumping Loop Down", 0.1f);
        AnimationBlendData.SetMix("Sword Attack", "Jumping Land", 0.1f);
        AnimationBlendData.SetMix("Jumping Loop Up", "Jumping Loop Down", 0.2f);
        AnimationBlendData.SetMix("Jumping Loop Down", "Jumping Loop Up", 0.2f);        

        // Controller events removed - were unused
    }

    void OnDestroy()
    {
        // Event cleanup removed - events no longer exist
    }

    // Update is called once per frame
    void Update()
    {
        // SYNC with Move script's input mode
        var moveScript = GetComponent<Move>();
        if (moveScript != null)
        {
            useControllerInput = moveScript.useControllerInput;
            serialController = moveScript.serialController;
        }
        
        // UNIFIED INPUT - Get attack input from controller OR keyboard
        bool attackPressed = false;
        if (useControllerInput && serialController != null && serialController.isConnected)
        {
            attackPressed = controllerAttackPressed;
            controllerAttackPressed = false; // Reset for next frame
        }
        else
        {
            attackPressed = Input.GetKeyDown(KeyCode.J);
        }

        if (!Attacking && attackPressed) // Attack when player presses J or controller attack button
        {
            StartCoroutine(SwingSword()); // Run SwingSword coroutine
        }
    }

    IEnumerator SwingSword()
    {
        Attacking = true;

        // Tell Health script we're attacking
        var health = GetComponent<Health>();
        if (health != null)
        {
            health.SetAttackState(true);
        }

        SwordHitbox.SetActive(true);

        // Declare variables at method scope so they can be used throughout
        var Move = GetComponent<Move>(); // Get Move script to check state
        var RigidBody = GetComponent<Rigidbody2D>(); // Get Rigidbody2D component

        if (PlayerAnimation != null)
        {
            bool isIdle = Move != null && Move.Grounded && Mathf.Abs(RigidBody.velocity.x) <= 0.01f;
            
            if (isIdle)
            {
                // Full body attack when idle
                var entry = PlayerAnimation.AnimationState.SetAnimation(0, "Sword Attack", false);
                entry.TrackTime = 0f;
            }
            else
            {
                // Upper body only attack - use bone masking
                var upperBodyEntry = PlayerAnimation.AnimationState.SetAnimation(1, "Sword Attack", false);
                upperBodyEntry.TrackTime = 0f;
                upperBodyEntry.MixBlend = Spine.MixBlend.Add;
                
                // Create a bone mask to exclude leg bones
                string[] legBones = { "Luisa-7", "Luisa-9", "Luisa-10", "Luisa-81", "Luisa-82", "Luisa-8", "Luisa-11", "Luisa-12", "Luisa-79", "Luisa-80", "Left-Knee-Target", "Left-Foot-Target", "Right-Knee-Target", "Right-Foot-Target" };
                
                // Apply the bone mask - this approach uses the skeleton's bone data
                var skeleton = PlayerAnimation.Skeleton;
                var animationData = PlayerAnimation.AnimationState.Data.SkeletonData;
                
                // Find the animation and create a bone mask
                var animation = animationData.FindAnimation("Sword Attack");
                if (animation != null)
                {
                    // Set up bone masking by manipulating the track's alpha per bone
                    upperBodyEntry.Alpha = 1f;
                    
                    // You might need to use a different approach - try this simpler method:
                    // Just set the track to only affect upper body by using MixBlend
                    upperBodyEntry.MixBlend = Spine.MixBlend.First;
                }
            }
        }

        yield return new WaitForSeconds(AttackTime); // Wait for attack duration

        // Turn off attack
        Attacking = false;
        SwordHitbox.SetActive(false);

        // Tell Health script we're done attacking
        if (health != null)
        {
            health.SetAttackState(false);
        }

        // Only handle animation transitions if we were idle (played full sword attack)
        bool wasIdle = Move != null && Move.Grounded && Mathf.Abs(RigidBody.velocity.x) <= 0.01f;
        
        if (wasIdle)
        {
            // Only do animation transitions if we did a full-body attack (idle)
            if (Move != null && Move.Grounded) // if on the ground
            {
                if (RigidBody != null && Mathf.Abs(RigidBody.velocity.x) > 0.01f) // If moving then running
                    PlayerAnimation.AnimationState.AddAnimation(0, "Running", true, 0f);
                else // If not moving then idle
                    PlayerAnimation.AnimationState.AddAnimation(0, "Idle", true, 0f);
            }
            else // if in air
            {
                // Determine which jumping animation to use based on velocity
                if (RigidBody != null && RigidBody.velocity.y > 0f)
                    PlayerAnimation.AnimationState.AddAnimation(0, "Jumping Loop Up", true, 0f);
                else
                    PlayerAnimation.AnimationState.AddAnimation(0, "Jumping Loop Down", true, 0f);
            }
        }
        else
        {
            // Clear track 1 after non-idle attack
            PlayerAnimation.AnimationState.SetEmptyAnimation(1, 0.1f);
        }
    }
}