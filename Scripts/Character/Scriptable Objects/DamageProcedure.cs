using UnityEngine;

//This object is meant to go on a HitProcedure object which itself is meant to go on a HurtboxScript and is meant to be accessible from the CharacterBase
[CreateAssetMenu(fileName = "DamageProcedure", menuName = "Scriptable Objects/DamageProcedure")]
public class DamageProcedure : ScriptableObject
{
    //Serialize fields
    public float blinkTime;
    public float hitFreeze;
}
