using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordHitBox : MonoBehaviour
{
    public GameObject SwordHitbox; // Sword hitbox game object assigned in Inspector
    private bool AlreadyHit = false; // Activates once per attack
    public Transform Player; // The player's transform assigned in Inspector
    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(false); // Deactivate the hitbox at the start because it was bugged otherwise
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter2D(Collider2D other)
{
    Debug.Log("=== SWORD HITBOX COLLISION ===");
    Debug.Log("Hit object: " + other.name);
    Debug.Log("Object tag: " + other.tag);
    Debug.Log("Object layer: " + other.gameObject.layer);
    Debug.Log("Layer name: " + LayerMask.LayerToName(other.gameObject.layer));

    if (AlreadyHit) return; // Only allow one hit per sword swing

    // IMPROVED SAFETY CHECK: Only hit objects on the Enemy layer (not EnemyAttack layer)
    if (other.gameObject.layer != LayerMask.NameToLayer("Enemy"))
    {
        Debug.Log("Ignoring collision - not on Enemy layer. Layer: " + LayerMask.LayerToName(other.gameObject.layer));
        return;
    }

    // Additional safety: Ignore attack hitboxes by name
    if (other.name.Contains("Attack") || other.name.Contains("Hitbox"))
    {
        Debug.Log("Ignoring attack hitbox collision with: " + other.name);
        return;
    }

    // Try to get enemy type
    PracticeDummy GroundEnemy = other.GetComponent<PracticeDummy>();
    FlyingPracticeDummy FlyingEnemy = other.GetComponent<FlyingPracticeDummy>();

    int DirectionPlayerFacing = (int)Mathf.Sign(Player.GetComponent<Move>().PlayerAnimation.skeleton.ScaleX);

    if (GroundEnemy != null) // If it's a ground enemy
    {
        Debug.Log("Hitting ground enemy!");
        GroundEnemy.TakeDamage(1, DirectionPlayerFacing);
        AlreadyHit = true;
    }
    else if (FlyingEnemy != null) // If it's a flying enemy
    {
        Debug.Log("Hitting flying enemy!");
        FlyingEnemy.TakeDamage(1, DirectionPlayerFacing);
        AlreadyHit = true;
    }
    else
    {
        Debug.Log("Hit something on Enemy layer but no enemy component found!");
    }
}

    void OnEnable()
    {
        AlreadyHit = false; // Reset at the start of each attack
    }
}
