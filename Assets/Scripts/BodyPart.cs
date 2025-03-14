using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Part { HEAD, BODY, ARM, LEG }

public class BodyPart : MonoBehaviour
{
    public Part part;
    public float damageMultiplier = 1;
    public HumanEntity parent;

    public void GetDamage (Weapon weapon, Direction direction)
    {
        if (!parent.HasWeapon (weapon))
        {
            parent.GetDamage(weapon.damage * damageMultiplier, part, direction);
        }
    }
}
