using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Modifier
{
    public Modifier ()
    {

    }
    public string modifierName, modifierDescription;
    public string seasons;
    public List<int> possibleSeasons = new List<int>();
    public int settlementType; //VillageType for villages, none for towns
    public int morale = 0;
    public int maxDays = 3;
    public float productionSpeed = 1f;
    public float resourceYield = 1f;

    public bool AnySettlementType()
    {
        return (VillageType)settlementType == VillageType.ANY;
    }

    public virtual bool IsPossible()
    {
        return possibleSeasons.Contains ((int)SeasonsHandler.Instance.GetCurrentSeason());
    }

    public virtual bool IsPossible(VillageType type)
    {
        return possibleSeasons.Contains((int)SeasonsHandler.Instance.GetCurrentSeason())
            && settlementType == (int)type;
    }

}
