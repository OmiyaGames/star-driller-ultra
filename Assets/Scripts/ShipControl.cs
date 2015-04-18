using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class ShipControl : MonoBehaviour
{
    static ShipControl instance = null;
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
    Collider hitCollider = null;
    [SerializeField]
    Collider ramCollider = null;

    [Header("Movement stats")]
    [SerializeField]
    [Range(0, 1000)]
    float forceTowardsTarget = 100f;
    [SerializeField]
    [Range(0, 100)]
    float forceRamming = 2000f;
    [SerializeField]
    [Range(0, 50)]
    float forceSidewaysSpeed = 10f;
    [SerializeField]
    [Range(0, 30)]
    float rotateLerp = 1f;

    [Header("Conditions")]
    [SerializeField]
    [ReadOnly]
    bool rammingOn = false;
    [SerializeField]
    [Range(0, 5)]
    float reverseFor = 2f;
    [SerializeField]
    [Range(1, 100)]
    int maxHealth = 10;
    [SerializeField]
    [Range(0, 1)]
    float enemyHitDamageRatio = 0.3f;
    [SerializeField]
    float predictiveMultiplierNormal = 10f;
    [SerializeField]
    float predictiveMultiplierRam = 10f;
    [SerializeField]
    [Range(0, 1)]
    float rammingDefenseMultiplier = 0.5f;

    Rigidbody bodyCache = null;
    Animator animatorCache = null;
    Vector2 controlInput = Vector2.zero;
    Vector3 targetToShip = Vector3.zero,
        moveDirection = Vector3.zero,
        forceCache = Vector3.zero;
    Quaternion currentRotation = Quaternion.identity;
    Quaternion lookRotation = Quaternion.identity;
    FlightMode direction = FlightMode.ToTheTarget;
    float timeCollisionStarted = -1f;
    int enemyHitDamage = 1,
        currentHealth = 0;
    
    public static ShipControl Instance
    {
        get
        {
            return instance;
        }
    }

    public static Transform TransformInfo
    {
        get
        {
            return instance.transform;
        }
    }

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
                hitCollider.gameObject.SetActive(rammingOn == false);
                ramCollider.gameObject.SetActive(rammingOn == true);
            }
        }
    }

    public Vector3 TargetToShip
    {
        get
        {
            return targetToShip;
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
            if(currentHealth != newHealth)
            {
                currentHealth = newHealth;

                // FIXME: do something on death!
                Debug.Log(currentHealth);

                if (currentHealth <= 0)
                {
                    Finish();
                }
            }
        }
    }

    void Start()
    {
        instance = this;
        Time.timeScale = 1;
        targets.Setup(this);
        currentHealth = maxHealth;
        enemyHitDamage = Mathf.RoundToInt(maxHealth * enemyHitDamageRatio);
    }

	void Update ()
    {
        // Make sure there are enemies
        if(targets.HasEnemy == false)
        {
            return;
        }

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
        moveDirection = targetToShip;
        if (direction == FlightMode.AwayFromTheTarget)
        {
            moveDirection *= -1f;
            if((Time.time - timeCollisionStarted) > reverseFor)
            {
                timeCollisionStarted = -1f;
                direction = FlightMode.ToTheTarget;
            }
        }
        lookRotation = Quaternion.LookRotation(moveDirection);

        // Update rotation
        //transform.LookAt(targets.CurrentEnemy.EnemyTransform);
    }

    void FixedUpdate()
    {
        // Add rotation
        Body.rotation = Quaternion.Slerp(Body.rotation, lookRotation, (Time.deltaTime * rotateLerp));

        // Add controls force
        forceCache.x = controlInput.x * forceSidewaysSpeed;
        forceCache.y = controlInput.y * forceSidewaysSpeed;
        forceCache.z = 0;
        Body.AddRelativeForce(forceCache, ForceMode.Impulse);

        // Add forward force
        if (IsRamming == true)
        {
            forceCache = moveDirection * forceRamming;
            Body.AddForce(forceCache, ForceMode.Impulse);
        }
        else
        {
            forceCache = moveDirection * forceTowardsTarget;
            Body.AddForce(forceCache, ForceMode.Force);
        }
    }

    void OnCollisionEnter(Collision info)
    {
        EnemyCollection.EnemyInfo enemy;
        PooledBullets bullet;
        if (targets.ColliderMap.TryGetValue(info.collider, out enemy) == true)
        {
            HitEnemy(enemy);
        }
        else if(PooledBullets.colliderMap.TryGetValue(info.collider, out bullet) == true)
        {
            if(IsRamming == true)
            {
                CurrentHealth -= Mathf.RoundToInt(bullet.Damage * rammingDefenseMultiplier);
            }
            else
            {
                CurrentHealth -= bullet.Damage;
            }
            bullet.Die();
        }
    }

    private void HitEnemy(EnemyCollection.EnemyInfo enemy)
    {
        if (IsRamming == true)
        {
            // Inflict damage to enemy
            enemy.EnemyScript.CurrentHealth -= 1;
        }
        else
        {
            // Inflict damage to self
            CurrentHealth -= enemyHitDamage;

            // Fly away from the enemy
            direction = FlightMode.AwayFromTheTarget;
            timeCollisionStarted = Time.time;
        }

        // Grab the next enemy if this one is dead
        if (enemy.EnemyScript.CurrentHealth <= 0)
        {
            if (targets.HasEnemy == true)
            {
                targets.NextEnemy();
            }
            else
            {
                Finish();
            }
        }
    }

    void Finish()
    {
        Time.timeScale = 0.1f;
    }
}
