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
    public Dictionary<NetworkConnection, PlayerInfo> players = new Dictionary<NetworkConnection, PlayerInfo>();
    public ClientMatchOperation clientAction;
    public string playerName = "Player";
    public ClientMatchMsg currentLobby {get; private set;} = new ClientMatchMsg {players = new PlayerInfo[0], yourMatch = null};

    void OnGUI()
    {
        if(GUI.Button(new Rect(Screen.width/2f, 0, 100, 30), "Test"))
        {

        }
    }

#region Server
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
                open = true,
                leaderConnectionId = conn.connectionId
            };

            PlayerInfo playerInfo = new PlayerInfo
            {
                matchId = matchId,
                name = msg.playerName,
                ready = false,
                connectionId = conn.connectionId
            };

            matches.Add(matchInfo);
            players.Add(conn, playerInfo);

            ServerAddPlayer(inRoomPlayerPrefab, matchInfo.key, conn);

            conn.Send<ClientMatchMsg>(new ClientMatchMsg
            {
                yourMatch = matchInfo,
                yours = playerInfo,
                players = new PlayerInfo[]{playerInfo}
            });
        });

        NetworkServer.RegisterHandler<JoinMatchMsg>((conn, msg)=>
        {
            if(!NetworkServer.active) return;

            MatchInfo matchInfo = matches.FirstOrDefault(m => m.matchId == msg.matchId);
            if(matchInfo != null)
            {
                PlayerInfo playerInfo = new PlayerInfo
                {
                    matchId = matchInfo.matchId,
                    ready = false,
                    name = msg.playerName,
                    connectionId = conn.connectionId
                };

                players.Add(conn, playerInfo);

                ServerAddPlayer(inRoomPlayerPrefab, matchInfo.key, conn);
                UpdateListPlayerInMatch(matchInfo);
            }
            else
            {
                Debug.LogWarning($"[Server] Couldn't find match {msg.matchId}");
            }
        });
    
        NetworkServer.RegisterHandler<ClientLobbyState>((conn, msg)=>
        {
            if(!players.ContainsKey(conn))
            {
                Debug.LogError("[Server] cannot set ready for client connection " + conn.connectionId);
                return;
            }

            MatchInfo matchInfo = matches.FirstOrDefault(m => m.matchId == players[conn].matchId);
            if(matchInfo == null)
            {
                Debug.LogError($"[Server] client connection {conn.connectionId} has match id invaild {players[conn].matchId}");
                return;
            }

            players[conn].ready = msg.ready;

            UpdateListPlayerInMatch(matchInfo);
        });
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);

        if(players.ContainsKey(conn))
        {
            Debug.Log("[Server] Removed player connection " + conn.connectionId);

            PlayerInfo playerInfo = players[conn];

            players.Remove(conn);
            MatchInfo matchInfo = matches.FirstOrDefault(m => m.matchId == playerInfo.matchId);
            UpdateListPlayerInMatch(matchInfo);
        }
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

    void UpdateListPlayerInMatch(MatchInfo matchInfo)
    {
        if(matchInfo == null) return;

        var otherPlayers = players.Where(p => p.Value.matchId == matchInfo.matchId);
        var playerInforArray = otherPlayers.Select(e => e.Value).ToArray();

        foreach (var p in otherPlayers)
        {
            Debug.Log("Send to: " + p.Key.connectionId);
            p.Key.Send<ClientMatchMsg>(new ClientMatchMsg
            {
                yourMatch = matchInfo,
                yours = p.Value,
                players = playerInforArray
            });
        }   
    }

#endregion

#region Client
    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<ClientMatchMsg>(msg=>
        {
            currentLobby = msg;
            Debug.Log("Your match is: " + currentLobby.yourMatch.matchId + "");
        });

        CustomEvent.Trigger(gameObject, "OnStartClient");
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        currentLobby = new ClientMatchMsg();
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);

        CustomEvent.Trigger(gameObject, "OnClientConnect");
    }

    public void ClientRequestCreateMatch(int maxPlayers = 8)
    {
        if(!NetworkClient.active) return;

        NetworkClient.Send<CreateMatchMsg>(new CreateMatchMsg
        {
            maxPlayers = maxPlayers,
            playerName = playerName + UnityEngine.Random.Range(100, 1000)
        });
    }

    public void ClientRequestJoinMatch(string matchId)
    {
        if(!NetworkClient.active) return;

        NetworkClient.Send<JoinMatchMsg>(new JoinMatchMsg
        {
            matchId = matchId,
            playerName = playerName + UnityEngine.Random.Range(100, 1000)
        });
    }

    public void ClientSetReady(bool ready)
    {
        if(!NetworkClient.active) return;
        NetworkClient.Send<ClientLobbyState>(new ClientLobbyState
        {
            ready = ready
        });
    }

    public bool IsLeader()
    {
        if(currentLobby.yourMatch == null)
        {
            Debug.LogWarning("[Client] you haven't created any lobbies");
            return false;
        }

        if(currentLobby.yours == null)
        {
            Debug.LogError("[Client] PlayerInfor is null thought you joined a lobby?");
            return false;
        }

        return currentLobby.yourMatch.leaderConnectionId == currentLobby.yours.connectionId;
    }
#endregion



}




