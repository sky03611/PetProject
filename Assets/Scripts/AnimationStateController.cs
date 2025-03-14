using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnimatorType { PLAYER, NPC }

public class AnimationStateController : MonoBehaviour
{
    public Direction direction;
    public AnimatorType type;
    [SerializeField] protected bool isCrouching, isBlocking, isHit, isAttacking;
    [SerializeField] protected float velocityX, velocityZ;
    [SerializeField] protected float movementSpeed, acceleration = 1, deceleration = 2;
    [SerializeField] protected HumanEntity thisEntity;

    protected Animator anim;
    protected int blockingHash;
    protected int hittingHash, hitHash;
    protected int VelocityXHash, VelocityZHash;
    protected int AttackDirectionHash;
    protected int HitTypeHash;

    public float VelocityX 
    { 
        get 
        { 
            return velocityX; 
        } 
        set 
        { 
            velocityX = value;
            if (velocityX > 1)
            {
                velocityX = 1;
            }
            if (velocityX < -1)
            {
                velocityX = -1;
            }
            anim.SetFloat(VelocityXHash, velocityX);
        } 
    }
    public float VelocityZ 
    { 
        get 
        { 
            return velocityZ; 
        } 
        set 
        { 
            velocityZ = value; 
            if (velocityZ > 1)
            {
                velocityZ = 1;
            }
            if (velocityZ < -0.5f)
            {
                velocityZ = -0.5f;
            }
            anim.SetFloat(VelocityZHash, velocityZ);
        } 
    }    

    public bool SameDirection (Direction attackDirection)
    {
        switch (direction)
        {
            case Direction.LEFT:
                if (attackDirection == Direction.RIGHT)
                {
                    return true;
                }
            break;
            case Direction.RIGHT:
                if (attackDirection == Direction.LEFT)
                {
                    return true;
                }
            break;
            default:
                if (attackDirection == Direction.UP || attackDirection == Direction.DOWN)
                {
                    return true;
                }
            break;
        }
        return false;
    }

    public void HitReceived (int hitType, Direction hitDirection)
    {
        if (!isHit)
        {
            if (isBlocking)
            {
                if (SameDirection (hitDirection))
                {
                    return;
                }
            }
            OnHitEnd(); //turn attack state off
            isHit = true;
            VelocityX = 0;
            VelocityZ = 0;
            anim.SetBool(hitHash, true);
            anim.SetInteger(HitTypeHash, hitType);
        }
    }

    public virtual void Start()
    {
        anim = GetComponent<Animator>();
        VelocityXHash = Animator.StringToHash("Velocity X");
        VelocityZHash = Animator.StringToHash("Velocity Z");
        blockingHash = Animator.StringToHash("isBlocking");
        hittingHash = Animator.StringToHash("isHitting");
        AttackDirectionHash = Animator.StringToHash("AttackDirection");
        hitHash = Animator.StringToHash("isHit");
        HitTypeHash = Animator.StringToHash("HitType");
    }

    public virtual void Update()
    {
        
    }

    public Direction GetAttackDirection()
    {
        return direction;
    }

    public bool IsHit()
    {
        return isHit;
    }

    public bool IsBlocking()
    {
        return isBlocking;
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }

    public virtual void MeleeBlock(HumanEntity enemy = null)
    {
        isBlocking = true;
        isAttacking = false;
        thisEntity.DeactivateWeapons();
        switch (enemy.GetAttackDirection())
        {
            case Direction.UP:
                direction = Direction.UP;
                anim.SetInteger(AttackDirectionHash, 0);
                break;
            case Direction.DOWN:
                direction = Direction.DOWN;
                anim.SetInteger(AttackDirectionHash, 2);
                break;
            case Direction.LEFT:
                direction = Direction.RIGHT;
                anim.SetInteger(AttackDirectionHash, 1);
                break;
            case Direction.RIGHT:
                direction = Direction.LEFT;
                anim.SetInteger(AttackDirectionHash, -1);
                break;
        }
        anim.SetBool(blockingHash, true);
    }

    public virtual void StopBlocking()
    {
        isBlocking = false;
        anim.SetBool(blockingHash, false);
    }

    public virtual void MeleeAttack()
    {
        if (isAttacking || isHit)
        {
            return;
        }
        isBlocking = false;
        isAttacking = true;
        thisEntity.ActivateWeapons(direction);
        direction = (Direction)Random.Range(0, 4);
        switch (direction)
        {
            case Direction.UP:
                anim.SetInteger(AttackDirectionHash, 0);
                break;
            case Direction.DOWN:
                anim.SetInteger(AttackDirectionHash, 2);
                break;
            case Direction.LEFT:
                anim.SetInteger(AttackDirectionHash, -1);
                break;
            case Direction.RIGHT:
                anim.SetInteger(AttackDirectionHash, 1);
                break;
        }
        anim.SetTrigger(hittingHash);
    }

    public void OnHitEnd()
    {
        isAttacking = false;
        thisEntity.DeactivateWeapons();
    }

    protected float GetAcceleration (float input)
    {
        if (isCrouching || isBlocking)
            return acceleration / 3;
        return acceleration;
    }

    public virtual void ToggleHit ()
    {
        isHit = false;
    }
}