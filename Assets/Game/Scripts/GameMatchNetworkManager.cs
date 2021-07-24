using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Bolt;

public class GameMatchNetworkManager : NetworkManager
{
    [Header("Matching")]
    public GameObject inRoomPlayerPrefab = null;
    [Scene] public string gameplayScene = null;

    [Header("Debug")]
    public string fixMatchId = null;


    public List<MatchInfo> matches = new List<MatchInfo>();
    public List<PlayerInfo> players = new List<PlayerInfo>();
    public ClientMatchOperation clientAction;

    void OnGUI()
    {
        if(GUI.Button(new Rect(Screen.width/2f, 0, 100, 30), "Test"))
        {

        }
    }

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<CreateMatchMsg>((conn, msg)=>
        {
            if(!NetworkServer.active) return;

            string matchId = new string(new char[]
            {
                ((char)UnityEngine.Random.Range(97, 123)),
                ((char)UnityEngine.Random.Range(97, 123)),
                ((char)UnityEngine.Random.Range(97, 123))
            });

            if(!string.IsNullOrEmpty(fixMatchId))
                matchId = fixMatchId;

            MatchInfo matchInfo = new MatchInfo
            {
                matchId = matchId,
                key = Guid.NewGuid(),
                maxPlayers = msg.maxPlayers,
                open = true
            };

            PlayerInfo playerInfo = new PlayerInfo
            {
                matchId = matchId,
                ready = false,
                connectionId = conn.connectionId
            };

            matches.Add(matchInfo);
            players.Add(playerInfo);

            ServerAddPlayer(inRoomPlayerPrefab, matchInfo.key, conn);

            conn.Send<ClientMatchMsg>(new ClientMatchMsg
            {
                yourMatch = matchInfo,
                players = new PlayerInfo[]{playerInfo}
            });
        });

        NetworkServer.RegisterHandler<JoinMatchMsg>((conn, msg)=>
        {
            if(!NetworkServer.active) return;

            MatchInfo matchInfo = matches.FirstOrDefault(m => m.matchId == msg.matchId);
            if(matchInfo != null)
            {
                List<PlayerInfo> otherPlayers = players.Where(p => p.matchId == matchInfo.matchId).ToList();
                PlayerInfo playerInfo = new PlayerInfo
                {
                    matchId = matchInfo.matchId,
                    ready = false,
                    connectionId = conn.connectionId
                };
                otherPlayers.Add(playerInfo);

                players.Add(playerInfo);

                ServerAddPlayer(inRoomPlayerPrefab, matchInfo.key, conn);

                conn.Send<ClientMatchMsg>(new ClientMatchMsg
                {
                    yourMatch = matchInfo,
                    players = otherPlayers.ToArray()
                });
            }
            else
            {
                Debug.LogWarning($"[Server] Couldn't find match {msg.matchId}");
            }
        });
    }

    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<ClientMatchMsg>(msg=>
        {
            Debug.Log("Your match is: " + msg.yourMatch.matchId);
            Debug.Log("all players is: " + msg.players.Length);
        });

        CustomEvent.Trigger(gameObject, "OnStartClient");
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        CustomEvent.Trigger(gameObject, "OnClientConnect");
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
        int index = players.FindIndex(p => p.connectionId == conn.connectionId);
        if(index >= 0)
        {
            Debug.Log("[Server] Removed player connection " + conn.connectionId);
            players.RemoveAt(index);
        }
    }

    public void ClientRequestCreateMatch(int maxPlayers = 8)
    {
        if(!NetworkClient.active) return;

        NetworkClient.Send<CreateMatchMsg>(new CreateMatchMsg
        {
            maxPlayers = maxPlayers
        });
    }

    public void ClientRequestJoinMatch(string matchId)
    {
        if(!NetworkClient.active) return;

        NetworkClient.Send<JoinMatchMsg>(new JoinMatchMsg
        {
            matchId = matchId
        });
    }

    void ServerAddPlayer(GameObject prefab, Guid key, NetworkConnection connection)
    {
        GameObject player = Instantiate(prefab);
        player.name = $"{prefab.name} [connId={connection.connectionId}]";
        NetworkMatchChecker matchChecker = player.GetComponent<NetworkMatchChecker>();
        if(matchChecker)
        {
            matchChecker.matchId = key;
        }
        else
        {
            Debug.LogError($"[Server] {prefab.name} must contain NetworkMatchChecker");
            Destroy(player);
            return;
        }

        NetworkServer.AddPlayerForConnection(connection, player);
    }
}




