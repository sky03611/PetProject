using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class GuardScript : VillagerScript
{
    public override void Initialize(TownManager _st, ToSpawn s)
    {
        FE = GetComponent<FollowerEntity>();
        seeker = GetComponent<Seeker>();
        startingTown = _st;
        factionID = _st.thisTown.factionID;
        FindClosestTown();
        partySize = Random.Range(8, 21);
    }

    private void FindClosestTown()
    {
        destinationTown = GlobalTownManager.Instance.GetRandomClosestTownByFaction(startingTown);
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
                        startingTown = destinationTown;
                        FindClosestTown();
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
