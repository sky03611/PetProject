using MicroWorldNS;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Town
{
    public int factionID;
    public string name;
    public Inventory inventory = new Inventory();
    [SerializeField] protected List<Business> businesses = new List<Business>();
    private List<InventoryItem> toRemove = new List<InventoryItem>();
    public float productionTimer = 3000;
    public int Coffers 
    {
        get
        {
            return coffers;
        }
        set
        {
            coffers = value;
            if (coffers < 0)
            {
                coffers = 0;
            }
            if (coffers > coffersCap)
            {
                coffers = coffersCap;
            }
        }
    }
    [SerializeField] protected int coffers, coffersCap;
    public int morale = 0;
    public Modifier modifier = null;

    public Town()
    {
        coffersCap = 100000;
    }

    public List<Business> GetBusinesses()
    {
        return businesses;
    }

    public virtual bool HasBusinessSlots()
    {
        return businesses.Count < 4;
    }

    public bool HasBusiness(BusinessType businessType)
    {
        return businesses.Find (x => x.businessType == businessType) != null;
    }

    public void AddBusiness (Business business, TownManager tm, bool _belongsToPlayer = false)
    {
        businesses.Add(new Business(business, tm, _belongsToPlayer));
    }

    public void ClearBusinesses()
    {
        foreach (Business business in businesses)
        {
            toRemove.Clear();
            foreach (var item in business.thisInventory.items)
            {
                if (item.toRemove)
                {
                    toRemove.Add(item);
                }
            }
            foreach (var t in toRemove)
            {
                business.thisInventory.items.Remove(t);
            }
        }
    }

    public virtual int GetSettlementType()
    {
        return 0;
    }

    public bool HasModifier()
    {
        return modifier != null && modifier.modifierName != string.Empty;
    }

    public int GetMorale() //adding +2 because the range of morale is -2 to 2
    {
        if (modifier == null)
        {
            return morale+2; 
        }
        else
        {
            return modifier.morale+2;
        }
    }

    public bool HasMoney (int _amount)
    {
        return Coffers >= _amount;
    }

    public void ChangeCoffers(int _amount = 0)
    {
        Coffers += _amount;
    }

    public virtual void AddProduction(Modifier _modifier)
    {
        foreach (var b in businesses)
        {
            b.AddProduction(this, _modifier);
        }
    }

    public virtual void AddMiscellaneous()
    {
        var suitableItems = GlobalTownManager.Instance.GetAllItems();
        foreach (var s in suitableItems)
        {
            if (Random.Range(1, 101) <= s.spawnChance)
            {
                int randomAmount = Random.Range(s.minAmount, s.maxAmount + 1);
                inventory.AddItem(GlobalTownManager.Instance.GetItemByName(s.name), randomAmount);
            }
        }
    }

    public Faction GetFaction()
    {
        return FactionScript.Instance.factions[factionID];
    }

    public int GetFood()
    {
        int foodAmount = 0;
        foreach (var item in inventory.items)
        {
            if (item.type == ItemType.FOOD)
                foodAmount += item.amount;
        }
        return foodAmount;
    }
}