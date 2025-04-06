using MicroWorldNS;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GlobalTownManager : SerializedSingleton<GlobalTownManager>
{
    public List<TownManager> towns = new List<TownManager>();

    private void Update()
    {
        foreach (var manager in towns)
        {
            manager.UpdateProductionTimer();
        }
    }

    public void OnDayPassed()
    {
        foreach (var t in towns)
        {
            t.OnDayPassed();
        }
    }

    public void AddTown (TownManager manager)
    {
        towns.Add(manager);
    }

    public TownManager GetRandomClosestTownByFaction (TownManager startingTown)
    {
        List<TownManager> filteredList = new List<TownManager>(GetTowns());

        if (startingTown != null)
        {
            filteredList.Remove(startingTown);
        }

        filteredList.RemoveAll(x => x.thisTown.factionID != startingTown.thisTown.factionID);
        filteredList.Sort((town1, town2) =>
            Vector3.Distance(startingTown.transform.position, town1.transform.position)
            .CompareTo(Vector3.Distance(startingTown.transform.position, town2.transform.position))
        );

        List<TownManager> closestTowns = filteredList.Take(3).ToList();
        TownManager randomTown;
        if (closestTowns.Count < 1)
        {
            randomTown = GetRandomClosestTown(startingTown);
        }
        else
        {
            randomTown = closestTowns[Random.Range(0, closestTowns.Count)];
        }

        return randomTown;
    }

    public TownManager GetRandomClosestTown(TownManager startingTown)
    {
        List<TownManager> filteredList = new List<TownManager>(GetTowns());

        if (startingTown != null)
        {
            filteredList.Remove(startingTown);
        }

        filteredList.Sort((town1, town2) =>
            Vector3.Distance(startingTown.transform.position, town1.transform.position)
            .CompareTo(Vector3.Distance(startingTown.transform.position, town2.transform.position))
        );

        List<TownManager> closestTowns = filteredList.Take(3).ToList();

        var randomTown = closestTowns[Random.Range(0, closestTowns.Count)];

        return randomTown;
    }


    public TownManager GetRandomTown (TownManager _exception = null)
    {
        List<TownManager> filteredList = new List<TownManager>(GetTowns());
        if (_exception != null)
        {
            filteredList.Remove(_exception);
        }
        return filteredList[Random.Range(0, filteredList.Count)];
    }

    public TownManager GetRandomVillage(TownManager _exception = null)
    {
        List<TownManager> filteredList = new List<TownManager>(GetVillages());
        if (_exception != null)
        {
            filteredList.Remove(_exception);
        }
        return filteredList[Random.Range(0, filteredList.Count)];
    }

    public List<TownManager> GetTowns()
    {
        return towns.FindAll(x => x.type == TownType.TOWN);
    }

    public List<TownManager> GetVillages()
    {
        return towns.FindAll(x => x.type == TownType.VILLAGE);
    }

    public List<VillageProduct> GetSuitableItems (VillageType type)
    {
        var suitableItems = JsonReader.Instance.villageProducts.FindAll(x => x.type == type);
        return suitableItems;
    }

    public List<VillageProduct> GetAllItems ()
    {
        return JsonReader.Instance.villageProducts;
    }

    public InventoryItem GetItemByName (string name)
    {
        return JsonReader.Instance.inventoryItems.Find(x => x.itemName == name);
    }

    public List<TownManager> GetNearbyVillages (TownManager _town, int numVillages = 3)
    {
        List<TownManager> closestVillages = new List<TownManager>();

        List<(TownManager village, float distance)> villageDistances = new List<(TownManager, float)>();
        foreach (var v in GetVillages())
        {
            float distance = Vector3.Distance(_town.transform.position, new Vector3(v.transform.position.x, _town.transform.position.y, v.transform.position.z));
            villageDistances.Add((v, distance));
        }

        villageDistances.Sort((a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < numVillages && i < villageDistances.Count; i++)
        {
            closestVillages.Add(villageDistances[i].village);
        }
        return closestVillages;
    }
}

[System.Serializable]
public class VillageProduct
{
    public string name;
    public int minAmount, maxAmount;
    public int spawnChance;
    public VillageType type;

    public VillageProduct ()
    {

    }

    public VillageProduct (string _name, int _minAmount, int _maxAmount, int _spawnChance, int type)
    {
        name = _name;
        minAmount = _minAmount;
        maxAmount = _maxAmount;
        spawnChance = _spawnChance;
        this.type = (VillageType)type;
    }
}
