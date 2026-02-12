using UnityEngine;

public class DonorBobble : MonoBehaviour
{
    public float ZValue = 0f;

    [Header("Random Walk Settings")]
    public float accelerationStrength = 5f;   // How strongly direction changes
    public float maxSpeed = 10f;              // Max drift speed
    public float damping = 0.98f;             // Floatiness (closer to 1 = floatier)

    [Header("XZ Bounds")]
    public Vector2 minBounds = new Vector2(-50f, -50f);
    public Vector2 maxBounds = new Vector2(50f, 50f);
    public float bounciness = 0.85f;
    
    private Vector3 velocity;
    private Vector3 position;

    void Start()
    {
        position = transform.position;
        velocity = Vector3.zero;
    }

    void Update()
    {
        // --- Random acceleration ---
        Vector2 randomDir = Random.insideUnitCircle;
        Vector3 randomAccel = new Vector3(randomDir.x, randomDir.y, 0f)
                              * accelerationStrength * Time.deltaTime;

        velocity += randomAccel;

        // Clamp max speed
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        // Apply damping (floatiness)
        velocity *= damping;

        // Move
        position += velocity * Time.deltaTime;

        // --- Bounce on X ---
        if (position.x < minBounds.x)
        {
            position.x = minBounds.x;
            velocity.x *= -bounciness;
        }
        else if (position.x > maxBounds.x)
        {
            position.x = maxBounds.x;
            velocity.x *= -bounciness;
        }

        // --- Bounce on Y ---
        if (position.y < minBounds.y)
        {
            position.y = minBounds.y;
            velocity.y *= -bounciness;
        }
        else if (position.y > maxBounds.y)
        {
            position.y = maxBounds.y;
            velocity.y *= -bounciness;
        }

        position.z = ZValue;

        transform.position = position;
    }

}
