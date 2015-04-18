using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PooledBullets : IPooledObject
{
    public static readonly Dictionary<Collider, PooledBullets> colliderMap = new Dictionary<Collider, PooledBullets>();

    [SerializeField]
    [Range(1, 10)]
    int damage = 2;
    [SerializeField]
    [Range(1, 100)]
    float force = 50f;
    [SerializeField]
    [Range(1, 20)]
    float dieAfter = 5f;

    Rigidbody bodyCache = null;
    float timeStart = 0f;
    Vector3 forceDirection = Vector3.forward;
    bool inDictionary = false;

    public int Damage
    {
        get
        {
            return damage;
        }
    }

    public Rigidbody Body
    {
        get
        {
            if (bodyCache == null)
            {
                bodyCache = GetComponent<Rigidbody>();
            }
            return bodyCache;
        }
    }

    public Quaternion Rotation
    {
        set
        {
            transform.rotation = value;
        }
    }

    // Use this for initialization
    public override void Start()
    {
        base.Start();
        forceDirection = transform.rotation * (new Vector3(0, 0, force));
        timeStart = Time.time;

        if(inDictionary == false)
        {
            Collider[] allColliders = GetComponentsInChildren<Collider>();
            foreach(Collider collider in allColliders)
            {
                colliderMap.Add(collider, this);
            }
            inDictionary = true;
        }
    }

    void Update()
    {
        Body.AddForce(forceDirection, ForceMode.Impulse);
        if ((Time.time - timeStart) > dieAfter)
        {
            Die();
        }
    }

    public void Die()
    {
        gameObject.SetActive(false);
    }
}
