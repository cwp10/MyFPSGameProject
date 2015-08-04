using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityStandardAssets.Characters.FirstPerson;

[NetworkSettings (channel = 0)]
public class NetworkMovement : NetworkBehaviour {

	[SyncVar] private Vector3 syncPosition;
	[SyncVar] private float syncBodyRotation;
	[SyncVar] private float syncHeadRotation;
	[SyncVar] private float syncAnimVal;
	
	[SerializeField] private float positionLerpStep = 5f;
	[SerializeField] private float rotationLerpStep = 15f;
	[SerializeField] private float minPosDist = 0.2f;
	[SerializeField] private float minBodyRotDist = 1f;
	[SerializeField] private float minHeadRotDis = 1f;

	private Vector3 lastSendPosition;
	private float lastSendBodyRotation;
	private float lastSendHeadRotation;

	private NetworkPlayer netPlayer;

	void Start() {

		netPlayer = GetComponent<NetworkPlayer>();
	}

	void FixedUpdate() {
		if(isLocalPlayer) {
			Vector3 pos = this.transform.position;
			float bodyRot = this.transform.eulerAngles.y;
			float headRot = netPlayer.firstPersonCharacter.transform.localEulerAngles.x;

			if( IsOverThreshold(lastSendPosition, pos, minPosDist) || 
			   IsOverThreshold(lastSendBodyRotation, bodyRot, minBodyRotDist) || 
			   IsOverThreshold(lastSendHeadRotation, headRot, minHeadRotDis)) {
				CmdSendData(pos, bodyRot, headRot);
				lastSendPosition = pos;
				lastSendBodyRotation = bodyRot;
				lastSendHeadRotation = headRot;
			} 
		}

		if(!isLocalPlayer) {
			Interpolate();
		}
	}

	[Client]
	public bool IsOverThreshold(Vector3 old, Vector3 last, float min) {
		return Vector3.Distance(old, last) > min;
	}

	[Client]
	public bool IsOverThreshold(float old, float last, float min) {
		return Mathf.Abs(old - last) > min;
	}

	[Client]
	private void Interpolate() {
		Vector3 newPos = Vector3.Lerp(this.transform.position, syncPosition, Time.deltaTime * positionLerpStep);
		Vector3 movement = newPos - this.transform.position; 
		netPlayer.characterController.Move(movement);

		if(Mathf.Abs(movement.normalized.magnitude) > 0.1f) {
			netPlayer.anim.SetFloat("Speed", 1.0f);
		} else {
			netPlayer.anim.SetFloat("Speed", 0.0f);
		}

		float newBodyRot = Mathf.LerpAngle(this.transform.eulerAngles.y, syncBodyRotation, Time.deltaTime * rotationLerpStep);
		this.transform.rotation = Quaternion.Euler(0, newBodyRot, 0);

		float newHeadRot = Mathf.LerpAngle(netPlayer.firstPersonCharacter.transform.localEulerAngles.x, syncHeadRotation, Time.deltaTime * rotationLerpStep);
		netPlayer.firstPersonCharacter.transform.localRotation = Quaternion.Euler(newHeadRot, 0, 0);

		if(newHeadRot < 360.0f && newHeadRot >= 270) {
			float angle = 360.0f - newHeadRot;
			float resultRot = angle / 90.0f;
			netPlayer.anim.SetFloat("BodyAngle", resultRot);
		} else{
			float angle = newHeadRot * -1f;
			float resultRot = angle / 75.0f;
			netPlayer.anim.SetFloat("BodyAngle", resultRot);
		}
	}

	[Command]
	private void CmdSendData(Vector3 pos, float bodyRot, float headRot) {
		syncPosition = pos;
		syncBodyRotation = bodyRot;
		syncHeadRotation = headRot;
	}
}
