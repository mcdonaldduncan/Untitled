using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretBehaviour : MonoBehaviour
{
    [SerializeField] private GameObject target, body, tip;
    [SerializeField] private float speed, range;
    private enum TurretState {LookingForTarget, ShootTarget};
    [SerializeField] private TurretState state;
    private enum TurretRotationType {full, half}; [Tooltip("Full = 360, Half = 180")]
    [SerializeField] private TurretRotationType rotationType;
    [SerializeField] private float delayBeforeFirstShot;

    Vector3 targetDiretion;
    Quaternion rotation;
    
    private float lastShot, ShootRate = .5f;

    private float lastValidY = 0f;

    void Start()
    {
       state = TurretState.LookingForTarget;
       rotationType = TurretRotationType.full;
    }
    void Update()
    {
        switch (state) //handles what the turret shhould be doing at cetain states.
        {
            case TurretState.LookingForTarget:
                Aim();
                LookForLineOfSight();
                break;
            case TurretState.ShootTarget:
                Aim();
                Shoot();
                break;
            default:
                break;
        }
    }
    void Aim() //This is pointing the torret towards the player as long as he is in range
    {
        float tempSpeed = speed;
        if (Vector3.Distance(this.transform.position, target.transform.position) < range)
        { 
            targetDiretion = transform.position - target.transform.position;

            if (rotationType == TurretRotationType.full)
            {
                rotation = Quaternion.LookRotation(targetDiretion);
            }
            if (rotationType == TurretRotationType.half)
            {
                Vector3 tempRotation = Quaternion.LookRotation(targetDiretion).eulerAngles;

                // This would be the best option if we want it to "lose sight" of the player when it gets behind it,
                // it wouldnt snap to the other side until the player enters their radius on that side.
                //if (tempRotation.y <= 180 && tempRotation.y >= 0)
                //{
                //    lastValidY = tempRotation.y;
                //}


                // This would be best if the turret always knows where you are, it will snap to the side closest to where the player would reenter their radius.
                // To make this work with the above commented out code, you would switch the if statements to if (tempRotation.y > 180) { tempRotation.y = lastValidY }
                if (tempRotation.y > 270 && tempRotation.y > 180)
                {
                    tempSpeed *= .1f;
                    tempRotation.y = 0.5f;
                }
                if (tempRotation.y <= 270 && tempRotation.y > 180)
                {
                    tempSpeed *= .1f;
                    tempRotation.y = 179.5f;
                }

                rotation = Quaternion.Euler(tempRotation);
            }
            
            // Lerp can be somewhat low performance because it starts dealing with extremely small increments at the end,
            // rotateTowards keeps the movement constant in degree/second

            //transform.rotation = Quaternion.Lerp(transform.rotation, rotation, tempSpeed * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, tempSpeed * Time.deltaTime * 180);
        }
       
    }
    void LookForLineOfSight() //Shoots raycasts at the player and if it hits the player then it has line of sight
    {
        RaycastHit hit;
        Debug.DrawRay(tip.transform.position, -targetDiretion, Color.green);
        Physics.Raycast(tip.transform.position, -targetDiretion, out hit, range);
        if (hit.collider != null)
        {
            if (hit.collider.tag == target.tag)
            { 
            Debug.Log("Player Detected");
            StartCoroutine(Delay());
        
            }
            else if (hit.collider.tag != target.tag)
            {
            Debug.Log("Player NOT Detected");
            }
        }
        else { }
    }
    void Shoot() //Shoots at the player
    {
        RaycastHit hit;
        Debug.DrawRay(tip.transform.position, -targetDiretion, Color.red);
       
        Physics.Raycast(tip.transform.position, -targetDiretion, out hit, range);
        try
        {
         if (hit.collider.tag == target.tag)
          {
             if (Time.time > ShootRate + lastShot)
             {
               Debug.Log("Player was shot, dealing damage.");
               lastShot = Time.time;
             }
          }
            else
            {
                StateLookingForTarget();
            }
        }
        catch (System.Exception)
        {
         StateLookingForTarget();
        }
    }
    void StateLookingForTarget() //swaps state to looking for target
    {
        state = TurretState.LookingForTarget;
    }
    void StateShootTarget() //swaps state to shooting target
    {
        state = TurretState.ShootTarget;
    }
    IEnumerator Delay() //Creating a delay before the first shot to give the player time to react
    {
        yield return new WaitForSeconds(delayBeforeFirstShot);
        StateShootTarget();
    }
}