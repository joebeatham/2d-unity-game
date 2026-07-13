using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingDummyAI : MonoBehaviour
{
    public Transform Player; // Variable of the player's Transform (location in the scene)
    private Vector3 SpawnPosition; // Enemy's starting position
    public float Speed = 2f; // How fast the enemy moves while patrolling
    public float Distance = 3f; // Distance the enemy can move from its starting position in each direction
    private int Direction = 1; // Direction the enemy is facing (1 = right, -1 = left)
    public float PatrolPauseDuration = 1f; // The amount of time the enemy pauses at patrol edges
    private float PauseAfterPatrolTimer = 0f; // The amount of time of pause at patrol edges
    public float ChaseSpeed = 3f; // How fast the enemy moves when chasing
    public float ChaseRange = 5f; // Range from the player to start chasing
    public float ChaseEndRange = 8f; // Range the chase ends
    public float PauseAfterChaseDuration = 1f; // The amount of time to pause after chasing
    private float PauseAfterChaseTimer = 0f; // Timer of pause after chase
    private float ReturnEdgeX; // X position of the patrol edge to return to after chase
    // State Boolean Variables
    private bool Paused = false;
    private bool AlreadyChasing = false;
    private bool ReturnToPatrol = false;
    private bool AfterChasePause = false;

    void Start()
    {
        // Record starting position and set initial states
        SpawnPosition = transform.position;
        Paused = false;
        AlreadyChasing = false;
        ReturnToPatrol = false;
        AfterChasePause = false;
    }

    void Update()
    {
        float DistanceFromPlayer = Player != null ? Vector2.Distance(transform.position, Player.position) : Mathf.Infinity; // Record distance from player (or infinity if no player)

        // Patrolling logic
        if (!AlreadyChasing && !ReturnToPatrol && !AfterChasePause) // Only run if not chasing, returning to patrol, or pausing after chase
        {
            float PatrolLeftEdge = SpawnPosition.x - Distance; // Calculate left patrol edge
            float PatrolRightEdge = SpawnPosition.x + Distance; // Calculate right patrol edge
            bool AtPatrolEdge = (Direction == 1 && transform.position.x >= PatrolRightEdge) || (Direction == -1 && transform.position.x <= PatrolLeftEdge); // Check if left or right patrol edge

            if (!AtPatrolEdge) // If not at patrol edge then keep moving
            {
                transform.Translate(Vector2.right * Direction * Speed * Time.deltaTime);
            }
            else if (!Paused) // If at patrol edge and not already paused then pause
            {
                Paused = true;
                PauseAfterPatrolTimer = PatrolPauseDuration;
            }
        }

        //  Pause logic for patrol edges
        if (Paused && !AlreadyChasing) // Only run if paused and not chasing
        {
            PauseAfterPatrolTimer -= Time.deltaTime; // Count down timer
            if (PauseAfterPatrolTimer <= 0f)
            {
                Paused = false;
                SetDirection(-Direction);

                // Nudge away from edge to prevent being stuck forever
                transform.position += Vector3.right * Direction * 0.05f;
            }
            return;
        }

        // Start chasing if player is in range logic
        if (!AlreadyChasing && DistanceFromPlayer <= ChaseRange)
        {
            // State changes
            AlreadyChasing = true;
            ReturnToPatrol = false;
            AfterChasePause = false;
            Paused = false;
        }

        // Chasing logic
        if (AlreadyChasing) // Only run if already chasing
        {
            if (DistanceFromPlayer > ChaseEndRange) // Stop chasing if player is out of range
            {
                AlreadyChasing = false;
                AfterChasePause = true;
                PauseAfterChaseTimer = PauseAfterChaseDuration; // Set timer
                return;
            }

            // Move directly toward player To make it look like enemy is flying
            float Step = ChaseSpeed * Time.deltaTime; // Calculate how far to move this frame
            Vector3 Target = new Vector3(Player.position.x, Player.position.y, transform.position.z); // Makes a target X and Y position but keeps current Z
            transform.position = Vector3.MoveTowards(transform.position, Target, Step); // Moves towards target

            // Flip sprite if needed to face player
            if ((Player.position.x - transform.position.x) * Direction < 0)
            {
                SetDirection((Player.position.x - transform.position.x) > 0 ? 1 : -1);
            }
            return;
        }

        // Pause after chase logic
        if (AfterChasePause)
        {
            PauseAfterChaseTimer -= Time.deltaTime; // Count down the timer
            if (PauseAfterChaseTimer <= 0f) // If timer is done
            {
                AfterChasePause = false;
                ReturnToPatrol = true;

                // Decision making for which patrol edge to return to
                // Calculates possitions of left and right patrol edges
                float LeftEdge = SpawnPosition.x - Distance;
                float RightEdge = SpawnPosition.x + Distance;
                // Calculates distance to each calculated edge
                float DistanceToLeft = Mathf.Abs(transform.position.x - LeftEdge);
                float DistanceToRight = Mathf.Abs(transform.position.x - RightEdge);
                // Sets return target to closest edge
                ReturnEdgeX = (DistanceToLeft < DistanceToRight) ? LeftEdge : RightEdge;
                SetDirection((ReturnEdgeX > transform.position.x) ? 1 : -1);
            }
            return; // If timer isn'y done
        }

        // Returning to patrol logic
        if (ReturnToPatrol)
        {
            // Move towards target that was set in pause after chase logic
            float Step = Speed * Time.deltaTime;
            Vector3 Target = new Vector3(ReturnEdgeX, SpawnPosition.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, Target, Step);

            // Flip sprite to face patrol edge
            if ((ReturnEdgeX - transform.position.x) * Direction < 0)
            {
                SetDirection((ReturnEdgeX - transform.position.x) > 0 ? 1 : -1);
            }

            // If close enough to patrol edge in X and Y then resume patrolling state to prevent endless small movements
            Vector3 TargetPosition = new Vector3(ReturnEdgeX, SpawnPosition.y, transform.position.z);
            float DistanceToPosition = Vector3.Distance(transform.position, TargetPosition);

            if (DistanceToPosition < 0.1f) // Only snap when close to the full X and Y target position
            {
                // Snap exactly to the patrol edge seamlessly
                transform.position = new Vector3(ReturnEdgeX, SpawnPosition.y, transform.position.z);
                ReturnToPatrol = false;

                // Set which direction to face when patrolling
                SetDirection((ReturnEdgeX == SpawnPosition.x - Distance) ? 1 : -1);

                // Slight nudge to the side of the edge to prevent flipping forever on next patrol check
                transform.position += Vector3.right * Direction * 0.1f;
            }
            return;
        }
    }

    private void SetDirection(int NewDirection) // Function to set direction and flip sprite
    {
        Direction = NewDirection;
        Vector3 scale = transform.localScale; // Get current scale
        scale.x = Mathf.Abs(scale.x) * Direction; // Flip sprite by changing X scale
        transform.localScale = scale; // Apply the new scale for visual flip
    }
}