using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class NetworkZombieAttack : NetworkBehaviour {

    private float attackRate = 1;
    private float nextAttack;
    private int damage = 10;
    private float minDistance = 2.5f;
    private float currnetDistance;
    private Transform myTransform;
    private NetworkZombieTarget targetScript;

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

                targetScript.targetTransform.GetComponent<NetworkHealth>().TakeDamage(damage, false);
            }
        }
    }

    IEnumerator Attack()
    {
        for (;;)
        {
            yield return new WaitForSeconds(0.2f);
            CheckIfTargetRange();
        }
    }
}


