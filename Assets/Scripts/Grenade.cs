using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Grenade : NetworkBehaviour {

	private float speed = 1500.0f;
	public NetworkInstanceId ownerId;
	public float damage;
	public GameObject explosionParticle;

	void Start () {
		Rigidbody rigidbody = GetComponent<Rigidbody>();
		rigidbody.AddRelativeForce(Vector3.forward * speed);

		if(isServer)
		{
			Invoke("Bomb", 5.0f);
		}
	}

	void Bomb() {

		Vector2 pos = Camera.main.WorldToScreenPoint(transform.position);

		if(pos.x > 0 && pos.x < Screen.width && pos.y > 0 && pos.y < Screen.height) {
			Camera.main.GetComponent<ObjectShake>().Shake();
		}

		LayerMask mask;
		mask = (1 << 8) + (1 << 9);
		Collider[] colls = Physics.OverlapSphere(transform.position, 10, mask);
		foreach(Collider coll in colls) {
			
			RaycastHit hit;
			Vector3 originPos = transform.position;
			Vector3 collPos = coll.transform.position;
			
			if(Physics.Linecast(originPos, collPos, out hit, mask)) {
				
				if(hit.collider.gameObject.layer == 8) {
					var tag = hit.transform.tag;
					NetworkInstanceId id;
					id = hit.transform.GetComponent<NetworkIdentity>().netId;
					CmdShoot(id);
				}

				if(hit.collider.gameObject.layer == 9) {
					var tag = hit.transform.tag;
					NetworkInstanceId id;
					id = hit.transform.GetComponent<NetworkIdentity>().netId;
					CmdZombieShoot(id, ownerId, true);
				}

				if(coll.GetComponent<Rigidbody>() != null) {	
					coll.GetComponent<Rigidbody>().AddExplosionForce(200.0f, transform.position, 10, 10);
				}
			}
		}
		
		CmdInvokeParticle(transform.position, Quaternion.identity);
	}

	[Command(channel = 1)]
	private void CmdInvokeParticle(Vector3 pos, Quaternion rot) {
		RpcInvokeParticle( pos, rot);
	}
	
	[ClientRpc(channel = 1)]
	private void RpcInvokeParticle( Vector3 pos, Quaternion rot) {
		
		var particles = Instantiate(explosionParticle, pos, rot);
		Destroy(particles, 5.0f);
		Destroy(gameObject);
	}

	[Command(channel = 1)]
	private void CmdShoot(NetworkInstanceId id) {
		GameObject player = NetworkServer.FindLocalObject(id);
		var healthScript = player.GetComponent<NetworkHealth>();
		if(healthScript == null) {
			Debug.LogError("no healthScripts attached to player");
			return;
		} 
		healthScript.TakeDamage(damage);
	}

	[Command(channel = 1)]
	private void CmdZombieShoot(NetworkInstanceId id, NetworkInstanceId playerId, bool explosion)
	{
		GameObject zombie = NetworkServer.FindLocalObject(id);
		var healthScript = zombie.GetComponent<NetworkZombieHealth>();
		if (healthScript == null)
		{
			Debug.LogError("no healthScripts attached to player");
			return;
		}
		healthScript.TakeDamage(damage, playerId, explosion);
	}
}
