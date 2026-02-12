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

    private bool IsDoingFinalFall = false;


    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        AddInitialForce();
        lastPosition = rb.position;
    }


    void FixedUpdate()
    {
        if (IsDoingFinalFall)
        {
            return;
        }

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

    public void StartFinalFall()
    {
        IsDoingFinalFall = true;
        DisableAllChildBoxColliders();
        MakeJump();
        rb.totalTorque = 0f;
    }

    public void MakeJump()
    {
        if (rb.bodyType != RigidbodyType2D.Dynamic)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        stillTime = 0;
        AddInitialForce();
    }

    private void AddInitialForce()
    {
        float initialXForce = UnityEngine.Random.Range(-initialXForceMax, initialXForceMax);
        rb.AddForceX(initialXForce);
        rb.AddForceY(initialYForce);
        int rotationDirection = initialXForce > 0 ? -1 : 1;
        rb.AddTorque(initialTorque * rotationDirection);
    }

    void DisableAllChildBoxColliders()
    {
        // Get all BoxCollider2D components in children, including inactive objects
        BoxCollider2D[] colliders = transform.GetComponentsInChildren<BoxCollider2D>(true);

        foreach (BoxCollider2D col in colliders)
        {
            col.enabled = false;
        }
    }

    void EnableAllChildBoxColliders()
    {
        BoxCollider2D[] colliders = transform.GetComponentsInChildren<BoxCollider2D>(true);

        foreach (BoxCollider2D col in colliders)
        {
            col.enabled = true;
        }
    }
}
