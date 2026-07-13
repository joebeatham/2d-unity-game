using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

public class PracticeDummy : MonoBehaviour
{
    public int MaxHealth = 999; // Max health of enemy
    private int CurrentHealth; // Tracks current HP
    public Vector2 KnockbackForce = new Vector2(5f, 1f); // X and Y force of knockback
    public float KnockbackTime = 0.2f; // Duration of the knockback force
    private float KnockbackTimer = 0f; // Time the enemy gets knocked back for
    private Vector3 KnockbackSpeed; // Speed of knockback each frame
    public Color FlashColor = Color.red; // Flash colour on damage (red for me)
    private SkeletonAnimation SkeletonAnimation; // Spine skeleton reference
    private Color OriginalColor; // Original colour of the spine skeleton
    private Coroutine DamageFlashCoroutine; // reference to damageflash coroutine
    private bool Dying = false; // Flag to prevent damage during death animation

    // Start is called before the first frame update
    void Start()
    {
        CurrentHealth = MaxHealth;
        SkeletonAnimation = GetComponent<SkeletonAnimation>();
        // I needed to find the SkeletonAnimation through the dummyAI script because it wouldnt find the skeleton for some reason
        if (SkeletonAnimation == null)
        {
            DummyAI dummyAI = FindObjectOfType<DummyAI>();
            if (dummyAI != null)
            {
                SkeletonAnimation = dummyAI.SkeletonAnimation;
                Debug.Log("Found SkeletonAnimation through DummyAI script");
            }
        }
        if (SkeletonAnimation != null)
        {
            OriginalColor = SkeletonAnimation.skeleton.GetColor();
            Debug.Log("SkeletonAnimation found! Original color: " + OriginalColor); // I was debugging cause the flash was a big problem to get working
        }
        else
        {
            Debug.LogError("SkeletonAnimation component NOT FOUND on " + gameObject.name);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // If timer is on then knock enemy back
        if (KnockbackTimer > 0f)
        {
            transform.position += KnockbackSpeed * Time.deltaTime;
            KnockbackTimer -= Time.deltaTime;
        }
    }

    // function to make the force of the knockback
    public void ApplyKnockback(Vector2 Force) 
    {
        // Calculate knockback speed by using force and time
        KnockbackSpeed = new Vector3(Force.x, Force.y, 0) / KnockbackTime;
        KnockbackTimer = KnockbackTime; 
    }

    // Function for dummy to take damage
    public void TakeDamage(int Amount, int PlayerDirection)
    {
        // Prevent damage during death animation
        if (Dying)
        {
            return;
        }
        
        CurrentHealth -= Amount; 
        CurrentHealth = Mathf.Max(CurrentHealth, 0); 

        Vector2 AppliedForce = new Vector2(KnockbackForce.x * PlayerDirection, KnockbackForce.y); // Apply force based on player direction
        ApplyKnockback(AppliedForce);

        // Play smoke effect from damage animation without affecting body movement
        if (SkeletonAnimation != null)
        { 
            StartCoroutine(PlaySmokeEffect());
        }

        // If flash happens while another flash is already happening then interrupt the first one
        if (SkeletonAnimation != null)
        {
            if (DamageFlashCoroutine != null)
            {
                StopCoroutine(DamageFlashCoroutine);
            }
            DamageFlashCoroutine = StartCoroutine(DamageFlash());
        }
        // check if health is 0 then trigger death animation
        if (CurrentHealth <= 0)
        {
            var dummyAI = FindObjectOfType<DummyAI>(); 
            if (dummyAI != null)
            {
                dummyAI.enabled = false;
            }
    
            StartCoroutine(PlayDeathAnimation());
        }
    }

    // Coroutine to handle flash on damage
    IEnumerator DamageFlash()
    {
        var Skeleton = SkeletonAnimation.skeleton; 
        // Animation timings
        float FadeInTime = 0.05f;  
        float HoldFlashTime = 0.1f;    
        float FadeOutTime = 0.2f;  
        
        float Timer = 0f; 
        // while loop for the fade in
            while (Timer < FadeInTime)
        {
                float t = Timer / FadeInTime; 
                Color CurrentColour = Color.Lerp(OriginalColor, FlashColor, t); 
                Skeleton.SetColor(CurrentColour);
            Timer += Time.deltaTime;
            yield return null;
        }
        
        // Hold flash colour for HoldFlashTime
        Skeleton.SetColor(FlashColor);
        yield return new WaitForSeconds(HoldFlashTime);
        
        // While loop for fade out
        Timer = 0f;
        while (Timer < FadeOutTime)
        {
                float t = Timer / FadeOutTime;
                Color CurrentColour = Color.Lerp(FlashColor, OriginalColor, t);
                Skeleton.SetColor(CurrentColour);
            Timer += Time.deltaTime;
            yield return null;
        }
        
        Skeleton.SetColor(OriginalColor); 
        DamageFlashCoroutine = null; 
    }

    // Coroutine to play smoke effect from damage animation
    IEnumerator PlaySmokeEffect() 
    {
        var Skeleton = SkeletonAnimation.skeleton;
        
        // Find smoke bones by name
        var SmokeBones = new List<Spine.Bone>();
        
        // Look for bones with "smoke" in their name
        foreach (var Bone in Skeleton.Bones.Items)
        {
            if (Bone.Data.Name.ToLower().Contains("smoke"))
            {
                SmokeBones.Add(Bone);
            }
        }
        
        if (SmokeBones.Count == 0)
        {
            yield break;
        }
        
        // Get the damage animation to read keyframe data from smoke bones only
        var DamageAnimation = SkeletonAnimation.skeletonDataAsset.GetSkeletonData(false).FindAnimation("Damaged");
        
        if (DamageAnimation == null)
        {
            yield break;
        }
        
        float AnimationLength = DamageAnimation.Duration;
        float Timer = 0f;
        
        // Animate only the smoke bones by reading from the damage animation
        while (Timer < AnimationLength)
        {
            float AnimationTime = Timer;
            
            DamageAnimation.Apply(Skeleton, 0, AnimationTime, false, null, 1f, Spine.MixBlend.Setup, Spine.MixDirection.In);
            
            foreach (var SmokeBone in SmokeBones)
            {
                SmokeBone.UpdateWorldTransform();
            }
            
            Timer += Time.deltaTime;
            yield return null;
        }
    }

    // Coroutine to play death animation
    IEnumerator PlayDeathAnimation()
    {
        Dying = true;
        // Let the damage flash complete naturally instead of interrupting it 
        if (DamageFlashCoroutine != null)
        {
            yield return DamageFlashCoroutine;
        }
        else
        {
            if (SkeletonAnimation != null)
            {
                SkeletonAnimation.skeleton.SetColor(OriginalColor);
            }
        }
        
        // Clear all animation tracks and play death animation
        if (SkeletonAnimation != null)
        {
            SkeletonAnimation.AnimationState.ClearTracks();
            SkeletonAnimation.AnimationState.SetAnimation(0, "Death", false);
            
            // Get the death animation duration
            var deathAnimation = SkeletonAnimation.skeletonDataAsset.GetSkeletonData(false).FindAnimation("Death");
            if (deathAnimation != null)
            {
                yield return new WaitForSeconds(deathAnimation.Duration);
            }
            else
            {
                yield return new WaitForSeconds(1f); 
            }
        }
    }
}