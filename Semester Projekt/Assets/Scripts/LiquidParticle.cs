using UnityEngine;

public class LiquidParticle : MonoBehaviour
{
    public Vector2 velocity;
    public Vector2 force;
    public float mass = 1f;
    public float damping = 0.95f;

    private SpriteRenderer spriteRenderer;
    private LiquidSimulation simulation;
    private CircleCollider2D circleCollider;

    // Gravity is applied separately so springs can never cancel it out
    private Vector2 gravityVelocity;

    // Ground detection
    private bool isGrounded = false;
    private Vector2 groundNormal = Vector2.up;

    public void Initialize(LiquidSimulation sim, Color color)
    {
        simulation = sim;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = CreateSquareSprite();
        }

        spriteRenderer.color = color;

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

    public void ApplyGravity(float gravityStrength)
    {
        gravityVelocity += Vector2.down * gravityStrength * Time.fixedDeltaTime;
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    public void UpdatePhysics(float deltaTime)
    {
        float maxForce = 150f;
        force = Vector2.ClampMagnitude(force, maxForce);

        Vector2 acceleration = force / mass;
        acceleration = Vector2.ClampMagnitude(acceleration, 80f);

        velocity += acceleration * deltaTime;

        // Apply gravity separately
        velocity += gravityVelocity;
        gravityVelocity = Vector2.zero;

        // Different damping for horizontal vs vertical
        // Preserve horizontal movement, dampen vertical more
        float horizontalDamp = isGrounded ? 0.97f : 0.98f;
        float verticalDamp = damping;
        velocity.x *= horizontalDamp;
        velocity.y *= verticalDamp;

        velocity = Vector2.ClampMagnitude(velocity, 25f);

        transform.position += (Vector3)(velocity * deltaTime);

        force = Vector2.zero;
        isGrounded = false;
    }

    public void CheckEnvironmentCollision(LayerMask collisionLayer)
    {
        // Always check overlap first
        CheckCollisionOverlap(collisionLayer);

        if (velocity.magnitude < 0.01f) return;

        Vector2 rayDirection = velocity.normalized;
        float rayDistance = velocity.magnitude * Time.fixedDeltaTime * 2f;

        RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position, rayDirection, rayDistance, collisionLayer);

        if (hit.collider != null)
        {
            Vector2 normal = hit.normal;

            // Separate velocity into normal and tangential components
            float normalSpeed = Vector2.Dot(velocity, normal);

            // Only reflect if moving INTO the surface
            if (normalSpeed < 0)
            {
                // Remove the into-surface component, keep the sliding component
                Vector2 tangent = velocity - normal * normalSpeed;

                // Keep most of the horizontal/tangential speed, bounce a tiny bit
                velocity = tangent * 0.95f + normal * Mathf.Abs(normalSpeed) * 0.1f;

                // Detect if this is a floor collision
                if (normal.y > 0.5f)
                {
                    isGrounded = true;
                    groundNormal = normal;
                }
            }

            transform.position = (Vector2)hit.point + normal * 0.12f;
        }
    }

    private void CheckCollisionOverlap(LayerMask collisionLayer)
    {
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(transform.position, 0.1f, collisionLayer);

        if (overlaps.Length > 0)
        {
            Collider2D closest = overlaps[0];
            float closestDist = float.MaxValue;

            // Find the actual closest collider
            foreach (var col in overlaps)
            {
                float dist = Vector2.Distance(transform.position, col.ClosestPoint(transform.position));
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = col;
                }
            }

            Vector2 closestPoint = closest.ClosestPoint(transform.position);
            Vector2 pushDir = ((Vector2)transform.position - closestPoint);

            if (pushDir.magnitude < 0.01f)
            {
                pushDir = ((Vector2)transform.position - (Vector2)closest.bounds.center).normalized;
            }
            else
            {
                pushDir = pushDir.normalized;
            }

            // Only kill velocity going INTO the surface, preserve sliding
            float intoSurface = Vector2.Dot(velocity, -pushDir);
            if (intoSurface > 0)
            {
                velocity += pushDir * intoSurface;
            }

            // Push out of surface
            transform.position = (Vector2)transform.position + pushDir * 0.12f;

            if (pushDir.y > 0.5f)
            {
                isGrounded = true;
                groundNormal = pushDir;
            }
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