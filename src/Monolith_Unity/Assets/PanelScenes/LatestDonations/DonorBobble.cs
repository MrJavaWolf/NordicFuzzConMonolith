using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DonorBobble : MonoBehaviour
{

    public string DonationId = string.Empty;
    public float ZValue = 0f;
    public TextMeshProUGUI Text;
    public Image Logo;
    public Image Background;
    public Image Border;
    public List<Image> AdditionalImages;
    public float FadeOutTime = 1f;
    public float FadeInTime = 0.75f;
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
        SetAlpha(0);
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

    public void SetAlpha(float alpha)
    {
        if (Text != null)
        {
            Text.alpha = alpha;
        }

        if (Logo != null)
        {
            var color = Logo.color;
            color.a = alpha;
            Logo.color = color;
        }

        if (Background != null)
        {
            var color = Background.color;
            color.a = alpha;
            Background.color = color;
        }

        if (AdditionalImages != null)
        {
            foreach (var additionalImage in AdditionalImages)
            {
                if (additionalImage != null)
                {
                    var color = additionalImage.color;
                    color.a = alpha;
                    additionalImage.color = color;
                }
            }
        }

        if (Border != null)
        {
            var color = Border.color;
            color.a = alpha;
            Border.color = color;
        }
    }

    public IEnumerator FadeIn()
    {
        float elapsed = 0f;
        float startAlpha = 0;
        float endAlpha = 1f;

        while (elapsed < FadeOutTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / FadeInTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, t);

            SetAlpha(currentAlpha);
            yield return null;
        }
        SetAlpha(endAlpha);
    }

    public IEnumerator FadeOutAndDie()
    {
        float elapsed = 0f;
        float startAlpha = 1;
        float endAlpha = 0f;

        while (elapsed < FadeOutTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / FadeOutTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, t);

            SetAlpha(currentAlpha);
            yield return null;
        }
        SetAlpha(endAlpha);
        Destroy(this.gameObject);
    }
}
