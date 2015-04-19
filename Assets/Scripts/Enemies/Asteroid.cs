using UnityEngine;
using System.Collections;

public class Asteroid : IEnemy
{
    [SerializeField]
    int recoverHealth = 5;

    protected override void Die()
    {
        base.Die();
        ShipControl.Instance.CurrentHealth += recoverHealth;
    }
}
