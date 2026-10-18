using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(TestScriptMain))]
public class TestScriptSub : MonoBehaviour
{
    //Store TestScriptMain
    TestScriptMain parent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Cache components
        parent = GetComponent<TestScriptMain>();

        //Wire up attack
        parent.onAttackPerform += () => { parent.RewriteCoroutine(() => AccessFromSub("Doesn't matter")); };
    }

    IEnumerator AccessFromSub(string doesntMatter)
    {
        parent.activeString = new StringHolder("Pressed attack from sub");
        yield return new WaitForSeconds(1.5f);
        print("AccessFromSub has finished");
        parent.NullCoroutine();
    }
}
