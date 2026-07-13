using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

public class Move : MonoBehaviour
{
    public float Speed = 5f; // Speed of the character
    public float JumpForce = 5f; // Maximum jump force if W is held
    public float JumpHoldForce = 2f; // Extra force applied while holding the jump key (W)
    public float JumpHoldLength = 0.2f; // Maximum length of time the player can hold the jump key (W)
    private float JumpTimer; // Timer for how long the player gets the initial jump boost
    private bool JumpHeld = false; // Boolean tracking all the time if jump key (W) is held by player
    private bool JumpFirstPressed = false; // Boolean that only activates when jump key (W) is first pressed
    private float DefaultUnityGravity; // Store the default gravity scale
    private Rigidbody2D RigidBody; // Reference to the Rigidbody2D Unity physics component
    public bool Grounded = true; // Checks if the player character is on the ground
    public bool LockMovement = false; // Lock movement of character momentarily after spawn
    public bool LoRInputReleased = true; // Track if left or right input has been let go by the player
    public float PreviousLoRInput = 0f; // Store previous left or right input (-1 left, 1 right, 0 none)
    public float AirWalkTime = 1.0f; // Time in seconds you can air walk
    public float AirWalkTimer = 0f; // Timer to track how long the player has been air walking
    private bool AirWalkStarted = false; // Track if air walk has started this frame
    private bool OverrideVerticalVelocity = false; // Controls whether vertical velocity is overridden
    private bool WasGrounded = true; // Track previous grounded state for animation transitions
    public Transform AttackHitbox; // Position, rotation and scale of the attack hitbox game object
    public bool OnDamagingSurface = false; // Check if the player is on a damaging surface like spikes
    public Transform GroundCheck; // Position, rotation and scale of ground check game object
    public float GroundCheckRadius = 0.1f; // Radius for ground check game object
    public LayerMask Ground; // Layer mask to identify ground
    float HitboxOffset = 1f; // DSistance of attack hitbox from player centre
    private float PreviousXSpeed = 0f; // Track previous horizontal speed for animation transitions
    private float AirWalkBlockTimer = 0f; // Timer to block air walk after use while still in air
    public PlayerAttack PlayerAttack; // The player's attack set in the Inspector
    public SkeletonAnimation PlayerAnimation; // The player's spine game object set in the Inspector
    public bool EnableWallClimb = true; // Enable wall jumping and wall sliding
    public Transform LeftWallCheck; // Left wall check position, rotation and scale
    public Transform RightWallCheck; // Right wall check position, rotation and scale
    public float WallCheckRadius = 0.1f; // Radius of left and right wall check game objects
    public LayerMask Wall; // Layer mask to identify walls
    public float WallSlideSpeed = 1.5f; // Speed of wall slide
    private bool TouchingWall = false; // Activates if player is touching a wall
    private bool WallSliding = false; // Activates if player is currently wall sliding
    private bool TouchingLeftWall = false; // Activates if player is touching wall on left side
    private bool TouchingRightWall = false; // Activates if player is touching wall on right side
    private float WallJumpLockoutTimer = 0f; // Stops wall sliding or wall jumping for a short time after wall jump (so you can't spam wall jumps)
    public float WallJumpLockoutTime = 0.15f; // Time in seconds to lock out wall sliding or wall jumping after a wall jump
    private float WallJumpControlLockTimer = 0f; // Timer to lock horizontal control after wall jump to look more natural and prevent immediate direction change
    public float WallJumpControlLockTime = 0.3f; // Time that horizontal movement is locked after wall jump
    private bool WallJumpStart = false; // Activates if player starts a wall jump
    private float WallJumpDirection = 0f; // Direction of the wall jump (1 = right, -1 = left)
    private float WallJumpXSpeed = 0f; // Current horizontal speed in wall jump
    private float WallJumpSmoothSpeed = 0f; // tracks velocity for SmoothDamp (creates smooth slowing)
    private float IdleVariationTimer = 0f; // Timer for idle variation checks
    public float IdleVariationCheckInterval = 2f; // How often to check for idle variations (in seconds)
    public float IdleBlinkChance = 0.15f; // 15% chance for Idle Blink
    public float IdleLookChance = 0.10f; // 10% chance for Idle Look
    private bool PlayingIdleVariation = false; // Track if we're currently playing a variation

    [Header("Controller Input")]
    public bool useControllerInput = false;
    public SerialControllerManager serialController;

    // Controller input tracking
    private float controllerHorizontal = 0f;
    private bool controllerJumpPressed = false;

    private bool controllerSpacePressed = false;

    // Start is called before the first frame update
    void Start()
    {

        if (PlayerAnimation == null) // Check if PlayerAnimation is assigned in the inspector
        {
            Debug.LogError("SkeletonAnimation is not assigned!");
        }
        else // If assigned set default animation to idle
        {
            PlayerAnimation.AnimationState.SetAnimation(0, "Idle", true);
        }
        Application.targetFrameRate = 120; // Set target frame rate to 120 FPS for smoother gameplay
        RigidBody = GetComponent<Rigidbody2D>(); // Get the Rigidbody2D component attached to this GameObject (safety net)
        DefaultUnityGravity = RigidBody.gravityScale; // Store the original gravity scale
        if (RigidBody == null)
        {
            Debug.LogError("Rigidbody2D is not assigned or not found on this GameObject!"); // Debug log for easy debugging
        }
        // Controller event cleanup removed - events no longer exist
    }

    void Update()
    {
        // TOGGLE INPUT MODE WITH TAB KEY
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            useControllerInput = !useControllerInput;
            Debug.Log("Input mode switched to: " + (useControllerInput ? "Controller" : "Keyboard"));
        }
        
        // UNIFIED INPUT SYSTEM - Get input from controller OR keyboard
        bool jumpPressed = false;
        bool jumpHeld = false;
        bool spacePressed = false;
        
        if (useControllerInput && serialController != null && serialController.isConnected)
        {
            // Use controller input
            jumpPressed = controllerJumpPressed;
            jumpHeld = serialController.jumpButton;
            spacePressed = controllerSpacePressed || serialController.interactButton;
            
            // Reset frame-based inputs
            controllerJumpPressed = false;
            controllerSpacePressed = false;
        }
        else
        {
            // Use keyboard input
            jumpPressed = Input.GetKeyDown(KeyCode.W);
            jumpHeld = Input.GetKey(KeyCode.W);
            spacePressed = Input.GetKey(KeyCode.Space);
        }

        // Jump input logic
        if (jumpPressed && Grounded) // Normal jump
        {
            JumpFirstPressed = true;
        }
        JumpHeld = jumpHeld; // Holding W activates JumpHeld

        // Air walk logic
        AirWalkStarted = spacePressed && AirWalkTimer < AirWalkTime && AirWalkBlockTimer <= 0f; // Start air walk if space is held, and timer not expired
        if (AirWalkStarted)
        {
            AirWalkTimer += Time.deltaTime; // Set timer to increase while air walking
        }

        // Air walk block timer logic
        if (AirWalkBlockTimer > 0f) // Counts down air walk block timer
        {
            AirWalkBlockTimer -= Time.deltaTime;
        } 

        // Respawn in safe position logic
        if (Grounded && !OnDamagingSurface && Mathf.Abs(RigidBody.velocity.y) < 0.01f)
        {
            bool GroundBelow = Physics2D.OverlapCircle(GroundCheck.position, GroundCheckRadius, Ground); // Checks for ground below and says yes or no
            if (GroundBelow)
            {
                GetComponent<Health>().LastSafePosition = transform.position; // Update and save last safe position every frame on solid ground
            }
        }

        // Wall sliding logic
        if (EnableWallClimb && WallJumpLockoutTimer <= 0f) // Only check for wall sliding if wall climb is enabled and not in lockout
        {
            TouchingLeftWall = Physics2D.OverlapCircle(LeftWallCheck.position, WallCheckRadius, Wall); // Check for wall on left side
            TouchingRightWall = Physics2D.OverlapCircle(RightWallCheck.position, WallCheckRadius, Wall); // Check for wall on right side
            TouchingWall = (TouchingLeftWall || TouchingRightWall) && !Grounded && RigidBody.velocity.y < 0; // Check if touching wall on either side if not grounded
            WallSliding = TouchingWall; // Enable wall sliding only if touching a wall
        }
        else
        {
            WallSliding = false; // Not wall sliding
        }

        // Wall jump logic
        if (EnableWallClimb && WallSliding && jumpPressed) // Wall jump
        {
            WallJumpDirection = TouchingLeftWall ? 1 : -1; // Determine jump direction based on which wall is being touched (left or right)
            WallJumpStart = true;
        }

        // Wall jump lockout timer logic
        if (WallJumpLockoutTimer > 0f) // Sets wall jump lockout count down
        {
            WallJumpLockoutTimer -= Time.deltaTime; // Counts down timer
            if (WallJumpLockoutTimer < 0f) // Makes sure the timer doesn't go below 0
            {
                WallJumpLockoutTimer = 0f; 
            }
        }

        // Animation state management logic
        if (PlayerAttack == null || !PlayerAttack.Attacking) // Only change animations if not attacking
        {
            // Play jumpset off first when leaving the ground
            if (!Grounded && WasGrounded)
            {
                PlayerAnimation.AnimationState.SetAnimation(0, "Jumping Set Off", false);
                PlayerAnimation.AnimationState.AddAnimation(0, "Jumping Loop Up", true, 0f);
                // Clearing the track 1 animation because it was a big hastle that took me all morning to fix, and a very simple fix
                // Basically the track 1 animation was still playing after a jump finished landing, causing weird animation overlaps
                PlayerAnimation.AnimationState.SetEmptyAnimation(1, 0f);
            }

            // Handle jumping animations based on vertical velocity (only for loop animations)
            if (!Grounded && PlayerAnimation.AnimationName == "Jumping Loop Up" || PlayerAnimation.AnimationName == "Jumping Loop Down")
            {
                // Only switch between loop animations, never interrupt Jumping Set Off
                if (RigidBody.velocity.y > 0f && PlayerAnimation.AnimationName != "Jumping Loop Up")
                {
                    // I was debugging to find the cause of the issue where the running animation was still playing when jumping
                    // It was such a hassle cause this debug log didnt show so i thought it was an issue here but it wasn't, its just broken here but the animations still work, weird
                    Debug.Log("SWITCHING TO JUMPING LOOP UP - Velocity.y: " + RigidBody.velocity.y + ", Current Animation: " + PlayerAnimation.AnimationName);
                    PlayerAnimation.AnimationState.SetAnimation(0, "Jumping Loop Up", true);
                }
                else if (RigidBody.velocity.y <= 0f && PlayerAnimation.AnimationName != "Jumping Loop Down")
                {
                    Debug.Log("SWITCHING TO JUMPING LOOP DOWN - Velocity.y: " + RigidBody.velocity.y + ", Current Animation: " + PlayerAnimation.AnimationName);
                    PlayerAnimation.AnimationState.SetAnimation(0, "Jumping Loop Down", true);
                }
            }

            // Only play Idle/Running when landing
            if (Grounded && !WasGrounded)
            {
                if (Mathf.Abs(RigidBody.velocity.x) > 0.01f) // If moving while landing
                {
                    // Play both landing and running simultaneously on different tracks
                    PlayerAnimation.AnimationState.SetAnimation(0, "Jumping Land", false);
                    PlayerAnimation.AnimationState.SetAnimation(1, "Running", true); // Track 1 for leg movement

                    // Queue running on main track after landing finishes
                    PlayerAnimation.AnimationState.AddAnimation(0, "Running", true, 0f);
                    // CLEAR Track 1 after landing animation finishes
                    PlayerAnimation.AnimationState.AddEmptyAnimation(1, 0f, PlayerAnimation.AnimationState.GetCurrent(0).AnimationTime);
                }
                else // If not moving while landing
                {
                    PlayerAnimation.AnimationState.SetAnimation(0, "Jumping Land", false);
                    PlayerAnimation.AnimationState.AddAnimation(0, "Idle", true, 0f);
                    PlayerAnimation.AnimationState.SetEmptyAnimation(1, 0f); // Clear Track 1 immediately
                }
            }   

            // Play Running when starting moving on ground (but not if we just started a jump animation)
            if (Grounded && Mathf.Abs(RigidBody.velocity.x) > 0.01f && Mathf.Abs(PreviousXSpeed) <= 0.01f && 
                PlayerAnimation.AnimationName != "Jumping Land" && 
                PlayerAnimation.AnimationName != "Jumping Set Off" && 
                PlayerAnimation.AnimationName != "Jumping Loop Up")
            {
                Debug.Log("STARTING RUNNING - Current Animation: " + PlayerAnimation.AnimationName + ", Velocity.x: " + RigidBody.velocity.x);
                PlayerAnimation.AnimationState.SetAnimation(0, "Running", true);
                PlayerAnimation.AnimationState.SetEmptyAnimation(1, 0f); // Clear Track 1
            }

            // Play Idle when stopping on ground
            if (Grounded && Mathf.Abs(RigidBody.velocity.x) <= 0.01f && Mathf.Abs(PreviousXSpeed) > 0.01f && PlayerAnimation.AnimationName != "Jumping Land")
            {
                Debug.Log("SWITCHING TO IDLE - Current Animation: " + PlayerAnimation.AnimationName + ", Velocity.x: " + RigidBody.velocity.x);
                PlayerAnimation.AnimationState.SetAnimation(0, "Idle", true);
                PlayerAnimation.AnimationState.SetEmptyAnimation(1, 0f); // Clear Track 1
                IdleVariationTimer = 0f; // Reset timer when starting idle
                PlayingIdleVariation = false;
            }

            // Air-walking animations
            if (!Grounded && spacePressed && AirWalkTimer < AirWalkTime)
            {
                if (Mathf.Abs(RigidBody.velocity.x) > 0.01f) // If moving horizontally while air-walking play running
                {
                    if (PlayerAnimation.AnimationName != "Running") // This prevents restarting the same animation every frame
                    {
                        PlayerAnimation.AnimationState.SetAnimation(0, "Running", true);
                        PlayerAnimation.AnimationState.SetEmptyAnimation(1, 0f); // Clear Track 1
                    }
                }
                else // Otherwise play idle
                {
                    if (PlayerAnimation.AnimationName != "Idle") // This prevents restarting the same animation every frame
                    {
                        PlayerAnimation.AnimationState.SetAnimation(0, "Idle", true);
                        PlayerAnimation.AnimationState.SetEmptyAnimation(1, 0f); // Clear Track 1
                    }
                }
            }
            // If air-walking just ended because timer expired or Space released, return to Jumping Loop
            else if (!Grounded && (AirWalkTimer >= AirWalkTime || !spacePressed) &&
            (PlayerAnimation.AnimationName == "Running" || PlayerAnimation.AnimationName == "Idle"))
            {
                if (RigidBody.velocity.y > 0f)
                {
                    PlayerAnimation.AnimationState.SetAnimation(0, "Jumping Loop Up", true);
                }
                else
                {
                    PlayerAnimation.AnimationState.SetAnimation(0, "Jumping Loop Down", true);
                    }
            }
            // various idle animtions logic
            if (Grounded && Mathf.Abs(RigidBody.velocity.x) <= 0.01f && 
            (PlayerAnimation.AnimationName == "Idle" || PlayerAnimation.AnimationName == "Idle Blink" || PlayerAnimation.AnimationName == "Idle Look") &&
            (PlayerAttack == null || !PlayerAttack.Attacking))
            {
                IdleVariationTimer += Time.deltaTime;
    
                // Check for idle variations at intervals
                if (IdleVariationTimer >= IdleVariationCheckInterval && !PlayingIdleVariation)
                {
                    float randomValue = Random.Range(0f, 1f);
        
                    if (randomValue <= IdleBlinkChance) // Play Idle Blink
                    {
                        Debug.Log("Playing Idle Blink variation");
                        var entry = PlayerAnimation.AnimationState.SetAnimation(0, "Idle Blink", false);
                        PlayerAnimation.AnimationState.AddAnimation(0, "Idle", true, 0f); // Return to normal idle
                        PlayingIdleVariation = true;
                    }
                    else if (randomValue <= IdleBlinkChance + IdleLookChance) // Play Idle Look
                    {
                        Debug.Log("Playing Idle Look variation");
                        var entry = PlayerAnimation.AnimationState.SetAnimation(0, "Idle Look", false);
                        PlayerAnimation.AnimationState.AddAnimation(0, "Idle", true, 0f); // Return to normal idle
                        PlayingIdleVariation = true;
                    }
        
                    IdleVariationTimer = 0f; // Reset timer
                }
    
                // Reset PlayingIdleVariation when back to normal idle
                if (PlayerAnimation.AnimationName == "Idle" && PlayingIdleVariation)
                {
                    PlayingIdleVariation = false;
                }
            }
            else
            {
                // Reset idle variation timer when not idle
                IdleVariationTimer = 0f;
                PlayingIdleVariation = false;
            }

            // --- FLIP CHARACTER BASED ON MOVEMENT DIRECTION ---
            if (Mathf.Abs(RigidBody.velocity.x) > 0.01f)
            {
                if (RigidBody.velocity.x > 0) // Moving right
                    PlayerAnimation.skeleton.ScaleX = Mathf.Abs(PlayerAnimation.skeleton.ScaleX); // Face right
                else
                    PlayerAnimation.skeleton.ScaleX = -Mathf.Abs(PlayerAnimation.skeleton.ScaleX); // Face left
            }
            float groundCheckOffsetX = Mathf.Abs(GroundCheck.localPosition.x); // Store original X position of ground check
            float groundCheckOffsetY = GroundCheck.localPosition.y; // Store original Y position of ground check

            if (PlayerAnimation.skeleton.ScaleX > 0) // keep ground check in front of player no matter which way they are facing
            {
                GroundCheck.localPosition = new Vector3(groundCheckOffsetX, groundCheckOffsetY, 0f); // right side
            }
            else
            {
                GroundCheck.localPosition = new Vector3(-groundCheckOffsetX, groundCheckOffsetY, 0f); // left side
            }
        }
        // Hitbox offset logic
        if (PlayerAnimation.skeleton.ScaleX > 0) // keep attack hitbox in front of player no matter which way they are facing
        {
            AttackHitbox.localPosition = new Vector3(Mathf.Abs(HitboxOffset), 0f, 0f); // right side
        }
        else
        {
            AttackHitbox.localPosition = new Vector3(-Mathf.Abs(HitboxOffset), 0f, 0f); // left side
        }

        // Set the "was" or "previous" states for next frame
        WasGrounded = Grounded;
        PreviousXSpeed = RigidBody.velocity.x;
    }

    // Physics stuff needs consistent timing so goes in FixedUpdate as it runs at fixed intervals independant of frame rate
    void FixedUpdate()
    {
        // UNIFIED INPUT - Get horizontal input from controller OR keyboard
        float horizontalInput = 0f;
        if (useControllerInput && serialController != null && serialController.isConnected)
        {
            horizontalInput = controllerHorizontal;
        }
        else
        {
            horizontalInput = Input.GetAxis("Horizontal");
        }
        
        OverrideVerticalVelocity = false; // Reset override vertical velocity each physics frame

        // Jump physics logic
        if (JumpFirstPressed)
        {
            RigidBody.velocity = new Vector2(RigidBody.velocity.x, 0); // Reset vertical velocity at start of jump for consistent jumps
            RigidBody.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);
            JumpTimer = JumpHoldLength; // Set timer
            Grounded = false;
            JumpFirstPressed = false; // Reset jump first pressed at end
        }

        // Variable jump height logic
        if (RigidBody.velocity.y > 0 && JumpHeld && JumpTimer > 0f) 
        {
            RigidBody.gravityScale = DefaultUnityGravity * 0.6f;
            RigidBody.AddForce(Vector2.up * JumpHoldForce, ForceMode2D.Force); // Apply extra upward force while W is held for variable jump height
            JumpTimer -= Time.fixedDeltaTime; // Count down JumpTimer
        }
        else if (RigidBody.velocity.y > 0) // if W released or timer ran out
        {
            if (!JumpHeld)
            {
                RigidBody.gravityScale = DefaultUnityGravity * 3.0f; // Always fast fall if W is released early as gravity scale higher
            }
            else if (JumpTimer <= 0f)
            {
                RigidBody.gravityScale = DefaultUnityGravity * 1.2f; // Fairly normal gravity if W held and JumpTimer ran out
            }
        }
        else if (RigidBody.velocity.y < 0)
        {
            RigidBody.gravityScale = DefaultUnityGravity * 1.8f; // Faster fall when descending
        }
        else
        {
            RigidBody.gravityScale = DefaultUnityGravity;
        }

       if (EnableWallClimb && WallSliding && WallJumpLockoutTimer <= 0f) 
        {
            if (RigidBody.velocity.y < -WallSlideSpeed)
            {       
                RigidBody.velocity = new Vector2(RigidBody.velocity.x, -WallSlideSpeed); // Set downward speed on wall slide to wall slide speed
            }
        }

        // Air walk physics logic
        if (AirWalkStarted)
        {
            RigidBody.gravityScale = 0f; // Disable gravity to make it look like character is air walking
            RigidBody.velocity = new Vector2(horizontalInput * Speed, 0f); // Set horizontal velocity as input and vertical velocity to zero
            OverrideVerticalVelocity = true; // Override vertical velocity so normal jump or fall physics don't apply
        }

        if (!OverrideVerticalVelocity && WallJumpControlLockTimer <= 0f)
        {
            RigidBody.velocity = new Vector2(horizontalInput * Speed, RigidBody.velocity.y); // Normal horizontal movement
        }
        
        // Wall jump physics logic
        if (WallJumpStart)
        {
            WallJumpXSpeed = WallJumpDirection * Speed * 1.2f; // Set initial horizontal velocity for SmoothDamp
            WallJumpSmoothSpeed = 0f; // Reset SmoothDamp velocity as still may be set from previous wall jump
            RigidBody.velocity = new Vector2(WallJumpXSpeed, 0); // Reset vertical velocity at start of wall jump for consistent jumps
            RigidBody.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);
            JumpTimer = JumpHoldLength; // Set timer
            WallSliding = false;
            WallJumpLockoutTimer = WallJumpLockoutTime;
            WallJumpControlLockTimer = WallJumpControlLockTime;
            WallJumpStart = false;
        }

        // Wall jump timer logic (controls physics during wall jump)
        if (WallJumpControlLockTimer > 0f) // If control timer active
        {
            WallJumpControlLockTimer -= Time.fixedDeltaTime; // Count down timer
            WallJumpXSpeed = Mathf.SmoothDamp(WallJumpXSpeed, 0f, ref WallJumpSmoothSpeed, WallJumpControlLockTime); // Smoothly lower horizontal velocity toward zero for a natural arc
            RigidBody.velocity = new Vector2(WallJumpXSpeed, RigidBody.velocity.y); // Apply the lowered velocity
            return;
        }
        if (LockMovement) // If movement is locked after respawn 
        {
            // Wait for the player to release all horizontal input after respawn to stop them walking off cliffs or something like that
            if (Mathf.Abs(horizontalInput) < 0.01f)
            {
                LoRInputReleased = true;
            }
            // Unlock movement on the first button press after release
            if (LoRInputReleased && Mathf.Abs(horizontalInput) > 0.01f)
            {
                LockMovement = false;
                LoRInputReleased = false; // Reset for next time
            }
            else  // Keep movement locked
            {
                horizontalInput = 0f;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the collision is with the ground
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // Set grounded to true on first contact with ground
            if (contact.normal.y > 0.5f)
            {
                Grounded = true;
                AirWalkTimer = 0f;
                return;
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Check if the collision is with the ground
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // Stay grounded if still in contact with ground
            if (contact.normal.y > 0.5f)
            {
                Grounded = true;
                AirWalkTimer = 0f;
                return;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // Reset Grounded when leaving the ground
        if (collision.gameObject.CompareTag("Ground"))
        {
            Grounded = false;
        }
    }

    void OnDestroy()
    {
        // Event cleanup removed - events no longer exist
    }

}