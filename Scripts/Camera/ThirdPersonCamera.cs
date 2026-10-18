using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    //Main serialize fields
    [SerializeField] InputActionAsset actionMap;
    [SerializeField] Vector3 targetOffset;

    //Targetting
    Transform _target;
    public Transform target { set { if (_target == null) { _target = value; AcquireTarget(); } } } //transform.position += _target.position + targetOffset; before AcquireTarget()

    //Camera modifiers
    [SerializeField] float turnSpeedX; //In angles per second. Covers planar turning
    float turnSpeedY;

    //Misc variables
    Vector2 targetTurnVelocity;
    float defaultRadius;
    float targetAngle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Activate and link action map
        if (!actionMap.FindActionMap("Player").enabled) { actionMap.FindActionMap("Player").Enable(); }

        //Set up camera controls
        InputAction moveCamera = actionMap.FindActionMap("Player").FindAction("Look");
        moveCamera.performed += (InputAction.CallbackContext cc) => { targetTurnVelocity = moveCamera.ReadValue<Vector2>(); };
        moveCamera.canceled += (InputAction.CallbackContext cc) => { targetTurnVelocity = Vector2.zero; };

        //Set up turn speed y
        turnSpeedY = turnSpeedX / 2;
    }

    // Update is called once per frame
    void Update()
    {
        //Calculate angle modifications along both dimensions
        Vector2 inputDelta = new Vector2(targetTurnVelocity.x * turnSpeedX * Time.deltaTime, -targetTurnVelocity.y * turnSpeedY * Time.deltaTime);
        targetAngle = Mathf.Clamp(targetAngle + inputDelta.y, -89, 89);

        //Calculate overhead
        float heightOffset = Mathf.Sin(Mathf.Deg2Rad * targetAngle) * defaultRadius;
        float planarRadius = Mathf.Cos(Mathf.Deg2Rad * targetAngle) * defaultRadius;
        Vector3 planarOffset = new Vector3(Mathf.Sin(Mathf.Deg2Rad * (transform.eulerAngles.y + inputDelta.x + 180)), 0, Mathf.Cos(Mathf.Deg2Rad * (transform.eulerAngles.y + inputDelta.x + 180))) * planarRadius;

        //Assemble final camera position and look point
        transform.position = _target.position + targetOffset + planarOffset + Vector3.up * heightOffset;
        transform.LookAt(_target.position + targetOffset);
    }

    void AcquireTarget()
    {
        //Position camera and use that to calculate initial values
        transform.LookAt(_target.position + targetOffset);
        targetAngle = transform.eulerAngles.x > 90 ? transform.eulerAngles.x - 360 : transform.eulerAngles.x;
        defaultRadius = (_target.position + targetOffset - transform.position).magnitude;
    }
}
