using UnityEngine;
using System.Collections;

public class PooledExplosion : IPooledObject
{
    [SerializeField]
    float dieAfter = 5f;

    ParticleSystem[] particlesCache = null;
    AudioMutator soundCache = null;

	// Use this for initialization
	public override void Start ()
    {
        if (particlesCache == null)
        {
            particlesCache = GetComponentsInChildren<ParticleSystem>();
        }
        if (soundCache == null)
        {
            soundCache = GetComponent<AudioMutator>();
        }
        soundCache.Play();
        foreach(ParticleSystem system in particlesCache)
        {
            system.Play();
        }
        StartCoroutine(Die());
	}

    IEnumerator Die()
    {
        yield return new WaitForSeconds(dieAfter);
        soundCache.Stop();
        foreach (ParticleSystem system in particlesCache)
        {
            system.Stop();
        }
        gameObject.SetActive(false);
    }
}
