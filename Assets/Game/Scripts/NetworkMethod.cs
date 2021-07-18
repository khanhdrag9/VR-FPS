using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Bolt;

public class NetworkMethod : NetworkBehaviour
{
    [SerializeField] Transform weaponPlace = null;
    [SerializeField] [SyncVar] string currentWeapon;

    public string CurrentWeapon => currentWeapon;

    public void SpawnWeapon(GameObject weapon)
    {
        CmdSpawnWeapon(weapon.name);
    }

    [Command(requiresAuthority = false)] void CmdSpawnWeapon(string name)
    {
        currentWeapon = name;
    }

    public void Shoot()
    {
        
    }
}
