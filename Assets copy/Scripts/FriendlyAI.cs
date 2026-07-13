using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FriendlyAI : MonoBehaviour
{
    private Vector3 SpawnPosition; // AI's spawn position
    public float Speed = 2f; // The speed the NPC moves when walking around
    public float Distance = 3f; // Distance the NPC can patrol left and right
    private int Direction = 1;  // Direction the NPC is looking (1 = right, -1 = left)
    public float PatrolPauseDuration = 1f; // The amount of time the NPC at patrol edge
    private float PauseAfterPatrolTimer = 0f; // The amount of time left on pause at patrol edge
    private bool Paused = false; // Boolean if NPC is paused at patrol edge or not
    public Transform LeftGroundCheck;   // Ground check for left of the NPC at the foot
    public Transform RightGroundCheck;  // Ground check for right of the NPC at the foot
    public float GroundCheckRadius = 0.1f; // Radius of ground check circle
    public LayerMask Ground; // Layer for ground
    public float LadderClimbOffset = 1f; // How far to the right of ladder tp climb because the NPC doesnt climb in the center of the ladder
    public LayerMask Wall; // Layer for wall
    public float WallDetectDistance = 2f; // Distance to sense walls infront of it for jumping
    public float WallJumpHeight = 3f; // the highest wall that could be jumped
    public float WallJumpArcHeight = 3f; // How high the arc goes over walls so that it looks more natural
    public float WallDropDetectDistance = 5f; // How far down to check for ground when dropping off a cliff
    public float WallDropForwardDistance = 1f; // How far forward to jump when dropping off a cliff
    public float WallDropJumpHeight = 1f; // How high to jump when dropping down cliffs (not much)
    public float GapJumpDistance = 5f; // Maximum distance to check for ground when jumping gaps
    public float GapJumpHeight = 2f; // How high the NPC jumps over gaps
    public float GapJumpSpeed = 3f; // Speed the NPC moves while jumping over gaps
    private bool GapJumping = false; // Boolean if the NPC is jumping over a gap
    public LayerMask Ladder; // Layer for ladders
    public float LadderCheckDistance = 5f; // How far to look for ladders (50 units is used later but this variable needs to be 5 so it works smoothly)
    public float ClimbSpeed = 1.5f; // Climbing speed of NPCs
    public bool Climbing = false; // Boolean if the NPC is ckimbing
    public Transform CurrentLadder = null; // ladder being currently climbed
    public Transform LastLadder = null; // Last ladder used for connection-based pathfinding bonuses and perminent exclusions
    public bool ForceClimb = false; // When true the NPC is forced to complete the climb it started, this is due to always jumping off the ladder straight away on the final ladder
    public List<Transform> ExcludedLadders = new List<Transform>(); // List of ladders to exclude from the current pathfind to prevent the NPC from using the same one multiple times
    public Vector3 PathfindTarget; // The target destination (a torch in the current case)
    public float ChatDetectDistance = 3f; // Distance to detect other NPCs to start a chat (shown in gizmos)
    public float ChatDuration = 3f; // How long the chat is
    public float ChatCooldown = 5f; // Cooldown to prevent infinite chatting
    public bool CanStartChats = true; // A flag for whether an NPC can start a chat
    private bool Chatting = false; // Boolean for if an NPC is chatting
    private FriendlyAI OtherChatter = null; // says which NPC one AI is chatting with
    private float ChatTime = 0f; // tracks current chat time
    private float ChatCooldownTime = 0f; // Tracks cooldown between chats
    public GameObject SpeechBubble; // Speech bubble game object to visualy show the chatting
    private float YPosition = 0f; // Tracks y position of NPC to detect target
    public AIState State = AIState.Patrolling; // current state of AI, starts at patroling
    public Vector3 Target; // Where the AI wants to go (torch)
    private List<Vector3> TargetPath = new List<Vector3>(); // Waypoints between ladders, jump points and straight paths to get to destination
    private int TargetWaypointIndex = 0; // Index of all smaller waypoints to get to target
    // AI decision making
    public enum AIState
    {
        Patrolling,
        Pathing,
        Climbing,
        Idle,
        Chatting
    }

    // Start is called before the first frame update
    void Start()
    {
        // Set all the NPC's starting variables
        SpawnPosition = transform.position;
        YPosition = transform.position.y;
        Paused = false;
        State = AIState.Patrolling;
    }

    // Update is called once per frame
   void Update()
    {
        // Check for nearby NPCs to chat with if not busy and allowed
        if (CanStartChats && !Chatting && State == AIState.Patrolling)
        {
            FindOtherNPCs();
        }
    
        // Skip normal movement logic while jumping so it doesnt just fall
        if (GapJumping) return;
    
        // Handle all AI states
        switch (State)
        {
            case AIState.Patrolling:
                if (!Climbing && !Chatting) HandlePatrolling(); // Don't patrol while climbing or chatting
                break;
            case AIState.Pathing:
                if (!Climbing && !Chatting) HandlePathing(); // Don't path while climbing or chatting
                break;
            case AIState.Climbing:
                HandleClimbing(); // Always handle climbing when in climbing state
                break;
            case AIState.Chatting:
                HandleChatting(); // Handle chat state
                break;
            case AIState.Idle:
                // Do nothing when idle
                break;
        }
    }

    // Method to handle climbing logic when in climbing state
    private void HandleClimbing()
    {
        // safety check
        if (CurrentLadder == null)
        {
            Climbing = false;
            ForceClimb = false;
            State = AIState.Pathing;
            SetEndGoal(Target);
            return;
        }
        // Check if all wayoints have been reached
        if (TargetWaypointIndex >= TargetPath.Count)
        {
            FinishClimbing();
            return;
        }
        // 
        Vector3 TargetWaypoint = TargetPath[TargetWaypointIndex];
        float LadderX = CurrentLadder.position.x + LadderClimbOffset;
    
        // Check if we've reached the current climbing waypoint (with some leniency)
        float WaypointDistance = Vector2.Distance(transform.position, TargetWaypoint);
        if (WaypointDistance < 0.5f)
        {
            TargetWaypointIndex++;
        
            if (TargetWaypointIndex >= TargetPath.Count)
            {
                FinishClimbing();
            }
        return;
        }
        
    
        // move to ladders x position first
        if (Mathf.Abs(transform.position.x - LadderX) > 0.3f)
        {
            float LadderDirection = LadderX > transform.position.x ? 1 : -1;
            SetDirection((int)LadderDirection);
        
            // Check ground ahead for normal movement
            Transform CurrentGroundCheck = CorrectGroundCheck();
            bool GroundAhead = Physics2D.OverlapCircle(CurrentGroundCheck.position, GroundCheckRadius, Ground);
        
            // Add wall detection for climbing state
            Vector3 WallDetectPosition = transform.position + Vector3.right * LadderDirection * 0.5f;
            bool DetectWall = Physics2D.OverlapCircle(WallDetectPosition, 0.3f, Wall);
            // debug logs for debugging climbing problems
            if (DetectWall)
            {
                if (WallJump())
                {
                    return;
                }
                else
                {
                    Climbing = false;
                    ForceClimb = false;
                    State = AIState.Pathing;
                    SetEndGoal(Target);
                    return;
                }
            }
        
            // if no ground ahead then just across gap, if no gap then drop down, if no drop then give up
            if (!GroundAhead)
            {
                if (StartWallDropJump())
                {
                    return;
                }
            
                
                float XDistanceToLadder = Mathf.Abs(transform.position.x - LadderX); // distance to ladder in x direction
                // extra safety check even though small gaps aren't in my obstacles, just for future
                if (XDistanceToLadder <= GapJumpDistance)
                {
                    Vector3 LadderJumpTarget = new Vector3(LadderX, transform.position.y, transform.position.z);
                    StartCoroutine(JumpToPoint(LadderJumpTarget));
                    return;
                }
                else
                {
                    Vector3 LandPoint = FindLandPoint();
                    if (LandPoint != Vector3.zero)
                    {
                        StartCoroutine(JumpToPoint(LandPoint));
                        return;
                    }
                    else
                    {
                        Climbing = false;
                        ForceClimb = false;
                        State = AIState.Pathing;
                        SetEndGoal(Target);
                        return;
                    }
                }
            }
            else
            {
                if (!NPCBlocking())
                {
                    transform.Translate(Vector2.right * LadderDirection * Speed * Time.deltaTime);
                    Debug.Log("Moving to ladder horizontally - Target X: " + LadderX + " Current X: " + transform.position.x);
                }
                else
                {
                    Debug.Log(gameObject.name + " waiting for NPC to move away from ladder path");
                }
            }
        }
        // Force the climbing to never be interrupted because the NPC was jumping straight off the last ladder after getting on, now it works though
        else
        {
            if (!ForceClimb)
            {
                ForceClimb = true;
                Debug.Log("FORCED CLIMBING ACTIVATED - NPC must complete vertical climb");
            }
        
            // FORCED VERTICAL CLIMBING: NPC cannot be interrupted during vertical movement (capitals because i was annoyed)
            Debug.Log("FORCED VERTICAL CLIMBING: Moving to target without interruption");
        
            // Turn off gravity during climb
            Rigidbody2D RigidBody = GetComponent<Rigidbody2D>();
            if (RigidBody != null)
            {
                RigidBody.gravityScale = 0f;
                RigidBody.velocity = Vector2.zero;
            }
        
            // Move vertically toward the waypoint
            if (Mathf.Abs(transform.position.y - TargetWaypoint.y) > 0.2f)
            {
                float ClimbPhysics = TargetWaypoint.y > transform.position.y ? 1 : -1;
                transform.Translate(Vector2.up * ClimbPhysics * ClimbSpeed * Time.deltaTime);
                Debug.Log("FORCED VERTICAL CLIMB: " + (ClimbPhysics > 0 ? "up" : "down") + " - Target Y: " + TargetWaypoint.y + " Current Y: " + transform.position.y);
            }
            else
            {
                transform.position = new Vector3(LadderX, TargetWaypoint.y, transform.position.z);
                Debug.Log("Snapped to final waypoint: " + TargetWaypoint);
            }
        }
    }

    // Handle patrolling logic
    private void HandlePatrolling()
    {
        // check for obstacles infront of NPC and avoid them
        if (!Paused)
        {
            Transform currentGroundCheck = CorrectGroundCheck();
            bool PatrolGroundAhead = Physics2D.OverlapCircle(currentGroundCheck.position, GroundCheckRadius, Ground);
            float PatrolLeftEdge = SpawnPosition.x - Distance;
            float PatrolRightEdge = SpawnPosition.x + Distance;
            bool AtPatrolEdge = (Direction == 1 && transform.position.x >= PatrolRightEdge) || (Direction == -1 && transform.position.x <= PatrolLeftEdge);
        
            if (PatrolGroundAhead && !AtPatrolEdge)
            {
                Vector3 wallCheckPosition = transform.position + Vector3.right * Direction * 0.5f;
                bool wallAhead = Physics2D.OverlapCircle(wallCheckPosition, 0.3f, Wall);
            
                if (wallAhead)
                {
                    if (WallJump())
                    {
                        // If wall jumping then skip rest of logic
                        return;
                    }
                    else
                    {
                        // If no jump point on wall then treat as edge
                        Paused = true;
                        PauseAfterPatrolTimer = PatrolPauseDuration;
                        return;
                    }
                }
            
                // Make sure that the NPCs arent blocked by each other when walking around
                if (!NPCBlocking())
                {
                    transform.Translate(Vector2.right * Direction * Speed * Time.deltaTime);
                }
                else
                {
                    // pause so that npc moves
                    if (!Paused)
                    {
                        Paused = true;
                        PauseAfterPatrolTimer = 1f; // Short pause to let other NPC move
                        Debug.Log(gameObject.name + " pausing because of NPC collision");
                    }
                }
            }
           else if (!PatrolGroundAhead && !AtPatrolEdge)
        {
            // Try wall drop jump first, then wall jump points, then regulr gap jumping
            if (StartWallDropJump())
            {
                return;
            }
    
            // Regular gap jumping logic
            Vector3 landingPoint = FindLandPoint();
            if (landingPoint != Vector3.zero)
            {
                StartCoroutine(JumpToPoint(landingPoint));
            }
            else
            {
                Paused = true;
                PauseAfterPatrolTimer = PatrolPauseDuration;
            }
        }
            else
            {
                Paused = true;
                PauseAfterPatrolTimer = PatrolPauseDuration;
            }
        }
        
        // Pause logic for patrol edges and cliffs
        if (Paused)
        {
            PauseAfterPatrolTimer -= Time.deltaTime;
        
            if (PauseAfterPatrolTimer <= 0f)
            {
                Paused = false;
                SetDirection(-Direction);
                transform.position += Vector3.right * Direction * 0.05f;
            }
        }
    }

    // Handle pathfinding to a destination
    private void HandlePathing()
    {
        if (TargetPath.Count == 0)
        {
            State = AIState.Idle;
            return;
        }
    
        // Get current waypoint
        if (TargetWaypointIndex >= TargetPath.Count)
        {
            Debug.Log("AI reached destination!");
            State = AIState.Idle;
            return;
        }
        // variable for current waypoint and distance to it
        Vector3 CurrentWaypoint = TargetPath[TargetWaypointIndex];
        float WaypointDistance = Vector2.Distance(transform.position, CurrentWaypoint);
    
        // Calculate Y distance to current waypoint
        float YDistance = CurrentWaypoint.y - transform.position.y;
    
        // Handle ladder climbing when there's significant vertical distance (mostly covered by NPCSeek)
        if (Mathf.Abs(YDistance) > 1f)
        {
            State = AIState.Idle;
            return;
        }
    
        // Move towards waypoint
        if (WaypointDistance < 0.5f)
        {
            // Reached waypoint so move to next one
            TargetWaypointIndex++;
        }
        else
        {
            Transform CurrentGroundCheck = CorrectGroundCheck();
            Vector3 WaypointDirection = (CurrentWaypoint - transform.position).normalized;
            SetDirection(WaypointDirection.x > 0 ? 1 : -1);
        
            if (NPCBlocking())
            {
                return; // Skip movement this frame
            }
            Vector3 WallCheckPosition = transform.position + Vector3.right * Direction * 0.5f;
            bool WallAhead = Physics2D.OverlapCircle(WallCheckPosition, 0.3f, Wall);
        
            if (WallAhead)
            {
                if (WallJump())
                {
                    return; // Wall jump initiated
                }
        
                if (StartWallDropJump())
                {
                    return; // Wall drop initiated
                }
            
                Debug.Log("Wall detected but no jump options found - stopping");
                State = AIState.Idle;
                return;
            }

            // Check for ground ahead (gap detection)
            bool GroundAhead = Physics2D.OverlapCircle(CurrentGroundCheck.position, GroundCheckRadius, Ground);
        
            if (!GroundAhead)
            {
                // Try wall drop first for gaps
                if (StartWallDropJump())
                {
                    return;
                }
            
                // Try regular gap jumping
                Vector3 LandPoint = FindLandPoint();
                if (LandPoint != Vector3.zero)
                {
                    StartCoroutine(JumpToPoint(LandPoint));
                    return;
                }
                else
                {
                    State = AIState.Idle;
                    return;
                }
            }
            else
            {
                // Normal movement - ground ahead, no walls
                transform.Translate(Vector2.right * Direction * Speed * Time.deltaTime);
            }
        }
    }

    // Method to look at the correct ground check based on direction incase of gaps or ledges
    private Transform CorrectGroundCheck()
    {
        return Direction == 1 ? RightGroundCheck : LeftGroundCheck;
    }

    // Method to set an end goal for the AI
    public void SetEndGoal(Vector3 Destination)
    {
        Target = Destination;
    
        // Check if this is a different target so we can reset ladder exlusions
        if (Vector3.Distance(PathfindTarget, Destination) > 1f)
        {
            ExcludedLadders.Clear();
            PathfindTarget = Destination;
        }
    
        State = AIState.Pathing;
        PlanPath();
        Debug.Log("AI end goal set to " + Destination);
    }

    // Method to stop NPC and go idle
    public void StopAndIdle()
    {
        State = AIState.Idle;
        Paused = false;
    }

    // Method to resume patrolling
    public void StartPatrolling()
    {
        State = AIState.Patrolling;
        Paused = false;
    }

    // Method to start climbing a nearby ladder
    public void StartClimbing(bool goUp = true)
    {
        // Fallback system in case of error with pathfinding
        Transform ClosestLadder = CurrentLadder; 
        if (ClosestLadder == null)
        {
            // Find the nearest ladder (excluding recently used ones)
            ClosestLadder = FindNearestLadder();
            if (ClosestLadder == null)
            {
                return;
            }
        }
        else
        {
            // Verify the preset ladder is still valid and close enough
            float DistanceToCurrentLadder = Vector3.Distance(transform.position, ClosestLadder.position);
            if (DistanceToCurrentLadder > LadderCheckDistance)
            {
                ClosestLadder = FindNearestLadder();
                if (ClosestLadder == null)
                {
                    Debug.Log("No suitable ladder found nearby");
                    return;
                }
            }
        }
        // Get ladder script
        Ladder LadderScript = ClosestLadder.GetComponent<Ladder>();
    
        // Set up climbing state properly
        CurrentLadder = ClosestLadder;
        Climbing = true;
        ForceClimb = false; 
        State = AIState.Climbing;
    
        // Create path for climbing
        TargetPath.Clear();
        TargetWaypointIndex = 0;
    
        // Determine which ladder endpoint NPC is closest to then go to opposite
        Vector3 TopPosition = LadderScript.FindTopPoint();
        Vector3 BottomPosition = LadderScript.FindBottomPoint();
        float DistanceToTop = Vector3.Distance(transform.position, TopPosition);
        float DistanceToBottom = Vector3.Distance(transform.position, BottomPosition);
    
        bool StartingAtBottom = DistanceToBottom < DistanceToTop;
    
        if (StartingAtBottom)
        {
            // Starting at bottom, so climb up to top
            TargetPath.Add(TopPosition);
        }
        else
        {
            // Vice versa 
            TargetPath.Add(BottomPosition);
        }
    }

// Finish climbing and jump to ladder-specific target if set
private void FinishClimbing()
{
    Climbing = false;
    ForceClimb = false;
    // exclude previously used ladder
    if (CurrentLadder != null && !ExcludedLadders.Contains(CurrentLadder))
    {
        ExcludedLadders.Add(CurrentLadder);
    }
    
    LastLadder = CurrentLadder;
    
    // Re-enable gravity
    Rigidbody2D RigidBody = GetComponent<Rigidbody2D>();
    if (RigidBody != null)
    {
        RigidBody.gravityScale = 1f;
    }
    
    // Check if this current ladder has jump targets before leaving it
    if (CurrentLadder != null)
    {
        Ladder LadderScript = CurrentLadder.GetComponent<Ladder>();
        if (LadderScript != null)
        {
            Vector3 JumpTarget = Vector3.zero;
            
            Vector3 TopPosition = LadderScript.FindTopPoint();
            Vector3 BottomPosition = LadderScript.FindBottomPoint();
            float DistanceToTop = Vector3.Distance(transform.position, TopPosition);
            float DistanceToBottom = Vector3.Distance(transform.position, BottomPosition);
            bool FinishAtTop = DistanceToTop < DistanceToBottom;
            
            if (FinishAtTop)
            {
                JumpTarget = LadderScript.FindTopJumpPoint();
            }
            else
            {
                JumpTarget = LadderScript.FindBottomJumpPoint();
            }
            
            if (JumpTarget != Vector3.zero && Vector3.Distance(transform.position, JumpTarget) > 0.5f)
            {               
                // Clear ladder reference and set state before jumping
                CurrentLadder = null; // Clear ladder reference
                State = AIState.Climbing; // Keep climbing state during jump so JumpToPoint knows to exit it
                
                StartCoroutine(JumpToPoint(JumpTarget));
                return; // Don't change state yet cause the jumptopoint coroutine will do it
            }
        }
    }
    
    // Always clear ladder and go to pathing if no jump
    CurrentLadder = null;
    State = AIState.Pathing;
    SetEndGoal(Target);
}

// Connection-based pathfinding system
private List<Vector3> BuildPath(Vector3 Start, Vector3 Target)
{
    List<Vector3> Path = new List<Vector3>();
    
    // Check if target is at same Y level and if so use direct path
    float YDifference = Mathf.Abs(Target.y - Start.y);
    if (YDifference < 1f)
    {
        Path.Add(Target);
        return Path;
    }
    
    // Find target ladder closest to end goal
    Collider2D[] LadderArray = Physics2D.OverlapCircleAll(Start, 50f, Ladder);
    
    Ladder TargetYLadder = null;
    bool TargetAtTop = false;
    float LadderDistance = Mathf.Infinity;
    
    foreach (Collider2D LadderCollider in LadderArray)
    {
        Ladder ladder = LadderCollider.GetComponent<Ladder>();
        if (ladder == null) continue;
        
        Vector3 TopPosition = ladder.FindTopPoint();
        Vector3 BottomPosition = ladder.FindBottomPoint();
        
        // Find which endpoint is closer to target
        float DistanceToTop = Vector3.Distance(Target, TopPosition);
        float DistanceToBottom = Vector3.Distance(Target, BottomPosition);
        
        float EndPointDistance = Mathf.Min(DistanceToTop, DistanceToBottom);
        
        if (EndPointDistance < LadderDistance)
        {
            LadderDistance = EndPointDistance;
            TargetYLadder = ladder;
            TargetAtTop = (DistanceToTop < DistanceToBottom);
        }
    }
    // If no ladder found then just go direct and hope for the best (usually happens when errors are in the code or something, or its unreachable)
    if (TargetYLadder == null)
    {
        Debug.Log("No ladders found in range, probably a glitch in the code");
        Path.Add(Target);
        return Path;
    }
    
    // Find connection path through ladders from current position to target y level ladder
    List<Ladder> CorrectLadderPath = FindLadderConnections(Start, TargetYLadder, TargetAtTop);
    
    if (CorrectLadderPath == null || CorrectLadderPath.Count == 0)
    {
        Path.Add(Target);
        return Path;
    }
    
    // Build waypoint path through connected ladders (like A* sort of)
    Debug.Log("Building path through " + CorrectLadderPath.Count + " connected ladders");
    
    for (int i = 0; i < CorrectLadderPath.Count; i++)
    {
        Ladder CurrentIterationLadder = CorrectLadderPath[i];
        
        // Determine entry and exit points based on specific endpoint connections
        if (i == 0)
        {
            Vector3 StartPosition = FindClosestLadderPoints(Start, CurrentIterationLadder);
            Path.Add(StartPosition);
            // find exit point to next ladder
            if (i < CorrectLadderPath.Count - 1)
            {
                Ladder NextLadder = CorrectLadderPath[i + 1];
                Vector3 ExitPoint = FindLadderConnectionPoint(CurrentIterationLadder, NextLadder);
                if (ExitPoint != Vector3.zero)
                {
                    Path.Add(ExitPoint);
                }
                else
                {
                    Path.Add(CurrentIterationLadder.FindTopPoint());
                }
            }
        }
        else if (i == CorrectLadderPath.Count - 1)
        {
            Ladder LastLadder = CorrectLadderPath[i - 1];
            Vector3 EnterPoint = FindLadderEntrance(CurrentIterationLadder, LastLadder);
            if (EnterPoint != Vector3.zero)
            {
                Path.Add(EnterPoint);
            }
            
            if (TargetAtTop)
            {
                Path.Add(CurrentIterationLadder.FindTopPoint());
            }
            else
            {
                Path.Add(CurrentIterationLadder.FindBottomPoint());
            }
        }
        // Middle ladders need to find both entry and exit points
        else
        {
            Ladder LastLadder = CorrectLadderPath[i - 1];
            Ladder NextLadder = CorrectLadderPath[i + 1];
            
            Vector3 EnterPoint = FindLadderEntrance(CurrentIterationLadder, LastLadder);
            if (EnterPoint != Vector3.zero)
            {
                Path.Add(EnterPoint);
            }
            
            Vector3 ExitPoint = FindLadderConnectionPoint(CurrentIterationLadder, NextLadder);
            if (ExitPoint != Vector3.zero)
            {
                Path.Add(ExitPoint);
            }
        }
    }
    
    // Add final target
    Path.Add(Target);
    
    Debug.Log("Connection-based path created with " + Path.Count + " waypoints");
    return Path;
}

// Find closest ladder endpoint relative to startpoint
private Vector3 FindClosestLadderPoints(Vector3 Position, Ladder ladder)
{
    Vector3 LadderTop = ladder.FindTopPoint();
    Vector3 LadderBottom = ladder.FindBottomPoint();
    
    float DistanceToTop = Vector3.Distance(Position, LadderTop);
    float DistanceToBottom = Vector3.Distance(Position, LadderBottom);
    
    return DistanceToTop < DistanceToBottom ? LadderTop : LadderBottom;
}

// Find connection points from current ladder to next ladder
private Vector3 FindLadderConnectionPoint(Ladder CurrentLadder, Ladder TargetLadder)
{
    // Check if current ladder's top connects to next ladder
    if (CurrentLadder.TopConnected(TargetLadder))
    {
        return CurrentLadder.FindTopPoint();
    }
    // Check if current ladder's bottom connects to next ladder
    else if (CurrentLadder.BottomConnected(TargetLadder))
    {
        return CurrentLadder.FindBottomPoint();
    }

    return Vector3.zero;
}

// Get the entry point on current ladder that connects from previous ladder
private Vector3 FindLadderEntrance(Ladder CurrentLadder, Ladder LastLadder)
{
    // Check if previous ladder's top connects to current ladder
    if (LastLadder.TopConnected(CurrentLadder))
    {
        Vector3 ConnectionPoint = LastLadder.TopConnectionPoint(CurrentLadder);
        return ConnectionPoint;
    }
    // Check if previous ladder's bottom connects to current ladder  
    else if (LastLadder.BottomConnected(CurrentLadder))
    {
        Vector3 ConnectionPoint = LastLadder.BottomConnectionPoint(CurrentLadder);
        return ConnectionPoint; 
    }
    
    return Vector3.zero;
}

// BFS (breadth-first search) to find a path of connected ladders (connected inside Unity)
private List<Ladder> FindLadderConnections(Vector3 Start, Ladder TargetLadder, bool TargetAtTop)
{ 
    // Get all ladders
    Collider2D[] LadderArray = Physics2D.OverlapCircleAll(Start, 50f, Ladder);
    List<Ladder> Ladders = new List<Ladder>();
    foreach (Collider2D LadderCollider in LadderArray)
    {
        Ladder ladder = LadderCollider.GetComponent<Ladder>();
        if (ladder != null && !ExcludedLadders.Contains(ladder.transform))
        {
            Ladders.Add(ladder);
        }
    }
    
    // Find starting ladder at same Y level as NPC 
    Ladder StartLadder = null;
    float StartY = Start.y;
    
    // Find any ladder with top or bottom at same Y level (ignore X)
    foreach (Ladder ladder in Ladders)
    {
        Vector3 TopPosition = ladder.FindTopPoint();
        Vector3 BottomPosition = ladder.FindBottomPoint();
        
        float TopYDistance = Mathf.Abs(TopPosition.y - StartY);
        float BottomYDistance = Mathf.Abs(BottomPosition.y - StartY);
        
        if (TopYDistance < 1f || BottomYDistance < 1f) // Within 1 unit of Y level
        {
            StartLadder = ladder;
            string EndPoint = TopYDistance < BottomYDistance ? "TOP" : "BOTTOM";
            float ClosestYDistance = Mathf.Min(TopYDistance, BottomYDistance);
            break;
        }
    }
    // fallback to closest ladder if nothing on same y level
    if (StartLadder == null)
    {
        Debug.Log("No starting ladder found at same Y level, falling back to closest");
        float ClosestDistance = Mathf.Infinity;
        foreach (Ladder ladder in Ladders)
        {
            float Distance = Vector3.Distance(Start, ladder.transform.position);
            if (Distance < ClosestDistance)
            {
                ClosestDistance = Distance;
                StartLadder = ladder;
            }
        }
    }
    // If no ladder found then just walk directly to the end goal and hope (this usually happens when there's an error in the code or something is unreachable)
    if (StartLadder == null)
    {
        return null;
    }
    
    if (StartLadder == TargetLadder)
    {
        return new List<Ladder> { StartLadder };
    }
    
    // BFS to find connection path
    Queue<List<Ladder>> PathfindQueue = new Queue<List<Ladder>>();
    HashSet<Ladder> AlreadyVisited = new HashSet<Ladder>();
    
    PathfindQueue.Enqueue(new List<Ladder> { StartLadder });
    AlreadyVisited.Add(StartLadder);
    
    while (PathfindQueue.Count > 0)
    {
        List<Ladder> CurrentPath = PathfindQueue.Dequeue();
        Ladder CurrentLadder = CurrentPath[CurrentPath.Count - 1];
        
        List<Ladder> ConnectedLadders = new List<Ladder>();
        ConnectedLadders.AddRange(CurrentLadder.TopConnections());
        ConnectedLadders.AddRange(CurrentLadder.BottomConnections());
        
        foreach (Ladder Connected in ConnectedLadders)
        {
            if (Connected == null || AlreadyVisited.Contains(Connected)) continue;
            
            List<Ladder> NewPathCreated = new List<Ladder>(CurrentPath);
            NewPathCreated.Add(Connected);
            
            if (Connected == TargetLadder)
            {
                return NewPathCreated;
            }
            
            if (!AlreadyVisited.Contains(Connected))
            {
                PathfindQueue.Enqueue(NewPathCreated);
                AlreadyVisited.Add(Connected);
            }
        }
    }
    
    return null;
}

// plan a path using other pathfinding methods with connection-based as priority
private void PlanPath()
{
    Vector3 CurrentPosition = transform.position;
    Vector3 TargetDestination = Target;
    // reset path and index
    TargetPath.Clear();
    TargetWaypointIndex = 0;
    
    // Try connection-based pathfinding
    List<Vector3> ConnectionPath = BuildPath(CurrentPosition, TargetDestination);
    if (ConnectionPath != null && ConnectionPath.Count > 1)
    {
        TargetPath = ConnectionPath;
        return;
    }
    
    // Fallback to ladder pathfinding if needed
    if (Mathf.Abs(TargetDestination.y - CurrentPosition.y) > 1f)
    {
        Transform ClosestLadder = FindNearestLadder();
        if (ClosestLadder != null)
        {
            Ladder LadderScript = ClosestLadder.GetComponent<Ladder>();
            if (LadderScript != null)
            {
                bool ClimbUpNeeded = TargetDestination.y > CurrentPosition.y;
                
                if (ClimbUpNeeded)
                {
                    TargetPath.Add(LadderScript.FindBottomPoint());
                    TargetPath.Add(LadderScript.FindTopPoint());
                }
                else
                {
                    TargetPath.Add(LadderScript.FindTopPoint());
                    TargetPath.Add(LadderScript.FindBottomPoint());
                }
                TargetPath.Add(TargetDestination);
                return;
            }
        }
    }
    
    // Direct path if that doesnt work and hope
    TargetPath.Add(TargetDestination);
}





// Check if there's another NPC blocking the path of the current NPC
private bool NPCBlocking()
{
    // Check a bit ahead of the NPC in the direction they're moving
    Vector3 checkPosition = transform.position + Vector3.right * Direction * 0.8f;
    
    // Look for other FriendlyAI components in a small radius
    Collider2D[] nearbyNPCs = Physics2D.OverlapCircleAll(checkPosition, 0.5f);

    foreach (var collider in nearbyNPCs)
    {
        FriendlyAI otherNPC = collider.GetComponent<FriendlyAI>();
        if (otherNPC != null && otherNPC != this)
        {
            return true; // Another NPC is blocking
        }
    }
    
    return false;
}

// Find the nearest ladder that also respects Y range
public Transform FindNearestLadder()
{
    float ClosestDistance = Mathf.Infinity;
    Transform ClosestLadder = null;
    float NPCYValue = transform.position.y;
    
    Collider2D[] LadderArray = Physics2D.OverlapCircleAll(transform.position, LadderCheckDistance, Ladder);

    foreach (Collider2D LadderCollider in LadderArray)
    {
        Transform ladder = LadderCollider.transform;
        
        // Skip ladders permanently excluded for this path
        if (ExcludedLadders.Contains(ladder))
        {
            continue;
        }

        // Let the connection system decide which ladders are reachable
        float Distance = Vector2.Distance(transform.position, ladder.position);
        
        if (Distance < ClosestDistance)
        {
            ClosestDistance = Distance;
            ClosestLadder = ladder;
        }
    }

    return ClosestLadder;
}

    // Find a landing point across a gap
    private Vector3 FindLandPoint()
    {
        Transform CurrentGroundCheck = CorrectGroundCheck();
        Vector3 GroundCheckPosition = CurrentGroundCheck.position;
        float DistanceForScan = 0.2f; 
        bool GapDetected = false;
        bool OnGround = Physics2D.OverlapCircle(GroundCheckPosition, GroundCheckRadius, Ground);
        // scan across gap to find a landin point
        if (OnGround)
        {
            while (DistanceForScan <= GapJumpDistance)
            {
                Vector3 GroundTestPoint = new Vector3(GroundCheckPosition.x + (Direction * DistanceForScan), GroundCheckPosition.y, GroundCheckPosition.z);
                
                if (!Physics2D.OverlapCircle(GroundTestPoint, GroundCheckRadius, Ground))
                {
                    GapDetected = true;
                    break;
                }
                DistanceForScan += 0.1f;
            }
        }
        else
        {
            GapDetected = true;
            DistanceForScan = 0.2f;
        }
        
        if (!GapDetected && OnGround)
        {
            return Vector3.zero;
        }
        // scan within gap jump distance allowed and add a safety margin to make sure npc lands safely
        while (DistanceForScan <= GapJumpDistance)
        {
            Vector3 GroundTestPoint = new Vector3(GroundCheckPosition.x + (Direction * DistanceForScan), GroundCheckPosition.y, GroundCheckPosition.z);
            
            if (Physics2D.OverlapCircle(GroundTestPoint, GroundCheckRadius, Ground))
            {
                float SafetyMargin = 1f;
                float FinalJumpDistance = DistanceForScan + SafetyMargin;
                float LandPointX = GroundCheckPosition.x + (Direction * FinalJumpDistance);
                
                float PatrolLeftEdge = SpawnPosition.x - Distance;
                float PatrolRightEdge = SpawnPosition.x + Distance;
                
                LandPointX = Mathf.Clamp(LandPointX, PatrolLeftEdge, PatrolRightEdge);
                
                return new Vector3(LandPointX, transform.position.y, transform.position.z);
            }
            
            DistanceForScan += 0.1f;
        }
        
        return Vector3.zero;
    }

// Set current ladder 
public void SetCurrentLadder(Transform ladder)
{
    CurrentLadder = ladder;
}

// Coroutine to handle jumping  for gaps and ladders
private IEnumerator JumpToPoint(Vector3 targetPoint)
{
    // set jumping state to stop other movements
    GapJumping = true;
    Vector3 JumpStartPoint = transform.position;
    Vector3 HighestPoint = new Vector3((JumpStartPoint.x + targetPoint.x) / 2, JumpStartPoint.y + GapJumpHeight, JumpStartPoint.z);

    float JumpTime = Vector3.Distance(JumpStartPoint, targetPoint) / GapJumpSpeed;
    float JumpTimer = 0;
    // parabolic jump using lerp and a high point
    while (JumpTimer < JumpTime)
    {
        float t = JumpTimer / JumpTime;

        Vector3 Lerp1 = Vector3.Lerp(JumpStartPoint, HighestPoint, t);
        Vector3 Lerp2 = Vector3.Lerp(HighestPoint, targetPoint, t);
        transform.position = Vector3.Lerp(Lerp1, Lerp2, t);

        JumpTimer += Time.deltaTime;
        yield return null;
    }

    transform.position = targetPoint;
    GapJumping = false;
    // if jump was for a ladder then go to it and climb, otherwise patrol
    if (State != AIState.Pathing && !(State == AIState.Climbing && CurrentLadder != null))
    {
        State = AIState.Patrolling;
    }
}

    // SetDirection method (your existing method)
    public void SetDirection(int SpriteDirection)
    {
        Direction = SpriteDirection;
        // Flip the sprite based on moving direction
        SpriteRenderer SpriteRenderer = GetComponent<SpriteRenderer>();
        if (SpriteRenderer != null)
        {
            SpriteRenderer.flipX = (Direction == -1);
        }
    }

    // Visualization for the project report screenshots
    void OnDrawGizmos()
{
    // Draw path to target
    if (TargetPath.Count > 1)
    {
        Gizmos.color = Color.green;
        for (int i = 0; i < TargetPath.Count - 1; i++)
        {
            Gizmos.DrawLine(TargetPath[i], TargetPath[i + 1]);
        }
    }
    
    // Draw ground checks
    if (LeftGroundCheck != null)
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(LeftGroundCheck.position, GroundCheckRadius);
    }
    if (RightGroundCheck != null)
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(RightGroundCheck.position, GroundCheckRadius);
    }
    
    // Draw chat detection range
    if (CanStartChats)
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ChatDetectDistance);
    }
    
    // Draw line to chat partner
    if (Chatting && OtherChatter != null)
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, OtherChatter.transform.position);
    }

    // Draw wall drop detection area
    if (Application.isPlaying)
    {
        Vector3 DropDetectStart = transform.position + Vector3.right * Direction * 1f;
        Gizmos.color = Color.cyan;
        for (float DropDetection = 1f; DropDetection <= WallDropDetectDistance; DropDetection += 1f)
        {
            Vector3 DropDetectionPosition = DropDetectStart + Vector3.down * DropDetection;
            Gizmos.DrawWireSphere(DropDetectionPosition, GroundCheckRadius);
        }
    }

    // Draw wall detection area
    Gizmos.color = Color.magenta;
    Vector3 WallDetectPosition = transform.position + Vector3.right * Direction * 0.5f;
    Gizmos.DrawWireSphere(WallDetectPosition, 0.3f);
    }

// Check for other NPCs to chat with
private void FindOtherNPCs()
{
    // Check if in cooldown
    if (ChatCooldownTime > 0f)
    {
        ChatCooldownTime -= Time.deltaTime;
        return; 
    }
    
    // Find all FriendlyAI in range
    Collider2D[] NPCColliderArray = Physics2D.OverlapCircleAll(transform.position, ChatDetectDistance);
    
    foreach (var Collider in NPCColliderArray)
    {
        FriendlyAI OtherNPC = Collider.GetComponent<FriendlyAI>();
        
        if (OtherNPC != null && OtherNPC != this && 
            OtherNPC.State == AIState.Patrolling && 
            !OtherNPC.Chatting && 
            OtherNPC.ChatCooldownTime <= 0f) 
        {
            // Start chat with this NPC
            StartChat(OtherNPC);
            return; 
        }
    }
}

// Start a chat
private void StartChat(FriendlyAI OtherNPC)
{
    // Both NPCs enter chat state
    Chatting = true;
    OtherChatter = OtherNPC;
    State = AIState.Chatting;
    ChatTime = ChatDuration;

    // Set up the other NPC for chat
    OtherNPC.Chatting = true;
    OtherNPC.OtherChatter = this;
    OtherNPC.State = AIState.Chatting;
    OtherNPC.ChatTime = ChatDuration;

    // Show speech bubbles
    if (SpeechBubble != null)
    {
        SpeechBubble.SetActive(true);
    }
    if (OtherNPC.SpeechBubble != null)
    {
        OtherNPC.SpeechBubble.SetActive(true);
    }
}

// Handle chatting behavior
private void HandleChatting()
{
    if (OtherChatter == null || !OtherChatter.Chatting)
    {
        // End chat is partner is missing
        EndChat();
        return;
    }
    
    // Count down chat timer
    ChatTime -= Time.deltaTime;
    
    if (ChatTime <= 0f)
    {
        // Either NPC can end the chat when their timer runs out
        EndChatForBoth();
    }
}

// End chat for this NPC only (just a safety check but probably wont be used)
private void EndChat()
{
    Chatting = false;
    OtherChatter = null;
    ChatTime = 0f;
    
    // Start cooldown timer
    ChatCooldownTime = ChatCooldown;
    
    // Return to previous state (usually patrolling)
    State = AIState.Patrolling;
    
    // Hide speech bubble
    if (SpeechBubble != null)
    {
        SpeechBubble.SetActive(false);
    }
}

// End chat for both NPCs
private void EndChatForBoth()
{
    Debug.Log("Chat ended between " + gameObject.name + " and " + OtherChatter.gameObject.name);
    // End chat for other NPC first
    if (OtherChatter != null)
    {
        OtherChatter.EndChat();
    }
    
    // Then end for this NPC
    EndChat();
}

// Check for wall ahead then jump if jump point
private bool WallJump()
{
    // Check if there's wall ahead
    Vector3 WallDetectPosition = transform.position + Vector3.right * Direction * 0.8f;
    Collider2D WallCollider = Physics2D.OverlapCircle(WallDetectPosition, 0.3f, Wall);
    
    if (WallCollider == null) 
    {
        return false; 
    }
    
    // Look for jump points near this wall
    GameObject[] JumpPointArray = GameObject.FindGameObjectsWithTag("WallJumpPoint");
    
    Vector3 BestBottomPoint = Vector3.zero;
    Vector3 BestTopPoint = Vector3.zero;
    // determine best jump points based on distance
    foreach (GameObject jumpPoint in JumpPointArray)
    {
        float DistancetoJumpPoint = Vector3.Distance(transform.position, jumpPoint.transform.position);
        
        if (DistancetoJumpPoint < WallDetectDistance * 2f) 
        {
            float DistanceToWall = Vector3.Distance(jumpPoint.transform.position, WallCollider.transform.position);
            
            if (DistanceToWall < 8f)
            {
                if (jumpPoint.name.Contains("Bottom"))
                {
                    BestBottomPoint = jumpPoint.transform.position;
                }
                else if (jumpPoint.name.Contains("Top"))
                {
                    BestTopPoint = jumpPoint.transform.position;
                }
            }
        }
    }
    
    // Choose the furthest point to jump to
    if (BestBottomPoint != Vector3.zero && BestTopPoint != Vector3.zero)
    {
        float DistanceToBottomPoint = Vector3.Distance(transform.position, BestBottomPoint);
        float DistanceToTopPoint = Vector3.Distance(transform.position, BestTopPoint);
        
        Vector3 TargetPoint;
        if (DistanceToTopPoint > DistanceToBottomPoint)
        {
            TargetPoint = BestTopPoint;
        }
        else
        {
            TargetPoint = BestBottomPoint;
        }
        
        StartCoroutine(WallJumpToPoint(TargetPoint));
        return true;
    }
    else
    {
        return false;
    }
}





// Jump coroutine for wall jumping
private IEnumerator WallJumpToPoint(Vector3 TargetPoint)
{
    GapJumping = true;
    Vector3 StartPosition = transform.position;
    // make the arc height
    float JumpHeight = GapJumpHeight + WallJumpArcHeight; 
    Vector3 HighestPoint = new Vector3((StartPosition.x + TargetPoint.x) / 2, 
                                   Mathf.Max(StartPosition.y, TargetPoint.y) + JumpHeight, 
                                   StartPosition.z);

    float JumpTime = Vector3.Distance(StartPosition, TargetPoint) / GapJumpSpeed;
    float JumpTimer = 0;
    // use lerp to make parabolic jump
    while (JumpTimer < JumpTime)
    {
        float t = JumpTimer / JumpTime;

        Vector3 Lerp1 = Vector3.Lerp(StartPosition, HighestPoint, t);
        Vector3 Lerp2 = Vector3.Lerp(HighestPoint, TargetPoint, t);
        transform.position = Vector3.Lerp(Lerp1, Lerp2, t);

        JumpTimer += Time.deltaTime;
        yield return null;
    }

    transform.position = TargetPoint;
    GapJumping = false;

    // Check for position change
    bool YChange = Mathf.Abs(TargetPoint.y - StartPosition.y) > 0.3f;

    if (YChange)
    {
        YPosition = transform.position.y;
    }

    // Handle both Pathing and Climbing states
    if (State == AIState.Pathing || State == AIState.Climbing)
    {
        // Check if vertical movement still needed
        float verticalDistance = Target.y - transform.position.y;
        if (Mathf.Abs(verticalDistance) > 1f)
        {
            State = AIState.Idle;
        }
        else
        {
            State = AIState.Pathing;
        }
    }
}

// Wall drop jump start
private bool StartWallDropJump()
{
    // Check downward for ground so it can jump
    Vector3 DropCheckStartPosition = transform.position + Vector3.right * Direction * WallDropForwardDistance;
    
    for (float DropDistance = 1f; DropDistance <= WallDropDetectDistance; DropDistance += 0.5f)
    {
        Vector3 DropCheckEndPosition = DropCheckStartPosition + Vector3.down * DropDistance;
        
        // Check if there's ground at this drop point
        if (Physics2D.OverlapCircle(DropCheckEndPosition, GroundCheckRadius, Ground))
        {
            Vector3 LandTarget = DropCheckEndPosition + Vector3.up * 0.5f; // Land slightly above the ground cause the midpoint of NPC is higher than the feet
            StartCoroutine(EndWallDropJump(LandTarget));
            return true;
        }
    }
    return false;
}

// wall drop jump end
private IEnumerator EndWallDropJump(Vector3 TargetPoint)
{
    GapJumping = true;
    Vector3 StartLocation = transform.position;
    
    // small arc for dropping down walls
    Vector3 HighestPoint = new Vector3((StartLocation.x + TargetPoint.x) / 2, 
                                   StartLocation.y + WallDropJumpHeight, 
                                   StartLocation.z);
    
    float JumpTime = Vector3.Distance(StartLocation, TargetPoint) / GapJumpSpeed;
    float JumpTimer = 0;
    // parabolic drop using lerp
    while (JumpTimer < JumpTime)
    {
        float t = JumpTimer / JumpTime;
        
        Vector3 Lerp1 = Vector3.Lerp(StartLocation, HighestPoint, t);
        Vector3 Lerp2 = Vector3.Lerp(HighestPoint, TargetPoint, t);
        transform.position = Vector3.Lerp(Lerp1, Lerp2, t);
        
        JumpTimer += Time.deltaTime;
        yield return null;
    }
    
    transform.position = TargetPoint;
    GapJumping = false;
    
    // Check for position change
    bool YChange = Mathf.Abs(TargetPoint.y - StartLocation.y) > 0.3f;
    
    if (YChange)
    {
        YPosition = transform.position.y;
    }
    
    // Resume behavior based on original state
    if (State == AIState.Pathing)
    {
        float YDistance = Target.y - transform.position.y;
        if (Mathf.Abs(YDistance) > 1f)
        {
            State = AIState.Idle;
        }
    }
}
}