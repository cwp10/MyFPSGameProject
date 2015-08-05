using UnityEngine;
using System.Collections;

public class WeaponContainer : MonoBehaviour {

	public Weapon[] weapons;
	public NetworkWeapon netWeapon;

    private int currentWeaponIndex;

	void OnEnable() {
		netWeapon.eventWeaponChange += SetWeapon;
        netWeapon.eventBulletCount += CurrentWeaponBullet;
    }

	void OnDisable() {
		netWeapon.eventWeaponChange -= SetWeapon;
        netWeapon.eventBulletCount -= CurrentWeaponBullet;
    }
	
	public void SetWeapon(int index) {

		netWeapon.firePos = null;
		netWeapon.muzzleParticle = null;
        netWeapon.weaponRate = 2.0f;
        netWeapon.damage = 0.0f;
        currentWeaponIndex = index;
        Invoke("ShowWeapon", 0.5f);
	}

    void ShowWeapon() {

        for (int i = 0; i < weapons.Length; ++i)
        {
            if (currentWeaponIndex == i)
            {
                weapons[i].gameObject.SetActive(true);
                netWeapon.weaponRate = weapons[i].weaponRate;
                netWeapon.damage = weapons[i].damage;

                netWeapon.totalBullet = weapons[i].totalBullet;
                netWeapon.currentBullet = weapons[i].currentBullet;
                netWeapon.maxBullet = weapons[i].maxBullet;
                netWeapon.maxBulletDist = weapons[i].maxBulletDist;

                if (weapons[i].firePos != null)
                {
                    netWeapon.firePos = weapons[i].firePos;
                }
                if (weapons[i].muzzleParticle != null)
                {
                    netWeapon.muzzleParticle = weapons[i].muzzleParticle;
                    netWeapon.muzzleParticle.Stop();
                }
            }
            else
            {
                weapons[i].gameObject.SetActive(false);
            }
        }

        netWeapon.SetMaxBullet();
    }

    public void CurrentWeaponBullet(int curB, int toB) {
        weapons[currentWeaponIndex].currentBullet = curB;
        weapons[currentWeaponIndex].totalBullet = toB;
        netWeapon.SetMaxBullet();
    }
}
