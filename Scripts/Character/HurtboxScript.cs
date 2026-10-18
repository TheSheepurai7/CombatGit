using System;
using UnityEngine;

public class HurtboxScript : MonoBehaviour
{
    //Serialize fields
    public HitPointObject hitPointGroup;
    public bool allowMultiHit;

    //Component cache
    CharacterBase charBase;

    void Start()
    {
        if (transform.root.GetComponentInChildren<CharacterBase>() is not null) { charBase = transform.root.GetComponentInChildren<CharacterBase>(); }
    }

    public void ReceiveHit(AttackObject attack, Vector3 hitLocation, Vector3 knockbackVector, ActionEvent actionEvent)
    {
        if (charBase is not null) { charBase.ReceiveHit(this, attack, allowMultiHit, hitLocation, knockbackVector, actionEvent); }
    }
}
