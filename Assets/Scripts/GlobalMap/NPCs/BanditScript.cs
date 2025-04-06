using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using MicroWorldNS;

public class BanditScript : VillagerScript
{
    [SerializeField] private float patrolRadius = 15f;
    private Vector3 spawnPoint;
    private BanditHideoutManager hideout;

    private List<Vector3> nearbyRoadCenters = new List<Vector3>();
    private Vector3? closestRoadTarget = null;
    [SerializeField] private float maxAllowedPathDistance = 30f;

    private void FindClosestRoadTarget(Vector3 center)
    {
        var worldGO = GameObject.FindGameObjectWithTag("MainWorld");
        if (worldGO == null) 
            return;

        var world = worldGO.GetComponent<MicroWorld>();
        if (world == null || world.Map == null || world.CellGeometry == null) 
            return;

        Vector3 closest = Vector3.zero;
        float closestDist = float.MaxValue;

        foreach (var hex in world.Map.AllHex())
        {
            var cell = world.Map[hex];
            if (!cell.HasContent(CellContent.IsRoad)) 
                continue;

            Vector3 roadPos = world.CellGeometry.Center(hex);
            float dist = Vector3.SqrMagnitude(roadPos - center);

            if (dist < closestDist)
            {
                closest = roadPos;
                closestDist = dist;
            }
        }

        closestRoadTarget = closest;
    }

    public void InitializeFromHideout(BanditHideoutManager hideout)
    {
        FE = GetComponent<FollowerEntity>();
        seeker = GetComponent<Seeker>();

        state = VillagerState.TRAVELING;
        factionID = -1;

        partySize = Random.Range(7, 16);
        float growthFactor = Mathf.Clamp01(DayNightHandler.Instance.TotalDays() / 100f);
        int bonusSize = Mathf.RoundToInt(Mathf.Lerp(0, 20, growthFactor));
        partySize += bonusSize;

        spawnPoint = transform.position;
        this.hideout = hideout;

        FindClosestRoadTarget(spawnPoint);
        StartCoroutine(ValidateAndWander());
    }

    private IEnumerator ValidateAndWander()
    {
        if (closestRoadTarget.HasValue)
        {
            var path = ABPath.Construct(transform.position, closestRoadTarget.Value);
            seeker.StartPath(path, _ => { });
            yield return new WaitUntil(() => path.IsDone());

            if (!path.error)
            {
                float totalPathLength = 0f;
                for (int i = 1; i < path.vectorPath.Count; i++)
                    totalPathLength += Vector3.Distance(path.vectorPath[i - 1], path.vectorPath[i]);

                if (totalPathLength <= maxAllowedPathDistance)
                {
                    nearbyRoadCenters.Clear();
                    nearbyRoadCenters.Add(closestRoadTarget.Value);
                }
                else
                {
                    nearbyRoadCenters.Clear();
                }
            }
        }

        timer = 0;
        state = VillagerState.WAITING;
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

        if (state == VillagerState.RECOVERING)
            return;

        if (!isJoiningBattle)
            LookForEnemies();

        switch (state)
        {
            case VillagerState.TRAVELING:
                if (FE.reachedDestination)
                {
                    if (!isJoiningBattle)
                    {
                        timer = maxTimer;
                        state = VillagerState.WAITING;
                    }
                }
                break;

            case VillagerState.WAITING:
                timer -= Time.deltaTime * DayNightHandler.Instance.GetTimeSpeed();
                if (timer <= 0)
                {
                    WanderToNewPoint();
                    state = VillagerState.TRAVELING;
                }
                break;
        }
    }

    private void WanderToNewPoint()
    {
        Vector3 destination;

        if (nearbyRoadCenters.Count == 0)
        {
            Vector2 offset = Random.insideUnitCircle * patrolRadius;
            destination = spawnPoint + new Vector3(offset.x, 0, offset.y);
        }
        else
        {
            Vector3 roadTarget = nearbyRoadCenters[Random.Range(0, nearbyRoadCenters.Count)];

            Vector2 offset = Random.insideUnitCircle * 3f;
            destination = roadTarget + new Vector3(offset.x, 0, offset.y);
        }

        if (Terrain.activeTerrain != null)
        {
            destination.y = Terrain.activeTerrain.SampleHeight(destination);
        }
        else
        {
            destination.y = transform.position.y;
        }

        seeker.StartPath(transform.position, destination, OnPathComplete);
    }

    public override void OnBattleEnded()
    {
        state = VillagerState.RECOVERING;
        FE.isStopped = false;

        FindClosestRoadTarget(transform.position);
        StartCoroutine(ValidateAndWander());
    }

    public override bool KeepStartingTown()
    {
        return false;
    }

    public override void ChangeMoney(int _amount)
    {
        // Bandits don't deal in money
    }

    public override bool HasEnoughMoney(int _amount)
    {
        return false;
    }

    public override void Die()
    {
        hideout.OnBanditDied(this);
        base.Die();
    }
}