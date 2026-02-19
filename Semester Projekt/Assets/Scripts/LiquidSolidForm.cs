using System.Collections.Generic;
using UnityEngine;

public class LiquidSolidForm : MonoBehaviour
{
    [Header("Solid Form Settings")]
    public float solidRadius = 1.5f;
    public float pushForce = 15f;
    public LayerMask movableLayer;

    [Header("State Settings")]
    public KeyCode stateChangeKey = KeyCode.E;
    public float solidDamping = 0.95f;
    public float liquidDamping = 0.99f;

    [Header("Visual Transformation")]
    public float solidParticleScale = 0.3f;
    public float liquidParticleScale = 0.15f;
    public float solidBlobExpansion = 1.8f;

    private LiquidSimulation simulation;
    private List<LiquidParticle> particles = new List<LiquidParticle>();
    private Vector2 blobCenter;
    private bool isSolid = false;
    private float stateChangeCooldown = 0f;
    private const float STATE_CHANGE_DELAY = 0.3f;
    private bool isInitialized = false;
    private float initialSolidRadius;
    private float transitionSpeed = 5f;
    private float currentParticleScale;
    private float currentBlobRadius;

    void Start()
    {
        InitializeSimulation();
    }

    void InitializeSimulation()
    {
        if (isInitialized) return;

        // Try to get LiquidSimulation from this GameObject first
        simulation = GetComponent<LiquidSimulation>();

        // If not found, try to find it in children
        if (simulation == null)
        {
            simulation = GetComponentInChildren<LiquidSimulation>();
        }

        // If still not found, try to find it in parent
        if (simulation == null)
        {
            simulation = GetComponentInParent<LiquidSimulation>();
        }

        // If still not found, search the scene
        if (simulation == null)
        {
            simulation = FindObjectOfType<LiquidSimulation>();
        }

        if (simulation == null)
        {
            Debug.LogError("LiquidSolidForm: LiquidSimulation component not found anywhere in the scene!");
            return;
        }

        initialSolidRadius = solidRadius;
        currentParticleScale = liquidParticleScale;
        currentBlobRadius = solidRadius;

        isInitialized = true;
        Debug.Log("LiquidSolidForm: LiquidSimulation found and initialized");
    }

    void Update()
    {
        // Try to initialize if not yet done
        if (!isInitialized)
        {
            InitializeSimulation();
        }

        // Handle state change input
        HandleStateChange();
    }

    void FixedUpdate()
    {
        if (simulation == null) return;

        // Update blob center and particles list
        UpdateBlobProperties();

        // Update particle damping based on state
        UpdateParticleDamping();

        // Smoothly transition particle scales and blob radius
        UpdateVisualTransition();

        // Push movable objects only in solid state
        if (isSolid)
        {
            PushMovableObjects();
        }
    }

    void HandleStateChange()
    {
        stateChangeCooldown -= Time.deltaTime;

        if (Input.GetKeyDown(stateChangeKey) && stateChangeCooldown <= 0f)
        {
            if (simulation == null)
            {
                Debug.LogError("LiquidSolidForm: Cannot change state - LiquidSimulation is null!");
                return;
            }

            isSolid = !isSolid;
            stateChangeCooldown = STATE_CHANGE_DELAY;

            // Visual feedback
            OnStateChanged();
        }
    }

    void OnStateChanged()
    {
        if (simulation == null) return;

        if (isSolid)
        {
            Debug.Log("State: SOLID - Particles are rigid, can push objects");
            // Increase spring strength for more rigid structure
            simulation.springStrength *= 1.5f;
            // Expand the solid radius
            solidRadius = initialSolidRadius * solidBlobExpansion;
        }
        else
        {
            Debug.Log("State: LIQUID - Particles are fluid, flowing freely");
            // Reset spring strength for fluid behavior
            simulation.springStrength /= 1.5f;
            // Reset the solid radius
            solidRadius = initialSolidRadius;
        }
    }

    void UpdateBlobProperties()
    {
        // Get all liquid particles
        particles.Clear();
        LiquidParticle[] allParticles = GetComponentsInChildren<LiquidParticle>();
        particles.AddRange(allParticles);

        // Calculate blob center (center of mass)
        if (particles.Count > 0)
        {
            blobCenter = Vector2.zero;
            foreach (var particle in particles)
            {
                blobCenter += (Vector2)particle.transform.position;
            }
            blobCenter /= particles.Count;
        }
    }

    void UpdateParticleDamping()
    {
        if (particles.Count == 0) return;

        float targetDamping = isSolid ? solidDamping : liquidDamping;

        foreach (var particle in particles)
        {
            particle.damping = targetDamping;
        }
    }

    void UpdateVisualTransition()
    {
        if (particles.Count == 0) return;

        // Determine target scale based on state
        float targetScale = isSolid ? solidParticleScale : liquidParticleScale;

        // Smoothly interpolate current scale
        currentParticleScale = Mathf.Lerp(currentParticleScale, targetScale, Time.fixedDeltaTime * transitionSpeed);

        // Smoothly interpolate blob radius
        currentBlobRadius = Mathf.Lerp(currentBlobRadius, solidRadius, Time.fixedDeltaTime * transitionSpeed);

        // Update all particle scales
        foreach (var particle in particles)
        {
            particle.transform.localScale = Vector3.one * currentParticleScale;
        }
    }

    void PushMovableObjects()
    {
        if (particles.Count == 0) return;

        // Find all movable objects near the blob
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(blobCenter, currentBlobRadius, movableLayer);

        foreach (var collider in nearbyColliders)
        {
            PushObject(collider);
        }
    }

    void PushObject(Collider2D collider)
    {
        // Get the object's Rigidbody2D
        Rigidbody2D rb = collider.GetComponent<Rigidbody2D>();
        if (rb == null || rb.isKinematic)
            return;

        // Calculate push direction (away from blob center)
        Vector2 pushDirection = ((Vector2)collider.transform.position - blobCenter).normalized;

        // Calculate push strength based on distance (closer = stronger push)
        float distance = Vector2.Distance(blobCenter, collider.transform.position);
        float distanceFactor = Mathf.Clamp01(1f - (distance / currentBlobRadius));

        // Apply force to the object
        Vector2 force = pushDirection * pushForce * distanceFactor;
        rb.linearVelocity += force * Time.fixedDeltaTime;

        // Optional: Add some rotation for visual effect
        float torque = Random.Range(-5f, 5f) * distanceFactor;
        rb.angularVelocity += torque * Time.fixedDeltaTime;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw solid form radius with color based on state
        Gizmos.color = isSolid ? new Color(1f, 0f, 0f, 0.3f) : new Color(0f, 1f, 1f, 0.3f);
        DrawCircle(blobCenter, currentBlobRadius, 32);

        // Draw state indicator text position
        if (isSolid)
        {
            Gizmos.color = Color.red;
        }
        else
        {
            Gizmos.color = Color.cyan;
        }
    }

    private void DrawCircle(Vector2 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector2(radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }

    public bool GetIsSolid() => isSolid;
}