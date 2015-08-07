using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class NetworkZombieTarget : NetworkBehaviour {

    private NavMeshAgent agent;
    private Transform myTransform;
    public Transform targetTransform;
    private LayerMask raycastLayer;
    private float radius = 50;
    private float searchRate = 2.0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        myTransform = transform;
        raycastLayer = 1 << LayerMask.NameToLayer("Player");
    }

    void FixedUpdate()
    {
        SearchForTarget();
        MoveToTarget();
    }

    void SearchForTarget()
    {
        if (!isServer)
        {
            return;
        }

        if (targetTransform == null)
        {
            Collider[] hitColliders = Physics.OverlapSphere(myTransform.position, radius, raycastLayer);

            if (hitColliders.Length > 0)
            {
                int randomint = Random.Range(0, hitColliders.Length);
                targetTransform = hitColliders[randomint].transform;
            }
        }

        if (targetTransform != null && targetTransform.GetComponent<NetworkPlayer>().isDead == true)
        {
            targetTransform = null;
        }
    }

    void MoveToTarget()
    {
        if (targetTransform != null && isServer)
        {
            SetNavDestination(targetTransform);
        }
    }

    void SetNavDestination(Transform dest)
    {
        agent.SetDestination(dest.position);
    }

    public void SetTarget(NetworkInstanceId id) {

        GameObject player = NetworkServer.FindLocalObject(id);
        targetTransform = player.transform;
    }
}

