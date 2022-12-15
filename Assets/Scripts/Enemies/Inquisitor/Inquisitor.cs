using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

public class Inquisitor : MonoBehaviour, IDamageable
{
    [SerializeField] Animator m_Animator;
    [SerializeField] GameObject m_Follower_GO;
    [SerializeField] GameObject m_AttackSpawn;
    [SerializeField] GameObject m_Shield;
    [SerializeField] GameObject m_DeathExplosion;
    [SerializeField] Transform m_Target;
    [SerializeField] float m_TimeBetweenAttacks;
    [SerializeField] float m_StartingHealth;
    [SerializeField] float m_FollowerCooldown;
    [SerializeField] float m_LaserDamage;
    [SerializeField] float m_MaxFollowerDistance;
    [SerializeField] float m_MaxForce;
    [SerializeField] float m_Speed;
    [SerializeField] public List<FakeOrbit> m_Orbits;
    [SerializeField] List<Transform> m_FollowerSpawns;

    float lastAttackTime;

    bool canAttack => Time.time - lastAttackTime > m_TimeBetweenAttacks && !isReacting;

    Follower m_Follower;

    public float Health { get; set; }

    float cooldownProgress;

    bool isTracking = false;
    bool shieldsUp = true;
    bool inPhase2;
    bool isAttacking;
    bool isReacting;

    Vector3 m_Velocity;
    Vector3 m_Acceleration;
    Vector3 m_TargetPosition;

    WaitForSeconds resetDelay = new WaitForSeconds(.25f);

    private void OnEnable()
    {
        //potentialTargets = FindObjectsOfType<FirstPersonController>().Select(item => item.transform).ToList();
    }

    void Start()
    {
        Init();
    }

    public void Init()
    {
        m_Target = LevelManager.Instance.Player.transform;
        Health = m_StartingHealth;
    }

    public void TakeDamage(float damageTaken)
    {
        Health -= damageTaken;
        CheckForDeath();
    }

    public void CheckOrbits()
    {
        if (m_Orbits.Where(x => x.gameObject.activeSelf).Any())
            return;

        if (!shieldsUp)
        {
            m_Animator.SetBool("isAttacking", false);
            m_Animator.SetBool("isReacting", true);
            isReacting = true;
            Invoke(nameof(DeactivateShield), 2);
        }

        if (shieldsUp && !inPhase2)
        {
            m_Animator.SetBool("Phase2", true);
            StartCoroutine(ResetOrbits());
            shieldsUp = false;
            inPhase2 = true;
        }
    }

    void DeactivateShield()
    {
        m_Shield.SetActive(false);
    }

    IEnumerator ResetOrbits()
    {
        int i = 0;

        while (i < 2 && i < m_Orbits.Count)
        {
            m_Orbits[i].gameObject.SetActive(true);
            m_Orbits[i].Health = 3;
            i++;
            yield return null;
        }

        foreach (var orbit in m_Orbits)
        {
            if (!orbit.gameObject.activeSelf)
            {
                yield return resetDelay;
                orbit.gameObject.SetActive(true);
                orbit.Health = 3;
            }
        }
    }

    public void SetTracking(bool state)
    {
        isTracking = state;
    }

    public void CheckForDeath()
    {
        if (Health <= 0)
        {
            //Debug.Log("Inquisitor Destroyed");
            m_DeathExplosion.SetActive(true);
            gameObject.SetActive(false);
            if (m_Follower == null) return;
            m_Follower.Despawn();
        }
    }

    public void ReactOver()
    {
        m_Animator.SetBool("isReacting", false);
        isReacting = false;
    }

    public void AttackOver()
    {
        m_Animator.SetBool("isAttacking", false);
    }

    public void MaintainAttack()
    {
        m_Animator.SetFloat("SpeedMult", 0);
        Invoke(nameof(EndAttack), 8);
        m_AttackSpawn.SetActive(true);
        isAttacking = true;
        m_TargetPosition = m_Target.position;
        m_AttackSpawn.transform.LookAt(LevelManager.Instance.Player.transform.position);
    }

    private void EndAttack()
    {
        isAttacking = false;
        m_Animator.SetFloat("SpeedMult", 1);
        Invoke(nameof(Endbeam), 1);
        lastAttackTime = Time.time;
    }

    void Endbeam()
    {
        m_AttackSpawn.SetActive(false);
    }

    void Update()
    {
        SpawnFollower();
        HandleAttacking();
        HandleRotation();
        HandleDamage();
        HandleTargeting();
    }

    public void SpawnFollower()
    {
        if (Vector3.Distance(transform.position, m_Target.position) > m_MaxFollowerDistance) return;
        
        if (isTracking) return;

        cooldownProgress += Time.deltaTime;

        if (cooldownProgress < m_FollowerCooldown) return;

        if (m_Follower == null)
        {
            m_Follower = Instantiate(m_Follower_GO).GetComponent<Follower>();
            m_Follower.Init(m_Target, this, m_FollowerSpawns[0].position);
        }
        else
        {
            m_Follower.Init(m_Target, this, m_FollowerSpawns[0].position);
        }
        
        isTracking = true;
        cooldownProgress = 0;
    }

    private void HandleAttacking()
    {
        if (!inPhase2) return;
        if (!canAttack) return;

        m_Animator.SetBool("isAttacking", true);
    }

    void HandleRotation()
    {
        if (!isAttacking) return;
        
        Vector3 lookDirection = transform.position - m_Target.position;
        Vector3 cross = Vector3.Cross(m_Target.TransformDirection(Vector3.up), lookDirection);
        Vector3 normalCross = cross.normalized;

        transform.LookAt(new Vector3(m_Target.position.x, transform.position.y, m_Target.position.z)  + normalCross * 15f);

        m_AttackSpawn.transform.LookAt(m_TargetPosition);

    }

    void HandleDamage()
    {
        if (!isAttacking) return;

        if (Physics.Raycast(m_AttackSpawn.transform.position, m_TargetPosition - m_AttackSpawn.transform.position, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Player"))
            {
                LevelManager.Instance.Player.TakeDamage(m_LaserDamage * Time.deltaTime);
            }
        }

    }

    void HandleTargeting()
    {
        m_Acceleration += CalculateSteering(m_Target.position);
        m_Velocity += m_Acceleration;
        m_TargetPosition += m_Velocity * Time.deltaTime;
        m_Acceleration = Vector3.zero;
    }

    Vector3 CalculateSteering(Vector3 currentTarget)
    {
        Vector3 desired = currentTarget - m_TargetPosition;
        desired = desired.normalized;
        desired *= m_Speed;
        Vector3 steer = desired - m_Velocity;
        steer = steer.normalized;
        steer *= m_MaxForce;
        return steer;
    }
}
