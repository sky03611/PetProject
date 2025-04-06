using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class GlobalBattleManager : SerializedSingleton<GlobalBattleManager>
{
    [SerializeField] private List<BattleInstance> activeBattles = new();

    public BattleInstance CreateBattle(VillagerScript a, VillagerScript b)
    {
        var battle = new BattleInstance();
        battle.battlePosition = (a.transform.position + b.transform.position) / 2f;
        battle.TeamA.Add(a);
        battle.TeamB.Add(b);
        battle.isActive = true;

        a.currentBattle = battle;
        b.currentBattle = battle;

        a.state = VillagerState.FIGHTING;
        b.state = VillagerState.FIGHTING;

        a.FE.isStopped = true;
        b.FE.isStopped = true;

        activeBattles.Add(battle);

        TryCallForReinforcements(battle, a);

        StartCoroutine(RunBattleLoop(battle));
        return battle;
    }

    private IEnumerator RunBattleLoop(BattleInstance battle)
    {
        Debug.Log($"[BattleManager] Battle started at {battle.battlePosition} with {battle.TeamA.Count} vs {battle.TeamB.Count}");

        while (battle.isActive && TotalPartySize(battle.TeamA) > 0 && TotalPartySize(battle.TeamB) > 0)
        {
            battle.TeamA.RemoveAll(v => v == null);
            battle.TeamB.RemoveAll(v => v == null);

            yield return new WaitForSeconds(3f);

            ApplyDamageRound(battle.TeamA, battle.TeamB);
            ApplyDamageRound(battle.TeamB, battle.TeamA);
        }

        EndBattle(battle);
    }


    private void ApplyDamageRound(List<VillagerScript> attackers, List<VillagerScript> defenders)
    {
        defenders.RemoveAll(d => d == null);

        if (defenders.Count == 0) 
            return;
        if (attackers.Count == 0)
            return;

        int totalAttackers = attackers.Where(a => a != null).Sum(a => a.partySize);
        float effectiveness = Random.Range(0.1f, 0.2f);
        float typeMultiplier = 1f;
        if (attackers[0].type == VillagerType.GUARD)
            typeMultiplier = 1.5f;
        else if (attackers[0].type == VillagerType.BANDIT)
            typeMultiplier = 1.2f;
        else
            typeMultiplier = 0.8f;
        int damage = Mathf.Max(1, Mathf.RoundToInt(totalAttackers * effectiveness * typeMultiplier));

        var target = defenders[Random.Range(0, defenders.Count)];
        target.partySize -= damage;

        if (target.partySize <= 0)
        {
            defenders.Remove(target);
            target.Die();
        }
    }


    private int TotalPartySize(List<VillagerScript> team)
    {
        return team.Sum(v => v.partySize);
    }

    private void EndBattle(BattleInstance battle)
    {
        Debug.Log("[BattleManager] Battle ended.");

        foreach (var unit in battle.TeamA.Concat(battle.TeamB))
        {
            if (unit == null) continue;

            if (unit.partySize > 0)
            {
                unit.OnBattleEnded();
            }

            unit.currentBattle = null;
        }

        RemoveBattle(battle);
    }

    public void RemoveBattle(BattleInstance battle)
    {
        battle.isActive = false;
        activeBattles.Remove(battle);
    }

    public List<BattleInstance> GetBattlesNear(Vector3 position, float radius)
    {
        return activeBattles.Where(b => Vector3.Distance(b.battlePosition, position) < radius && b.isActive).ToList();
    }

    public void TryCallForReinforcements(BattleInstance battle, VillagerScript caller)
    {
        caller.StartCoroutine(CallReinforcementsCoroutine(battle, caller));
    }

    private IEnumerator CallReinforcementsCoroutine(BattleInstance battle, VillagerScript caller)
    {
        yield return new WaitForSeconds(0.5f); // Optional delay

        List<VillagerScript> myTeam = battle.TeamA.Contains(caller) ? battle.TeamA : battle.TeamB;
        List<VillagerScript> enemyTeam = battle.TeamA.Contains(caller) ? battle.TeamB : battle.TeamA;

        Collider[] hits = Physics.OverlapSphere(battle.battlePosition, caller.fightRadius);

        foreach (var hit in hits)
        {
            VillagerScript unit = hit.GetComponent<VillagerScript>();
            if (unit == null || unit == caller || unit.state == VillagerState.FIGHTING || unit.currentBattle != null)
                continue;

            bool canFight = false;
            foreach (var enemy in enemyTeam)
            {
                if (unit.ShouldFightWith(enemy))
                {
                    canFight = true;
                    break;
                }
            }

            if (canFight)
            {
                unit.JoinBattle(battle, myTeam);
            }
        }
    }

}

[System.Serializable]
public class BattleInstance
{
    public List<VillagerScript> TeamA = new();
    public List<VillagerScript> TeamB = new();
    public Vector3 battlePosition;
    public bool isActive = true;
}