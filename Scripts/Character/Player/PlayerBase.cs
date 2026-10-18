using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerBase : MonoBehaviour
{
    //Constant
    const int BOUNCE_MAX = 5;
    const float SKIN = 0.01f;

    //Inspector fields
    [SerializeField] InputActionAsset controls;
    [SerializeField] float groundExtension; //Might be a good idea to make this public so the jump script can read it and tell me if jumping simply doesn't work
    [SerializeField] float slopeLimit;

    //Events
    public event Action<Vector2> onMovePerform;
    public event Action onMoveCancel;
    public event Action onJumpPerform;
    public event Action onJumpCancel;
    public event Action onAttackPerform;
    ActionEvent endVelocity;

    //Component references
    Animator anim;
    CapsuleCollider cc;
    Rigidbody rb;
    PlayerMovement pm;
    PlayerJump pj;

    //States
    public bool grounded { get; private set; } //Change this into a proper property (Find a way to get past a need for RayCastHit, though)
    public bool inCommon { get { return InCommon(); } }

    //Misc variables
    public VelocityOverwrite velocityOverwrite { private get; set; } //I need to instead make this the velocity overwrite function in CharacterBase to make sure it works for CharacterBase
    Coroutine rewriteVelocity;
    int groundingMask;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Cache components
        cc = GetComponent<CapsuleCollider>();
        pm = GetComponent<PlayerMovement>();
        pj = GetComponent<PlayerJump>();
        rb = GetComponent<Rigidbody>();
        if (TryGetComponent(out Animator thisAnim)) { anim = thisAnim; }

        //Wire up events
        controls.FindActionMap("Player").FindAction("Move").performed +=
            (InputAction.CallbackContext cc) => { onMovePerform?.Invoke(controls.FindActionMap("Player").FindAction("Move").ReadValue<Vector2>()); };
        controls.FindActionMap("Player").FindAction("Move").canceled += (InputAction.CallbackContext cc) => { onMoveCancel?.Invoke(); };
        controls.FindActionMap("Player").FindAction("Jump").performed += (InputAction.CallbackContext cc) => { onJumpPerform?.Invoke(); };
        controls.FindActionMap("Player").FindAction("Jump").canceled += (InputAction.CallbackContext cc) => { onJumpCancel?.Invoke(); };
        controls.FindActionMap("Player").FindAction("Attack").performed += (InputAction.CallbackContext cc) => { onAttackPerform?.Invoke(); };

        //Just assign the grounding mask here for ease
        groundingMask = 1 << LayerMask.NameToLayer("Default");

        //For now set grounding to true as a matter of course
        grounded = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Determine grounding status
        if (Physics.SphereCast(rb.position + Vector3.up * (cc.radius + SKIN), cc.radius, Vector3.down, out RaycastHit hit, groundExtension + SKIN, groundingMask) &&
            (Vector3.Angle(Vector3.up, hit.normal) < slopeLimit || Mathf.Approximately(Vector3.Angle(Vector3.up, hit.normal), slopeLimit)))
        { grounded = true; }
        else { grounded = false; }

        //If there is no velocity overwrite, put together a linear acceleration from PlayerMove and PlayerJump
        bool sameDirection = false;
        if (velocityOverwrite is null)
        {
            sameDirection = false;
            Vector3 horizontal = TryGetComponent(out pm) && pm.enabled ? pm.GetPlanarVelocity(InCommon(), out sameDirection) : Vector3.zero;
            horizontal = grounded ? Vector3.ProjectOnPlane(horizontal, hit.normal) : horizontal;
            Vector3 vertical = TryGetComponent(out pj) && pj.enabled ? pj.GetVerticalVelocity() : Vector3.zero;
            rb.linearVelocity = CollideAndSlide(horizontal * Time.deltaTime, horizontal, rb.position, false, 0) / Time.deltaTime +
                CollideAndSlide(vertical * Time.deltaTime, vertical, rb.position, true, 0) / Time.deltaTime;
        }

        //Otherwise apply the velocity overwrite
        else { rb.linearVelocity = velocityOverwrite.velocityOverwrite; velocityOverwrite = null; }

        //Here's where I would deal with animation variables
        if (anim is not null)
        {
            anim.SetBool("SameDirection", sameDirection);
            anim.SetBool("Grounded", grounded);
            anim.SetFloat("RisingVelocity", rb.linearVelocity.y);
        }
    }

    //I think I need this to return a coroutine
    public Coroutine RewriteVelocity(ActionEvent callback, CoroutineDelegate inRewriteVelocity) //Add an action event argument here
    {
        if (rewriteVelocity is not null) { StopCoroutine(rewriteVelocity); }
        rewriteVelocity = StartCoroutine(inRewriteVelocity()); //I think I need to pass VelocityOverwrite to what ever delegate is passed here. Return the VelocityOverwrite maybe?
        callback.action += () => EndVelocity(callback, rewriteVelocity);
        return rewriteVelocity;
    }

    void EndVelocity(ActionEvent callback, Coroutine coroutine)
    {
        StopCoroutine(coroutine);
        callback = null;
    }

    Vector3 CollideAndSlide(Vector3 vel, Vector3 velInit, Vector3 pos, bool gravityPass, int depth)
    {
        //Cut the algorithm
        if (depth >= BOUNCE_MAX) { return Vector3.zero; }

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

        //No sliding necessary
        return vel;
    }

    bool InCommon()
    {
        return anim.GetNextAnimatorStateInfo(0).fullPathHash is 0 ? anim.GetCurrentAnimatorStateInfo(0).IsTag("Common") : anim.GetNextAnimatorStateInfo(0).IsTag("Common");
    }
}
