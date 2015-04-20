using UnityEngine;
using System.Collections.Generic;

public class Beam : MonoBehaviour
{
    const string BeamField = "beam";

    public static readonly Dictionary<Collider, Beam> colliderMap = new Dictionary<Collider, Beam>();

    [SerializeField]
    [Range(0, 5)]
    int damage = 1;

    bool isShooting = false;
    Rigidbody body = null;
    Animator beamAnimation = null;

    public int Damage
    {
        get
        {
            return damage;
        }
    }

    public bool IsShooting
    {
        get
        {
            return isShooting;
        }
        set
        {
            if (isShooting != value)
            {
                isShooting = value;
                if (beamAnimation == null)
                {
                    beamAnimation = GetComponent<Animator>();
                }
                beamAnimation.SetBool(BeamField, isShooting);
            }
        }
    }

    public Rigidbody Body
    {
        get
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }
            return body;
        }
    }

    void Start()
    {
        Collider[] allCOlliders = GetComponentsInChildren<Collider>();
        foreach(Collider collider in allCOlliders)
        {
            colliderMap.Add(collider, this);
        }
    }
}
