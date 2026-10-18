using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

//I should probably call this like camera suite or something
public class CameraManager : MonoBehaviour
{
    //Singleton
    public static CameraManager instance;

    //General parameters
    public bool useFollowCamera;
    [SerializeField] PlayerMovement player;
    [SerializeField] ScriptableRendererFeature fullScreenFlash; //I should have the death flash here. I don't actually need it in the death procedure object

    //Shake parameters
    [Header("Shake controls")]
    [SerializeField] float shakeDuration; //In seconds
    [SerializeField] float shakeMagnitude; //In viewport ratio ideally (0 - 0.5)
    [SerializeField] float shakeSpeed; //How fast (in seconds) it takes each impulse to travel from the center to the edge of a single dimension

    //Misc variables
    public Transform chosenCamera { get; private set; }
    Coroutine cameraShake;
    Coroutine deathFlash;
    Coroutine freezeFrame;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Enforce singleton
        if (instance != null && instance != this) { Destroy(this); } else { instance = this; }

        //Slow down the time scale so I can better see the problem with the animation
        Time.timeScale = 1;

        //Look in children for the first instance of the right camera and deactivate all the others
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Camera>() == null) { child.gameObject.SetActive(false); }
            else
            {
                if (chosenCamera == null && ((useFollowCamera && child.GetComponent<ThirdPersonCamera>() != null) || (!useFollowCamera && child.GetComponent<ThirdPersonCamera>() == null)))
                { child.gameObject.SetActive(true); chosenCamera = child; }
                else { child.gameObject.SetActive(false); }
            }
        }

        //Depending on which kind of camera is being used, toggle the dimension settings on character movement
        player.is3d = useFollowCamera;
    }

    void OnDisable()
    {
        print("Turned off the camera script");
        if (fullScreenFlash is not null) { fullScreenFlash.SetActive(false); }
    }

    public void CameraShake()
    {
        //Refresh the camera shake coroutine if it's already in progress
        if (cameraShake != null) { StopCoroutine(cameraShake); cameraShake = null; }

        //Play a new camera shake coroutine
        cameraShake = StartCoroutine(CameraShakeCoroutine());
    }

    public void DeathFlash(float flashTime)
    {
        if (deathFlash is not null) { StopCoroutine(deathFlash); }
        deathFlash = StartCoroutine(DeathFlashCoroutine(flashTime));
    }

    public void FreezeFrame(float timeFreeze, Vector2 timePair)
    {
        if (freezeFrame is not null) { StopCoroutine(freezeFrame); }
        freezeFrame = StartCoroutine(FreezeFrameCoroutine(timeFreeze, timePair));
    }

    IEnumerator CameraShakeCoroutine()
    {
        yield return null;
    }

    IEnumerator DeathFlashCoroutine(float flashTime)
    {
        fullScreenFlash.SetActive(true);
        yield return new WaitForSecondsRealtime(flashTime);
        fullScreenFlash.SetActive(false);
        deathFlash = null;
    }

    IEnumerator FreezeFrameCoroutine(float timeFreeze, Vector2 timePair)
    {
        //Turn off time scale
        Time.timeScale = 0;

        //Run the timer
        yield return new WaitForSecondsRealtime(timeFreeze);

        //Operate time slow if applicable
        Time.timeScale = timePair.x;
        yield return new WaitForSecondsRealtime(timePair.y);

        //Reset time scale
        Time.timeScale = 1;

        //Nullify coroutine
        freezeFrame = null;
    }
}
