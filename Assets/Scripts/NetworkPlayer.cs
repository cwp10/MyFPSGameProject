using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityStandardAssets.Characters.FirstPerson;

public class NetworkPlayer : NetworkBehaviour {
	
	public GameObject firstPersonCharacter;
	public GameObject firstPerson;
	public GameObject thirdPerson;
    public GameObject weaponCam;

	[HideInInspector] public Animator anim;
	[HideInInspector] public CharacterController characterController;
	[HideInInspector] public NetworkMovement movement;
	[HideInInspector] public NetworkHealth health;

	public bool isDead;

	public delegate void OnResetPlayer();
	public event OnResetPlayer eventResetPlayer;

    private GameObject[] spawnPoints;

	void Awake () {
	
		movement = GetComponent<NetworkMovement>();
		health = GetComponent<NetworkHealth>();
        spawnPoints = GameObject.FindGameObjectsWithTag("SwpawnPoint");
    }

	void Start() {

		if(isLocalPlayer) {
			GameObject.Find("Game Camera").SetActive(false);
			firstPerson.SetActive(true);
			thirdPerson.SetActive(false);
  
            anim = firstPerson.GetComponent<Animator>();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

        } else {
			firstPerson.SetActive(false);
			thirdPerson.SetActive(true);
           
            anim = thirdPerson.GetComponent<Animator>();
		}

		characterController = this.GetComponent<CharacterController>();
		GetComponent<FirstPersonController>().enabled = isLocalPlayer;

		firstPersonCharacter.GetComponent<AudioListener>().enabled = isLocalPlayer;
        firstPersonCharacter.GetComponent<Camera>().enabled = isLocalPlayer;
        weaponCam.SetActive(isLocalPlayer);
    }

	public void PlayerDead(bool isExplosion) {

		if(isDead)
			return;

		isDead = true;
		anim.SetBool("IsDead", isDead);
		characterController.enabled = false;
		GetComponent<FirstPersonController>().enabled = false;
		GetComponent<NetworkWeapon>().enabled = false;
		GetComponent<NetworkMovement>().enabled = false;
        weaponCam.SetActive(false);

        if (isLocalPlayer) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (isExplosion) {
            CmdVisiblePlayer(false);
        }
        Invoke("RespawnReady", 2.0f);
        Invoke("Respawn", 6.0f);
	}

    [Command(channel = 1)]
    private void CmdVisiblePlayer(bool isVisible)
    {
        RpcVisiblePlayer(isVisible);
    }

    [ClientRpc(channel = 1)]
    private void RpcVisiblePlayer(bool isVisible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer ren in renderers)
        {
            ren.enabled = isVisible;
        }
    }

	void RespawnReady() {
        CmdVisiblePlayer(false);
        
        characterController.enabled = true;
        GetComponent<FirstPersonController>().enabled = isLocalPlayer;

        transform.position = spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position;
    }

    void Respawn() {
        isDead = false;
       
        GetComponent<NetworkWeapon>().enabled = true;
        GetComponent<NetworkMovement>().enabled = true;
        weaponCam.SetActive(isLocalPlayer);

        if (isLocalPlayer)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        anim.SetBool("IsDead", isDead);
        CmdVisiblePlayer(true);
        eventResetPlayer();
    }
}
