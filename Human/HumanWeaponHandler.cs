using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanWeaponHandler : MonoBehaviour
{
    private GameObject _weaponObject;
   
    
    public void EquipWeapon(GameObject weaponPrefab)
    {
        UnEquipWeapon();

        //anim
        _weaponObject = Instantiate(weaponPrefab, transform);
    }
    public void UnEquipWeapon()
    {
        if (_weaponObject == null) return;
        //anim
        Destroy(_weaponObject);
    }
}
