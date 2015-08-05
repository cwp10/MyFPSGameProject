using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class NetworkZombieHealth : NetworkBehaviour
{
    public bool isDead;
    public float health = 50;

    private Animator anim;
    private NetworkZombieTarget zombieTarget;

    public GameObject bloodExplosionPrefab;

    void Start() {
        anim = GetComponent<Animator>();
        zombieTarget = GetComponent<NetworkZombieTarget>();
        isDead = false;
    }

    public void TakeDamage(float dmg, NetworkInstanceId id, bool explosion)
    {
        health -= dmg;
        zombieTarget.SetTarget(id);
        RpcUpdateHealth(health, explosion);   
    }

    [ClientRpc(channel = 1)]
    private void RpcUpdateHealth(float hlt, bool explosion)
    {
        health = hlt;
        
        if (health <= 0 && !isDead)
        {
            isDead = true;
            anim.SetTrigger("Dead");

            if (explosion) {
                Instantiate(bloodExplosionPrefab, transform.position, transform.rotation);
                Destroy(gameObject);
            }
            else {
                Destroy(gameObject, 2.0f);
            }   
        }
    }
}

