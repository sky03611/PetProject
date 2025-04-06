using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;

public enum VillagerType { VILLAGER, CARAVAN, GUARD, BANDIT }
public enum VillagerState { TRAVELING, WAITING, RETURNING, FIGHTING, RECOVERING }

public class VillagerScript : MonoBehaviour
{
    public string dialogueName;
    [SerializeField] protected bool isHidden, isHiddenByPlayer;
    public int factionID;
    public int partySize;
    public VillagerType type;
    public VillagerState state;
    [SerializeField] protected int money;
    public Inventory thisInventory;
    public FollowerEntity FE;
    protected Seeker seeker;
    [SerializeField] protected TownManager startingTown, destinationTown;
    [SerializeField] protected float timer, maxTimer;
    [SerializeField] protected VillagerUIManager villagerUIManager;
    public float fightRadius = 10f;
    [SerializeField] protected bool isJoiningBattle = false;
    public BattleInstance currentBattle;

    protected List<InventoryItem> toRemove = new List<InventoryItem>();

    protected Transform playerTransform;

    [SerializeField] private string dialogueFolder;

    public bool IsHostileTowardsPlayer ()
    {
        if (type == VillagerType.BANDIT)
            return true;
        return false;
    }

    public int GetPriceForItem(InventoryItem _item, bool _buyingItem = false)
    {
        if (_item.newPrice != 0)
        {
            return _item.newPrice;
        }
        var itemsCount = thisInventory.GetAllItemsOfName(_item.itemName).Count;

        float priceMultiplier = 1.0f;

        if (itemsCount > 0)
        {
            priceMultiplier = System.Math.Max(0.2f, 1 - (itemsCount * 0.05f));
        }
        else
        {
            priceMultiplier = 2.0f;
        }

        if (type != VillagerType.VILLAGER)
        {
            priceMultiplier *= 1.1f;
        }

        if (_buyingItem) //if a player is selling this item to the town
        {
            priceMultiplier /= 1.1f;
        }
        else
        {
            if (GetFaction().Fame >= 75)
            {
                priceMultiplier *= 0.9f;
            }
            if (GetFaction().Fame <= 25)
            {
                priceMultiplier *= 1.1f;
            }
        }

        int calculatedPrice = (int)(_item.defaultPrice * priceMultiplier);

        return System.Math.Max(calculatedPrice, (int)(_item.defaultPrice * 0.2f));
    }

    public virtual string GetGreetingsDialogue()
    {
        return string.Format("Dialogues/{0}/Greetings", dialogueFolder);
    }

    public string GetStartingTown()
    {
        return L.G(startingTown.thisTown.name);
    }

    public string GetDestinationTown()
    {
        return L.G(destinationTown.thisTown.name);
    }

    public string GetProducts()
    {
        string productString = "";
        for (int i = 0; i < thisInventory.items.Count; i++)
        {
            if (i < thisInventory.items.Count - 1)
                productString += string.Format("<color=yellow>{0}</color>", thisInventory.items[i].amount) + " " + thisInventory.items[i].itemName + ", ";
            else
                productString += string.Format("<color=yellow>{0}</color>", thisInventory.items[i].amount) + " " + thisInventory.items[i].itemName + ".";
        }
        return productString;
    }

    public bool IsPlayerNearby()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= CameraController.Instance.visibilityDistance)
        {
            return true;
        }
        return false;
    }

    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        timer = maxTimer;
        if (villagerUIManager == null)
        {
            villagerUIManager = GetComponent<VillagerUIManager>();
        }
        thisInventory.owner = gameObject;
        villagerUIManager.Initialize();
    }

    public virtual bool IsVisible()
    {
        return !isHiddenByPlayer && !isHidden;
    }

    private float GetSpeed ()
    {
        float basicSpeed = 3f;
        switch (type)
        {
            case VillagerType.CARAVAN:
                basicSpeed = 4f;
            break;
            default:
                basicSpeed = 3f;
            break;
        }
        float updatedSpeed = basicSpeed - partySize * 0.05f;
        if (updatedSpeed < 1)
            updatedSpeed = 1;
        return updatedSpeed;
    }

    public void ChangeVisibility(bool _enabled)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (isHiddenByPlayer)
        {
            foreach (var r in renderers)
            {
                r.enabled = false;
            }
        }
        else
        {
            foreach (var r in renderers)
            {
                r.enabled = _enabled && !isHidden;
            }
        }
    }

    public Inventory GetInventory()
    {
        return thisInventory;
    }

    public int GetMoney()
    {
        return money;
    }

    public virtual void ChangeMoney (int _amount)
    {
        money += _amount;
        if (money < 0)
        {
            money = 0;
        }
    }

    public virtual bool HasEnoughMoney (int _amount)
    {
        return money >= _amount;
    }

    public virtual void SetStartingTown (TownManager _st)
    {
        startingTown = _st;
    }

    public virtual void Initialize(TownManager _st, ToSpawn s)
    {
        FE = GetComponent<FollowerEntity>();
        seeker = GetComponent<Seeker>();
        startingTown = _st;
        factionID = _st.thisTown.factionID;
        destinationTown = GlobalTownManager.Instance.GetRandomClosestTownByFaction(startingTown);
        seeker.StartPath(transform.position, ClosestPointToRoad(destinationTown.transform.position), OnPathComplete);
        money = Random.Range(s.minMoney, s.maxMoney);
        _st.GiveItems(this);
        state = VillagerState.TRAVELING;
        partySize = Random.Range(3, 11);
    }

    protected virtual void OnPathComplete(Path path)
    {
        if (path.error)
        {
            Debug.LogError("Pathfinding error: " + path.errorLog + " for " + name + " heading to " + destinationTown.thisTown.name);
            return;
        }

        if (path.vectorPath.Count > 0)
        {
            Vector3 destinationPoint = path.vectorPath.Last();
            FE.SetDestination(destinationPoint);
        }
        else
        {
            Debug.LogError("Path could not be completed: No valid path found.");
        }
    }

    protected virtual Vector3 ClosestPointToRoad (Vector3 fromPosition)
    {
        var selectedGraph = AstarPath.active.graphs[1];
        NNInfo nearestNodeInfo = selectedGraph.GetNearest(fromPosition);
        return nearestNodeInfo.position;
    }

    public virtual bool KeepStartingTown()
    {
        return true;
    }

    protected virtual void LateUpdate()
    {
        FE.maxSpeed = GetSpeed();
        isHiddenByPlayer = !IsPlayerNearby();
        if (!isHiddenByPlayer)
        {
            if (!isHidden)
            {
                ChangeVisibility(true);
            }
        }
        else
        {
            ChangeVisibility(false);
        }

    }

    protected virtual void Update()
    {
        toRemove.Clear();
        foreach (var item in thisInventory.items)
        {
            if (item.toRemove)
            {
                toRemove.Add(item);
            }
        }
        foreach (var t in toRemove)
        {
            thisInventory.items.Remove(t);
        }
        switch (state)
        {
            case VillagerState.TRAVELING:
                if (FE.reachedDestination)
                {
                    if (!isJoiningBattle)
                    {
                        destinationTown.SellAllItems(thisInventory, this);
                        destinationTown.AddVisitor(this);
                        state = VillagerState.WAITING;
                        ChangeVisibility(false);
                        isHidden = true;
                    }
                }
                break;
            case VillagerState.WAITING:
                timer -= Time.deltaTime * DayNightHandler.Instance.GetTimeSpeed();
                if (timer <= 0)
                {
                    isHidden = false;
                    ChangeVisibility(true);
                    destinationTown.RemoveVisitor(this);
                    destinationTown = startingTown;
                    seeker.StartPath(transform.position, ClosestPointToRoad(destinationTown.transform.position), OnPathComplete);
                    timer = maxTimer;
                    state = VillagerState.RETURNING;
                }
                break;
            case VillagerState.RETURNING:
                if (FE.reachedDestination)
                {
                    destinationTown.ChangeCoffers(money);
                    destinationTown.SellAllItems(thisInventory, this);
                    destinationTown.RemoveSpawned(this);
                    Die();
                }
                break;
        }
    }

    protected virtual void LookForEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, fightRadius);
        foreach (var hit in hits)
        {
            VillagerScript other = hit.GetComponent<VillagerScript>();
            if (other != null && other != this)
            {
                if (ShouldFightWith(other))
                {
                    StartCoroutine(FightWith(other));
                    break;
                }
            }
        }
    }

    public bool ShouldFightWith(VillagerScript other)
    {
        if (other.isHidden)
            return false;

        if (type == VillagerType.GUARD && other.type == VillagerType.BANDIT)
            return false;

        if (type == VillagerType.BANDIT && other.type != VillagerType.BANDIT)
            return true;

        return false;
    }

    protected IEnumerator FightWith(VillagerScript enemy)
    {
        Vector3 midpoint = (transform.position + enemy.transform.position) / 2f;

        state = VillagerState.TRAVELING;
        enemy.state = VillagerState.TRAVELING;

        seeker.StartPath(transform.position, midpoint, OnPathComplete);
        enemy.seeker.StartPath(enemy.transform.position, midpoint, enemy.OnPathComplete);

        while (Vector3.Distance(transform.position, midpoint) > 0.5f ||
               Vector3.Distance(enemy.transform.position, midpoint) > 0.5f)
        {
            if (currentBattle != null || enemy.currentBattle != null) yield break;
            yield return null;
        }

        GlobalBattleManager.Instance.CreateBattle(this, enemy);
    }

    public void JoinBattle(BattleInstance battle, List<VillagerScript> allyTeam)
    {
        isJoiningBattle = true;
        StartCoroutine(MoveToBattleAndJoin(battle, allyTeam));
    }

    private IEnumerator MoveToBattleAndJoin(BattleInstance battle, List<VillagerScript> allyTeam)
    {
        if (seeker == null)
            seeker = GetComponent<Seeker>();
        if (FE == null)
            FE = GetComponent<FollowerEntity>();

        seeker.StartPath(transform.position, battle.battlePosition, OnPathComplete);

        while (Vector3.Distance(transform.position, battle.battlePosition) > 0.5f)
        {
            if (!battle.isActive)
            {
                state = VillagerState.TRAVELING;
                isJoiningBattle = false;
                yield break;
            }
            yield return null;
        }

        state = VillagerState.FIGHTING;
        FE.isStopped = false;
        isJoiningBattle = false;

        if (!allyTeam.Contains(this))
            allyTeam.Add(this);
    }

    public virtual void OnBattleEnded()
    {
        state = VillagerState.TRAVELING;
        FE.isStopped = false;
    }

    public virtual void Die()
    {
        villagerUIManager.Clear();
        Destroy(gameObject);
    }


    public void AddItem (InventoryItem item, int _amount = 0)
    {
        thisInventory.AddItem(item, _amount);
    }

    public Faction GetFaction()
    {
        return FactionScript.Instance.factions[factionID];
    }
}
