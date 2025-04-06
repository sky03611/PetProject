using UnityEngine;
using Pathfinding;

public class VillageManager : TownManager
{
    public override bool IsSupplyingBusiness(BusinessType businessType)
    {
        switch ((VillageType)thisTown.GetSettlementType())
        {
            case VillageType.FARM:
                if (businessType == BusinessType.MILL)
                    return true;
                break;
            case VillageType.HUNTING:
                if (businessType == BusinessType.BUTCHERY || businessType == BusinessType.TAILOR || businessType == BusinessType.TAVERN)
                    return true;
                break;
            case VillageType.ORE:
                if (businessType == BusinessType.SMITHY)
                    return true;
                break;
            case VillageType.BREWERY:
                if (businessType == BusinessType.TAVERN)
                    return true;
                break;
            case VillageType.WOOL:
                if (businessType == BusinessType.TAILOR)
                    return true;
                break;
            case VillageType.HONEY:
                if (businessType == BusinessType.TAVERN || businessType == BusinessType.CANDLESHOP)
                    return true;
                break;
        }
        return false;
    }
}