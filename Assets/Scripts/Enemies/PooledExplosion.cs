using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
public class PooledExplosion : IPooledObject
{
    [SerializeField]
    float dieAfter = 5f;

    ParticleSystem cache = null;

	// Use this for initialization
	public override void Start ()
    {
        if(cache == null)
        {
            cache = GetComponent<ParticleSystem>();
        }
        cache.Play();
        StartCoroutine(Die());
	}

    IEnumerator Die()
    {
        yield return new WaitForSeconds(dieAfter);
        gameObject.SetActive(false);
    }
}
