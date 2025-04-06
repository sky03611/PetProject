using UnityEngine;

public enum VillageType { FARM, HUNTING, ORE, BREWERY, WOOL, HONEY, ANY } //any is needed for modifiers

[System.Serializable]
public class Village : Town
{
    public VillageType villageType;
    public Village()
    {
        productionTimer = 500;
        coffersCap = 5000;
        do
        {
            villageType = (VillageType)Random.Range(0, System.Enum.GetValues(typeof(VillageType)).Length);
        } while (villageType == VillageType.ANY);
    }

    public override bool HasBusinessSlots()
    {
        return false;
    }

    public override int GetSettlementType()
    {
        return (int)villageType;
    }

    /*public override void AddProduction(Modifier _modifier) //replaced by Town method
    {
        var suitableItems = GlobalTownManager.Instance.GetSuitableItems(villageType);
        foreach (var s in suitableItems)
        {
            if (Random.Range (1, 101) <= s.spawnChance)
            {
                int randomAmount = Random.Range(s.minAmount, s.maxAmount + 1);
                if (_modifier != null)
                {
                    randomAmount = Mathf.FloorToInt (randomAmount * _modifier.resourceYield);
                }
                inventory.AddItem(GlobalTownManager.Instance.GetItemByName (s.name), randomAmount);
            }
        }
    }*/
}