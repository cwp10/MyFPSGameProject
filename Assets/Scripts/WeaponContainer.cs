using UnityEngine;
using System.Collections;

public class WeaponContainer : MonoBehaviour {

	public Weapon[] weapons;
	public NetworkWeapon netWeapon;

	void OnEnable() {
		netWeapon.eventWeaponChange += SetWeapon;
	}

	void OnDisable() {
		netWeapon.eventWeaponChange -= SetWeapon;
	}
	
	public void SetWeapon(int index) {

		netWeapon.firePos = null;
		netWeapon.muzzleParticle = null;
        netWeapon.weaponRate = 2.0f;
        netWeapon.damage = 0.0f;

        for (int i = 0; i < weapons.Length; ++i) {
			if(index == i) {
				weapons[i].gameObject.SetActive(true);
                netWeapon.weaponRate = weapons[i].weaponRate;
                netWeapon.damage = weapons[i].damage;

                if (weapons[i].firePos != null) {
					netWeapon.firePos = weapons[i].firePos;
				}
				if(weapons[i].muzzleParticle != null) {
					netWeapon.muzzleParticle = weapons[i].muzzleParticle;
					netWeapon.muzzleParticle.Stop();
				}
			} else {
				weapons[i].gameObject.SetActive(false);
			}
		}
	}
}
