using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Spine.Unity;

public class DummyAI : MonoBehaviour
{
    public Transform Player; // Location of the player
    private Vector3 SpawnPosition; // Enemy's spawning position
    public float Speed = 2f; // How fast enemy moves when patrolling
    public float PatrolDistance = 3f; // Distance enemy can patrol from its spawning position each way
    private int Direction = 1;  // Direction the enemy is facing (1 = right, -1 = left)
    public float PatrolPauseTime = 1f; // amount of time enemy pauses at patrol edge
    private float PatrolPauseTimer = 0f; // amount of time on timer that is left for patrol edge
    public float ChaseSpeed = 3f; // How fast enemy moves in chasing state
    public float ChaseRange = 5f; // Range from player to start chasing
    public float ChaseEndRange = 8f; // Range chase ends when player runs away
    public float PauseAfterChaseTime = 1f; // Amount of time to pause after chasing
    private float PauseAfterChaseTimer = 0f; // amount of time on timer that is left for pause after chase
    private float ReturnEdgeXPosition; // X position of patrol edge to return to after chase
    private bool Paused = false; // boolean if enemy is paused
    private bool AlreadyChasing = false; // boolean if enemy chasing
    private bool ReturnToPatrol = false; // boolean if resturning to patrol from chase
    private bool AfterChasePause = false; // boolean for pausing after chase
    public Transform GroundCheck; // Position of ground check
    public float GroundCheckRadius = 0.1f; // Radius of ground check
    public LayerMask Ground; // ground layer
    public SkeletonAnimation SkeletonAnimation; // Lets me set the skeleton animation component in the inspector
    public float AttackRange = 1.5f; // How close to player before attacking
    public float AttackCooldown = 2f; // attack cooldown
    private float LastAttackTime = 0f; // Track when last attack happened
    private bool Attacking = false; // Voolean for if currently attacking
    public GameObject AttackHitbox; // Hitbox for attack 
    public float AttackDamage = 10f; // Damage dealt to player

    // TO DO: Add animations and go to spine to add in step event
    // Start is called before the first frame update
    void Start()
    {
        // Set start states
        SpawnPosition = transform.position;
        Paused = false;
        AlreadyChasing = false;
        ReturnToPatrol = false;
        AfterChasePause = false;
        SkeletonAnimation.AnimationState.SetAnimation(0, "Idle", true);
    }

    // Update is called once per frame
    void Update()
    {
        float DistanceFromPlayer = Player != null ? Vector2.Distance(transform.position, Player.position) : Mathf.Infinity; // Record distance from player (or infinity if no player)

        // patrollling logic that only runs if not in blocking states
        if (!AlreadyChasing && !ReturnToPatrol && !AfterChasePause) 
        {
            bool PatrolGroundAhead = Physics2D.OverlapCircle(GroundCheck.position, GroundCheckRadius, Ground); 
            float PatrolLeftEdge = SpawnPosition.x - PatrolDistance; 
            float PatrolRightEdge = SpawnPosition.x + PatrolDistance;
            bool AtPatrolEdge = (Direction == 1 && transform.position.x >= PatrolRightEdge) || (Direction == -1 && transform.position.x <= PatrolLeftEdge); // Check if at left or right patrol edge

            // Check if path is safe
            if (PatrolGroundAhead && !AtPatrolEdge)
            {
                if (SkeletonAnimation.AnimationName != "Walking")
                    SkeletonAnimation.AnimationState.SetAnimation(0, "Walking", true);

                transform.Translate(Vector2.right * Direction * Speed * Time.deltaTime);
            }
            else if (!Paused) 
            {
                Paused = true;
                PatrolPauseTimer = PatrolPauseTime;
                SkeletonAnimation.AnimationState.SetAnimation(0, "Idle", true); 
            }
        }

        // Pause logic for patrol edges and parts where ground isnt there
        if (Paused && !AlreadyChasing) 
        {
            if (SkeletonAnimation.AnimationName != "Idle")
                SkeletonAnimation.AnimationState.SetAnimation(0, "Idle", true);

            PatrolPauseTimer -= Time.deltaTime; 
            if (PatrolPauseTimer <= 0f)
            {
                Paused = false;
                EnemyDirection(-Direction);

                // Nudge away from edge to avoid being stuck forever
                transform.position += Vector3.right * Direction * 0.05f;
            }
            return;
        }
        
        // Start chasing if player is in range
        if (!AlreadyChasing && DistanceFromPlayer <= ChaseRange)
        {
            AlreadyChasing = true;
            ReturnToPatrol = false;
            AfterChasePause = false;
            Paused = false;
            SkeletonAnimation.AnimationState.SetAnimation(0, "Running", true); // Set running animation when starting chase
        }

        // Chasing logic
        if (AlreadyChasing) 
        {
            // Check if close enough to attack first
            if (DistanceFromPlayer <= AttackRange && Time.time >= LastAttackTime + AttackCooldown && !Attacking)
            {
                StartCoroutine(HandleAttack());
                return; 
            }

            // actual chase logic starts now
            if (!Attacking)
            {
                if (DistanceFromPlayer > ChaseEndRange) 
                {
                    AlreadyChasing = false;
                    AfterChasePause = true;
                    PauseAfterChaseTimer = PauseAfterChaseTime;
                    SkeletonAnimation.AnimationState.SetAnimation(0, "Idle", true);
                    return;
                }

                // Always face the player first
                if ((Player.position.x - transform.position.x) * Direction < 0)
                {
                    EnemyDirection((Player.position.x - transform.position.x) > 0 ? 1 : -1);
                }

                float PlayerDistance = Mathf.Abs(Player.position.x - transform.position.x);
                bool NeedToMove = PlayerDistance > 0.1f; 
                
                bool ShouldMoveToPlayer = NeedToMove && ((Player.position.x > transform.position.x && Direction == 1) || 
                                             (Player.position.x < transform.position.x && Direction == -1));
                // check for ground before chasing
                if (ShouldMoveToPlayer)
                {
                    bool GroundAhead = Physics2D.OverlapCircle(GroundCheck.position, GroundCheckRadius, Ground);
                    
                    if (GroundAhead)
                    {
                        if (SkeletonAnimation.AnimationState.GetCurrent(0) == null || SkeletonAnimation.AnimationState.GetCurrent(0).Animation.Name != "Running") 
                        {
                            SkeletonAnimation.AnimationState.SetAnimation(0, "Running", true);
                        }

                        float Step = ChaseSpeed * Time.deltaTime;
                        Vector3 TargetPosition = new Vector3(Player.position.x, transform.position.y, transform.position.z);
                        transform.position = Vector3.MoveTowards(transform.position, TargetPosition, Step);
                    }
                    else
                    {
                        if (SkeletonAnimation.AnimationName != "Idle")
                            SkeletonAnimation.AnimationState.SetAnimation(0, "Idle", true);
                    }
                }
                else
                {
                    if (SkeletonAnimation.AnimationName != "Idle")
                        SkeletonAnimation.AnimationState.SetAnimation(0, "Idle", true);
                }
            }
            return;
        }

        // Pause after chase logic
        if (AfterChasePause)
        {
            if (SkeletonAnimation.AnimationName != "Idle")
                SkeletonAnimation.AnimationState.SetAnimation(0, "Idle", true);

            PauseAfterChaseTimer -= Time.deltaTime; 
            if (PauseAfterChaseTimer <= 0f) 
            {
                AfterChasePause = false;
                ReturnToPatrol = true;

                // Calculates positions of left and right patrol edges for which to go to
                float LeftEdge = SpawnPosition.x - PatrolDistance;
                float RightEdge = SpawnPosition.x + PatrolDistance;
                float DistanceToLeft = Mathf.Abs(transform.position.x - LeftEdge);
                float DistanceToRight = Mathf.Abs(transform.position.x - RightEdge);
                ReturnEdgeXPosition = (DistanceToLeft < DistanceToRight) ? LeftEdge : RightEdge;
                EnemyDirection((ReturnEdgeXPosition > transform.position.x) ? 1 : -1);
            }
            return;
        }

        // Returning to patrol logic
        if (ReturnToPatrol)
        {
            if (SkeletonAnimation.AnimationName != "Walking")
                SkeletonAnimation.AnimationState.SetAnimation(0, "Walking", true);
            // check for ground again
            bool groundAhead = Physics2D.OverlapCircle(GroundCheck.position, GroundCheckRadius, Ground); // Check for ground
            if (!groundAhead)
            {
                return;
            }
            float Step = Speed * Time.deltaTime;
            Vector3 TargetPosition = new Vector3(ReturnEdgeXPosition, transform.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, TargetPosition, Step);

            // Flip sprite
            if ((ReturnEdgeXPosition - transform.position.x) * Direction < 0)
            {
                EnemyDirection((ReturnEdgeXPosition - transform.position.x) > 0 ? 1 : -1);
            }

            // check iif patrol edge reached
            if (Mathf.Abs(transform.position.x - ReturnEdgeXPosition) < 0.05f)
            {
                transform.position = new Vector3(ReturnEdgeXPosition, transform.position.y, transform.position.z);
                ReturnToPatrol = false;

                EnemyDirection((ReturnEdgeXPosition == SpawnPosition.x - PatrolDistance) ? 1 : -1);

                // Slight nudge to the side of the edge to stop flipping forever on next patrol check (glitch that happened)
                transform.position += Vector3.right * Direction * 0.05f;
            }
            return;
        }
    }

    // function which flips the enemy direction
    private void EnemyDirection(int NewDirection) 
    {
        Direction = NewDirection;
        if (SkeletonAnimation != null)
        {
            SkeletonAnimation.skeleton.ScaleX = Direction > 0 ? 1 : -1;
        }
    }
    // draw gizmos for project report
    void OnDrawGizmos() 
    {
        if (GroundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(GroundCheck.position, GroundCheckRadius);
        }
        
        // Draw chase detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ChaseRange);
        
        // Draw chase end range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, ChaseEndRange);
        
        // Draw attack range
        Gizmos.color = new Color(1f, 0.5f, 0f); 
        Gizmos.DrawWireSphere(transform.position, AttackRange);
    }

    // attack coroutine for hitbox timing and animations
    IEnumerator HandleAttack()
    {
        Attacking = true;
        LastAttackTime = Time.time;

        // Play attack animation
        if (SkeletonAnimation != null)
        {
            SkeletonAnimation.AnimationState.SetAnimation(0, "Attack", false); 
        
            var attackAnimation = SkeletonAnimation.skeletonDataAsset.GetSkeletonData(false).FindAnimation("Attack");
            float attackDuration = attackAnimation != null ? attackAnimation.Duration : 0.5f; // Default to 0.5s if animation not found
        
            // Enable hitbox partway through attack so it lines up with animation
            float hitboxActivationTime = attackDuration * 0.8f; 
            float hitboxActiveTime = 0.6f;
        
            yield return new WaitForSeconds(hitboxActivationTime);
        
            if (AttackHitbox != null)
            {
                AttackHitbox.SetActive(true);
            }
            yield return new WaitForSeconds(hitboxActiveTime);
        
            if (AttackHitbox != null)
            {
                AttackHitbox.SetActive(false);
            }
        
            float remainingTime = attackDuration - hitboxActivationTime - hitboxActiveTime;
            if (remainingTime > 0)
            {
                yield return new WaitForSeconds(remainingTime);
            }
        }

        Attacking = false;
    
        // Return to running animation if still chasing
        if (AlreadyChasing)
        {
            SkeletonAnimation.AnimationState.SetAnimation(0, "Running", true);
        }
    }
}