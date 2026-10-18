using UnityEngine;

//I think the ultimate purpose of this class is going to be to process feedback from hitboxes and hurtboxes.
//Since players also have these and need something to process them I think I'm going to have it handled by an interface

public class EnemyBase : CharacterBase
{
    const float SPEED = 5;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] bool pinata;
    Vector3 startPosition;

    protected override void Start()
    {
        //Hopefully this triggers Start properly
        base.Start();

        //Pinata needs to record the starting position of the character to work
        startPosition = rb.position;
    }

    // Update is called once per frame
    // PlayerJump's gravity logic needs to be incorporated into CharacterBase alongside a toggle to ignore gravity
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        //This is going to modify the horizontal and vertical parameters. Shouldn't matter if it's hit or not
        if (pinata)
        {
            float distance = (rb.position - startPosition).magnitude;
            if (Mathf.Approximately(distance, 0)) { rb.linearVelocity = Vector3.zero; }
            else if (distance <= SPEED * Time.fixedDeltaTime) { rb.linearVelocity = (startPosition - rb.position) / Time.fixedDeltaTime; }
            else { rb.linearVelocity = (startPosition - rb.position).normalized * SPEED; }
        }
    }

    public void MarkDead()
    {
        pinata = false;
    }
}
