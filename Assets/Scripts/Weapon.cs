using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType { FISTS, CUTTING, PIERCING, BLUNT }

public class Weapon : MonoBehaviour
{
    public Direction direction;
    public WeaponType type;
    public int damage;
    public Collider collider;
    public float attackDistance = 0.75f;

    public void Activate (Direction _direction)
    {
        collider.enabled = true;
        direction = _direction;
    }

    public void Deactivate ()
    {
        collider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<BodyPart>() != null)
        {
            other.GetComponent<BodyPart>().GetDamage(this, direction);
        }
    }
}
