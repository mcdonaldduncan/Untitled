using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SoldierEnemyScript : MonoBehaviour, IDamageable
{
    [SerializeField]
    private Animator animator;

    [SerializeField] private LayerMask layerMask = 0; // for wondering
    [SerializeField] private float minWaitTimeWander, maxWaitTimeWander, wonderDistanceRange; // wait timer for wandering
    
    float waitTimer;
    bool wanderMiniState; //state for wandering and wanderingIdle

    private enum SoldierState {guard, wanderer, chase, originalSpot, retaliate, emergencyHeal};
    [Tooltip("/ Guard = Stand in one place until the player breaks line of sight / Wanderer = walks around / Chase = when the soldier goes after the enemy")]
    [SerializeField] private SoldierState st;
    private SoldierState origianlst;
    [SerializeField] private GameObject tip, visionPoint, body;
    [SerializeField] private float rotationspeed, range;
    [SerializeField] private NavMeshAgent agent; 
    Vector3 targetDiretion, originalPos;
    Quaternion rotation, originalrot;

    [SerializeField] List<Vector3> wanderSpots = new List<Vector3>();
    [Tooltip("Distance to current wander spot before the player moves to next wander spot.")]
    [SerializeField] private float wanderSpotOffset = 1f, delayBeforeMove = 2, originalPosOFFSET = 1f;
    private float lastShot;
    int i = 0;
    bool changeDir = false;

    [SerializeField] float ShootRate = .5f;
   
    public TrailRenderer BulletTrail;
    [SerializeField] private float Damage;

    public float Health { get { return health; } set { health = value; } }
    [SerializeField] private float health, maxHealth;
    float distanceToPlayer;
    //FirstPersonController playerController;



    // READ
    // You were checking distance like 15 times per frame, if you are going to have class variables use them as such
    // There is no point in saving a class variable distancetoplayer if you are just going to check the distance manually every time you need it, set all the variables that need
    // updating at the beginning of each frame and run calculations off those variables, dont recalulate variables over and over again on the same frame
    // Be conscious of your code when building features


    string playerTag = "Player";

    public void TakeDamage(float damageTaken)
    {
        if (st == SoldierState.guard || st == SoldierState.wanderer)
        {
            st = SoldierState.retaliate;
        }
        Health -= damageTaken;
        CheckForDeath();
    }

    public void CheckForDeath()
    {
        if (Health <= 0)
        {
            if (distanceToPlayer <= LevelManager.Instance.Player.DistanceToHeal)
            {
                LevelManager.Instance.Player.Health += (LevelManager.Instance.Player.PercentToHeal * maxHealth);
            }
            //this.gameObject.GetComponent<CheckForDrops>().DropOrNot();
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        maxHealth = health;
        //target = GameObject.FindWithTag("Player");
        //playerController = target.GetComponent<FirstPersonController>();
        origianlst = st;
        originalPos = transform.position;
        originalrot = this.transform.rotation;
        var pos = this.transform.position;
    }

    //Spaces between methods!!!
    // Update is called once per frame
    void FixedUpdate()
    {
        distanceToPlayer = Vector3.Distance(gameObject.transform.position, LevelManager.Instance.Player.transform.position);
        animator.SetFloat("Blend", agent.velocity.magnitude);
        switch (st)
        {
            case SoldierState.guard:
                Aim();
                LineOfSightWithPlayer();
                break;
            case SoldierState.wanderer:
                Aim();
                LineOfSightWithPlayer();
                if (wanderMiniState)
                {
                    ShouldWander();
                }
                else if (!wanderMiniState)
                {
                    WanderIde();
                }
                break;
                
                case (SoldierState.chase):
                Aim();
                Shoot();
                ChasePlayer();
                break;
            case (SoldierState.originalSpot):
                ReturnToOriginalSpot();
                break;
            case (SoldierState.retaliate):
                RetaliationAim();
                RetaliationShoot();
                RetaliationChasePlayer();
                break;

            //Doesnt work yet - THEN DONT LEAVE IT IN!
            //case (SoldierState.emergencyHeal): // 1 = got to destination, 0 = no destination, -1 = going to destination
                //if (RunAway() == 1)
                //{
                //    //HealAfterRunAway();
                //}
                //if (RunAway() == 0)
                //{
                //    //st = SoldierState.retaliate; // no closest edge with cover from player = last stand fight to death
                //}
                //if (RunAway() == -1)
                //{
                //    //RunAway();
                //}
                //break;

            default:
                break;
        }
        
    }

    void Aim() //This is pointing the soldier towards the player as long as he is in range
    {
        float tempSpeed = rotationspeed;
        if (distanceToPlayer < range)
        {
            targetDiretion = LevelManager.Instance.Player.transform.position - tip.transform.position;
            rotation = Quaternion.LookRotation(targetDiretion);
            body.transform.rotation = Quaternion.RotateTowards(body.transform.rotation, rotation, tempSpeed * Time.deltaTime * 180);
        }
    }
    
    void LineOfSightWithPlayer()
    {
        RaycastHit hit;
        Debug.DrawRay(visionPoint.transform.position, targetDiretion, Color.green);
        Physics.Raycast(visionPoint.transform.position, targetDiretion, out hit, range/1.2f);
        if (hit.collider != null)
        {
            // Dont look up the players tag, we know it is player and it never changes
            if (hit.collider.CompareTag(playerTag))
            {
                //Debug.Log("Player Detected");
                Invoke("StateChase", 2);
            }
        }
        else { }
    }

    void Shoot() //Shoots at the player
    {
        RaycastHit hit, hit2;
        Debug.DrawRay(tip.transform.position, targetDiretion, Color.red);
        var offsetx = 0;
        var offsety = 0;
        if (distanceToPlayer > range/2)
        {
            offsetx = Random.Range(-5, 5);
            offsety = Random.Range(0, 5);
        }
        if (distanceToPlayer > ((range / 3) * 2))
        {
            offsetx = Random.Range(-10, 10);
            offsety = Random.Range(0, 5);
        }
        // Why dont you do this same step for z? Our game is 3 dimensional, if you are on the same x plane they would only have a chance to miss on y and given the player is tall missing slightly in y will probably still hit
        Physics.Raycast(tip.transform.position, new Vector3(targetDiretion.x + offsetx, targetDiretion.y + offsety, targetDiretion.z), out hit, range);
        if (hit.collider != null)
        {
            if (Physics.Raycast(tip.transform.position, targetDiretion, out hit2, range)) //check line of sight
            {
                if (hit2.collider.tag == playerTag) //if player is in line of sight, shoot
                {
                    // The player tag never changes, why get the tag from the player each time you check
                    if (Time.time > ShootRate + lastShot)
                    {
                        var bt = Instantiate(BulletTrail, tip.transform.position, rotation);
                        bt.GetComponent<MoveForward>().origin = this.gameObject.transform.rotation;
                        bt.GetComponent<MoveForward>().target = hit.point;
                        //bt.GetComponent<MoveForward>().damage = Damage;
                        //Debug.Log("Player was shot, dealing damage.");
                        //Use compare tag not equivalency
                        if (hit.collider.CompareTag(playerTag))
                        {
                            LevelManager.Instance.Player.TakeDamage(Damage);
                        }
                        lastShot = Time.time;
                    }
                }
            }    
        }
        if (distanceToPlayer > range)
        {
            StateReturnOriginalSpot();
        }
    }

    // WHY DO YOU USE VECTOR3.DISTANCE 9 MILLION TIMES WHEN YOU SAVE A VARIABLE CALLED DISTANCETOPLAYER!!!!!!!
    // I removed all of them

    private void ShouldWander() 
    {
        if (waitTimer > 0) // if wait timer is > 0 do nothing except waiting
        {
            waitTimer -= Time.deltaTime;
            return;
        }
        agent.SetDestination(RandomPosInSphere(originalPos, wonderDistanceRange, layerMask)); //Set destination inside a random sphere
                                                          //wander
        wanderMiniState = !wanderMiniState;
        // I just made a bool to make sure it doesnt call again after it sets path to make animating it easier and prevent setting more paths while its not completed
    }

    private void WanderIde()
    {
        if (agent.pathStatus != NavMeshPathStatus.PathComplete)
        return;

        waitTimer = Random.Range(minWaitTimeWander, maxWaitTimeWander);
        wanderMiniState = !wanderMiniState; // I just made a bool to make sure it doesnt call again after it completes path to make animating it easier.
    }

    Vector3 RandomPosInSphere(Vector3 origin, float distance, LayerMask layerMask)
    {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * distance; //Create a sphere
        randomDirection += origin; //add origin to place it at the origin

        NavMeshHit navHit;

        NavMesh.SamplePosition(randomDirection, out navHit, distance, layerMask); //get an appropriate navHit where we can set destination later

        return navHit.position; //return hit position to set destination on it.
    }

    void ChasePlayer()
    {
        // Bruh, why do you calculate distance so many times each frame?
       //var dist = Vector3.Distance(this.transform.position, LevelManager.Instance.Player.transform.position);

       if (distanceToPlayer <= range/3)
       {
            agent.ResetPath();
       }
       else if ( distanceToPlayer < range &&  distanceToPlayer > range/1.25)
       {
            agent.SetDestination(LevelManager.Instance.Player.transform.position);
       }

    }
    void StateChase() //swaps state to Chase
    {
        st = SoldierState.chase;
    }
    void StateReturnOriginalSpot()
    {
        st = SoldierState.originalSpot;
    }
    void ReturnToOriginalSpot()
    {
        agent.SetDestination(originalPos);
        if (Vector3.Distance(this.transform.position, originalPos) < originalPosOFFSET)
        {
            this.transform.rotation = originalrot;
            ReturnToOriginalState();
        }
    }
    void ReturnToOriginalState()
    {
        st = origianlst;
    }

    void RetaliationAim() //This is pointing the soldier towards the player as long as he is in range
    {
        float tempSpeed = rotationspeed;

        targetDiretion = LevelManager.Instance.Player.transform.position - transform.position;
        rotation = Quaternion.LookRotation(targetDiretion);
        body.transform.rotation = Quaternion.RotateTowards(body.transform.rotation, rotation, tempSpeed * Time.deltaTime * 180);

    }

    // Why do you have a retaliation aim and retaliation shoot methods that are practically identical to normal shoot and aim
    void RetaliationShoot()
    {
        RaycastHit hit, hit2;
        Debug.DrawRay(tip.transform.position, targetDiretion, Color.red);
        var offsetx = 0;
        var offsety = 0;
        if (distanceToPlayer > range / 2)
        {
            offsetx = Random.Range(-5, 5);
            offsety = Random.Range(0, 5);
        }
        if (distanceToPlayer > ((range / 3)*2))
        {
            offsetx = Random.Range(-10, 10);
            offsety = Random.Range(0, 5);
        }
        // Why dont you do this same step for z? Our game is 3 dimensional, if you are on the same x plane they would only have a chance to miss on y and given the player is tall missing slightly in y will probably still hit
        Physics.Raycast(tip.transform.position, new Vector3(targetDiretion.x + offsetx, targetDiretion.y + offsety, targetDiretion.z), out hit, range);
        if (hit.collider != null)
        {
            if (Physics.Raycast(tip.transform.position, targetDiretion, out hit2, range)) //check line of sight
            {
                if (hit2.collider.CompareTag(playerTag)) //if player is in line of sight, shoot
                {
                    if (Time.time > ShootRate + lastShot)
                    {
                        var bt = Instantiate(BulletTrail, tip.transform.position, rotation);
                        bt.GetComponent<MoveForward>().origin = this.gameObject.transform.rotation;
                        bt.GetComponent<MoveForward>().target = hit.point;
                        //bt.GetComponent<MoveForward>().damage = Damage;
                        //Debug.Log("Player was shot, dealing damage.");
                        if (hit.collider.CompareTag(playerTag))
                        {
                            LevelManager.Instance.Player.TakeDamage(Damage);
                        }
                        lastShot = Time.time;
                    }
                }
            }
        }
    }

    void RetaliationChasePlayer()
    {
        var dist = Vector3.Distance(this.transform.position, LevelManager.Instance.Player.transform.position);
        if (dist <= range / 3)
        {
            agent.ResetPath();
        }
        else if (dist > range)
        {
            agent.SetDestination(LevelManager.Instance.Player.transform.position);
        }
    }

    int RunAway()
    {
        NavMeshHit hit;
        RaycastHit hitRC;
        if (NavMesh.FindClosestEdge(transform.position, out hit, NavMesh.AllAreas))
        {
            Physics.Raycast(tip.transform.position, targetDiretion, out hitRC);
            //if (hitRC.collider.tag != target.tag)
            //{
                agent.SetDestination(hit.position);
            //}
            if (this.transform.position == hit.position)
            {
                agent.ResetPath();
                
                return 1;
            }
        }
        else
        {
            return 0;
        }
        return -1;
    }

    void HealAfterRunAway()
    {
        if (Time.time > ShootRate + lastShot)
        {
            TakeDamage(-10);
            lastShot = Time.time;
        }
    }
}
