using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class ShipControl : MonoBehaviour
{
    public enum FlightMode
    {
        ToTheTarget,
        AwayFromTheTarget
    }

    [SerializeField]
    Transform target = null;
    [SerializeField]
    Transform camera = null;

    [SerializeField]
    [Range(0, 1000)]
    float forceTowardsTarget = 100f;
    [SerializeField]
    [Range(0, 20)]
    float forceSidewaysSpeed = 10f;
    [SerializeField]
    [Range(0, 30)]
    float rotateLerp = 1f;

    Rigidbody bodyCache = null;
    Vector2 controlInput = Vector2.zero;
    Vector3 targetToShip = Vector3.zero,
        forceCache = Vector3.zero;
    Quaternion currentRotation = Quaternion.identity;
    Quaternion lookRotation = Quaternion.identity;

    Rigidbody Body
    {
        get
        {
            if(bodyCache == null)
            {
                bodyCache = GetComponent<Rigidbody>();
            }
            return bodyCache;
        }
    }

	void Update ()
    {
        // Grab controls
        controlInput.x = Input.GetAxis("Horizontal");
        controlInput.y = Input.GetAxis("Vertical");

        // Figure out the direction to look at
        targetToShip = (target.position - transform.position);
        targetToShip.Normalize();
        lookRotation = Quaternion.LookRotation(targetToShip, camera.up);

        // Update rotation
        Body.rotation = Quaternion.Lerp(Body.rotation, lookRotation, (Time.deltaTime * rotateLerp));
    }

    void FixedUpdate()
    {
        // Add controls force
        forceCache.x = controlInput.x * forceSidewaysSpeed;
        forceCache.y = controlInput.y * forceSidewaysSpeed;
        forceCache.z = 0;
        Body.AddRelativeForce(forceCache, ForceMode.Impulse);

        // Add forward force
        forceCache.x = 0;
        forceCache.y = 0;
        forceCache.z = forceTowardsTarget;
        Body.AddRelativeForce(forceCache, ForceMode.Force);
    }
}
