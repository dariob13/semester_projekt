using UnityEngine;

public class LiquidParticle : MonoBehaviour
{
    public Vector2 velocity;
    public Vector2 force;
    public float mass = 1f;
    public float damping = 0.99f;

    private SpriteRenderer spriteRenderer;
    private LiquidSimulation simulation;
    private CircleCollider2D circleCollider;

    public void Initialize(LiquidSimulation sim, Color color)
    {
        simulation = sim;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Generate dark blue square sprite if no sprite is assigned
        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = CreateSquareSprite();
        }

        spriteRenderer.color = color;

        // Add collider for environment interaction
        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = 0.1f;
            circleCollider.isTrigger = true;
        }
    }

    public void ApplyForce(Vector2 f)
    {
        force += f;
    }

    public void UpdatePhysics(float deltaTime)
    {
        // Clamp forces to prevent explosions
        float maxForce = 100f;
        force = Vector2.ClampMagnitude(force, maxForce);

        // Euler integration with lower acceleration
        Vector2 acceleration = force / mass;
        acceleration = Vector2.ClampMagnitude(acceleration, 50f);

        velocity += acceleration * deltaTime;
        velocity = Vector2.ClampMagnitude(velocity, 20f);
        velocity *= damping;

        transform.position += (Vector3)(velocity * deltaTime);

        // Reset force
        force = Vector2.zero;
    }

    public void CheckEnvironmentCollision(LayerMask environmentLayer)
    {
        if (velocity.magnitude < 0.01f)
        {
            // Still check for overlap when stationary
            CheckCollisionOverlap(environmentLayer);
            return;
        }

        // Raycast in movement direction
        Vector2 rayDirection = velocity.normalized;
        float rayDistance = velocity.magnitude * Time.fixedDeltaTime * 2f;

        RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position, rayDirection, rayDistance, environmentLayer);

        if (hit.collider != null)
        {
            // Bounce off surface
            Vector2 normal = hit.normal;
            velocity = Vector2.Reflect(velocity, normal) * 0.5f;

            // Move particle away from surface
            transform.position = (Vector2)transform.position + normal * 0.15f;
        }
        else
        {
            CheckCollisionOverlap(environmentLayer);
        }
    }

    private void CheckCollisionOverlap(LayerMask environmentLayer)
    {
        // Check if particle is overlapping with environment
        Collider2D[] overlappingColliders = Physics2D.OverlapCircleAll(transform.position, 0.1f, environmentLayer);

        if (overlappingColliders.Length > 0)
        {
            // Find closest collider
            Collider2D closest = overlappingColliders[0];
            Vector2 direction = ((Vector2)transform.position - (Vector2)closest.bounds.center).normalized;

            velocity = direction * velocity.magnitude * 0.5f;
            transform.position = (Vector2)transform.position + direction * 0.15f;
        }
    }

    private Sprite CreateSquareSprite()
    {
        int size = 16;
        Texture2D texture = new Texture2D(size, size);
        Color darkBlue = new Color(0.1f, 0.2f, 0.4f, 1f);

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = darkBlue;
        }

        texture.SetPixels(pixels);
        texture.filterMode = FilterMode.Point;
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }
}