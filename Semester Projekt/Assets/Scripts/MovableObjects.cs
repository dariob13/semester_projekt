using UnityEngine;

public class MovableObject : MonoBehaviour
{
    [Header("Movable Settings")]
    public bool canBeMoved = true;
    public float weightMultiplier = 1f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // Heavier objects are harder to push
        rb.mass = weightMultiplier;
        rb.gravityScale = 1f;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void OnPushed(Vector2 direction, float strength)
    {
        // Flash briefly when pushed
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(originalColor, Color.white, 0.3f);
            Invoke(nameof(ResetColor), 0.1f);
        }
    }

    void ResetColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}