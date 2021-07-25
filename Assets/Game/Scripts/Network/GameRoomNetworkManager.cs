using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Bolt;
using System.Linq;
public enum RoomMsgType
{
    CREATE, JOIN
}

public class GameRoomNetworkManager : NetworkRoomManager
{
    public List<string> matchID = new List<string>();
    public RoomMsgType roomAction;

    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<CreateLobbyMsg>(msg =>
        {

        });

        NetworkClient.RegisterHandler<JoinLobbyMsg>(msg =>
        {

        });

        CustomEvent.Trigger(gameObject, "OnStartClient");
    }

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<CreateLobbyMsg>((conn, msg)=>
        {
            NetworkMatchChecker match = conn.identity.GetComponent<NetworkMatchChecker>();
            match.matchId = System.Guid.NewGuid();
        });

        NetworkServer.RegisterHandler<JoinLobbyMsg>((conn, msg)=>
        {
            Debug.Log("Is null: " + NetworkServer.connections.Count);
            // NetworkMatchChecker match = conn.identity.GetComponent<NetworkMatchChecker>();
            // match.matchId = System.Guid.Parse("1234");

        });

        CustomEvent.Trigger(gameObject, "OnStartServer");
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        CustomEvent.Trigger(gameObject, "OnClientConnect");
    }


    public void CreateLobby()
    {
        if(NetworkClient.active == false) 
            return;
        
        NetworkClient.Send<CreateLobbyMsg>(new CreateLobbyMsg());
    }

    public void JoinLobby(int id)
    {
        if(NetworkClient.active == false) 
            return;

        NetworkClient.Send<JoinLobbyMsg>(new JoinLobbyMsg
        {
            lobbyId = id,
            netId = (int)NetworkClient.localPlayer.netId
        });
    }



    public struct CreateLobbyMsg : NetworkMessage
    {
    }

    public struct JoinLobbyMsg : NetworkMessage
    {
        public int lobbyId;
        public int netId;
    }
}
