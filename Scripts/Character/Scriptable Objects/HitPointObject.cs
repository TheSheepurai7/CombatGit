using System;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

[CreateAssetMenu(fileName = "HitPointObject", menuName = "Scriptable Objects/HitPointObject")]
public class HitPointObject : ScriptableObject
{
    //Serialize fields
    [SerializeField] int setMaxHealth;

    //Accessible properties
    public DamageProcedure damageProcedure;
    public DeathProcedure deathProcedure;
    public int maxHealth { get; private set; }
    public int currentHealth { get; private set; }

    //Internal variables
    string lastAttackName;
    ActionEvent action;

    void Awake()
    {
        maxHealth = setMaxHealth;
        currentHealth = setMaxHealth;
    }

    //Perhaps I should instead have the return value be based on whether the hit was successful rather than if the character is dead. I have other ways of checking that
    public bool ChangeHP(AttackObject attack, bool allowMultihit)
    {
        bool returnValue = false;
        //Only modify HP if allowing multihit or the attack name is different from the last attack name (Should instead use attack objects instead of the name)
        if (currentHealth > 0 && (allowMultihit || lastAttackName != attack.name)) 
        {
            currentHealth = Mathf.Clamp(currentHealth - attack.damage, 0, maxHealth);
            returnValue = true;
        }

        //Record attack regardless and tell me if the attack was successful
        lastAttackName = attack.name;
        return returnValue;
    }

    //It made it all the way here. Now I just need to figure out how to only make it trigger once then unsubscribe
    //Do I just make a wrapper class around the action? Can I even do that?
    public void SubscribeToReset(ActionEvent actionEvent)
    {
        action = actionEvent;
        actionEvent.action += PerformAction;
    }

    //I have confirmed that this works completely
    void PerformAction()
    {
        lastAttackName = "";
        if (action is not null) { action.action -= PerformAction; }
        action = null; //So this is causing problems for combos, but I would certainly like some way to nullify the pointer
    }
}
