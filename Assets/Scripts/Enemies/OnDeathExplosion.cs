using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnDeathExplosion : MonoBehaviour
{
    [SerializeField] float blinkrate = .3f, lastblink, fallspeed;
    Renderer m;
    bool dead = false, landed = false, explosion = false;
    Rigidbody rb;
    public float ExplosionDuration = 1.5f;

    [SerializeField] private GameObject explosionGO, Body, prefab;

    int deathFrames;

    public GameObject explosionparticle, temp;
    bool explodeOnce = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (dead == true) // you do not need to evaluate as true dead is a boolean so if (dead == true) is the same thing as if (dead)
        {
            blink();
            FallOnDeath();

            if (explosion == true)
            {
                if (ExplosionDuration > 0)
                {
                    if (explodeOnce == false)
                    {
                    explodeOnce = true;
                    temp = Instantiate(explosionparticle, this.transform); // spawn in the explosion particle once during the explosion
                    }
                ExplosionDuration -= Time.deltaTime;
                }
                if (ExplosionDuration < 0)
                {
                stopExplosion();
                }
            }
        }
    }

    public void OnDeathVariables()
    {
        m = this.gameObject.GetComponent<Renderer>();
        dead = true;
        fallspeed = 1;
    }  
    
    void blink()
    {
        if (Time.time > blinkrate + lastblink && m.material.color == Color.white)
        {
            m.material.color = Color.red;
            lastblink = Time.time;
        }
        if (Time.time > blinkrate + lastblink && m.material.color == Color.red)
        {
            m.material.color = Color.white;
            lastblink = Time.time;
        }
    }

    void FallOnDeath()
    {
        if (deathFrames == 0)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = true;
            Vector3 explosiveForce = new Vector3(Random.Range(-5f, 5f), Random.Range(3f, 7f), Random.Range(-5f, 5f));
            rb.AddForce(explosiveForce, ForceMode.Impulse);
        }
        deathFrames++;
    }

    void ExplodeOnImpact()
    {
        explosion = true;
        explosionGO.GetComponent<SphereCollider>().enabled = true;
    }

    void stopExplosion()
    {
        explosionGO.GetComponent<SphereCollider>().enabled = false;
        Destroy(temp);


        prefab.SetActive(false);
        ResetVariables();
    }

    public void ResetVariables()
    {
        dead = false;
        landed = false;
        explosion = false;
        explodeOnce = false;
        deathFrames = 0;
        ExplosionDuration = 1.5f;
        Destroy(rb);
        m.material.color = Color.white;
        this.transform.position = prefab.transform.position;
        this.transform.rotation = prefab.transform.rotation;
        var explosiondealdamage = explosionGO.GetComponent<ExplosionDealDamage>();
        explosiondealdamage.ClearList();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (dead == false) return;
        if (collision.collider.gameObject.layer != 9 && collision.collider.gameObject.layer != 8) // Make sure the collision is with something other than enemy because it would collide with itself since the parent object has a collider
        {
            landed = true;
            ExplodeOnImpact();
        }
    }
}
