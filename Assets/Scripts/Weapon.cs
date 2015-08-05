using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour {

	public Transform firePos;
	public ParticleSystem muzzleParticle;
    public float weaponRate;
    public float damage;

    public int totalBullet;
    public int currentBullet;
    public int maxBullet;
    public float maxBulletDist;
}
