using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class ShipControl : MonoBehaviour
{
    public const string RamField = "ramming";

    public enum FlightMode
    {
        ToTheTarget,
        AwayFromTheTarget
    }

    [SerializeField]
    EnemyCollection targets = null;
    [SerializeField]
    Transform camera = null;

    [SerializeField]
    [Range(0, 1000)]
    float forceTowardsTarget = 100f;
    [SerializeField]
    [Range(0, 2000)]
    float forceRamming = 2000f;
    [SerializeField]
    [Range(0, 50)]
    float forceSidewaysSpeed = 10f;
    [SerializeField]
    [Range(0, 30)]
    float rotateLerp = 1f;

    [SerializeField]
    [ReadOnly]
    bool rammingOn = false;

    Rigidbody bodyCache = null;
    Animator animatorCache = null;
    Vector2 controlInput = Vector2.zero;
    Vector3 targetToShip = Vector3.zero,
        forceCache = Vector3.zero;
    Quaternion currentRotation = Quaternion.identity;
    Quaternion lookRotation = Quaternion.identity;
    FlightMode direction = FlightMode.ToTheTarget;

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

    Animator Animate
    {
        get
        {
            if (animatorCache == null)
            {
                animatorCache = GetComponent<Animator>();
            }
            return animatorCache;
        }
    }

    public bool IsRamming
    {
        get
        {
            return rammingOn;
        }
        private set
        {
            if (rammingOn != value)
            {
                rammingOn = value;
                Animate.SetBool(RamField, rammingOn);
            }
        }
    }

    void Start()
    {
        targets.Setup(this);
    }

	void Update ()
    {
        // Grab controls
        controlInput.x = Input.GetAxis("Horizontal");
        controlInput.y = Input.GetAxis("Vertical");
        IsRamming = Input.GetButton("Fire1");
        if (Input.GetButtonDown("NextTarget") == true)
        {
            targets.NextEnemy();
        }
        else if(Input.GetButtonDown("PreviousTarget") == true)
        {
            targets.PreviousEnemy();
        }

        // Figure out the direction to look at
        targetToShip = (targets.CurrentEnemy.EnemyTransform.position - transform.position);
        targetToShip.Normalize();
        if (direction == FlightMode.AwayFromTheTarget)
        {
            targetToShip *= -1f;
        }
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
        if (IsRamming == true)
        {
            forceCache.x = targetToShip.x * forceRamming;
            forceCache.y = targetToShip.y * forceRamming;
            forceCache.z = targetToShip.z * forceRamming;
        }
        else
        {
            forceCache.x = targetToShip.x * forceTowardsTarget;
            forceCache.y = targetToShip.y * forceTowardsTarget;
            forceCache.z = targetToShip.z * forceTowardsTarget;
        }
        Body.AddForce(forceCache, ForceMode.Force);
    }
}
