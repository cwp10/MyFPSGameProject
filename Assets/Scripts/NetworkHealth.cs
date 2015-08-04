using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

[NetworkSettings (channel = 1)]
public class NetworkHealth : NetworkBehaviour {

	[Range(0, 100)]
	public float playerHealth = 100.0f;
	private NetworkPlayer netPlayer;

	void Awake() {
		
		netPlayer = GetComponent<NetworkPlayer>();
		netPlayer.eventResetPlayer += OnResetHealth;
	}

	public override void OnNetworkDestroy () {
		netPlayer.eventResetPlayer -= OnResetHealth;
	}

	public void TakeDamage(float dmg) {
		playerHealth -= dmg;
		RpcUpdateHealth(playerHealth);
	}

	[ClientRpc(channel = 1)]
	private void RpcUpdateHealth(float health) {
		playerHealth = health;

		if(playerHealth <= 0.0f) {
			netPlayer.PlayerDead();
		}
	}

	public void OnResetHealth() {
		playerHealth = 100;
	}
}
