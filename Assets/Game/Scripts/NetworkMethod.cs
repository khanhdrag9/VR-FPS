using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Bolt;

public class NetworkMethod : NetworkBehaviour
{
    [SerializeField] public Camera playerCamera = null;
    [SerializeField] public Camera weaponCamera = null;
    [SerializeField] Transform weaponPlace = null;
    [SerializeField] InputManager input = null;

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
        CmdShoot(NetworkClient.localPlayer.netId);
    }

    [Command(requiresAuthority = false)] void CmdShoot(uint from)
    {
        CustomEvent.Trigger(gameObject, "ServerShoot");  
        RpcShoot(from);
    }

    [ClientRpc] void RpcShoot(uint from)
    {
        if(NetworkClient.localPlayer.netId != from)
            CustomEvent.Trigger(gameObject, "RpcShoot");  
    }

    public void HitTarget(GameObject hit, int damage)
    {
        if(isServer)
            CmdhitTarget(hit, damage);
    }

    [Command(requiresAuthority = false)] void CmdhitTarget(GameObject hit, int damage)
    {
        RpcHitTarget(hit, damage);
    }

    [ClientRpc] void RpcHitTarget(GameObject player, int damage)    // player network
    {
        Debug.Log("hit" + player.name + " damage: " + damage);
    }
}
