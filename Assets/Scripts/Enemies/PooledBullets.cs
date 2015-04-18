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
    [SerializeField]
    [Range(0, 20)]
    float homeFor = 0.5f;
    [SerializeField]
    [Range(0, 20)]
    float homingLerp = 5f;

    Rigidbody bodyCache = null;
    float timeStart = 0f;
    Vector3 forceDirection = Vector3.forward,
        forceVector;
    Quaternion targetLookTo = Quaternion.identity,
        currentLookTo = Quaternion.identity;
    bool inDictionary = false,
        homing = true;

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

        forceDirection = new Vector3(0, 0, force);
        timeStart = Time.time;
        homing = true;

        if(inDictionary == false)
        {
            Collider[] allColliders = GetComponentsInChildren<Collider>();
            foreach(Collider collider in allColliders)
            {
                colliderMap.Add(collider, this);
            }
            inDictionary = true;
        }

        // Add some force
        currentLookTo = transform.rotation;
    }

    void Update()
    {
        float duration = (Time.time - timeStart);
        if (duration > dieAfter)
        {
            Die();
        }
        else if ((homing == true) && (duration > homeFor))
        {
            homing = false;
        }

        if(homing == true)
        {
            targetLookTo = Quaternion.LookRotation(ShipControl.TransformInfo.position - transform.position);
        }
        currentLookTo = Quaternion.Lerp(currentLookTo, targetLookTo, (Time.deltaTime * homingLerp));
    }

    void FixedUpdate()
    {
        Body.rotation = currentLookTo;
        Body.AddRelativeForce(forceDirection, ForceMode.Acceleration);
    }

    public void Die()
    {
        gameObject.SetActive(false);
    }
}
