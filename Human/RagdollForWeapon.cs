using MagicaCloth2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollForWeapon : MonoBehaviour
{
    public void SeperateWeaponsFromRagdoll(Vector3 targetVel)
    {
        transform.SetParent(GameManager._Instance._EnvironmentTransform, true);
        /*if (transform.Find("AttackCollider") != null)
            transform.Find("AttackCollider").gameObject.SetActive(false);
        if (transform.Find("AttackWarning") != null)
            transform.Find("AttackWarning").gameObject.SetActive(false);*/

        PlaySoundOnCollision weaponMeshPlaySound = GetComponentInChildren<PlaySoundOnCollision>();

        /*if (GetComponentInChildren<MagicaCloth>() != null)
            weaponMeshPlaySound._SoundClip = SoundManager._Instance.GetRandomSoundFromList(SoundManager._Instance._FabricHitSounds);
        else
            weaponMeshPlaySound._SoundClip = SoundManager._Instance.GetRandomSoundFromList(SoundManager._Instance._WeaponHitSounds);*/

        weaponMeshPlaySound.enabled = true;
        GameObject weaponMesh = weaponMeshPlaySound.gameObject;

        Rigidbody rb = gameObject.AddComponent(typeof(Rigidbody)) as Rigidbody;
        rb.mass = 18f;
        //rb.interpolation = RigidbodyInterpolation.Interpolate;
        //rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (weaponMesh.GetComponentInChildren<MeshRenderer>() != null)
            weaponMesh.GetComponentInChildren<MeshRenderer>().gameObject.AddComponent(typeof(MeshCollider)).GetComponent<MeshCollider>().convex = true;
        else
            //weaponMesh.AddComponent(typeof(BoxCollider));
            Debug.LogError("Mesh Renderer Not Found For Ragdoll Weapon!");

        rb.AddForce(targetVel * 4.5f);
    }
    public void DisableRagdoll()
    {
        /*if (transform.Find("AttackCollider") != null)
            transform.Find("AttackCollider").gameObject.SetActive(false);
        if (transform.Find("AttackWarning") != null)
            transform.Find("AttackWarning").gameObject.SetActive(false);*/

        PlaySoundOnCollision weaponMeshPlaySound = GetComponentInChildren<PlaySoundOnCollision>();
        weaponMeshPlaySound.enabled = false;
        GameObject weaponMesh = weaponMeshPlaySound.gameObject;

        if (weaponMesh.GetComponentInChildren<MeshCollider>() != null)
            Destroy(weaponMesh.GetComponentInChildren<MeshCollider>());
        if (GetComponentInChildren<Rigidbody>() != null)
            Destroy(GetComponentInChildren<Rigidbody>());
    }
}
