using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

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

    [ClientRpc(includeOwner = false)] void RpcSpawnObject(string name)
    {
        Debug.Log("My self");
        var prefab = Resources.Load(name) as GameObject;
        var obj = Instantiate(prefab, weaponPlace);
    }
}
