﻿using UnityEngine;
using System.Collections;

public class Shooter : IEnemy
{
    enum State
    {
        Aiming,
        Charging,
        Cooldown
    }

    [SerializeField]
    PooledBullets bullets = null;
    [SerializeField]
    Transform[] spawnPoints = null;
    [SerializeField]
    [Range(0, 5)]
    float shootEveryMin = 1f;
    [SerializeField]
    [Range(0, 5)]
    float shootEveryMax = 2f;
    [SerializeField]
#if UNITY_EDITOR
    [ReadOnly]
#endif
    float shootEvery = 0f;
    [SerializeField]
    [Range(0, 2)]
    float charge = 0.5f;
    [SerializeField]
    [Range(0, 2)]
    float cooldown = 0.5f;
    [SerializeField]
    [Range(0, 10)]
    float rotateLerp = 5f;

    State state = State.Aiming;
    float lastShot = 0f;
    Quaternion lookRotation;
    ParticleSystem[] allParticles = null;
    AudioMutator audio = null;

    public override void Start()
    {
        base.Start();
        state = State.Aiming;
        lastShot = 0f;

        shootEvery = Random.RandomRange(shootEveryMin, shootEveryMax);

        if(allParticles == null)
        {
            allParticles = GetComponentsInChildren<ParticleSystem>();
        }
        if(audio == null)
        {
            audio = GetComponent<AudioMutator>();
        }
    }

	// Update is called once per frame
	protected void Update ()
    {
        switch(state)
        {
            case State.Charging:
                if((Time.time - lastShot) > charge)
                {
                    foreach(Transform spawnAt in spawnPoints)
                    {
                        GameObject instance = Singleton.Get<PoolingManager>().GetInstance(bullets.gameObject, spawnAt.position, transform.rotation);
                        PooledBullets bullet = instance.GetComponent<PooledBullets>();
                        bullet.Rotation = transform.rotation;
                    }
                    foreach (ParticleSystem system in allParticles)
                    {
                        system.Play();
                    }
                    audio.Play();
                    lastShot = Time.time;
                    state = State.Cooldown;
                }
                break;
            case State.Cooldown:
                if ((Time.time - lastShot) > cooldown)
                {
                    lastShot = Time.time;
                    state = State.Aiming;
                }
                break;
            case State.Aiming:
            default:
                lookRotation = Quaternion.LookRotation(ShipControl.TransformInfo.position - transform.position);
                if ((Time.time - lastShot) > shootEvery)
                {
                    lastShot = Time.time;
                    state = State.Charging;
                }
                break;
        }
        UpdateRotation();
	}

    protected virtual void UpdateRotation()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, (Time.deltaTime * rotateLerp));
    }
}
