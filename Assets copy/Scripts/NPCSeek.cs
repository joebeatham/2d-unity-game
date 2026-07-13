using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCSeek : MonoBehaviour
{
    public GameObject PathfindTarget; // Torch goes here but it can be any object in the finished game, torch for testing
    public bool PathfindStraightAway = false; // Start seeking immediately when game starts (i added it for an option but i dont use it really, i prefer using pathfind key)
    public KeyCode PathfindKey = KeyCode.None; // Key to start pathfinding
    public float IdleTimer = 1f; // Timer for going idle after reaching target
    private float LastYPosition = 0f; // Track last Y position so changes get detected
    private float PathfindStartTime = 0f; // When seeking started
    private FriendlyAI FriendlyAI; // Reference to other script
    private bool Pathfinding = false; // boolean for currently seeking
    private bool ClimbingLastFrame = false; // detect when finishing climbing
    
    // Start is called before the first frame update
    void Start()
    {
        FriendlyAI = GetComponent<FriendlyAI>();
        // safety check
        if (FriendlyAI == null)
        {
            return;
        }
        // Start Y position tracking properly
        LastYPosition = transform.position.y;  
        
        // Start seeking immediately if enabled
        if (PathfindStraightAway && PathfindTarget != null)
        {
            StartPathing();
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        // Optional manual control that I use
        if (PathfindKey != KeyCode.None && Input.GetKeyDown(PathfindKey))
        {
            StartPathing();
        }
    
        // Always track Y position
        float YPosition = transform.position.y;
        bool YChanged = Mathf.Abs(YPosition - LastYPosition) > 0.3f;
    
        // Always update LastYPosition so changes get detected
        LastYPosition = YPosition;
    
        // Track climbing state changes
        bool ClimbingBoolean = (FriendlyAI.State == FriendlyAI.AIState.Climbing);
    
        // Detect when NPC finishes climbing
        if (ClimbingLastFrame && !ClimbingBoolean)
        {  
            if (Pathfinding)
            {
                // check for connections after climbing
                if (FriendlyAI.LastLadder != null)
                {    
                    var (connectedLadder, smartDirection) = FindBestNextLadder();
                    // take a connection if it can
                    if (connectedLadder != null)
                    {
                        FriendlyAI.SetCurrentLadder(connectedLadder);
                        FriendlyAI.StartClimbing(smartDirection);
                        ClimbingLastFrame = ClimbingBoolean; 
                        return; 
                    }
                }
            
                // resume pathing
                FriendlyAI.State = FriendlyAI.AIState.Pathing;
                FriendlyAI.SetEndGoal(PathfindTarget.transform.position);
            }
        }
        ClimbingLastFrame = ClimbingBoolean;
    
        // Pathfinding logic
        if (Pathfinding)
        {
            float YDistance = PathfindTarget.transform.position.y - transform.position.y;
            float XDistance = Mathf.Abs(transform.position.x - PathfindTarget.transform.position.x);
        
            // Don't interrupt if in climbing state
            if (FriendlyAI.State == FriendlyAI.AIState.Climbing || FriendlyAI.Climbing)
            {
                return;
            }
        
            // dont interupt climbing if close to target ladder
            if (FriendlyAI.CurrentLadder != null)
            {
                float DistanceToLadder = Vector3.Distance(transform.position, FriendlyAI.CurrentLadder.position);
                if (DistanceToLadder < 3f)
                {
                    return;
                }
            }
        
            if (Time.time - PathfindStartTime < 1f) 
            {
                return;
            }
        
            // Force exit climbing if stuck for some reason
            if (FriendlyAI.Climbing && FriendlyAI.State != FriendlyAI.AIState.Climbing)
            {
                FriendlyAI.Climbing = false;
                FriendlyAI.CurrentLadder = null;
                FriendlyAI.State = FriendlyAI.AIState.Pathing;
            }
        
            // Re-evaluate when position changes significantly
            if (YChanged && 
                FriendlyAI.State != FriendlyAI.AIState.Climbing && 
                !FriendlyAI.Climbing &&
                FriendlyAI.CurrentLadder == null)
            {            
                // Use connection-based ladder selection
                var (connectionLadder, smartDirection) = FindBestNextLadder();
            
                if (connectionLadder != null)
                {
                    FriendlyAI.SetCurrentLadder(connectionLadder);
                    FriendlyAI.StartClimbing(smartDirection);
                    return;
                }
            }

            // fallback system if no connections found, needed in order to climb for some reason
            if (FriendlyAI.State == FriendlyAI.AIState.Idle)
            {
            
                // Check direct connections from last ladder
                Transform directConnection = FindConnections();
            
                if (directConnection != null)
                {
                    FriendlyAI.SetCurrentLadder(directConnection);
                    // Use smart direction detection for the connected ladder
                    var (_, smartDirection) = EvaluateConnectedLadder(directConnection);
                    FriendlyAI.StartClimbing(smartDirection);
                    return;
                }
            
                // Another  fallback to connection search
                var (connectionLadder, smartDirection2) = FindBestNextLadder();
            
                if (connectionLadder != null)
                {
                    FriendlyAI.SetCurrentLadder(connectionLadder);
                    FriendlyAI.StartClimbing(smartDirection2);
                    return;               
                }
            }

            // handle normal pathing if near target
            if (FriendlyAI.State == FriendlyAI.AIState.Idle)
            {
                if (XDistance < 1f && Mathf.Abs(YDistance) < 1f)
                {
                    Pathfinding = false;
                    StartCoroutine(IdleThenPatrol());
                }
                else if (Mathf.Abs(YDistance) <= 1f)
                {
                    FriendlyAI.State = FriendlyAI.AIState.Pathing;
                    FriendlyAI.SetEndGoal(PathfindTarget.transform.position);
                }
            }
        }
    }

// Find best connection ladder
private (Transform ladder, bool GoUp) FindBestNextLadder()
{
        Collider2D[] LadderArray = Physics2D.OverlapCircleAll(transform.position, 30f, FriendlyAI.Ladder);

        Transform BestLadder = null;
        bool BestDirection = true;
        float HighestScore = -1f;
        // evaluate all ladders based on connections
        foreach (Collider2D LadderCollider in LadderArray)
        {
            Transform ladder = LadderCollider.transform;
        
            // Skip excluded ladders
            if (FriendlyAI.ExcludedLadders.Contains(ladder))
            {
                continue;
            }
        
            Ladder Ladder = ladder.GetComponent<Ladder>();
            if (Ladder == null) continue;
        
            // Evaluate up and down for each ladder
            float UpScore = EvaluateConnections(Ladder, true);  
            float DownScore = EvaluateConnections(Ladder, false); 
        
            // Choose the better direction for this ladder
            if (UpScore > DownScore && UpScore > HighestScore)
            {
                HighestScore = UpScore;
                BestLadder = ladder;
                BestDirection = true; // up
            }
            else if (DownScore > HighestScore)
            {
                HighestScore = DownScore;
                BestLadder = ladder;
                BestDirection = false; // down
            }
        }   
        return (BestLadder, BestDirection);
}

// Check direct connections from the last used ladder
private Transform FindConnections()
{
    // check if last ladder exists
    if (FriendlyAI.LastLadder == null)
    {
        return null;
    }
    // 
    Ladder LadderScript = FriendlyAI.LastLadder.GetComponent<Ladder>();
      
    // Determine which ladder endpoint the NPC exited ladder from
    Vector3 NPCPosition = transform.position;
    Vector3 LadderTopPoint = LadderScript.FindTopPoint();
    Vector3 LadderBottomPoint = LadderScript.FindBottomPoint();
    
    bool ExitedLadderTop = Vector3.Distance(NPCPosition, LadderTopPoint) < Vector3.Distance(NPCPosition, LadderBottomPoint);
    
    // get connections from the ladders exit point
    List<Ladder> LadderConnectionArray;
    if (ExitedLadderTop)
    {
        LadderConnectionArray = LadderScript.TopConnections();
    }
    else
    {
        LadderConnectionArray = LadderScript.BottomConnections();
    }
    
    // Find the closest connection
    Transform BestLadder = null;
    float NearestLadderDistance = float.MaxValue;
    
    foreach (Ladder ConnectedLadder in LadderConnectionArray)
    {
        if (ConnectedLadder == null) continue;
        
        if (FriendlyAI.ExcludedLadders.Contains(ConnectedLadder.transform)) continue;
        
        float DistanceToLadder = Vector3.Distance(NPCPosition, ConnectedLadder.transform.position);
        
        if (DistanceToLadder < NearestLadderDistance)
        {
            NearestLadderDistance = DistanceToLadder;
            BestLadder = ConnectedLadder.transform;
        }
    }    
    return BestLadder;
}

// Evaluate which direction to climb on a directly connected ladder
private (Transform, bool) EvaluateConnectedLadder(Transform ladderTransform)
{
    Ladder Ladder = ladderTransform.GetComponent<Ladder>();
    if (Ladder == null) return (ladderTransform, true);

    // determine the closest ladder endpoint to the target
    Vector3 TargetPosition = PathfindTarget.transform.position;
    Vector3 LadderTopPoint = Ladder.FindTopPoint();
    Vector3 LadderBottomPoint = Ladder.FindBottomPoint();

    float TopDistance = Vector3.Distance(LadderTopPoint, TargetPosition);
    float BottomDistance = Vector3.Distance(LadderBottomPoint, TargetPosition);

    bool NeedGoUp = BottomDistance < TopDistance; 

    return (ladderTransform, NeedGoUp);
}

    private float EvaluateConnections(Ladder ladder, bool needGoUp)
    {
        float Score = 0f;
        // get connections 
        List<Ladder> LadderConnectionsList = needGoUp ? ladder.TopConnections() : ladder.BottomConnections();
        if (LadderConnectionsList.Count == 0)
        {
            // No connections means very low score but not negative
            return 0.1f;
        }
        // MASSIVE bonus if this ladder is acutally connected
        if (FriendlyAI.LastLadder != null)
        {
            Ladder LastLadderScript = FriendlyAI.LastLadder.GetComponent<Ladder>();
            if (LastLadderScript != null)
            {
                // Check if ladders connect to each other
                bool ConnectedBoolean = false;
                if (LastLadderScript.TopConnected(ladder) || LastLadderScript.BottomConnected(ladder))
                {
                    ConnectedBoolean = true;
                }
                else if (ladder.TopConnected(LastLadderScript) || ladder.BottomConnected(LastLadderScript))
                {
                    ConnectedBoolean = true;
                }
                if (ConnectedBoolean)
                {
                    Score += 100f; // MASSIVE bonus for being connected to last used ladder
                }
            }
        }
        // Check if ladder is at same Y level as NPC for score bonus
        Ladder LadderScript = ladder.GetComponent<Ladder>();
        if (LadderScript != null)
        {
            Vector3 TopPosition = LadderScript.FindTopPoint();
            Vector3 BottomPosition = LadderScript.FindBottomPoint();
            float YPosition = transform.position.y;
            if (Mathf.Abs(TopPosition.y - YPosition) < 1f || Mathf.Abs(BottomPosition.y - YPosition) < 1f)
            {
                Score += 25f; // less of a huge bonus cause connections are way more important
            }
        }
        // Check if any connection gets closer to target
        float DistanceToTarget = Vector3.Distance(transform.position, PathfindTarget.transform.position);
        foreach (Ladder ConnectedLadder in LadderConnectionsList)
        {
            if (ConnectedLadder == null) continue;
            // Score based on how much closer the connection gets us to target
            float ConnectionToTargetDistance = Vector3.Distance(ConnectedLadder.transform.position, PathfindTarget.transform.position);
            if (ConnectionToTargetDistance < DistanceToTarget)
            {
                float Improvement = DistanceToTarget - ConnectionToTargetDistance;
                Score += Improvement;
                // Bonus for getting same Y level as target from connection
                if (Mathf.Abs(ConnectedLadder.transform.position.y - PathfindTarget.transform.position.y) < 2f)
                {
                    Score += 10f; // bonus for target-level connections
                }
            }
            else
            {
                Score += 1f; // Small bonus for having any connection cause why not
            }
        }
        // penalise for long distance to ladder
        float LadderDistance = Vector3.Distance(transform.position, ladder.transform.position);
        float PenaltyForDistance = LadderDistance * 0.1f;
        Score = Mathf.Max(0.1f, Score - PenaltyForDistance); // make sure score never goes below 0.1 for safety
        return Score;
    }
  
// Simple method to start pathing - let FriendlyAI handle everything
public void StartPathing()
{
    // safety check
    if (FriendlyAI == null || PathfindTarget == null)
    {
        return;
    }
    // don't start if already pathfinding
    if (Pathfinding)
    {
        return;
    }
    
    Pathfinding = true;
    PathfindStartTime = Time.time; 
    
    // get friendlyAI to handle pathfinding
    FriendlyAI.SetEndGoal(PathfindTarget.transform.position);
}
    
    // Coroutine to idle for a sec then patrol
    private IEnumerator IdleThenPatrol()
    {
        // wait for climbing to finish if still climbing (safety check)
        while (FriendlyAI.State == FriendlyAI.AIState.Climbing)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Force the NPC to idle state
        FriendlyAI.StopAndIdle();
        
        // Wait for IdleTimer
        yield return new WaitForSeconds(IdleTimer);
        
        // Start patrolling
        if (FriendlyAI != null)
        {
            FriendlyAI.StartPatrolling();
        }
    }
}