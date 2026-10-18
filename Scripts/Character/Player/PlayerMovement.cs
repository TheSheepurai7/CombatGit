using UnityEngine;

[RequireComponent(typeof(PlayerBase))]
public class PlayerMovement : MonoBehaviour
{
    [HideInInspector] public bool is3d { private get; set; }

    //Inspector fields
    [SerializeField] float maxSpeed; //In meters per second (Maybe I can give access to PlayerBase for other scripts that need it?)
    [SerializeField] float maxTurnSpeed; //Degrees per second. I don't think I can use this with anything but 3d
    [SerializeField] float sprintMultiplier; //How many times the max speed the character can move while sprinting
    [SerializeField] float accelerationTime; //In seconds until full acceleration
    [SerializeField] float decelerationTime; //In seconds until full deceleration
    [SerializeField] float airAccelerationTime; //In seconds until full acceleration
    [SerializeField] float airDecelerationTime; //In seconds until full deceleration
    [SerializeField] float maxAirTurnSpeed; //Degrees per second. I don't think I can use this with anything but 3d

    //Component references
    PlayerBase charBase;
    Rigidbody rb;

    //Misc variables
    Vector2 moveVector;
    float currentVelocity;
    float recordedVelocity;
    bool isRunning;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Cache components
        charBase = GetComponent<PlayerBase>();
        rb = GetComponent<Rigidbody>();

        //Connect to onMove
        charBase.onMovePerform += (Vector2 inputValue) => { moveVector = inputValue; };
        charBase.onMoveCancel += () => { moveVector = Vector2.zero; recordedVelocity = 0; };
    }

    public Vector3 GetPlanarVelocity(bool commonState, out bool sameDirection)
    {
        sameDirection = false;
        Vector3 returnValue = is3d ? GetPlanarVelocity3D() : GetPlanarVelocity2D(commonState, out sameDirection);
        return returnValue;
    }

    Vector3 GetPlanarVelocity2D(bool commonState, out bool sameDirection)
    {
        //Calculate target velocity
        float targetVelocity = maxSpeed * moveVector.x * (charBase.grounded && isRunning ? sprintMultiplier : Mathf.Max(currentVelocity / maxSpeed, 1));

        //Calculate velocity change
        float accelMPS = maxSpeed / (charBase.grounded ? accelerationTime : airAccelerationTime);
        float decelMPS = maxSpeed / (charBase.grounded ? decelerationTime : airDecelerationTime);
        float velocityChange = SameDirection(targetVelocity, currentVelocity) && Mathf.Abs(recordedVelocity) >= currentVelocity ? accelMPS : decelMPS;

        //Then I need to shift both the recorded velocity and actual velocity based on whether the recorded velocity is lower than the actual velocity
        if (commonState)
        {
            if (Mathf.Approximately(Mathf.Abs(currentVelocity), Mathf.Abs(recordedVelocity)))
            { currentVelocity = Mathf.MoveTowards(currentVelocity, targetVelocity, velocityChange * Time.deltaTime); }
            else if (Mathf.Abs(currentVelocity) > Mathf.Abs(recordedVelocity))
            { currentVelocity = Mathf.MoveTowards(currentVelocity, recordedVelocity, velocityChange * Time.deltaTime); }
            else { recordedVelocity = currentVelocity; } //For now this properly handles sliding upon landing after an air attack and no movement input, but this may change

            if (targetVelocity != 0) { recordedVelocity = Mathf.MoveTowards(recordedVelocity, currentVelocity, velocityChange * Time.deltaTime); }
        }
        else 
        {
            currentVelocity = Mathf.MoveTowards(currentVelocity, 0, decelMPS * Time.deltaTime);
            recordedVelocity = Mathf.MoveTowards(recordedVelocity, currentVelocity, decelMPS * Time.deltaTime);
        }

        //Determine what direction the character is facing 
        if (targetVelocity != 0 && commonState) { rb.rotation = Quaternion.Euler(Vector3.up * (CameraManager.instance.chosenCamera.eulerAngles.y + (currentVelocity < 0 ? -90 : 90))); }

        //Set same direction
        sameDirection = SameDirection(targetVelocity, currentVelocity);

        //Return move vector
        return new Vector3(Mathf.Sin(Mathf.Deg2Rad * rb.rotation.eulerAngles.y), 0, Mathf.Cos(Mathf.Deg2Rad * rb.rotation.eulerAngles.y)) * Mathf.Abs(currentVelocity);
    }

    Vector3 GetPlanarVelocity3D()
    {
        return Vector3.zero;
    }

    bool SameDirection(float targetVelocity, float velocity)
    {
        return (targetVelocity > 0 && velocity >= 0) || (targetVelocity < 0 && velocity <= 0);
    }
}
