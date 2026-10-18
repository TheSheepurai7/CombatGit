using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//I should call this "hitbox script" and see if I can include it as a field in the attack object to activate 
public class HitboxScript : MonoBehaviour
{
    //Stats
    AttackObject activeAttack;

    //Events
    //event Action onReset;
    ActionEvent actionEvent;

    //Misc variables
    List<GameObject> objectsHit = new List<GameObject>();

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out HurtboxScript hurtBox) && hurtBox.transform.root != transform.root && !objectsHit.Contains(hurtBox.transform.root.gameObject))
        {
            //Calculate the hit point here based on the method in the ApplyHitEffect coroutine
            Vector3 hitEffectSpawn = Vector3.zero;
            if (!CameraManager.instance.useFollowCamera)
            {
                Vector3 spawnPoint3d = other.ClosestPoint(transform.position + (Quaternion.Euler(transform.eulerAngles) * Vector3.forward).normalized * 0.01f);
                hitEffectSpawn = new Vector3(15f, spawnPoint3d.y, spawnPoint3d.z);
            }

            //Actually I think I can just use the direction the character is facing regardless
            Vector2 planarVector = new Vector2(Mathf.Sin(Mathf.Deg2Rad * transform.root.eulerAngles.y), Mathf.Cos(Mathf.Deg2Rad * transform.root.eulerAngles.y)) * activeAttack.knockbackVector.x;
            //Pass the onReset event by ref to the hurtbox
            hurtBox.ReceiveHit(activeAttack, hitEffectSpawn, new Vector3(planarVector.x, activeAttack.knockbackVector.y, planarVector.y), actionEvent); 
        }
    }

    public void ActivateWeapon(bool status) { if (TryGetComponent(out Collider collider)) { collider.enabled = status; } }

    //The way I have it set up now only allows something to be hit once. I need to re-enable this somehow
    //Perhaps what I do is have HP Objects subscribe to an event on hit (funnelled through here) that triggers when a combo is done and nullifies the last attack object reference
    //Naturally the hit object has to unsubscribe from the event, though. 
    /*Let's try and see if I can load the delegate here then change the corrosponding function in PlayerAttack
    public void LoadDelegate(ref Action comboEvent)
    {
        comboEvent += () => { onReset?.Invoke(); };
    }
    //*/

    public void LoadDelegate(ActionEvent inActionEvent) { actionEvent = inActionEvent; }

    public void LoadStats(AttackObject inAttack) { activeAttack = inAttack; }
}
