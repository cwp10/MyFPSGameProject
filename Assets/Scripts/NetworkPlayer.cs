using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityStandardAssets.Characters.FirstPerson;

public class NetworkPlayer : NetworkBehaviour {
	
	public GameObject firstPersonCharacter;
	public GameObject firstPerson;
	public GameObject thirdPerson;

	[HideInInspector] public Animator anim;
	[HideInInspector] public CharacterController characterController;
	[HideInInspector] public NetworkMovement movement;
	[HideInInspector] public NetworkHealth health;

	public bool isDead;

	public delegate void OnResetPlayer();
	public event OnResetPlayer eventResetPlayer;

	void Awake () {
	
		movement = GetComponent<NetworkMovement>();
		health = GetComponent<NetworkHealth>();
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
    }

	public void PlayerDead() {

		if(isDead)
			return;

		isDead = true;
		anim.SetBool("IsDead", isDead);
		characterController.enabled = false;
		GetComponent<FirstPersonController>().enabled = false;
		GetComponent<NetworkWeapon>().enabled = false;
		GetComponent<NetworkMovement>().enabled = false;

        if (isLocalPlayer) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
      
        Invoke("Respawn", 5.0f);
	}

	void Respawn() {
		isDead = false;
		anim.SetBool("IsDead", isDead);
		characterController.enabled = true;
		GetComponent<FirstPersonController>().enabled = isLocalPlayer;
		GetComponent<NetworkWeapon>().enabled = true;
		GetComponent<NetworkMovement>().enabled = true;

        if (isLocalPlayer)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        eventResetPlayer();
	}
}
