using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

[NetworkSettings (channel = 1)]
public class NetworkHealth : NetworkBehaviour {

	[Range(0, 100)]
	public float playerHealth = 100.0f;
	private NetworkPlayer netPlayer;
   
    private float apcolor;

	void Awake() {
		
		netPlayer = GetComponent<NetworkPlayer>();
		netPlayer.eventResetPlayer += OnResetHealth;
    }

    void Update() {

        if (apcolor > 0.0f && isLocalPlayer) {
            apcolor -= Time.deltaTime * 0.3f;
            GameManager.Instance.takeDamageImage.color = new Color(1, 1, 1, apcolor);
        }
    }

	public override void OnNetworkDestroy () {
		netPlayer.eventResetPlayer -= OnResetHealth;
	}

	public void TakeDamage(float dmg) {
		playerHealth -= dmg;

        if (playerHealth <= 0)
        {
            playerHealth = 0.0f;
        }
        GameManager.Instance.healthText.text = playerHealth.ToString();
        RpcUpdateHealth(playerHealth);
    }

	[ClientRpc(channel = 1)]
	private void RpcUpdateHealth(float health) {
		playerHealth = health;

		if(playerHealth <= 0.0f) {
			netPlayer.PlayerDead();
		}

        apcolor = 0.5f;
    }

	public void OnResetHealth() {
		playerHealth = 100;
	}
}
