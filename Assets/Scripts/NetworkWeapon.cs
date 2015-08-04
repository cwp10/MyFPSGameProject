using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

[NetworkSettings (channel = 1)]
public class NetworkWeapon : NetworkBehaviour {
	
	public float maxBulletDist = 100;
	public float wallParticleTime = 2;
	public float bloodParticleTime = 2;

	public Transform firstPersonCharacter;
	public GameObject wallParticlePrefab;
	public GameObject playerParticlePrefab;

    public Transform firePos;
    public ParticleSystem muzzleParticle;
    public GameObject bulletPrefab;


	public float weaponRate = 0.2f;
	public float damage = 20.0f;

	private float fireRate;
	private NetworkPlayer netPlayer;
	
	void Start() {
		
		netPlayer = GetComponent<NetworkPlayer>();
	}
	
	[ClientCallback]
	void Update() {

		if(Input.GetButton("Fire1") && isLocalPlayer && netPlayer.anim.GetCurrentAnimatorStateInfo(1).normalizedTime > 0.95f) {
			fireRate += Time.deltaTime;

			if(fireRate >= weaponRate) {

                //CmdInvokeBullet();
                muzzleParticle.Stop();
                muzzleParticle.Play();

                Instantiate(bulletPrefab, firePos.position, firePos.rotation);

                var ray = new Ray(firstPersonCharacter.position, firstPersonCharacter.forward);
				var hit = new RaycastHit();
				
				if(Physics.Raycast(ray, out hit, maxBulletDist)) {
					var tag = hit.transform.tag;
					
					switch(tag) {
					case "Player":
						CmdInvokeParticle(tag, hit.point, hit.normal);
						NetworkInstanceId id = hit.transform.GetComponent<NetworkIdentity>().netId;
						CmdShoot(id);
						break;
					default:
						CmdInvokeParticle(tag, hit.point, hit.normal);
						break;
					}
				}
				netPlayer.anim.SetTrigger("Shoot");
				fireRate = 0.0f;
			}
		}

		if(Input.GetKeyDown(KeyCode.R) && isLocalPlayer && netPlayer.anim.GetCurrentAnimatorStateInfo(1).normalizedTime > 0.95f) {
			netPlayer.anim.SetTrigger("Reload");
			fireRate = 0.0f;
		}
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
    private void CmdInvokeBullet() {
        RpcInvokeBullet();
    }

    [ClientRpc(channel = 1)]
    private void RpcInvokeBullet() {
        ShowBullet();
    }

    [Command(channel = 1)]
	private void CmdInvokeParticle(string tag, Vector3 pos, Vector3 normal) {
		RpcInvokeParticle(tag, pos, normal);
	}

	[ClientRpc(channel = 1)]
	private void RpcInvokeParticle(string tag, Vector3 pos, Vector3 normal) {

		GameObject prefab;
		float time;

		switch(tag) {
		case "Player":
			prefab = playerParticlePrefab;
			time = bloodParticleTime;
			break;
		default:
			prefab = wallParticlePrefab;
			time = wallParticleTime;
			break;
		}

		ShowParticles(prefab, pos, normal, time);
	}

	public void ShowParticles(GameObject prefab, Vector3 pos, Vector3 normal, float time) {
		var particles = Instantiate(prefab, pos, Quaternion.LookRotation(normal));
		Destroy(particles, time);
	}

    public void ShowBullet() {
        muzzleParticle.Stop();
        muzzleParticle.Play();

        Instantiate(bulletPrefab, firePos.position, firePos.rotation);
    }
}
