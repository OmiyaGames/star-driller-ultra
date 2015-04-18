using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PooledBullets : IPooledObject
{
    [SerializeField]
    [Range(1, 100)]
    float force = 50f;
    [SerializeField]
    [Range(1, 20)]
    float dieAfter = 5f;

    Rigidbody bodyCache = null;
    float timeStart = 0f;
    Vector3 forceDirection = Vector3.forward;

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
