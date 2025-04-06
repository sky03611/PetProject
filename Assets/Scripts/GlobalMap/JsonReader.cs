using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

public class JsonReader : SerializedSingleton<JsonReader>
{
    public List<Village> villageInfo = new List<Village>();
    public List<Town> townInfo = new List<Town>();
    public List<VillageProduct> villageProducts = new List<VillageProduct>();
    public List<InventoryItem> inventoryItems = new List<InventoryItem>();
    public List<Modifier> villageModifiers = new List<Modifier>();
    public List<Modifier> townModifiers = new List<Modifier>();

    public List<TownManager> towns = new List<TownManager>();
    public List<VillageManager> villages = new List<VillageManager>();

    public List<Business> businesses = new List<Business>();
    public List<Business> villageBusinesses = new List<Business>();

    public InventoryItem GetItemByName(string name)
    {
        return inventoryItems.Find(item => item.itemName == name);
    }


    private void Start()
    {
        LoadJSONInfo();
    }

    private void LoadJSONInfo()
    {
        villageInfo.Clear();
        townInfo.Clear();
        villageProducts.Clear();
        inventoryItems.Clear();
        villageModifiers.Clear();
        townModifiers.Clear();
        businesses.Clear();
        villageBusinesses.Clear();
        villageInfo.AddRange(TypeArrayDeserializer.LoadResourceFromJson<Village>("Villages"));
        townInfo.AddRange(TypeArrayDeserializer.LoadResourceFromJson<Town>("Towns"));
        villageProducts.AddRange(TypeArrayDeserializer.LoadResourceFromJson<VillageProduct>("VillageTypes"));
        inventoryItems.AddRange(TypeArrayDeserializer.LoadResourceFromJson<InventoryItem>("InventoryItems"));
        villageModifiers.AddRange(TypeArrayDeserializer.LoadResourceFromJson<Modifier>("Modifiers/Villages"));
        townModifiers.AddRange(TypeArrayDeserializer.LoadResourceFromJson<Modifier>("Modifiers/Towns"));
        businesses.AddRange(TypeArrayDeserializer.LoadResourceFromJson<Business>("Businesses"));
        villageBusinesses.AddRange(TypeArrayDeserializer.LoadResourceFromJson<Business>("VillageBusinesses"));
        SetModifiers(ref villageModifiers);
        SetModifiers(ref townModifiers);
    }

    public Business GetBusiness (BusinessType type)
    {
        Business targetBusiness = null;
        targetBusiness = businesses.Find(x => x.businessType == type);
        if (targetBusiness == null)
            targetBusiness = villageBusinesses.Find (x => x.businessType == type);
        return targetBusiness;
    }

    public Business GetCoreBusiness(TownManager townManager)
    {
        foreach (Business business in villageBusinesses)
        {
            if (business.isCoreBusiness && business.IsSuitableForThisVillage(townManager))
            {
                return business;
            }
        }

        Debug.LogWarning("No core business found for this town: " + townManager.thisTown.name);
        return null;
    }


    public List<Business> GetSuitableBusinesses (List<TownManager> villages)
    {
        List<Business> tmpBusinesses = new List<Business>();
        foreach (var v in villages)
        {
            foreach (var t in businesses)
            {
                if (v.IsSupplyingBusiness(t.businessType))
                {
                    tmpBusinesses.Add(t);
                }
            }
        }
        return tmpBusinesses;
    }

    public List<Business> GetSuitableBusinesses (TownManager village)
    {
        List<Business> tmpBusinesses = new List<Business>();
        if (village.IsVillage())
        {
            foreach (var t in villageBusinesses)
            {
                if (t.IsSuitableForThisVillage(village))
                {
                    tmpBusinesses.Add(t);
                }
            }
        }
        else
        {
            foreach (var t in businesses)
            {
                tmpBusinesses.Add(t);
            }
        }
        return tmpBusinesses;
    }

    private void SetModifiers (ref List<Modifier> settlementModifiers)
    {
        List<string> tmpHelper = new List<string>();
        foreach (var v in settlementModifiers)
        {
            tmpHelper = v.seasons.Split(',').ToList();
            foreach (var s in tmpHelper)
            {
                v.possibleSeasons.Add(int.Parse(s));
            }
        }
    }

    public Modifier GetRandomModifier (TownManager town)
    {
        List<Modifier> tmpModifiers = new List<Modifier>();
        if (town.IsVillage())
        {
            foreach (var v in villageModifiers)
            {
                if (v.possibleSeasons.Contains ((int)SeasonsHandler.Instance.GetCurrentSeason()))
                {
                    if (v.settlementType == town.thisTown.GetSettlementType() || v.AnySettlementType())
                    {
                        tmpModifiers.Add(v);
                    }
                }
            }
        }
        else
        {
            foreach (var t in townModifiers)
            {
                if (t.possibleSeasons.Contains((int)SeasonsHandler.Instance.GetCurrentSeason()))
                {
                    if (t.settlementType == town.thisTown.GetSettlementType() || t.AnySettlementType())
                    {
                        tmpModifiers.Add(t);
                    }
                }
            }
        }
        return tmpModifiers[Random.Range (0, tmpModifiers.Count)];
    }

    public void AddTown(TownManager _town)
    {
        towns.Add(_town);
    }

    public void AddVillage(VillageManager _village)
    {
        villages.Add(_village);
    }

    public void GeneratePlaces<T>(List<T> placeInfo, List<TownManager> placeManagers) where T : class
    {
        ShufflePlaceInfo(placeInfo);

        int placeCount = Mathf.Min(placeManagers.Count, placeInfo.Count);

        for (int i = 0; i < placeCount; i++)
        {
            if (placeManagers[i] is TownManager townManager && placeInfo[i] is Town town)
            {
                townManager.thisTown = town;
            }
            else if (placeManagers[i] is VillageManager villageManager && placeInfo[i] is Village village)
            {
                villageManager.thisTown = village;
            }
        }
    }

    private void ShufflePlaceInfo<T>(List<T> placeInfo) where T : class
    {
        System.Random rng = new System.Random();
        int n = placeInfo.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = placeInfo[k];
            placeInfo[k] = placeInfo[n];
            placeInfo[n] = value;
        }
    }


    public void GenerateTowns()
    {
        GeneratePlaces(townInfo, towns);
    }

    public void GenerateVillages()
    {
        List<TownManager> villageManagersAsTowns = new List<TownManager>(villages);
        GeneratePlaces(villageInfo, villageManagersAsTowns);
    }
}
