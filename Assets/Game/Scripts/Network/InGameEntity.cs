using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class InGameEntity : NetworkBehaviour
{
    public InGameInfo info {get; protected set; } = null;

    public override void OnStartClient()
    {
        SyncInfo();
    }

    public virtual void Initialize(string matchId, string stringParam)
    {}

    // Update info field at all clients
    public void SyncInfo()
    {
        if(isServer)
            RpcSyncInfo(info);
        
        if(isClient)
            CmdSyncInfo(info);
    }

    [Command] void CmdSyncInfo(InGameInfo value)
    {
        RpcSyncInfo(value);
    }

    [ClientRpc(includeOwner = false)] void RpcSyncInfo(InGameInfo value)
    {
        this.info = value;
    }
}
