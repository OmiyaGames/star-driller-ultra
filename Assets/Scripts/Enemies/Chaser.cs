using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Chaser : Shooter
{
    [Header("Chaser stats")]
    [SerializeField]
    [Range(1, 20)]
    float force = 50f;
    [SerializeField]
    [Range(0, 20)]
    float homingLerp = 5f;

    Rigidbody bodyCache = null;
    Vector3 forceDirection = Vector3.forward,
        forceVector;
    Quaternion targetLookTo = Quaternion.identity,
        currentLookTo = Quaternion.identity;

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

    public override void Start()
    {
        base.Start();
        forceDirection = new Vector3(0, 0, force);

        // Add some force
        currentLookTo = transform.rotation;
    }

    protected override void UpdateRotation()
    {
        targetLookTo = Quaternion.LookRotation(ShipControl.TransformInfo.position - transform.position);
        currentLookTo = Quaternion.Lerp(currentLookTo, targetLookTo, (Time.deltaTime * homingLerp));
    }

    protected void FixedUpdate()
    {
        Body.rotation = currentLookTo;
        Body.AddRelativeForce(forceDirection, ForceMode.Impulse);
    }
}
