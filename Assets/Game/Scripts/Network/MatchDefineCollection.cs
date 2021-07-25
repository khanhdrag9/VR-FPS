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

public struct InMatchOrGameRequest : NetworkMessage
{
    public ClientMatchOperation clientOperation;
    public ServerMatchOperation serverOperation;

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

public struct Response : NetworkMessage
{
    public string msg;
}

[Serializable]
public class MatchInfo
{
    public string matchId;
    public Guid key;
    public int maxPlayers = 8;
    public bool open;
    public int leaderConnectionId;
    public bool isPlaying = false;
}

[Serializable]
public class PlayerInfo
{
    public string matchId;
    public string name = "player";
    public bool ready;
    public int connectionId;    // Client.connection is 0, so use it carefully
    public bool isInGame = false;
}

[Serializable]
public class InGameInfo
{
    public string matchId;
}

[Serializable]
public class Competitive : InGameInfo
{
    public int maxRound = 30;
    public int currentRound = 1;
    public int[] team1RoundWin = new int[0];
    public int[] team2RoundWin = new int[0];
}

 public enum ServerMatchOperation : byte
{
    None,
    Start
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
    BeginGame,
    Started
}