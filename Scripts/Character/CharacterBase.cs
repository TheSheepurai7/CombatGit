using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VelocityOverwrite { public readonly Vector3 velocityOverwrite; public VelocityOverwrite(Vector3 velocityOverwrite) { this.velocityOverwrite = velocityOverwrite; } }
public delegate IEnumerator CoroutineDelegate();

//I need to trace what ever applies the push forward of attacks and instead have it access the Rewrite Velocity function (or make sure it's already doing that)
//I think I can do this easily by just setting velocityOverwrite to private and looking for what ever throws an error
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider))]
public abstract class CharacterBase : MonoBehaviour //Make it abstract later
{
    //Constants
    const float KNOCKBACK_TIME = 0.1f;
    const float DEATH_SCALE = 2;
    const int BOUNCE_MAX = 5;
    const float SKIN = 0.01f;

    //Serialized components
    [SerializeField] float groundExtension; //Might be a good idea to make this public so the jump script can read it and tell me if jumping simply doesn't work
    [SerializeField] float slopeLimit;

    //Component cache
    protected Animator anim;
    protected Rigidbody rb;
    Collider col;

    //Damage blink
    List<Material> materialList;
    Dictionary<Material, Color> colorDictionary;
    Coroutine damageBlink;

    //Knockback
    Vector3 originalKnockbackVector;
    float remainingKnockbackMagnitude;
    Coroutine rewriteVelocity;

    //States
    public bool grounded { get; private set; } //Change this into a proper property (Find a way to get past a need for RayCastHit, though)
    public bool inCommon { get { return InCommon(); } }

    //Hitbox dictionary
    Dictionary<HurtboxScript, HitPointObject> hurtboxes = new Dictionary<HurtboxScript, HitPointObject>();

    //Movement variables
    protected Vector3 horizontal;
    protected Vector3 vertical;
    protected bool sameDirection;

    //Animator fields (Checks to see if they exist)
    bool hasSameDirection;
    bool hasGrounded;
    bool hasRisingVelocity;

    //Misc variables
    public VelocityOverwrite velocityOverwrite;
    int groundingMask;

    protected virtual void Start()
    {
        //Cache components
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        //I need to link one of the hitpoint objects to the primary HP bar
        //My first thought is to have a flag on the object itself and then simply pick up the first such object with that flag to ensure only one is flagged

        //I collect all of the hurtboxes attached to the object and store them in the hurtboxes dictionary
        List<HitPointObject> hitPointObjects = new List<HitPointObject>();
        foreach (HurtboxScript hurtbox in GetComponentsInChildren<HurtboxScript>())
        {
            HitPointObject instance = null;
            foreach (HitPointObject hpo in hitPointObjects) { if (hpo.name == hurtbox.hitPointGroup.name) { instance = hpo; break; } }
            if (instance == null) { instance = Instantiate(hurtbox.hitPointGroup); instance.name = hurtbox.hitPointGroup.name; hitPointObjects.Add(instance); }
            hurtboxes.Add(hurtbox, instance);
        }

        //Cache materials in renderers
        materialList = new List<Material>();
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>()) { materialList.Add(renderer.material); }
        foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>()) { materialList.Add(renderer.material); }

        //Cache colors in materials
        colorDictionary = new Dictionary<Material, Color>();
        foreach (Material material in materialList) { colorDictionary.Add(material, material.color); }

        //Just assign the grounding mask here for ease
        groundingMask = 1 << LayerMask.NameToLayer("Default");

        //Scrub the animator for common variables
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            switch (param.name) 
            {
                case "SameDirection": hasSameDirection = true; break;
                case "Grounded": hasGrounded = true; break;
                case "RisingVelocity": hasRisingVelocity = true; break;
            }
        }
    }

    //I need to manage common animations and stuff here. Probably finally integrate this with EnemyBase and PlayerBase while I'm at it. 
    protected virtual void FixedUpdate()
    {
        //Determine grounding status
        RaycastHit hit = new RaycastHit();
        grounded = GroundCharacter(ref hit);

        //If there is no velocity overwrite, get a velocity input from some kind of source and collide/slide it
        if (velocityOverwrite is null)
        {
            //It seems I need horizontal and vertical as global variables that are modified before the base FixedUpdate function runs
            horizontal = grounded ? Vector3.ProjectOnPlane(horizontal, hit.normal) : horizontal;

            //I think I can only process one collider at a time. But I can at least switch for one of the three types
            rb.linearVelocity = CollideAndSlide(horizontal * Time.deltaTime, horizontal, rb.position, false, 0) / Time.deltaTime +
                CollideAndSlide(vertical * Time.deltaTime, vertical, rb.position, true, 0) / Time.deltaTime;
        }

        //Otherwise apply the velocity overwrite
        else { print("Attempting to rewrite velocity"); rb.linearVelocity = velocityOverwrite.velocityOverwrite; velocityOverwrite = null; }

        //Here's where I would deal with animation variables
        if (anim is not null)
        {
            //I need a way to only execute these if the variable of the given name actually exists

            if (hasSameDirection) { anim.SetBool("SameDirection", sameDirection); }
            if (hasGrounded) { anim.SetBool("Grounded", grounded); }
            if (hasRisingVelocity) { anim.SetFloat("RisingVelocity", rb.linearVelocity.y); }
        }
    }

    //Public functions
    public void ReceiveHit(HurtboxScript hurtbox, AttackObject attack, bool allowMultihit, Vector3 hitLocation, Vector3 knockbackVector, ActionEvent actionEvent)
    {
        //Attempt to change HP and execute hit or death code depending on resulting HP if it was successful
        if (hurtboxes[hurtbox].ChangeHP(attack, allowMultihit))
        {
            //Do damage procedure
            if (hurtboxes[hurtbox].currentHealth > 0)
            {
                //Run or potentially restart the damage blink
                if (damageBlink is not null) { StopCoroutine(damageBlink); }
                damageBlink = StartCoroutine(DamageBlink(hurtboxes[hurtbox].damageProcedure.blinkTime));

                //Run the below code in this block if there isn't enough hyper armor to prevent it (Need a system for this later)

                //Play the animation
                anim.CrossFadeInFixedTime("Hit", 0.25f);

                //I need to overwrite any coroutines that write to VelocityOverwrite
                //Probably what I'll do is have a function that takes in where the request came from. If the source changes then the current coroutine ends and the new one begins

                //Subscribe the HP object to the delegate
                hurtboxes[hurtbox].SubscribeToReset(actionEvent);

                //Apply knockback (Comment this out while I check if the hit effect is actually being applied in the right location)
                RewriteVelocity(() => ApplyKnockBack(knockbackVector.normalized, attack.knockbackMagnitude));
            }

            //Otherwise do death procedure
            else
            {
                //Now I just need to properly plug in the death procedure
                CameraManager.instance.DeathFlash(hurtboxes[hurtbox].deathProcedure.deathFlashTime);

                //I think, based on if the death procedure has it ragdoll on death, I have applyKnockBack become a different coroutine. Otherwise just ApplyKnockBack as normal
                if (hurtboxes[hurtbox].deathProcedure.ragdoll)
                {
                    //I need to unlock rotation for the rigidbody(ies)
                    if (TryGetComponent(out EnemyBase enemyBase)) { enemyBase.MarkDead(); } //With this becoming integrated with playerbase I need to change or even delete this
                    rb.freezeRotation = false; rb.useGravity = true;
                    RewriteVelocity(() => ApplyRagdollHit(knockbackVector.normalized, attack.knockbackMagnitude * hurtboxes[hurtbox].deathProcedure.deathMultiplier, hitLocation));
                }
                //Either way, I think CharacterBase needs to be deactivated (That means the hurtbox needs to be modified so it only fires if there's an active CharacterBase)
                enabled = false;
            }

            //Pretty sure I run the hit effect here
            StartCoroutine(ApplyHitEffect(hitLocation, attack.knockbackVector, hurtboxes[hurtbox].currentHealth > 0 ? 1 : DEATH_SCALE));

            //Run freeze frame
            CameraManager.instance.FreezeFrame(hurtboxes[hurtbox].damageProcedure.hitFreeze, hurtboxes[hurtbox].currentHealth > 0 ? new Vector2(1, 0) : new Vector2(hurtboxes[hurtbox].deathProcedure.deathSlowScale, hurtboxes[hurtbox].deathProcedure.deathSlowTime));
        }
    }

    public void RewriteVelocity(CoroutineDelegate inRewriteVelocity)
    {
        if (rewriteVelocity is not null) { StopCoroutine(rewriteVelocity); }
        rewriteVelocity = StartCoroutine(inRewriteVelocity());
    }

    //Private functions
    Vector3 CollideAndSlide(Vector3 vel, Vector3 velInit, Vector3 pos, bool gravityPass, int depth)
    {
        //Cut the algorithm
        if (depth >= BOUNCE_MAX) { return Vector3.zero; }

        //I think I need to set up 3 different blocks for the primitive types from the outset
        if (col is CapsuleCollider)
        {
            //Obviously cast as a CapsuleCollider
            CapsuleCollider cc = (CapsuleCollider)col;

            //Obstacle detected (Perhaps I change the way skinning is applied based on gravity pass)
            Vector3 castPoint = pos + Vector3.up * (gravityPass ? cc.radius + SKIN : cc.radius);
            float radius = gravityPass ? cc.radius : cc.radius - SKIN;
            if (Physics.CapsuleCast(castPoint, pos + Vector3.up * (cc.height - cc.radius), radius, vel.normalized, out RaycastHit hit,
            vel.magnitude + SKIN, groundingMask))
            {
                //Create snap and slide vectors
                Vector3 snap = hit.distance > SKIN ? vel.normalized * (hit.distance - SKIN) : Vector3.zero;
                Vector3 slide = Vector3.ProjectOnPlane(vel - snap, hit.normal).normalized * (vel - snap).magnitude;

                //Prep hit angle for special cases
                float angle = Vector3.Angle(Vector3.up, hit.normal);

                //Snap to surface if in the gravity pass and the surface meets the slope tolerance
                if ((angle < slopeLimit || Mathf.Approximately(angle, slopeLimit)) && gravityPass)
                {
                    return snap;
                }

                //Otherwise slide along walls if not in the gravity pass
                else if (angle > slopeLimit && !Mathf.Approximately(angle, slopeLimit) && !gravityPass)
                {
                    //I should probably fiddle with this so that the y coordinate is completely erased
                    //I could use this as a way to screw with horizontal acceleration
                    slide *= 1 - Vector3.Dot(new Vector3(hit.normal.x, 0, hit.normal.z).normalized, -new Vector3(velInit.x, 0, velInit.z).normalized);
                }

                //Now it should work normally (until I need to kneecap the magnitude along walls)
                return snap + CollideAndSlide(slide, velInit, pos + snap, gravityPass, depth + 1);
            }
        }
        else { print("Collider of the type present is not accounted for"); }

        //No sliding necessary
        return vel;
    }

    bool GroundCharacter(ref RaycastHit hit)
    {
        //Well, first I need to loop through all of the grounding colliders
        //I think what I'm inevitably going to have to do is test for it being one of the three primative types and have a different cast for each
        //If it's a mesh collider I just throw an error, freeze the game, and say "This character can't have a mesh collider as a grounding collider"
        if (col is CapsuleCollider)
        {
            //Calculate the center and axis of the collider
            CapsuleCollider capsule = col as CapsuleCollider;
            Vector3 center = transform.position + capsule.center;
            Vector3 direction = Vector3.zero;
            switch (capsule.direction)
            {
                case 0: direction = transform.right; break;
                case 1: direction = transform.up; break;
                case 2: direction = transform.forward; break;
                default: print("Something is wrong with the capsule direction"); break;
            }
            //Adjust the direction length here
            direction = direction.normalized * capsule.height / 2;

            //Do a simple cast downward just to make sure the hit result shows up in the FixedUpdate loop
            if (Physics.CapsuleCast(center + direction, center - direction, capsule.radius - SKIN, Vector3.down, out hit, groundExtension + SKIN, groundingMask) &&
                Vector3.Angle(Vector3.up, hit.normal) < slopeLimit || Mathf.Approximately(Vector3.Angle(Vector3.up, hit.normal), slopeLimit))
            { return true; }
        }
        else { print("A collider of this type has not been accounted for"); }

        return false;
    }

    bool InCommon()
    {
        return anim.GetNextAnimatorStateInfo(0).fullPathHash is 0 ? anim.GetCurrentAnimatorStateInfo(0).IsTag("Common") : anim.GetNextAnimatorStateInfo(0).IsTag("Common");
    }

    //Coroutines
    IEnumerator ApplyHitEffect(Vector3 location, Vector2 knockbackVector, float scale)
    {
        //Establish direction first since both position and rotation depend on it
        Vector3 direction = (Quaternion.Euler(transform.eulerAngles) * Vector3.forward).normalized;

        //Create and transform the effect
        GameObject hitEffect = Instantiate(Resources.Load<GameObject>("Prefabs/Hit Effect"), location, Quaternion.identity);
        foreach (ParticleSystem particleSystem in hitEffect.GetComponentsInChildren<ParticleSystem>())
        { var main = particleSystem.main; main.startRotation = Vector2.Angle(Vector2.up, knockbackVector) * Mathf.Deg2Rad; main.startSize = main.startSize.constant * scale; }

        //Cycle through a list of particle systems in hitEffect and check until either the hitEffect is null or there are no particle systems left
        while (hitEffect != null && hitEffect.GetComponentsInChildren<ParticleSystem>().Length > 0) { yield return null; }

        //Destroy the main loop effect object if it still exists
        if (hitEffect != null) { Destroy(hitEffect); }
    }

    IEnumerator ApplyKnockBack(Vector3 knockbackVector, float knockbackMagnitude)
    {
        //I need to recalculate knockback vector and magnitude in case there are already values for these present
        originalKnockbackVector = (knockbackVector.normalized + originalKnockbackVector.normalized).normalized;
        remainingKnockbackMagnitude += knockbackMagnitude;

        //For the time being just set it to continuously push it back
        float timer = 0;
        float originalKnockbackMagnitude = remainingKnockbackMagnitude;
        while (timer < KNOCKBACK_TIME)
        {
            //Calculate the velocity needed to move the character by the knockbackMagnitude in the knockback time
            float magnitude = originalKnockbackMagnitude / KNOCKBACK_TIME;

            //Apply the knockback to the rigidbody
            velocityOverwrite = new VelocityOverwrite(originalKnockbackVector * magnitude);

            //Add time scale to relevant variables
            remainingKnockbackMagnitude -= originalKnockbackMagnitude / KNOCKBACK_TIME * Time.fixedDeltaTime;
            timer += Time.fixedDeltaTime;

            //Wait for next fixed update
            yield return new WaitForFixedUpdate();
        }

        //Reset knockback variables and cancel coroutine
        //Debug.Break(); //Have this here to make sure the knockback actually moves the object where it needs to
        rb.linearVelocity = Vector3.zero;
        originalKnockbackVector = Vector3.zero;
        remainingKnockbackMagnitude = 0;
        rewriteVelocity = null;
    }

    IEnumerator ApplyRagdollHit(Vector3 knockbackVector, float knockbackMagnitude, Vector3 hitLocation)
    {
        yield return null;
    }

    IEnumerator DamageBlink(float blinkTime)
    {
        //Change the material color
        foreach (Material material in materialList) { material.color = Color.red; }

        //Run the timer
        yield return new WaitForSeconds(blinkTime);

        //Switch the material color back
        foreach (Material material in materialList) { material.color = colorDictionary[material]; }

        //Nullify coroutine
        damageBlink = null;
    }
}
