using System;
using Unity.Mathematics;
using UnityEngine;

public class TotalAmountCoin : MonoBehaviour
{

    public float initialXForceMax = 10000;
    public float initialYForce = 1000;
    public float initialTorque = 500f;

    public float movementThreshold = 0.01f; // world units
    public float settleTime = 0.5f;          // seconds

    private Rigidbody2D rb;
    private Vector2 lastPosition;
    private float stillTime;


    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        float initialXForce = UnityEngine.Random.Range(-initialXForceMax, initialXForceMax);
        rb.AddForceX(initialXForce);
        rb.AddForceY(initialYForce);
        int rotationDirection = initialXForce > 0 ? -1 : 1;
        rb.AddTorque(initialTorque * rotationDirection);
        lastPosition = rb.position;
    }


    void FixedUpdate()
    {
        if (rb.bodyType != RigidbodyType2D.Dynamic)
            return;

        Vector2 currentPosition = rb.position;
        float sqrMoved = (currentPosition - lastPosition).sqrMagnitude;

        if (sqrMoved < movementThreshold * movementThreshold)
        {
            stillTime += Time.fixedDeltaTime;

            if (stillTime >= settleTime)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Static;
            }
        }
        else
        {
            stillTime = 0f;
        }

        lastPosition = currentPosition;
    }
}
