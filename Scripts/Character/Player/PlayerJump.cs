using System.Collections;
using UnityEngine;

//I need to make variable jump work before air attacks, but it also means I need a way to transition from jump start to jump loop preemptively if the velocity goes below 0
[RequireComponent(typeof(PlayerBase))]
public class PlayerJump : MonoBehaviour
{
    //Inspector variables
    [SerializeField] float jumpHeight; //In meters
    [SerializeField] float timeToJumpApex; //In seconds
    [SerializeField] float downwardMovementMultiplier; //I may only need to divide the time to jump apex by this number to get time to descent
    [SerializeField] float jumpCutOff; //Uses a gravity multiplier for variable jump heights, so I'm going to need tools to tell me what up and down gravities are
    [SerializeField] float terminalVelocity; //The speed cap for character falling in m/s
    [SerializeField] float coyoteTime; //In seconds
    [SerializeField] float jumpBuffer; //In seconds

    //Component cache
    PlayerBase charBase;

    //Misc variables
    Coroutine jumpQueue;
    float currentVelocity;
    float gForce; //This is the base for all gravity interactions. This value is multiplied by modifiers for variable jump height and falling gravity
    float gModifier; //Stores what ever gravity multiplier is active.
    float initForce;
    bool jumpQueued;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Cache component
        charBase = GetComponent<PlayerBase>();

        //Set up vertical movement controls
        charBase.onJumpPerform += () => { if (jumpQueue != null) { StopCoroutine(jumpQueue); } jumpQueue = StartCoroutine(JumpQueue()); };
        charBase.onJumpCancel += () => { if (currentVelocity > 0) { gModifier = jumpCutOff; } };

        //Calculate variables based on statistics given
        gForce = (2 * jumpHeight) / (timeToJumpApex * timeToJumpApex);
        initForce = Mathf.Sqrt(2 * gForce * jumpHeight); //print(initForce);
    }

    //Might need an out bool to tell me if the character has spent enough time rising and ungrounded to execute actions in the air
    public Vector3 GetVerticalVelocity()
    {
        //Grounding logic
        if (charBase.grounded)
        {
            //No matter what the gModifier is set back to one
            gModifier = 1;

            //Initiate a jump
            if (jumpQueued && currentVelocity <= 0)
            {
                currentVelocity = initForce;
                StopCoroutine(jumpQueue);
                jumpQueued = false;
                jumpQueue = null;
            }

            //Just push the character gently to the ground
            else
            {
                currentVelocity = -gForce * Time.deltaTime;
            }
        }

        //Gravity logic
        else
        {
            //This needs to only work if the current velocity is equal to or less than zero. Otherwise the gravity modifier is changed by releasing the jump button
            //gModifier = currentVelocity <= 0 ? downwardMovementMultiplier : 1;
            if (currentVelocity <= 0) { gModifier = downwardMovementMultiplier; }
            currentVelocity = Mathf.Clamp(currentVelocity - gForce * Time.deltaTime * gModifier, -terminalVelocity, Mathf.Infinity);
        }

        //Perhaps cutting the velocity entirely here if not in a common state will work
        if (!charBase.inCommon) { currentVelocity = 0; }

        //Return the final vector
        return Vector3.up * currentVelocity;
    }

    IEnumerator JumpQueue()
    {
        jumpQueued = true;
        float timer = 0;
        while (timer < jumpBuffer) { timer += Time.deltaTime; yield return null; }
        jumpQueued = false;
        jumpQueue = null;
    }
}
