using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Bolt;
using UnityEngine.SceneManagement;

public class GameMatchNetworkManager : NetworkManager
{
    [Header("Matching")]
    public GameObject inRoomPlayerPrefab = null;
    [Scene] public string gameplayScene = null;


    [Tooltip("match id which is used to join a lobby online, leave null or empty to join a host via IP address")]
    public string matchId;

    [Tooltip("lock match ID created, useful for hosting (must be != empty or null)")]
    public string fixMatchId = null;

    [Tooltip("Match informations. Server only")]
    public List<MatchInfo> matches = new List<MatchInfo>();

    [Tooltip("All player informations. Server only")]
    public Dictionary<NetworkConnection, PlayerInfo> players = new Dictionary<NetworkConnection, PlayerInfo>();
    public Dictionary<string, InGameEntity> gameEntities = new Dictionary<string, InGameEntity>(); 
    
    [Tooltip("start action, which indicate what you planned to do: Create room or join a lobby,.... Client only")]
    public ClientMatchOperation clientAction;

    [Tooltip("Name of player which is registed and displayed to other players in lobby and in-game play")]
    public string playerName = "Player";

    public ClientMatchMsg currentLobby {get; private set;} = new ClientMatchMsg {players = new PlayerInfo[0], yourMatch = null};


    const string NOT_FOUND_LOBBY = "not_found_lobby";
    const string NOT_CREATE_LOBBY = "not_create_lobby";

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

            int count = 10;
            string matchId = "";

            while(count >= 0 && matches.FirstOrDefault(m => m.matchId == matchId) != null)
            {
                matchId = new string(new char[]
                {
                    ((char)UnityEngine.Random.Range(97, 123)),
                    ((char)UnityEngine.Random.Range(97, 123)),
                    ((char)UnityEngine.Random.Range(97, 123))
                });

                --count;
            }
            
            if(!string.IsNullOrEmpty(fixMatchId))
                matchId = fixMatchId;

            if(count < 0)
            {
                Debug.LogWarning("[Server] Rare case: Couldn't get a matchID");
                conn.Send<Response>(new Response
                {
                    msg = NOT_CREATE_LOBBY
                });
                return;
            }

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
                connectionId = conn.connectionId,
                isInGame = false
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

            void NotFound() => conn.Send<Response>(new Response
            {
                msg = NOT_FOUND_LOBBY
            });

            MatchInfo matchInfo = GetMatchInfo(msg.matchId);
            if(matchInfo != null)
            {
                if(!matchInfo.open)
                {
                    Debug.Log($"[Server] Match {matchInfo.matchId} closed");
                    NotFound();
                    return;
                }

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
                NotFound();
        });
    
        NetworkServer.RegisterHandler<ClientLobbyState>((conn, msg)=>
        {
            if(GetPlayerInforAndMatchInfo(conn, out PlayerInfo playerInfo, out MatchInfo matchInfo))
            {
                players[conn].ready = msg.ready;
                UpdateListPlayerInMatch(matchInfo);
            }
        });
    
        NetworkServer.RegisterHandler<InMatchOrGameRequest>((conn, msg)=>
        {
            if(msg.clientOperation == ClientMatchOperation.BeginGame)
            {
                if(!GetPlayerInforAndMatchInfo(conn, out PlayerInfo playerInfo, out MatchInfo matchInfo)) return;
                
                if(!IsAllPlayerReadyOnServer(matchInfo, out KeyValuePair<NetworkConnection, PlayerInfo>[] allPlayerInfo))
                {
                    Debug.LogWarning("[Server] All lobby member haven't been ready yet or there aren't any players in the lobby");
                    return;
                }

                matchInfo.open = false;

                foreach (var info in allPlayerInfo)
                {
                    info.Key.Send<InMatchOrGameRequest>(new InMatchOrGameRequest
                    {
                        clientOperation = msg.clientOperation,
                        serverOperation = ServerMatchOperation.Start
                    });
                }

                if(mode == NetworkManagerMode.ServerOnly)
                    CustomEvent.Trigger(gameObject, "ServerBeginGame", matchInfo);
            }
            else if(msg.clientOperation == ClientMatchOperation.Started)
            {
                if(!GetPlayerInforAndMatchInfo(conn, out PlayerInfo playerInfo, out MatchInfo matchInfo)) return;

                if(matchInfo.isPlaying)
                    return;

                ServerAddPlayer(playerPrefab, matchInfo.key, conn);

                playerInfo.isInGame = true;
                bool connectedAll = players.Where(p => p.Value.matchId == matchInfo.matchId).All(p => p.Value.isInGame);
                if(connectedAll)
                    matchInfo.isPlaying = true;

                CustomEvent.Trigger(gameObject, "AnotherPlayerConnected", connectedAll);
            }
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

    public GameObject SpawnObject(GameObject prefab, Guid key, bool isNetworkSpawn)
    {
        GameObject result = Instantiate(prefab);
        NetworkMatchChecker matchChecker = result.GetComponent<NetworkMatchChecker>();
        if(matchChecker)
        { 
            matchChecker.matchId = key;

            if(isNetworkSpawn)
                NetworkServer.Spawn(result);
        }
        else
        {
            Debug.LogError($"[Server] {prefab.name} must contain NetworkMatchChecker");
            Destroy(result);
            return null;
        }

        return result;
    }

    void ServerAddPlayer(GameObject prefab, Guid key, NetworkConnection connection)
    {
        GameObject player = SpawnObject(prefab, key, false);
        if(player)
        {
            player.name = $"{prefab.name} [connId={connection.connectionId}]";
            NetworkServer.AddPlayerForConnection(connection, player);
        }
    }

    void UpdateListPlayerInMatch(MatchInfo matchInfo)
    {
        if(matchInfo == null) return;

        var otherPlayers = players.Where(p => p.Value.matchId == matchInfo.matchId);
        var playerInforArray = otherPlayers.Select(e => e.Value).ToArray();

        if(playerInforArray.Length == 0)
        {
            bool removed = matches.Remove(matchInfo);
            Debug.Log($"There aren't any players in match {matchInfo.matchId}, so remove this match -> {removed}");
            return;
        }

        foreach (var p in otherPlayers)
        {
            p.Key.Send<ClientMatchMsg>(new ClientMatchMsg
            {
                yourMatch = matchInfo,
                yours = p.Value,
                players = playerInforArray
            });
        }   
    }

    bool GetPlayerInforAndMatchInfo(NetworkConnection conn, out PlayerInfo playerInfo, out MatchInfo matchInfo)
    {
        playerInfo = null;
        matchInfo = null;

        var temp_playerInfo = GetPlayerInfo(conn.connectionId); if(temp_playerInfo == null) return false; 
        var temp_matchInfo = GetMatchInfo(temp_playerInfo.matchId); if(temp_matchInfo == null) return false; 

        playerInfo = temp_playerInfo;
        matchInfo = temp_matchInfo;
        return true;
    }

    PlayerInfo GetPlayerInfo(int connectionId)
    {
        var result = players.FirstOrDefault(p => p.Value.connectionId == connectionId).Value;
        if(result == null)
        {
            Debug.LogError($"[Server] Couldn't find player has connectionId {connectionId}");
        }
        return result;
    }

    MatchInfo GetMatchInfo(string matchId)
    {
        var result = matches.FirstOrDefault(m => m.matchId == matchId);
        if(result == null)
        {
            Debug.LogError($"[Server] Couldn't find match has matchId {matchId}");
        }
        return result;
    }

    bool IsAllPlayerReadyOnServer(MatchInfo matchInfo, out KeyValuePair<NetworkConnection, PlayerInfo>[] allPlayerInfo)
    {
        allPlayerInfo = new KeyValuePair<NetworkConnection, PlayerInfo>[0];
        if(matchInfo == null)
            return false;

        allPlayerInfo = players.Where(p => p.Value.matchId == matchInfo.matchId).ToArray();
        return allPlayerInfo.Length > 0 && allPlayerInfo.All(p => p.Value.ready);
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

        NetworkClient.RegisterHandler<InMatchOrGameRequest>(msg=>
        {
            if(msg.serverOperation == ServerMatchOperation.Start)
            {
                if(NetworkServer.active) // This is host mode
                {
                    NetworkServer.isLoadingScene = true;
                }

                clientAction = msg.clientOperation;

                NetworkClient.isLoadingScene = true;
                loadingSceneAsync = SceneManager.LoadSceneAsync(gameplayScene);
            }
        });

        NetworkClient.RegisterHandler<Response>(msg =>
        {
            switch(msg.msg)
            {
                case NOT_FOUND_LOBBY:
                    if(mode == NetworkManagerMode.Host)
                        StopHost();
                    else if(mode == NetworkManagerMode.ClientOnly)
                        StopClient();
                break;
                case NOT_CREATE_LOBBY:
                    if(mode == NetworkManagerMode.Host)
                        StopHost();
                    else if(mode == NetworkManagerMode.ClientOnly)
                        StopClient();
                break;
                default:
                break;
            }
        });

        CustomEvent.Trigger(gameObject, "OnStartClient");
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        Reset();
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);

        CustomEvent.Trigger(gameObject, "OnClientConnect");
    }

    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        base.OnClientSceneChanged(conn);
        if(clientAction == ClientMatchOperation.BeginGame)
        {
            if(mode == NetworkManagerMode.Host)
                CustomEvent.Trigger(gameObject, "ServerBeginGame", currentLobby.yourMatch);

            if(!autoCreatePlayer && NetworkClient.localPlayer == null)
            {
                NetworkClient.Send<InMatchOrGameRequest>(new InMatchOrGameRequest
                {
                    clientOperation = ClientMatchOperation.Started
                });
            }
        }
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

    public void ClientRequestStartMatch()
    {
        if(!IsAllReady())
        {
            Debug.LogWarning("[Client ]All lobby member haven't been ready yet");
            return;
        }

        NetworkClient.Send<InMatchOrGameRequest>(new InMatchOrGameRequest
        {
            clientOperation = ClientMatchOperation.BeginGame
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

    public bool IsAllReady()
    {
        if(!BasicCheckCurrentLobby())
            return false;
        
        return currentLobby.players.All(p => p.ready);
    }

    public bool IsLeader()
    {
        if(!BasicCheckCurrentLobby())
            return false;

        return currentLobby.yourMatch.leaderConnectionId == currentLobby.yours.connectionId;
    }

    void Reset()
    {
        currentLobby = new ClientMatchMsg {players = new PlayerInfo[0], yourMatch = null};
        clientAction = ClientMatchOperation.None;
        matchId = string.Empty;
    }
    bool BasicCheckCurrentLobby()
    {
        if(currentLobby.yourMatch == null)
        {
            return false;
        }

        if(currentLobby.yours == null)
        {
            Debug.LogError("[Client] PlayerInfor is null thought you joined a lobby?");
            return false;
        }

        return true;
    }
#endregion



}




