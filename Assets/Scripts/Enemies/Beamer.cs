using UnityEngine;
using System.Collections;

public class Beamer : IEnemy
{
    const string BeamField = "beam";

    [SerializeField]
    Beam beam = null;
    [SerializeField]
    [Range(0, 10)]
    float shootEveryMin = 1f;
    [SerializeField]
    [Range(0, 10)]
    float shootEveryMax = 2f;
    [SerializeField]
#if UNITY_EDITOR
    [ReadOnly]
#endif
    float shootEvery = 0f;
    [SerializeField]
    [Range(0, 10)]
    float fireMin = 1f;
    [SerializeField]
    [Range(0, 10)]
    float fireMax = 2f;
    [SerializeField]
#if UNITY_EDITOR
    [ReadOnly]
#endif
    float fireDuration = 0f;
    [SerializeField]
    [Range(0, 3)]
    float rotateLerp = 1f;

    float lastShot = 0f;
    Quaternion lookRotation;
    AudioMutator audio = null;
    Rigidbody body = null;

    public override void Start()
    {
        base.Start();
        beam.IsShooting = false;
        lastShot = 0f;

        shootEvery = Random.RandomRange(shootEveryMin, shootEveryMax);
        fireDuration = Random.RandomRange(fireMin, fireMax);

        if(audio == null)
        {
            audio = GetComponent<AudioMutator>();
        }
        if(body == null)
        {
            body = GetComponent<Rigidbody>();
        }
    }

	// Update is called once per frame
	protected void Update ()
    {
        if (beam.IsShooting == true)
        {
            // Check if we're done
            if((Time.time - lastShot) > fireDuration)
            {
                beam.IsShooting = false;
                lastShot = Time.time;
                audio.Stop();
            }
        }
        else if ((Time.time - lastShot) > shootEvery)
        {
            lastShot = Time.time;
            beam.IsShooting = true;
            audio.Play();
        }

        lookRotation = Quaternion.LookRotation(ShipControl.TransformInfo.position - transform.position);
        body.rotation = Quaternion.Slerp(body.rotation, lookRotation, (Time.deltaTime * rotateLerp));
        beam.Body.rotation = body.rotation;
	}

    protected override void Die()
    {
        base.Die();
        beam.gameObject.SetActive(false);
    }
}
