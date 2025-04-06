using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;

public class CaravanScript : VillagerScript
{
    public override void Initialize(TownManager _st, ToSpawn s)
    {
        FE = GetComponent<FollowerEntity>();
        seeker = GetComponent<Seeker>();
        startingTown = _st;
        factionID = _st.thisTown.factionID;
        FindClosestTown();
        _st.GiveMoneyToCaravan (Random.Range(s.minMoney, s.maxMoney), this);
        _st.BuyItems(this);
        partySize = Random.Range(11, 21);
    }

    private void FindClosestTown()
    {
        destinationTown = GlobalTownManager.Instance.GetRandomClosestTown(startingTown);
        seeker.StartPath(transform.position, ClosestPointToRoad(destinationTown.transform.position), OnPathComplete);
        state = VillagerState.TRAVELING;
    }

    public override bool KeepStartingTown()
    {
        return false;
    }

    protected override void Update()
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
                        isHidden = true;
                        ChangeVisibility(false);
                    }
                }
            break;
            case VillagerState.WAITING:
                timer -= Time.deltaTime * DayNightHandler.Instance.GetTimeSpeed();
                if (timer <= 0)
                {
                    if (destinationTown.HasItems(this))
                    {
                        isHidden = false;
                        ChangeVisibility(true);
                        destinationTown.BuyItems(this);
                        destinationTown.RemoveVisitor(this);
                        FindClosestTown();
                        timer = maxTimer;
                    }
                }
            break;
        }
    }
}
