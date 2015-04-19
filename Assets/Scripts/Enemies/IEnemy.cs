using UnityEngine;
using System.Collections;

public abstract class IEnemy : IPooledObject
{
    [SerializeField]
    string displayName = "Asteroid";
    [SerializeField]
    int maxHealth = 1;
    [SerializeField]
    PooledExplosion explosion = null;

    EnemyCollection collection;
    int currentHealth = 0;

    public string DisplayName
    {
        get
        {
            return displayName;
        }
    }

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
