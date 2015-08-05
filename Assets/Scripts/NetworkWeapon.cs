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
	public Camera weaponCam;

    public Transform firePos;
    public ParticleSystem muzzleParticle;
    public GameObject bulletPrefab;


	public float weaponRate = 0.5f;
	public float damage = 20.0f;
	
	private float fireRate;
	private NetworkPlayer netPlayer;
	private int weaponIndex = 0;

	public delegate void OnWeaponChange(int index);
	public event OnWeaponChange eventWeaponChange;
	
	void Start() {
		
		netPlayer = GetComponent<NetworkPlayer>();
		weaponCam = firstPersonCharacter.FindChild("WeaponCam").GetComponent<Camera>();
	}
	
	[ClientCallback]
	void Update() {

        fireRate -= Time.deltaTime;

        if (Input.GetButton("Fire1") && isLocalPlayer && netPlayer.anim.GetCurrentAnimatorStateInfo(1).normalizedTime > 0.95f) {
			
			if(fireRate <= 0) {

                bool isExplosion = false;

                if (weaponIndex == 4 || weaponIndex == 7) {
                    isExplosion = true;
                }
                CmdInvokeBullet();

                var ray = new Ray(firstPersonCharacter.position, firstPersonCharacter.forward);
				var hit = new RaycastHit();
				
				if(Physics.Raycast(ray, out hit, maxBulletDist)) {
					var tag = hit.transform.tag;
                    NetworkInstanceId id;
                    switch (tag) {
					    case "Player":
                            CmdInvokeParticle(tag, hit.point, hit.normal);
                            id = hit.transform.GetComponent<NetworkIdentity>().netId;

                            if (id != netPlayer.GetComponent<NetworkIdentity>().netId) {
                                CmdShoot(id);
                            }
                            break;
                        case "Zombie":
                            CmdInvokeParticle(tag, hit.point, hit.normal);
                            id = hit.transform.GetComponent<NetworkIdentity>().netId;

                            if (id != netPlayer.GetComponent<NetworkIdentity>().netId)
                            {
                                CmdZombieShoot(id, netPlayer.GetComponent<NetworkIdentity>().netId, isExplosion);
                            }
                            break;

                        default:
                            CmdInvokeParticle(tag, hit.point, hit.normal);
                            break;

                    }
				}
				netPlayer.anim.SetTrigger("Shoot");
                fireRate = weaponRate;
            }
		}


		if(Input.GetKeyDown(KeyCode.Alpha1) && isLocalPlayer && netPlayer.anim.GetCurrentAnimatorStateInfo(1).normalizedTime > 0.95f) {
			ZoomMode(false);
			weaponIndex = 0;
			CmdWeaponChange(weaponIndex);
			fireRate = 0.5f;
		}

        if (Input.GetKeyDown(KeyCode.Alpha2) && isLocalPlayer && netPlayer.anim.GetCurrentAnimatorStateInfo(1).normalizedTime > 0.95f)
        {
            ZoomMode(false);
            weaponIndex = 4;
            CmdWeaponChange(weaponIndex);
            fireRate = 0.5f;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) && isLocalPlayer && netPlayer.anim.GetCurrentAnimatorStateInfo(1).normalizedTime > 0.95f) {
			ZoomMode(false);
			weaponIndex = 3;
			CmdWeaponChange(weaponIndex);
			fireRate = 0.5f;
		}

		if(Input.GetKeyDown(KeyCode.Alpha4) && isLocalPlayer && netPlayer.anim.GetCurrentAnimatorStateInfo(1).normalizedTime > 0.95f) {
			ZoomMode(false);
			weaponIndex = 5;
			CmdWeaponChange(weaponIndex);
            fireRate = 0.5f;
        }



		if(Input.GetKeyDown(KeyCode.R) && isLocalPlayer && netPlayer.anim.GetCurrentAnimatorStateInfo(1).normalizedTime > 0.95f) {
			netPlayer.anim.SetTrigger("Reload");
            fireRate = 0.0f;
        }

		if(Input.GetButton("Fire2") && isLocalPlayer) {

			if(weaponIndex == 5 || weaponIndex == 6) {
				ZoomMode(true);
			}
		}

		if(Input.GetButtonUp("Fire2") && isLocalPlayer) {
			ZoomMode(false);
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

    [Command(channel = 1)]
    private void CmdInvokeBullet() {
        RpcInvokeBullet();
    }

    [ClientRpc(channel = 1)]
    private void RpcInvokeBullet() {

		if(muzzleParticle != null) {
			muzzleParticle.Stop();
			muzzleParticle.Play();
		}
		
		if(firePos != null) {
			Instantiate(bulletPrefab, firePos.position, firePos.rotation);
		}
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
            case "Zombie":
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

	[Command(channel = 1)]
	private void CmdWeaponChange(int idx) {
		RpcWeaponChange(idx);
	}

	[ClientRpc(channel = 1)]
	private void RpcWeaponChange(int idx) {
		netPlayer.anim.SetInteger("WeaponType", idx);
		eventWeaponChange(idx);
	}

	public void ShowParticles(GameObject prefab, Vector3 pos, Vector3 normal, float time) {
		var particles = Instantiate(prefab, pos, Quaternion.LookRotation(normal));
		Destroy(particles, time);
	}

	void ZoomMode(bool isZoom) {

		if(isZoom) {
			if(weaponIndex == 5 || weaponIndex == 6) {
				firstPersonCharacter.GetComponent<Camera>().fieldOfView = 20.0f;
				weaponCam.enabled = false;
				GameManager.Instance.zoomCrossHair.color = Color.white;
			}
		} else {
			firstPersonCharacter.GetComponent<Camera>().fieldOfView = 60.0f;
			weaponCam.enabled = true;
			GameManager.Instance.zoomCrossHair.color = Color.clear;
		}
	} 
}
