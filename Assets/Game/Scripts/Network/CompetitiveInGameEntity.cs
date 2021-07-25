using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompetitiveInGameEntity : InGameEntity
{
    public override void Initialize(string matchId, string stringParam)
    {
        info = new Competitive
        {
            matchId = matchId,
            maxRound = int.Parse(stringParam.Split(',')[0])
        };
    }
}
