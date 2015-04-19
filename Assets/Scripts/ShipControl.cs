using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class ShipControl : MonoBehaviour
{
    static ShipControl instance = null;
    public const string RamField = "ramming";
    public const string HorizontalField = "horizontal";
    public const string VerticalField = "vertical";
    public const string HitTrigger = "hit";
    public const string KilledTrigger = "kill";
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
    [Range(0, 20)]
    int enemyHitDamage = 5;
    [SerializeField]
    float predictiveMultiplierNormal = 10f;
    [SerializeField]
    float predictiveMultiplierRam = 10f;
    [SerializeField]
    [Range(0, 1)]
    float rammingDefenseMultiplier = 0.5f;

    [Header("Drill Stats")]
    [SerializeField]
    [Range(0, 10)]
    float drillMax = 10f;
    [SerializeField]
    [Range(0, 3)]
    float drillDepletionRate = 1f;
    [SerializeField]
    [Range(0, 3)]
    float drillCooldownSmall = 1f;
    [SerializeField]
    [Range(0, 3)]
    float drillCooldownLong = 3f;
    [SerializeField]
    [Range(0, 3)]
    float drillRecoverRate = 1f;
    [SerializeField]
    [Range(0, 0.5f)]
    float pauseKillLength = 0.1f;
    [SerializeField]
    [Range(0, 0.5f)]
    float pauseHurtLength = 0.05f;

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
    [SerializeField]
    LevelCompleteMenu completeMenu;
    [SerializeField]
    LevelFailedMenu deadMenu;

    [Header("Target")]
    [SerializeField]
    GameObject targetReticle = null;
    [SerializeField]
    Text distanceLabel = null;
    [SerializeField]
    Text enemyNameLabel = null;
    [SerializeField]
    Text enemyNumbersLabel = null;

    [Header("Sound")]
    [SerializeField]
    AudioMutator jetSound = null;
    [SerializeField]
    AudioMutator hitSound = null;
    [SerializeField]
    AudioMutator emptySound = null;
    [SerializeField]
    AudioMutator refillSound = null;

    [Header("Particles")]
    [SerializeField]
    ParticleSystem ramParticles = null;
    [SerializeField]
    Animator cameraAnimation = null;
    [SerializeField]
    PooledExplosion hitExplosion = null;

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
        timeLastDrilled = 0,
        pauseStartedRealTime = -1f,
        pauseFor = 1f;
    int currentHealth = 0;
    
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
                cameraAnimation.SetBool(RamField, rammingOn);
                hitCollider.gameObject.SetActive(rammingOn == false);
                ramCollider.gameObject.SetActive(rammingOn == true);
                if(rammingOn == true)
                {
                    jetSound.Play();
                    ramParticles.Play();
                }
                else
                {
                    jetSound.Stop();
                    ramParticles.Stop();
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
                    cameraAnimation.SetTrigger(HitTrigger);
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
                    Finish(false);
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
            if(drillCurrent < 0)
            {
                if (emptySound.Audio.isPlaying == false)
                {
                    emptySound.Play();
                }
            }
        }
    }

    void Start()
    {
        instance = this;
        Time.timeScale = 0;

        // Setup targets
        targets.Setup(this);

        // Setup stats
        currentHealth = maxHealth;
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
        ramParticles.Stop();
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
        enemyNameLabel.text = targets.CurrentEnemy.EnemyScript.DisplayName;
        enemyNumbersLabel.text = targets.AllEnemies.Count.ToString();

        // Grab controls
        controlInput.x = CrossPlatformInputManager.GetAxis("Horizontal");
        controlInput.y = CrossPlatformInputManager.GetAxis("Vertical");
        IsRamming = CheckIfRamming();
        Animate.SetFloat(HorizontalField, controlInput.x);
        Animate.SetFloat(VerticalField, controlInput.y);
        if ((CrossPlatformInputManager.GetButtonDown("NextTarget") == true) || (CrossPlatformInputManager.GetAxis("Xbox360ControllerTriggers") > 0.5f))
        {
            targets.NextEnemy();
        }
        else if ((CrossPlatformInputManager.GetButtonDown("PreviousTarget") == true) || (CrossPlatformInputManager.GetAxis("Xbox360ControllerTriggers") < -0.5f))
        {
            targets.PreviousEnemy();
        }
        if(CrossPlatformInputManager.GetButtonUp("Pause") == true)
        {
            Singleton.Get<PauseMenu>().Show();
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

        if((pauseStartedRealTime > 0) && ((Time.unscaledTime - pauseStartedRealTime) > pauseFor))
        {
            Time.timeScale = 1;
            pauseStartedRealTime = -1f;
        }
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
            Singleton.Get<PoolingManager>().GetInstance(hitExplosion.gameObject, info.contacts[0].point, Quaternion.identity);
        }
        else if(PooledBullets.colliderMap.TryGetValue(info.collider, out bullet) == true)
        {
            if(IsRamming == false)
            {
                CurrentHealth -= bullet.Damage;

                // Pause for a short bit
                Pause(pauseHurtLength);
            }
            bullet.Die();
            Singleton.Get<PoolingManager>().GetInstance(hitExplosion.gameObject, info.contacts[0].point, Quaternion.identity);
        }
    }

    private void HitEnemy(EnemyCollection.EnemyInfo enemy)
    {
        if (IsRamming == true)
        {
            // Inflict damage to enemy
            enemy.EnemyScript.CurrentHealth -= 1;
            cameraAnimation.SetTrigger(KilledTrigger);

            // Pause for a short bit
            Pause(pauseKillLength);
        }
        else
        {
            // Fly away from the enemy
            FlightDirection = FlightMode.AwayFromTheTarget;
            timeCollisionStarted = Time.time;

            // Pause for a short bit
            Pause(pauseHurtLength);

            // Inflict damage to self
            CurrentHealth -= enemyHitDamage;
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
                Finish(true);
            }
        }
    }

    void Finish(bool complete)
    {
        if(complete == true)
        {
            completeMenu.Show();

            // Unlock the next level
            GameSettings settings = Singleton.Get<GameSettings>();
            if(Application.loadedLevel >= settings.NumLevelsUnlocked)
            {
                settings.NumLevelsUnlocked = Mathf.Clamp((Application.loadedLevel + 1), 1, settings.NumLevels);
            }
        }
        else
        {
            deadMenu.Show();
        }

        pauseStartedRealTime = -1f;

        jetSound.Stop();
        enabled = false;
        Time.timeScale = 0.1f;
    }

    bool CheckIfRamming()
    {
        bool returnFlag = false;

        // Check if we have the button down
        if (CrossPlatformInputManager.GetButton("Drill") == true)
        {
            // Check if we have any drill left
            if(CurrentDrill > 0)
            {
                // Indicate we are ramming
                returnFlag = true;

                // Decrement drilling
                CurrentDrill -= (Time.deltaTime * drillDepletionRate);
            }
            else if (emptySound.Audio.isPlaying == false)
            {
                emptySound.Play();
            }

            // Keep track of when we were drilling
            timeLastDrilled = Time.time;
        }
        else if (timeLastDrilled > 0)
        {
            if (CurrentDrill > 0)
            {
                // Check if we need to recover drilling
                RecoverDrill(drillCooldownSmall, false);
            }
            else
            {
                // Check if we need to recover drilling
                RecoverDrill(drillCooldownLong, true);
            }
        }
        return returnFlag;
    }

    private void RecoverDrill(float cooldown, bool playSound)
    {
        if ((Time.time - timeLastDrilled) > cooldown)
        {
            if ((playSound == true) && (refillSound.Audio.isPlaying == false))
            {
                // Play the refill sound
                refillSound.Play();
            }

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

    private void Pause(float length)
    {
        pauseFor = length;

        // Pause for a short bit
        Time.timeScale = 0;
        pauseStartedRealTime = Time.unscaledTime;
    }
}
