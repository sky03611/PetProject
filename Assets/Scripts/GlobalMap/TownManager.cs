using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Pathfinding;
using static UnityEngine.EventSystems.EventTrigger;

public enum TownType { VILLAGE, TOWN }

public class TownManager : SerializedMonoBehaviour
{
    public bool isVisited;
    public TownType type;
    public int modifierDays;
    public float productionTimer = 250;
    [SerializeField] internal Town thisTown;
    public TownUIManager townUIManager;
    [SerializeField] private List<ToSpawn> toSpawn;
    [SerializeField] private List<VillagerScript> spawnedByThis = new List<VillagerScript>();
    [SerializeField] private List<VillagerScript> visitors = new List<VillagerScript>();
    private List<InventoryItem> toRemove = new List<InventoryItem>();

    [SerializeField] private List<Morale> settlementMorale;

    [SerializeField] private List<TownManager> closestVillages = new List<TownManager>();

    public List<Business> GetSuitableBusinesses()
    {
        return JsonReader.Instance.GetSuitableBusinesses(this);
    }

    public virtual bool IsSupplyingBusiness (BusinessType businessType)
    {
        return false;
    }

    public Morale GetMorale ()
    {
        return settlementMorale[thisTown.GetMorale()];
    }

    public void SetFaction (int factionID)
    {
        thisTown.factionID = factionID;
        townUIManager.SetFaction();
    }

    public int GetPriceForItem(InventoryItem _item, bool _buyingItem = false)
    {
        if (_item.newPrice != 0)
        {
            return _item.newPrice;
        }
        var itemsCount = thisTown.inventory.GetAllItemsOfName(_item.itemName).Count;

        float priceMultiplier = 1.0f;

        if (itemsCount == 0)
        {
            priceMultiplier = 1.6f;
        }
        else
        {
            priceMultiplier = System.Math.Min(1.5f, 1.0f + (itemsCount * 0.05f));
        }

        if (!IsVillage())
        {
            priceMultiplier *= 1.25f;
        }

        if (_buyingItem)
        {
            priceMultiplier *= settlementMorale[thisTown.GetMorale()].PricesMultiplier();
            if (thisTown.GetFaction().Fame >= 50)
            {
                priceMultiplier /= 1.1f;
            }
            else
            {
                priceMultiplier /= 1.25f;
            }
        }
        else
        {
            priceMultiplier /= settlementMorale[thisTown.GetMorale()].PricesMultiplier();
            if (thisTown.GetFaction().Fame >= 75)
            {
                priceMultiplier *= 0.9f;
            }
            if (thisTown.GetFaction().Fame <= 25)
            {
                priceMultiplier *= 1.1f;
            }
        }

        int calculatedPrice = (int)(_item.defaultPrice * priceMultiplier);
        return System.Math.Max(calculatedPrice, (int)(_item.defaultPrice * 0.2f));
    }

    public int GetPriceForItemCaravan(InventoryItem _item, VillagerScript _caravan)
    {
        var itemsCount = thisTown.inventory.GetAllItemsOfName(_item.itemName).Count;

        float priceMultiplier = 1.0f;

        if (itemsCount == 0)
        {
            priceMultiplier = 1.6f;
        }
        else
        {
            priceMultiplier = System.Math.Min(1.5f, 1.0f + (itemsCount * 0.05f));
        }

        if (!IsVillage())
        {
            priceMultiplier *= 1.25f;
        }
        priceMultiplier /= settlementMorale[thisTown.GetMorale()].PricesMultiplier();
        if (_caravan.factionID == thisTown.factionID)
        {
            priceMultiplier *= 0.9f;
        }
        else
        {
            priceMultiplier *= 1.1f;
        }

        int calculatedPrice = (int)(_item.defaultPrice * priceMultiplier);
        return System.Math.Max(calculatedPrice, (int)(_item.defaultPrice * 0.2f));
    }

    public string GetSettlementType()
    {
        switch (type)
        {
            case TownType.VILLAGE:
                return "VILLAGE";
            case TownType.TOWN:
                return "TOWN";
        }
        return "TOWN";
    }

    public void GiveMoneyToCaravan (int _amount, VillagerScript _caravan)
    {
        if (_amount > thisTown.Coffers)
        {
            _amount = thisTown.Coffers;
        }
        _caravan.ChangeMoney(_amount);
        thisTown.ChangeCoffers(-_amount);
    }

    public void ChangeCoffers(int _amount)
    {
        thisTown.ChangeCoffers(_amount);
    }

    public void AddVisitor (VillagerScript _visitor)
    {
        if (!_visitor.KeepStartingTown())
            _visitor.SetStartingTown(this);
        visitors.Add(_visitor);
    }

    public void RemoveVisitor (VillagerScript _visitor)
    {
        if (visitors.Contains (_visitor))
        {
            visitors.Remove (_visitor);
        }
    }

    public void RemoveSpawned (VillagerScript _spawned)
    {
        if (spawnedByThis.Contains (_spawned))
        {
            spawnedByThis.Remove (_spawned);
        }
    }    

    public bool HasItems (VillagerScript _buyer)
    {
        foreach (var i in thisTown.inventory.items)
        {
            if (i.previousOwner != _buyer)
            {
                return true;
            }
        }
        return false;
    }

    public void BuyItems (VillagerScript _buyer)
    {
        List<InventoryItem> toRemove = new List<InventoryItem> ();
        foreach (var i in thisTown.inventory.items)
        {
            if (i.previousOwner != _buyer)
            {
                if (GetPriceForItemCaravan (i, _buyer) < i.defaultPrice)
                {
                    for (int j = 0; j < i.amount; j++)
                    {
                        if (_buyer.HasEnoughMoney(GetPriceForItemCaravan(i, _buyer)))
                        {
                            _buyer.AddItem(i, 1);
                            _buyer.ChangeMoney(-GetPriceForItemCaravan(i, _buyer));
                            ChangeCoffers(GetPriceForItemCaravan(i, _buyer));
                            toRemove.Add(i);
                        }
                    }
                }
            }
        }
        foreach (var j in toRemove)
        {
            thisTown.inventory.RemoveItem(j, 1);
        }
    }

    public void BuyItems(Business _buyer)
    {
        List<InventoryItem> toRemove = new List<InventoryItem>();
        foreach (var i in thisTown.inventory.items)
        {
            if (_buyer.IsSuitable (i))
            {
                for (int j = 0; j < i.amount; j++)
                {
                    if (_buyer.HasEnoughMoney(GetPriceForItem(i)))
                    {
                        _buyer.AddItem(i, 1);
                        _buyer.ChangeMoney(-GetPriceForItem(i));
                        ChangeCoffers(GetPriceForItem(i));
                        toRemove.Add(i);
                    }
                }
            }
        }
        foreach (var j in toRemove)
        {
            thisTown.inventory.RemoveItem(j, 1);
        }
    }

    public void SellAllItems (Inventory inventory, VillagerScript _beneficiary = null)
    {
        if (_beneficiary != null)
        {
            foreach (var item in inventory.items)
            {
                _beneficiary.ChangeMoney(GetPriceForItemCaravan(item, _beneficiary) * item.amount);
                _beneficiary.thisInventory.RemoveItem(item);
            }
            thisTown.inventory.AddAllItems(inventory.items, _beneficiary);
        }
    }

    public void GiveItems (VillagerScript _villager)
    {
        var item = thisTown.inventory.GiveRandomItem();
        if (item != null)
        {
            _villager.AddItem(item, 1);
        }
    }

    public void OnDayPassed(bool _increaseCoffers = true)
    {
        if (_increaseCoffers)
            thisTown.ChangeCoffers();
        SpawnVillagers();
        if (thisTown.HasModifier())
        {
            modifierDays--;
            if (modifierDays <= 0)
            {
                thisTown.modifier = null;
            }
            if (thisTown.modifier != null)
            {
                if (thisTown.modifier.possibleSeasons == null)
                {
                    thisTown.modifier.possibleSeasons = new List<int>();
                }
                if (!thisTown.modifier.possibleSeasons.Contains((int)SeasonsHandler.Instance.GetCurrentSeason()))
                {
                    thisTown.modifier = null;
                }
            }
        }
        if (Random.Range (1, 101) <= 70)
        {
            SetModifier();
        }
    }

    private void SpawnVillagers()
    {
        int maxSpawn = 0, counter = 0;
        foreach (var s in toSpawn)
        {
            maxSpawn += s.chance;
        }
        int i = Random.Range(0, maxSpawn);
        int spawnCounter = 0;
        foreach (var s in toSpawn)
        {
            counter += s.chance;
            if (s.HasToSpawn() || i < counter)
            {
                spawnCounter = spawnedByThis.FindAll(x => x.type == s.villager.type).Count;
                if (s.HasLimit (spawnCounter))
                {
                    var v = Instantiate(s.villager, ClosestPointToRoad(transform.position), s.villager.transform.rotation);
                    v.Initialize(this, s);
                    spawnedByThis.Add(v);
                    if (!s.HasToSpawn())
                    {
                        break;
                    }
                }
            }
        }
    }

    public bool IsVillage()
    {
        return type == TownType.VILLAGE;
    }

    private void Start()
    {
        if (townUIManager == null)
        {
            townUIManager = GetComponent<TownUIManager>();
        }
        thisTown.inventory.owner = gameObject;
        townUIManager.Initialize(this);
        switch (type)
        {
            case TownType.VILLAGE:
                thisTown.Coffers = Random.Range(5, 16) * 10;
                break;
            case TownType.TOWN:
                thisTown.Coffers = Random.Range(25, 51) * 10;
                break;
        }
        StartCoroutine(AddInitialBusinesses());
        AddInitialProduction();
        productionTimer = 50;
        if (Random.Range(1, 101) <= 70)
        {
            SetModifier();
        }
        if (Random.Range (1, 101) <= 50)
        {
            Invoke("SpawnVillagers", Random.Range (5f, 10f));
        }
    }

    protected void AddInitialProduction()
    {
        switch (type)
        {
            case TownType.VILLAGE:
                for (int i = 0; i < Random.Range(3, 6); i++)
                {
                    thisTown.AddProduction(null);
                }

            break;
            case TownType.TOWN:
                for (int i = 0; i < Random.Range(5, 11); i++)
                {
                    thisTown.AddMiscellaneous();
                }
            break;
        }
    }

    public void OpenNewBusiness (BusinessType type)
    {
        thisTown.AddBusiness(JsonReader.Instance.GetBusiness(type), this, true);
    }

    protected IEnumerator AddInitialBusinesses()
    {
        yield return new WaitForSeconds(1.5f);
        int _businessesCount = Random.Range(1, 4);
        if (IsVillage())
        {
            var allBusinesses = JsonReader.Instance.GetSuitableBusinesses(this);
            Business coreBusiness = JsonReader.Instance.GetCoreBusiness(this);

            if (!thisTown.HasBusiness(coreBusiness.businessType))
            {
                thisTown.AddBusiness(coreBusiness, this);
            }

            int roll = Random.Range(0, 4);
            if (roll == 1)
            {
                var additionalBusinesses = new List<Business>(allBusinesses);
                additionalBusinesses.RemoveAll(b => b.businessType == coreBusiness.businessType);

                if (additionalBusinesses.Count > 0)
                {
                    Business extra = additionalBusinesses[Random.Range(0, additionalBusinesses.Count)];
                    if (!thisTown.HasBusiness(extra.businessType))
                    {
                        thisTown.AddBusiness(extra, this);
                    }
                }
            }
        }
        else
        {
            closestVillages = GlobalTownManager.Instance.GetNearbyVillages(this);
            var businesses = JsonReader.Instance.GetSuitableBusinesses(closestVillages);

            for (int i = 0; i < _businessesCount; i++)
            {
                Business business = businesses[Random.Range(0, businesses.Count)];
                if (!thisTown.HasBusiness(business.businessType))
                {
                    thisTown.AddBusiness(business, this);
                }
            }
        }
    }

    protected void SetModifier ()
    {
        thisTown.modifier = JsonReader.Instance.GetRandomModifier(this);
        modifierDays = thisTown.modifier.maxDays;
    }


    private void Update()
    {
        toRemove.Clear();
        foreach (var item in thisTown.inventory.items)
        {
            if (item.toRemove)
            {
                toRemove.Add(item);
            }
        }
        foreach (var t in toRemove)
        {
            thisTown.inventory.items.Remove(t);
        }
        thisTown.ClearBusinesses();
        if (isVisited)
        {
            if (Vector3.Distance(transform.position, CameraController.Instance.GetHeroPosition(transform.position.y)) > 3)
            {
                isVisited = false;
            }
        }
    }

    public void UpdateProductionTimer()
    {
        productionTimer -= Time.deltaTime * DayNightHandler.Instance.GetTimeSpeed();
        if (productionTimer <= 0)
        {
            thisTown.AddProduction(thisTown.modifier);
            if (thisTown.modifier != null)
            {
                productionTimer = Mathf.FloorToInt (thisTown.productionTimer / thisTown.modifier.productionSpeed);
            }
            else
            {
                productionTimer = thisTown.productionTimer;
            }
        }
    }

    private Vector3 ClosestPointToRoad(Vector3 fromPosition)
    {
        var selectedGraph = AstarPath.active.graphs[1];
        NNInfo nearestNodeInfo = selectedGraph.GetNearest(fromPosition);

        return nearestNodeInfo.position;
    }

    public void OnVisit()
    {
        isVisited = true;
    }
}

[System.Serializable]
public class ToSpawn
{
    public bool hasToSpawn;
    public int chance;
    public int spawnLimit;
    public int minMoney, maxMoney;
    public VillagerScript villager;

    public ToSpawn ()
    {

    }

    public bool HasToSpawn ()
    {
        return hasToSpawn;
    }

    public bool HasLimit (int spawnCount)
    {
        if (spawnLimit == 0)
            return true;
        if (spawnCount < spawnLimit)
            return true;
        return false;
    }
}