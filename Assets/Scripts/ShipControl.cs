using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class ShipControl : MonoBehaviour
{
    [SerializeField]
    Transform target = null;
    [SerializeField]
    Transform camera = null;

    [SerializeField]
    [Range(0, 100)]
    float moveTowardsTarget = 10f;
    [SerializeField]
    [Range(0, 100)]
    float moveSidewaysSpeed = 10f;
    [SerializeField]
    [Range(0, 30)]
    float rotateLerp = 1f;

    Rigidbody bodyCache = null;
    Vector2 controlInput = Vector2.zero;
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
        lookRotation = Quaternion.LookRotation((target.position - transform.position), camera.up);

        // Update rotation
        Body.rotation = Quaternion.Lerp(Body.rotation, lookRotation, (Time.deltaTime * rotateLerp));
    }

    void FixedUpdate()
    {
        // Check if we're doing any controls
        if((Mathf.Approximately(controlInput.x, 0) == false) || (Mathf.Approximately(controlInput.y, 0) == false))
        {
            // Apply velocity
            Body.velocity = lookRotation * (controlInput * moveSidewaysSpeed);
        }
    }
}
