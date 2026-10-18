using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Hollow Knight attack data has each attack being about a quarter of a second each. Windup is about 5 frames, impact 3 frames, and recovery 6 frames (How fast is it sampled, though?)

//Because of how Ground Attack 3 works, I should include a way to hold multiple attack buffers. Then events that currently only clear one instead clear all of them
//Naturally AttackObject needs a way to determine how many attack buffers are needed

public class ActionEvent
{
    public event Action action;
    public void LaunchEvent() { action?.Invoke(); }
}

[RequireComponent(typeof(PlayerBase))]
public class PlayerAttack : MonoBehaviour
{
    //Serialize fields
    [SerializeField] float bufferSpeed;
    [SerializeField] HitboxScript weapon; //I need to find a way to get all potential hitboxes from an attack object. Load them in I guess? For now I'll just do this. 
    [SerializeField] List<ComboObject> comboList; //Maybe have a combo object and have it be a list of those (with the combo object being a list of attack objects)

    //Component references
    PlayerBase charBase;
    Animator anim;
    Rigidbody rb;

    //Events
    ActionEvent comboReset;
    ActionEvent endVelocity;

    //Coroutines
    Coroutine runAttack;
    Coroutine attackBuffer;

    //Properties
    bool inRecovery { get { return TryGetComponent(out Animator locAnim) ? (runAttack is not null && locAnim.GetNextAnimatorStateInfo(0).IsTag("Common")) : false; } }

    //Misc variables
    ComboObject activeCombo;
    int comboCounter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Cache components
        charBase = GetComponent<PlayerBase>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        //Subscribe to events necessary to make the attacks work
        //I don't think I need inCommon anymore and can just look up from the PlayerBase
        charBase.onAttackPerform += () => { ExecuteAttack(false, 0.25f); };

        //Run through the hitboxes childed to this and force them to subscribe to onComboReset
        comboReset = new ActionEvent();
        weapon.LoadDelegate(comboReset);

        //I should probably create a priority system later, but for now I should check if there's an active combo and the index of its attack. This is only when I have a complete combo.
    }

    //For some reason it's not moving on to the final attack. Why?
    void ExecuteAttack(bool runQueue, float transitionTime)
    {
        //Tell me if we're currently in a common state. Should be "true" at first but then "false" if I click in the middle of an attack
        if (runAttack is null || runQueue) //"runAttack is null" needs to also have an "inRecovery" property be false
        {
            //Look up the attack best suited for the situation
            AttackObject attack = LookUpAttack();

            if (attack is not null)
            {
                //In case this is accessed through runQueue, nullify the execute attack coroutine
                if (runAttack is not null) { StopCoroutine(runAttack); runAttack = null; } //Send an event saying to nullify the coroutine instead of stopping it here maybe

                //Run attack (Need a better selection process later)
                if (transitionTime > 0) { anim.CrossFadeInFixedTime(attack.name, transitionTime, 0); }
                else { anim.Play(attack.name, 0); }

                //runAttack = StartCoroutine(RunAttack(attack));
                endVelocity = new ActionEvent();
                runAttack = charBase.RewriteVelocity(endVelocity, () => RunAttack(attack));
            }
        }

        //Otherwise queue an attack
        else
        {
            if (attackBuffer is not null) { StopCoroutine(attackBuffer); attackBuffer = null; }
            attackBuffer = StartCoroutine(AttackBuffer());
        }
    }

    AttackObject LookUpAttack()
    {
        //First see if an active combo can be continued
        //I should rework this later so that it can potentially continue combos or transition to different combos
        if (activeCombo is not null) 
        {
            //I think I should have a completely separate condition telling me just if it's not in recovery (but runAttack IS running, too)
            //if (comboCounter + 1 < activeCombo.attacks.Count && activeCombo.Authenticate(charBase.inCommon && !inRecovery, charBase.grounded)) 
            if (comboCounter + 1 < activeCombo.attacks.Count && activeCombo.Authenticate(charBase.grounded) && runAttack is not null && !inRecovery)
            {
                comboCounter++;
                return activeCombo.attacks[comboCounter];
            } 
        }

        //I need to make it so this only works for starting combos. As such it should be an "else" clause (for now at least)
        else
        {
            foreach (ComboObject combo in comboList)
            {
                //I think I should have a completely separate condition telling me just if it's not in recovery (but runAttack IS running, too)
                //if (combo.Authenticate(charBase.inCommon && !inRecovery, charBase.grounded))
                if (charBase.inCommon && !inRecovery && combo.Authenticate(charBase.grounded))
                {
                    activeCombo = combo;
                    return combo.attacks[0];
                }
            }
        }

        //Return null if no attack fits the situation
        return null;
    }

    IEnumerator AttackBuffer()
    {
        yield return new WaitForSecondsRealtime(bufferSpeed);
        attackBuffer = null;
    }

    IEnumerator RunAttack(AttackObject attackData)
    {
        //Wait a frame for everything to get properly situated
        yield return null;

        //Set up the hitbox script with values it's going to need (and reset its hit cache)
        weapon.LoadStats(attackData);

        //Check the normalized time of the animation for key events
        float normalizedTime = 0f;
        while (normalizedTime < 1f)
        {
            //Get the attack's current normalized time
            AnimatorStateInfo attackState;
            if (anim.GetNextAnimatorStateInfo(0).IsTag("Attack")) { attackState = anim.GetNextAnimatorStateInfo(0); }
            else if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack")) { attackState = anim.GetCurrentAnimatorStateInfo(0); }
            else { if (normalizedTime == 0) { print("Could not find an attack state. Make sure what ever you're transitioning to is properly labeled"); } break; }
            normalizedTime = attackState.normalizedTime;

            //Turn on weapon collider during impact frames (off when not in impact frames)
            //During impact frames reset the velocity so that it will push forward the proper distance over the attack frame duration
            //Otherwise, if it is after input period, set velocity to zero (But only if not inCommon). I might be able to weave between current and prospective velocities
            //For now I'll coast to see if it's jarring or not
            if (normalizedTime >= attackData.impactStart && normalizedTime < attackData.impactEnd)
            {
                //Activate the weapon
                weapon.ActivateWeapon(true);

                //Push forward for impact
                //Unsurprisingly I think PlayerBase is overwritting the velocity. Should I send an overwrite velocity back to it that resets when checked?
                float pushForwardMagnitude = attackData.pushForward / (attackState.length * (attackData.impactEnd - attackData.impactStart));
                charBase.velocityOverwrite = new VelocityOverwrite(new Vector3(Mathf.Sin(Mathf.Deg2Rad * rb.rotation.eulerAngles.y), 0, Mathf.Cos(Mathf.Deg2Rad * rb.rotation.eulerAngles.y)) * pushForwardMagnitude);
            }
            else { weapon.ActivateWeapon(false); }

            //Process attack queuing between the end of impact frames and the end of impact end
            if (normalizedTime > attackData.impactEnd && normalizedTime <= attackData.inputEnd)
            {
                //If the attack buffer isn't null then move on to the next attack
                if (attackBuffer is not null)
                {
                    //Nullify buffer
                    StopCoroutine(attackBuffer); attackBuffer = null;

                    //Run the queued attack
                    ExecuteAttack(true, 0f);
                }
                else 
                {
                    comboReset.LaunchEvent();
                }
            }

            //Wait for next frame
            yield return null;
        }

        //Reset the combo trackers
        activeCombo = null;
        comboCounter = 0;

        //Run the Execute Attack function again if there's a buffer lined up
        if (attackBuffer != null)
        {
            //Nullify buffer
            StopCoroutine(attackBuffer); attackBuffer = null;

            //Run the queued attack
            ExecuteAttack(true, 0f);
        }

        //Otherwise nullify the coroutine because Execute Attack isn't going to handle it
        else { runAttack = null; }
    }
}
