using UnityEngine;
using System.Collections;

public class IEnemy : IPooledObject
{
    [SerializeField]
    int maxHealth = 1;
    [SerializeField]
    PooledExplosion explosion = null;

    EnemyCollection collection;
    int currentHealth = 0;

    public int CurrentHealth
    {
        get
        {
            return currentHealth;
        }
        set
        {
            int newHealth = Mathf.Clamp(value, 0, maxHealth);
            if (currentHealth != newHealth)
            {
                currentHealth = newHealth;
                if(currentHealth <= 0)
                {
                    Die();
                }
            }
        }
    }

    public EnemyCollection Collection
    {
        get
        {
            return collection;
        }
        set
        {
            collection = value;
        }
    }

	// Use this for initialization
	public override void Start ()
    {
        CurrentHealth = maxHealth;
	}

    protected virtual void Die()
    {
        if(explosion != null)
        {
            // Create an explosion here
            Singleton.Get<PoolingManager>().GetInstance(explosion.gameObject, transform.position, transform.rotation);
        }
        if (Collection != null)
        {
            Collection.RemoveEnemy(this);
        }
        gameObject.SetActive(false);
    }

    public bool MatchEnemy(EnemyCollection.EnemyInfo obj)
    {
        return (obj.EnemyScript == this);
    }
}
