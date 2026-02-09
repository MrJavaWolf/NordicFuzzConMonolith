using UnityEngine;

public class DonorBobble : MonoBehaviour
{
    public float ZValue = 0f;

    [Header("Wiggle Settings")]
    public float wiggleStrength = 100f;
    public float wiggleSpeed = 0.3f;

    [Header("Drift Settings")]
    public float driftStrength = 1f;
    public float driftSpeed = 0.05f;

    [Header("XZ Bounds")]
    public Vector2 minBounds = new Vector2(-50f, -50f); // Min X, Min Z
    public Vector2 maxBounds = new Vector2(50f, 50f);   // Max X, Max Z

    private Vector3 startPosition;
    private float wiggleXSeed;
    private float wiggleYSeed;
    private float driftXSeed;
    private float driftYSeed;

    void Start()
    {
        startPosition = transform.position;

        wiggleXSeed = Random.Range(0f, 100f);
        wiggleYSeed = Random.Range(0f, 100f);
        driftXSeed = Random.Range(0f, 100f);
        driftYSeed = Random.Range(0f, 100f);
    }

    void Update()
    {
        // --- Slow drifting center ---
        float driftX = Mathf.PerlinNoise(driftXSeed, Time.time * driftSpeed) - 0.5f;
        float driftY = Mathf.PerlinNoise(driftYSeed, Time.time * driftSpeed) - 0.5f;

        Vector3 driftOffset = new Vector3(driftX, driftY, 0f) * driftStrength;
        Vector3 driftCenter = startPosition + driftOffset;

        // --- Local wiggle around the drifting center ---
        float wiggleX = Mathf.PerlinNoise(wiggleXSeed, Time.time * wiggleSpeed) - 0.5f;
        float wiggleY = Mathf.PerlinNoise(wiggleYSeed, Time.time * wiggleSpeed) - 0.5f;

        Vector3 wiggleOffset = new Vector3(wiggleX, wiggleY, 0f) * wiggleStrength;

        Vector3 targetPosition = driftCenter + wiggleOffset;

        // --- Clamp to explicit min/max bounds ---
        targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        targetPosition.z = ZValue;
        transform.position = targetPosition;
    }

    // Optional: visualize bounds in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3(
            (minBounds.x + maxBounds.x) / 2f,
            transform.position.y,
            (minBounds.y + maxBounds.y) / 2f
        );

        Vector3 size = new Vector3(
            maxBounds.x - minBounds.x,
            0.01f,
            maxBounds.y - minBounds.y
        );

        Gizmos.DrawWireCube(center, size);
    }
}
