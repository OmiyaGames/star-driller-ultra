using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class ShipControl : MonoBehaviour
{
    static ShipControl instance = null;
    public const string RamField = "ramming";
    public const string HorizontalField = "horizontal";
    public const string VerticalField = "vertical";
    public const string FlightTowardsTarget = "Forward";
    public const string FlightAwayFromTarget = "Reverse";

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
    [Range(0, 5)]
    float invincibleFor = 2f;
    [SerializeField]
    [Range(1, 100)]
    int maxHealth = 10;
    [SerializeField]
    [Range(1, 50)]
    int displayDangerBelow = 3;
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

    [Header("Drill Stats")]
    [SerializeField]
    float drillMax = 10f;
    [SerializeField]
    float drillDepletionRate = 1f;
    [SerializeField]
    float drillCooldownSmall = 1f;
    [SerializeField]
    float drillCooldownLong = 3f;
    [SerializeField]
    float drillRecoverRate = 1f;

    [Header("Menus")]
    [SerializeField]
    Text flightModeLabel;
    [SerializeField]
    Slider lifeBar;
    [SerializeField]
    Slider drillBar;
    [SerializeField]
    Text emptyDrill;
    [SerializeField]
    Text dangerHealth;

    [Header("Target")]
    [SerializeField]
    GameObject targetReticle = null;
    [SerializeField]
    Text distanceLabel = null;

    [Header("Target")]
    [SerializeField]
    AudioSource jetSound = null;
    [SerializeField]
    AudioSource hitSound = null;
    [SerializeField]
    AudioSource emptySound = null;

    Rigidbody bodyCache = null;
    Animator animatorCache = null;
    Vector2 controlInput = Vector2.zero;
    Vector3 targetToShip = Vector3.zero,
        moveDirection = Vector3.zero,
        forceCache = Vector3.zero;
    Quaternion currentRotation = Quaternion.identity;
    Quaternion lookRotation = Quaternion.identity;
    FlightMode direction = FlightMode.ToTheTarget;
    float timeCollisionStarted = -1f,
        timeInvincible = -1f,
        drillCurrent = 0,
        timeLastDrilled = 0;
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
                if(rammingOn == true)
                {
                    jetSound.Play();
                }
                else
                {
                    jetSound.Stop();
                }
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
            // Check invincibility frames
            if ((Time.time - timeInvincible) < invincibleFor)
            {
                return;
            }

            // Update health
            int newHealth = Mathf.Clamp(value, 0, maxHealth);
            if(currentHealth != newHealth)
            {
                // If decreasing for health, flag for invincibility
                if(newHealth < currentHealth)
                {
                    timeInvincible = Time.time;
                    hitSound.Play();
                }

                // Setup health
                currentHealth = newHealth;

                // Setup UI
                lifeBar.value = currentHealth;
                dangerHealth.enabled = (currentHealth <= displayDangerBelow);

                // Check for death
                if (currentHealth <= 0)
                {
                    // FIXME: do something on death!
                    Finish();
                }
            }
        }
    }

    public FlightMode FlightDirection
    {
        get
        {
            return direction;
        }
        set
        {
            if(direction != value)
            {
                direction = value;
                if(direction == FlightMode.ToTheTarget)
                {
                    flightModeLabel.text = FlightTowardsTarget;
                }
                else
                {
                    flightModeLabel.text = FlightAwayFromTarget;
                }
            }
        }
    }

    float CurrentDrill
    {
        get
        {
            return drillCurrent;
        }
        set
        {
            drillCurrent = value;
            emptyDrill.enabled = (drillCurrent < 0);
            drillBar.value = Mathf.Clamp(value, 0, drillMax);
            if((drillCurrent < 0) && (emptySound.isPlaying == false))
            {
                emptySound.Play();
            }
        }
    }

    void Start()
    {
        instance = this;
        Time.timeScale = 1;

        // Setup targets
        targets.Setup(this);

        // Setup stats
        currentHealth = maxHealth;
        enemyHitDamage = Mathf.RoundToInt(maxHealth * enemyHitDamageRatio);
        drillCurrent = drillMax;
        timeLastDrilled = -1;
        timeCollisionStarted = -1f;
        timeInvincible = -1f;

        // Setup UI
        lifeBar.wholeNumbers = true;
        lifeBar.minValue = 0;
        lifeBar.maxValue = maxHealth;
        lifeBar.value = currentHealth;

        drillBar.wholeNumbers = false;
        drillBar.minValue = 0;
        drillBar.maxValue = drillMax;
        drillBar.value = currentHealth;

        flightModeLabel.text = FlightTowardsTarget;
        
        dangerHealth.enabled = false;
        emptyDrill.enabled = false;
    }

	void Update ()
    {
        // Make sure there are enemies
        if(targets.HasEnemy == false)
        {
            targetReticle.SetActive(false);
            return;
        }

        // Position the target reticle
        targetReticle.SetActive(true);
        targetReticle.transform.position = targets.CurrentEnemy.EnemyTransform.position;
        targetReticle.transform.rotation = camera.rotation;
        distanceLabel.text = Vector3.Distance(transform.position, targets.CurrentEnemy.EnemyTransform.position).ToString("0.0");

        // Grab controls
        controlInput.x = Input.GetAxis("Horizontal");
        controlInput.y = Input.GetAxis("Vertical");
        IsRamming = CheckIfRamming();
        Animate.SetFloat(HorizontalField, controlInput.x);
        Animate.SetFloat(VerticalField, controlInput.y);
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
        if (FlightDirection == FlightMode.AwayFromTheTarget)
        {
            moveDirection *= -1f;
            if((Time.time - timeCollisionStarted) > reverseFor)
            {
                timeCollisionStarted = -1f;
                FlightDirection = FlightMode.ToTheTarget;
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
            FlightDirection = FlightMode.AwayFromTheTarget;
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

    bool CheckIfRamming()
    {
        bool returnFlag = false;

        // Check if we have the button down
        if(Input.GetButton("Fire1") == true)
        {
            // Check if we have any drill left
            if(CurrentDrill > 0)
            {
                // Indicate we are ramming
                returnFlag = true;

                // Decrement drilling
                CurrentDrill -= (Time.deltaTime * drillDepletionRate);
            }

            // Keep track of when we were drilling
            timeLastDrilled = Time.time;
        }
        else if (timeLastDrilled > 0)
        {
            if (CurrentDrill > 0)
            {
                // Check if we need to recover drilling
                RecoverDrill(drillCooldownSmall);
            }
            else
            {
                // Check if we need to recover drilling
                RecoverDrill(drillCooldownLong);
            }
        }
        return returnFlag;
    }

    private void RecoverDrill(float cooldown)
    {
        if ((Time.time - timeLastDrilled) > cooldown)
        {
            // Increment drill
            CurrentDrill += (Time.deltaTime * drillRecoverRate);

            // Make sure it doesn't exceed max
            if (CurrentDrill > drillMax)
            {
                CurrentDrill = drillMax;
                timeLastDrilled = -1f;
            }
        }
    }
}
