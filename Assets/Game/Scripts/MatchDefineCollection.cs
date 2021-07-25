using System;
using Mirror;
using UnityEngine;
public struct CreateMatchMsg : NetworkMessage
{
    public int maxPlayers;
    public string playerName;
}

public struct JoinMatchMsg: NetworkMessage
{
    public string matchId;
    public string playerName;
}

public struct ClientMatchMsg : NetworkMessage
{
    public MatchInfo yourMatch;
    public PlayerInfo yours;
    public PlayerInfo[] players;
}

public struct ClientLobbyState : NetworkMessage
{
    public bool ready;
}

[Serializable]
public class MatchInfo
{
    public string matchId;
    public Guid key;
    public int maxPlayers = 8;
    public bool open;
    public int leaderConnectionId;
}

[Serializable]
public class PlayerInfo
{
    public string matchId;
    public string name = "player";
    public bool ready;
    public int connectionId;    // Client.connection is 0, so use it carefully
}

 public enum ServerMatchOperation : byte
{
    None,
    Create,
    Cancel,
    Start,
    Join,
    Leave,
    Ready
}

public enum ClientMatchOperation : byte
{
    None,
    List,
    Created,
    Cancelled,
    Joined,
    Departed,
    UpdateRoom,
    Started
}