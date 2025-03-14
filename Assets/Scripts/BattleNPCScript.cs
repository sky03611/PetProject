using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;

public enum NPCState { CHARGING, HITTING, DEAD }

public class BattleNPCScript : MonoBehaviour
{
    public bool movementStopped;
    public NPCState state;
    public HumanEntity thisEntity, closestEnemy;
    private AnimationStateController anim;
    private FollowerEntity FE;
    private float stateTimer, attackStateTimer;

    void Start()
    {
        anim = GetComponent<AnimationStateController>();
        thisEntity = GetComponent<HumanEntity>();
        FE = GetComponent<FollowerEntity>();
        ScanForEnemies();
    }

    // Update is called once per frame
    void Update()
    {
        anim.VelocityX = transform.InverseTransformDirection(FE.velocity).x/FE.maxSpeed;
        anim.VelocityZ = transform.InverseTransformDirection(FE.velocity).z/FE.maxSpeed;
        if (movementStopped || anim.IsAttacking())
        {
            FE.maxSpeed = 0;
        }
        else
        {
            FE.maxSpeed = 3;
        }
        StateUpdate();
        switch (state)
        {
            case NPCState.CHARGING:
                if (closestEnemy != null)
                {
                    FE.destination = closestEnemy.transform.position;
                    if (!anim.IsHit())
                    {
                        if (Vector3.Distance(transform.position, closestEnemy.transform.position) < thisEntity.GetAttackDistance())
                        {
                            movementStopped = true;
                            state = NPCState.HITTING;
                        }
                        else
                        {
                            movementStopped = false;
                        }
                    }
                }
                break;
            case NPCState.HITTING:
                if (Vector3.Distance(transform.position, closestEnemy.transform.position) < thisEntity.GetAttackDistance())
                {
                    if (!anim.IsBlocking() && attackStateTimer <= 0)
                    {
                        anim.MeleeAttack();
                        attackStateTimer = 1;
                    }
                }
                else
                {
                    if (!anim.IsAttacking())
                    {
                        state = NPCState.CHARGING;
                        attackStateTimer = 0;
                    }
                }
                break;
            case NPCState.DEAD:

                break;
        }
    }

    private void StateUpdate()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer < 0)
        {
            if (closestEnemy.IsAttacking())
            {
                if (Vector3.Distance(transform.position, closestEnemy.transform.position) < (closestEnemy.GetAttackDistance() + 2))
                {
                    if (!anim.IsBlocking())
                    {
                        anim.MeleeBlock(closestEnemy);
                    }
                    else
                    {
                        if (!anim.SameDirection(closestEnemy.GetAttackDirection()))
                        {
                            anim.StopBlocking();
                        }
                    }
                }
            }
            else
            {
                if (anim.IsBlocking())
                {
                    anim.StopBlocking();
                }
            }
            stateTimer = 0.4f;
        }
        if (attackStateTimer > 0)
        {
            attackStateTimer -= Time.deltaTime;
        }
    }

    private void ScanForEnemies()
    {
        List<HumanEntity> _enemies = new List<HumanEntity>();
        foreach (HumanEntity e in FindObjectsOfType<HumanEntity>().ToList())
        {
            if (e.gameObject != gameObject)
            {
                _enemies.Add(e);
            }
        }
        closestEnemy = _enemies[0];
        foreach (var e in _enemies)
        {
            if (Vector3.Distance (transform.position, e.transform.position) < Vector3.Distance (transform.position, closestEnemy.transform.position))
            {
                closestEnemy = e;
            }
        }
    }

    public void StopMovementTemporarily()
    {
        FE.maxSpeed = 0;
    }

    public void ContinueMovement()
    {
        FE.maxSpeed = 3;
    }
}
