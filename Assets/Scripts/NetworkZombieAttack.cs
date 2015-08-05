using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class NetworkZombieAttack : NetworkBehaviour {

    private float attackRate = 3;
    private float nextAttack;
    private int damage = 10;
    private float minDistance = 2;
    private float currnetDistance;
    private Transform myTransform;
    private NetworkZombieTarget targetScript;

    /*
    [SerializeField]
    private Material zombieGreen;
    [SerializeField]
    private Material zombieRed;*/

    void Start()
    {
        myTransform = transform;
        targetScript = GetComponent<NetworkZombieTarget>();

        if (isServer)
        {
            StartCoroutine(Attack());
        }
    }

    void CheckIfTargetRange()
    {
        if (targetScript.targetTransform != null)
        {
            currnetDistance = Vector3.Distance(targetScript.targetTransform.position, myTransform.position);

            if (currnetDistance < minDistance && Time.time > nextAttack)
            {
                nextAttack = Time.time + attackRate;

                targetScript.targetTransform.GetComponent<NetworkHealth>().TakeDamage(damage);
                //StartCoroutine(ChangeZombieMat());
                //RpcChangZombieAppearance();
            }
        }
    }

    /*
    IEnumerator ChangeZombieMat()
    {
        GetComponent<Renderer>().material = zombieRed;
        yield return new WaitForSeconds(attackRate / 2);
        GetComponent<Renderer>().material = zombieGreen;
    }

    [ClientRpc]
    void RpcChangZombieAppearance()
    {
        StartCoroutine(ChangeZombieMat());
    }*/

    IEnumerator Attack()
    {
        for (;;)
        {
            yield return new WaitForSeconds(0.2f);
            CheckIfTargetRange();
        }
    }
}


