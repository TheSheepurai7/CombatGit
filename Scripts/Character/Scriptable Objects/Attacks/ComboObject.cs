using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Combo Object")]
public class ComboObject : ScriptableObject
{
    //Check whether the conditions for the combo are still valid. Necessary because some combos can theoretically have either a false or true state to be valid
    enum StateCheck { FALSE, TRUE, EITHER }

    //Attacks in the combo
    public List<AttackObject> attacks;

    //Combo conditions
    [Header("Conditions")]
    [SerializeField] StateCheck onGround;

    //I think I may just have onGround and no inCommon for now. This might change, though, if I create branching combos
    public bool Authenticate(bool onGround) //I need to be able to properly parse recovery. I think I can do this in the attack script proper, though
    {
        //Authenticate individual critera
        bool onGroundCorrect = this.onGround == StateCheck.EITHER || onGround == Convert.ToBoolean(Convert.ToInt32(this.onGround));

        //Authenticate all criteria
        return onGroundCorrect;
    }
}
