using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class StringHolder { public readonly string theString; public StringHolder(string inString) { theString = inString; } }

public class TestScriptMain : MonoBehaviour
{
    //The goal is to have a coroutine here and a coroutine in TestScriptSub that is connected to this object
    //Each script handles a different input to trigger the coroutine 
    //When that input is pressed, it prints a message then waits a certain number of seconds
    //And if the central coroutine gets interrupted, it tells me
    //The coroutines take in a VelocityOverwrite like object that modifies the message this script plays when Update is accessed and the object is not null
    //The two coroutines also take additional arguments that are wildly different, but do nothing as a stress test

    //Mimic the control structure of the real thing
    [SerializeField] InputActionAsset controls;

    //Events to mimic the real thing
    public event Action onAttackPerform;
    public event Action onJumpPerform;

    //Misc variables
    public StringHolder activeString; //I think the simple truth is I need this to be public after all, route it through the coroutine, package it in a delegate, then send it to RewriteCoroutine
    Coroutine runningMan;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Wire up events
        controls.FindActionMap("Player").FindAction("Jump").performed += (InputAction.CallbackContext cc) => { onJumpPerform?.Invoke(); };
        controls.FindActionMap("Player").FindAction("Attack").performed += (InputAction.CallbackContext cc) => { onAttackPerform?.Invoke(); };

        //Wire up jump
        onJumpPerform += () => { RewriteCoroutine(() => AccessFromMain(12, new Vector3(0, 3.7f, 5.5f))); };
    }

    // Update is called once per frame
    void Update()
    {
        //Print the string if there is one to print
        if (activeString is not null) { print(activeString.theString); activeString = null; }
    }

    public void RewriteCoroutine(CoroutineDelegate rewriteString)
    {
        if (runningMan is not null) { StopCoroutine(runningMan); print("Rewriting coroutine"); }
        runningMan = StartCoroutine(rewriteString());
    }

    public void NullCoroutine()
    {
        runningMan = null;
    }

    //It's starting to seem like I must access activeString directly instead of having it be a parameter since it seems to default to only being a value copy
    IEnumerator AccessFromMain(int nonsense, Vector3 bubcus)
    {
        activeString = new StringHolder("Pressed jump from main");
        yield return new WaitForSeconds(2.5f);
        print("AccessFromMain has finished");
        NullCoroutine();
    }
}
