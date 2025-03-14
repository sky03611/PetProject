using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanEntity : MonoBehaviour
{
    [SerializeField] private int health, maxHealth;
    [SerializeField] private List<Weapon> weapons;

    public int Health
    {
        get
        {
            return health;
        }
        set
        {
            health = value;
            if (health > maxHealth)
            {
                health = maxHealth;
            }
            if (health < 0)
            {
                health = 0;
            }
        }
    }

    public List<BodyPart> bodyParts;
    [SerializeField] private AnimationStateController animator;

    public bool IsAttacking()
    {
        return animator.IsAttacking();
    }

    public float GetAttackDistance ()
    {
        return weapons[0].attackDistance;
    }

    public Direction GetAttackDirection()
    {
        return animator.GetAttackDirection();
    }

    public bool HasWeapon(Weapon _weapon)
    {
        return weapons.Contains(_weapon);
    }

    public void ActivateWeapons(Direction direction)
    {
        foreach (var w in weapons)
        {
            w.Activate(direction);
        }
    }

    public void DeactivateWeapons()
    {
        foreach (var w in weapons)
        {
            w.Deactivate();
        }
    }

    public void GetDamage (float damage, Part part, Direction _direction)
    {
        Health -= Mathf.FloorToInt(damage);
        switch (part)
        {
            case Part.HEAD:
                animator.HitReceived(1, _direction);
                break;
            default:
                animator.HitReceived(0, _direction);
                break;
        }
    }
}
