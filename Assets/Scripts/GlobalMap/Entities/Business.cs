using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public enum BusinessType { 

    NONE = 0,
    BUTCHERY = 1,
    SMITHY = 2,
    TAVERN = 3,
    TAILOR = 4,
    CANDLESHOP = 5,
    MILL = 6,

    FARM_MILL = 100,
    ORCHARD = 101,
    VEGETABLE_PATCH = 102,
    HUNTERS_HUT = 103,
    TANNERY = 104,
    FLETCHER = 105,
    MINE = 106,
    STONECUTTER = 107,
    JEWELER = 108,
    BREWERS_SHED = 109,
    BOTANIST = 110,
    HERBALIST_HUT = 111,
    SHEPHERDS_HUT = 112,
    WEAVING_SHED = 113,
    DYE_WORKS = 114,
    BEEHIVES = 115,
    DRYHOUSE = 116
}

[System.Serializable]
public class Business
{
    public int level = 1;
    public int experience;
    public List<Employee> employees = new List<Employee>();
    public List<Policy> activePolicies = new List<Policy>();
    public bool belongsToPlayer = false;
    public bool isCoreBusiness = false; //needed for villages to always spawn the core business first
    public string title, description;
    public string genericTitle, genericDescription;
    public bool producesProducts; //true - produces items, false - produces money
    [Range(0, 1f)] public float taxPercentage = 0.5f; // how much do we leave in budget. The leftovers are given to the owner (or the town)
    public int profitPerItem = 100;
    public TownManager thisTown;
    public int money, dailyMoneyCap, productionCap = 1, lastProfit = 0;
    public BusinessType businessType;
    public VillageType villageType;
    public Inventory thisInventory = new Inventory();
    public string speaker = "";
    public string dialoguePath = "";

    public Business ()
    {

    }

    public bool HasPolicy(string policyID) => activePolicies.Any(p => p.policyID == policyID);

    public bool AddPolicy(Policy policy)
    {
        if (activePolicies.Any(p => p.tier == policy.tier))
            return false; // Already have one in this tier

        activePolicies.Add(policy);
        return true;
    }

    public string GetGenericName()
    {
        return genericTitle;
    }

    public string GetGenericDescription()
    {
        return genericDescription;
    }

    public bool IsSuitableForThisVillage (TownManager village)
    {
        return (int)villageType == village.thisTown.GetSettlementType();
    }

    public int GetBusinessPrice(bool newBusiness = false)
    {
        int price = Mathf.Max(lastProfit, profitPerItem * productionCap);
        if (newBusiness)
            return price *= 20;
        price *= 100;
        return price;
    }

    public int GetProfit ()
    {
        return lastProfit;
    }

    public string GetNecessaryItems()
    {
        var items = new List<string>();
        foreach (var item in JsonReader.Instance.inventoryItems)
        {
            if (IsSuitable(item))
            {
                items.Add(item.itemName);
            }
        }

        if (items.Count == 0)
            return string.Empty;

        return string.Join(", ", items) + ".";
    }

    public Business (Business other, TownManager town, bool _belongsToPlayer = false)
    {
        string[] tmpString;
        businessType = other.businessType;
        isCoreBusiness = other.isCoreBusiness;
        title = other.title;
        if (title.Contains ('/'))
        {
            tmpString = title.Split ('/');
            title = tmpString[Random.Range(0, tmpString.Length)];
        }
        description = other.description;
        if (description.Contains('/'))
        {
            tmpString = description.Split('/');
            description = tmpString[Random.Range(0, tmpString.Length)];
        }
        dialoguePath = other.dialoguePath;
        speaker = other.speaker;
        money = other.money;
        dailyMoneyCap = other.dailyMoneyCap;
        productionCap = other.productionCap;
        thisTown = town;
        producesProducts = other.producesProducts;
        belongsToPlayer = _belongsToPlayer;
        foreach (var e in other.employees)
        {
            employees.Add(new Employee(e));
        }
    }


    public bool IsSuitable (InventoryItem item)
    {
        return item.IsSuitableForBusiness(businessType);
    }


    public int GetMoney()
    {
        return money;
    }

    public virtual void ChangeMoney(int _amount)
    {
        money += _amount;
        if (money < 0)
        {
            money = 0;
        }
    }

    public virtual bool HasEnoughMoney(int _amount)
    {
        return money >= _amount;
    }

    public bool IsFunctioning()
    {
        if (thisInventory.items.Count > 0)
        {
            return thisInventory.items[0].amount > 0;
        }
        return false;
    }

    public int GetTotalEmployeeSalary()
    {
        if (!belongsToPlayer) 
            return 0;

        float salaryMultiplier = 1f;
        foreach (var policy in activePolicies)
        {
            if (policy.effectType == PolicyEffectType.SalaryMultiplier)
                salaryMultiplier *= policy.effectValue;
        }

        return Mathf.RoundToInt(employees.Sum(e => e.salary * e.count) * salaryMultiplier);
    }


    public float GetProductionMultiplier(Modifier modifier = null)
    {
        float multiplier = 1f + employees.Sum(e =>
        {
            float tierBonus = e.efficiency switch
            {
                EmployeeEfficiency.NOVICE => 0.15f,
                EmployeeEfficiency.MIDDLE => 0.25f,
                EmployeeEfficiency.MASTER => 0.4f,
                _ => 0f
            };
            return tierBonus * e.count;
        });

        foreach (var policy in activePolicies)
        {
            if (policy.effectType == PolicyEffectType.ProductionMultiplier)
                multiplier *= policy.effectValue;
        }

        if (modifier != null)
        {
            multiplier *= modifier.resourceYield;
        }

        return multiplier;
    }

    public void AddProduction(Town _parentTown, Modifier _modifier)
    {
        int totalSalaries = GetTotalEmployeeSalary();
        ChangeMoney(-totalSalaries);

        float efficiency = GetProductionMultiplier(_modifier);

        if (!producesProducts)
        {
            int profit = 0;

            int effectiveProductionCap = Mathf.CeilToInt(productionCap * efficiency);

            for (int i = 0; i < effectiveProductionCap; i++)
            {
                if (!IsFunctioning())
                    break;

                profit += profitPerItem;
                thisInventory.items[0].ChangeAmount(-1);
                GainExperience(1);
                if (thisInventory.items[0].toRemove)
                {
                    thisInventory.items.RemoveAt(0);
                    if (thisInventory.items.Count == 0)
                        break;
                }
            }

            lastProfit = profit;
            float profitMultiplier = 1f;
            foreach (var policy in activePolicies)
            {
                if (policy.effectType == PolicyEffectType.ProfitMultiplier)
                    profitMultiplier *= policy.effectValue;
            }

            lastProfit = Mathf.RoundToInt(lastProfit * profitMultiplier);

            int goesToOwner = Mathf.FloorToInt(profit * taxPercentage);
            _parentTown.ChangeCoffers(goesToOwner);
            money += profit - goesToOwner;
        }
        else
        {
            ProduceItemsAndSell(efficiency);
        }
        if (thisInventory.items.Count < productionCap)
        {
            BuyItems();
        }
    }

    public void ProduceItemsAndSell(float efficiency = 1f)
    {
        if (!producesProducts)
            return;

        var profile = BusinessProductionManager.Instance.GetProfile(businessType);
        if (profile == null)
            return;

        foreach (var entry in profile.producibleItems)
        {
            if (entry.requiredResources.Count == 0 && Random.value > entry.spawnChance)
                continue;

            int possibleBatches = int.MaxValue;

            foreach (var requirement in entry.requiredResources)
            {
                var item = thisInventory.GetItem(requirement.inputItemName);
                if (item == null || item.amount < requirement.inputAmount)
                {
                    possibleBatches = 0;
                    break;
                }

                int availableBatches = item.amount / requirement.inputAmount;
                possibleBatches = Mathf.Min(possibleBatches, availableBatches);
            }

            int batchesToProduce = Mathf.Min(possibleBatches, Mathf.CeilToInt(productionCap * efficiency));
            if (batchesToProduce <= 0) 
                continue;

            foreach (var requirement in entry.requiredResources)
            {
                thisInventory.RemoveItem(thisInventory.GetItem(requirement.inputItemName), requirement.inputAmount * batchesToProduce);
            }

            var templateItem = JsonReader.Instance.GetItemByName(entry.outputItemName);
            if (templateItem == null)
                continue;

            InventoryItem product = new InventoryItem(templateItem);
            product.amount = entry.outputAmount * batchesToProduce;
            GainExperience(product.amount);

            thisTown.thisTown.inventory.AddItem(product);

            int pricePerUnit = thisTown.GetPriceForItem(product);
            int totalProfit = pricePerUnit * product.amount;
            lastProfit = totalProfit;

            int goesToOwner = Mathf.FloorToInt(totalProfit * taxPercentage);
            if (belongsToPlayer)
                PlayerController.Instance.ChangeMoney(goesToOwner);
            else
                thisTown.ChangeCoffers(goesToOwner);

            money += totalProfit - goesToOwner;
        }
    }

    private void GainExperience(int amount)
    {
        if (level >= 5)
            return;
        experience += amount;

        int xpToNext = GetExperienceToNextLevel();

        while (experience >= xpToNext)
        {
            experience -= xpToNext;
            level++;
            xpToNext = GetExperienceToNextLevel();
        }
    }

    public float GetExperiencePercentage()
    {
        return (float)(experience / GetExperienceToNextLevel());
    }

    private int GetExperienceToNextLevel()
    {
        return level * 20;
    }

    public void BuyItems()
    {
        thisTown.BuyItems(this);
    }

    public void AddItem(InventoryItem item, int _amount = 0)
    {
        thisInventory.AddItem(item, _amount);
    }

    public bool CanHire(Employee target)
    {
        return target.count < target.maxCount;
    }

    public bool HireEmployee(Employee target)
    {
        if (!CanHire(target))
            return false;

        int totalCost = target.initialPay;
        if (money < totalCost)
            return false;

        money -= totalCost;
        target.count++;
        return true;
    }

    public bool HireEmployee(string employeeName)
    {
        var employee = employees.Find(e => e.employeeName == employeeName);
        if (employee == null) 
            return false;
        return HireEmployee(employee);
    }

    public bool CanFire(Employee target)
    {
        return target.count > 0 && !target.keyWorker;
    }

    public bool FireEmployee(Employee target)
    {
        if (!CanFire(target))
            return false;

        target.count--;
        return true;
    }

    public bool FireEmployee(string employeeName)
    {
        var employee = employees.Find(e => e.employeeName == employeeName);
        if (employee == null) 
            return false;
        return FireEmployee(employee);
    }
}


[System.Serializable]
public enum EmployeeEfficiency { NOVICE, MIDDLE, MASTER }

[System.Serializable]
public class Employee
{
    public bool keyWorker;
    public string employeeName;
    public int initialPay;
    public int salary;
    public EmployeeEfficiency efficiency;
    public int count, maxCount;

    public Employee()
    {

    }

    public Employee (Employee other)
    {
        keyWorker = other.keyWorker;
        employeeName = other.employeeName; 
        initialPay = other.initialPay;
        salary = other.salary;
        efficiency = other.efficiency;
        count = other.count;
        maxCount = other.maxCount;
    }
}

public enum PolicyEffectType
{
    ProductionMultiplier,
    SalaryMultiplier,
    ProfitMultiplier,
    MaterialEfficiency,
    AddMaxEmployee,
    UniqueEffect // e.g. premium items, bonuses
}

[System.Serializable]
public class Policy
{
    public List<BusinessType> applicableBusinessTypes = new List<BusinessType>();
    public string policyID;
    public int tier;
    public string title;
    public string description;
    public PolicyEffectType effectType;
    public float effectValue;
}
