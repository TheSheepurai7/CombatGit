using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Attack Object")]
public class AttackObject : ScriptableObject
{
    //I should attach the state checks to the combos instead of the individual attacks. But individual attacks SHOULD be able to read and parse input buffer data
    //enum StateCheck { FALSE, TRUE, EITHER }

    //Should probably have a way to check conditions to see if the attack can be performed. Likewise the combo object should probably have a priority system
    //Additionally the player attack script should re-order the combo list by priority on start

    public string stateName;

    [Header("Animation event information")]
    [Range(0f, 1f)] public float impactStart;
    [Range(0f, 1f)] public float impactEnd; //When the actual attack ends
    [Range(0f, 1f)] public float inputEnd; //When the attack starts to transition back to a common state
    //Here I should put a cross fade point and figure out how to manually transition with interruption from next state
    //I should have a transition object which contains a normalized time and another attack object to transition to then have a list of them instead of the input check

    //I may need a vector component for pushForward and possibly recoil. Maybe even knockback, too
    [Header("Statistical information")]
    public int damage;
    public float pushForward; //Pushes the character forward this distance towards where they're facing from animation start to impact end (Measure root motion translations for this)
    public Vector2 knockbackVector; //The direction the enemy will pushed in
    public float knockbackMagnitude; //Knocks the enemy back this distance over 0.1 seconds
}
