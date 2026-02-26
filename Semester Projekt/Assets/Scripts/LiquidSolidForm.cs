using System.Collections.Generic;
using UnityEngine;

public class LiquidSolidForm : MonoBehaviour
{
    [Header("Solid Form Settings")]
    public float solidRadius = 1.5f;
    public float pushForce = 25f;
    public LayerMask movableLayer;

    [Header("State Settings")]
    public KeyCode stateChangeKey = KeyCode.E;
    public float solidDamping = 0.95f;
    public float liquidDamping = 0.99f;
    public float gasDamping = 0.80f;

    [Header("Visual Transformation")]
    public float solidParticleScale = 0.3f;
    public float liquidParticleScale = 0.15f;
    public float gasParticleScale = 0.1f;
    public float solidBlobExpansion = 1.8f;
    public float gasBlobExpansion = 2.5f;

    [Header("Gas Settings")]
    public float gasGravityMultiplier = -0.3f;
    public float gasMoveSpeedMultiplier = 0.3f;
    public Color gasColor = new Color(0.6f, 0.7f, 0.9f, 0.4f);

    [Header("Death Settings")]
    public LayerMask hotZoneLayer;

    private LiquidSimulation simulation;
    private List<LiquidParticle> particles = new List<LiquidParticle>();
    private Vector2 blobCenter;
    private Vector2 previousBlobCenter;
    private Vector2 blobVelocity;
    private MatterState currentState = MatterState.Liquid;
    private float stateChangeCooldown = 0f;
    private const float STATE_CHANGE_DELAY = 0.3f;
    private bool isInitialized = false;
    private float initialSolidRadius;
    private float initialSpringStrength;
    private float transitionSpeed = 5f;
    private float currentParticleScale;
    private float currentBlobRadius;
    private bool isDead = false;

    public event System.Action OnPlayerDied;

    void Start()
    {
        InitializeSimulation();
    }

    void InitializeSimulation()
    {
        if (isInitialized) return;

        simulation = GetComponent<LiquidSimulation>();
        if (simulation == null)
            simulation = GetComponentInChildren<LiquidSimulation>();
        if (simulation == null)
            simulation = GetComponentInParent<LiquidSimulation>();
        if (simulation == null)
            simulation = FindObjectOfType<LiquidSimulation>();

        if (simulation == null)
        {
            Debug.LogError("LiquidSolidForm: LiquidSimulation not found!");
            return;
        }

        initialSolidRadius = solidRadius;
        initialSpringStrength = simulation.springStrength;
        currentParticleScale = liquidParticleScale;
        currentBlobRadius = solidRadius;

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized)
            InitializeSimulation();

        if (isDead) return;

        HandleStateChange();
    }

    void FixedUpdate()
    {
        if (simulation == null || isDead) return;

        UpdateBlobProperties();
        UpdateParticleDamping();
        UpdateVisualTransition();
        UpdateParticleColors();

        if (currentState == MatterState.Solid)
        {
            PushMovableObjects();
        }

        if (currentState == MatterState.Gas)
        {
            CheckHotZones();
        }
    }

    void HandleStateChange()
    {
        stateChangeCooldown -= Time.deltaTime;

        if (Input.GetKeyDown(stateChangeKey) && stateChangeCooldown <= 0f)
        {
            if (simulation == null) return;

            // Cycle: Liquid -> Solid -> Gas -> Liquid
            switch (currentState)
            {
                case MatterState.Liquid:
                    currentState = MatterState.Solid;
                    break;
                case MatterState.Solid:
                    currentState = MatterState.Gas;
                    break;
                case MatterState.Gas:
                    currentState = MatterState.Liquid;
                    break;
            }

            stateChangeCooldown = STATE_CHANGE_DELAY;
            simulation.SetMatterState(currentState);
            OnStateChanged();
        }
    }

    void OnStateChanged()
    {
        if (simulation == null) return;

        // Reset spring strength to initial value first
        simulation.springStrength = initialSpringStrength;

        switch (currentState)
        {
            case MatterState.Solid:
                Debug.Log("State: SOLID - Can push objects, can jump");
                simulation.springStrength *= 1.5f;
                solidRadius = initialSolidRadius * solidBlobExpansion;
                break;

            case MatterState.Liquid:
                Debug.Log("State: LIQUID - Flowing freely");
                solidRadius = initialSolidRadius;
                break;

            case MatterState.Gas:
                Debug.Log("State: GAS - Slow and floaty, avoid hot zones!");
                simulation.springStrength *= 0.5f;
                solidRadius = initialSolidRadius * gasBlobExpansion;
                break;
        }
    }

    void UpdateBlobProperties()
    {
        particles.Clear();
        LiquidParticle[] allParticles = GetComponentsInChildren<LiquidParticle>();
        particles.AddRange(allParticles);

        previousBlobCenter = blobCenter;

        if (particles.Count > 0)
        {
            blobCenter = Vector2.zero;
            foreach (var particle in particles)
            {
                blobCenter += (Vector2)particle.transform.position;
            }
            blobCenter /= particles.Count;

            blobVelocity = (blobCenter - previousBlobCenter) / Time.fixedDeltaTime;
        }
    }

    void UpdateParticleDamping()
    {
        if (particles.Count == 0) return;

        float targetDamping;
        switch (currentState)
        {
            case MatterState.Solid:
                targetDamping = solidDamping;
                break;
            case MatterState.Gas:
                targetDamping = gasDamping;
                break;
            default:
                targetDamping = liquidDamping;
                break;
        }

        foreach (var particle in particles)
        {
            particle.damping = targetDamping;
        }
    }

    void UpdateVisualTransition()
    {
        if (particles.Count == 0) return;

        float targetScale;
        switch (currentState)
        {
            case MatterState.Solid:
                targetScale = solidParticleScale;
                break;
            case MatterState.Gas:
                targetScale = gasParticleScale;
                break;
            default:
                targetScale = liquidParticleScale;
                break;
        }

        currentParticleScale = Mathf.Lerp(currentParticleScale, targetScale, Time.fixedDeltaTime * transitionSpeed);
        currentBlobRadius = Mathf.Lerp(currentBlobRadius, solidRadius, Time.fixedDeltaTime * transitionSpeed);

        foreach (var particle in particles)
        {
            particle.transform.localScale = Vector3.one * currentParticleScale;
        }
    }

    void UpdateParticleColors()
    {
        if (particles.Count == 0) return;

        Color targetColor;
        switch (currentState)
        {
            case MatterState.Gas:
                targetColor = gasColor;
                break;
            default:
                targetColor = simulation.liquidColor;
                break;
        }

        foreach (var particle in particles)
        {
            SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Color.Lerp(sr.color, targetColor, Time.fixedDeltaTime * transitionSpeed);
            }
        }
    }

    void CheckHotZones()
    {
        if (particles.Count == 0) return;

        // Check if any particle is inside a hot zone
        foreach (var particle in particles)
        {
            Collider2D hotZone = Physics2D.OverlapCircle(particle.transform.position, 0.1f, hotZoneLayer);
            if (hotZone != null)
            {
                Die();
                return;
            }
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("DEAD! Gas entered a hot zone!");

        // Disable all particles visually
        foreach (var particle in particles)
        {
            SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(1f, 0.3f, 0.1f, 0.5f);
            }
            particle.velocity = Vector2.zero;
            particle.force = Vector2.zero;
        }

        // Notify listeners
        OnPlayerDied?.Invoke();
    }

    void PushMovableObjects()
    {
        if (particles.Count == 0) return;

        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(blobCenter, currentBlobRadius, movableLayer);

        foreach (var collider in nearbyColliders)
        {
            PushObject(collider);
        }
    }

    void PushObject(Collider2D collider)
    {
        Rigidbody2D rb = collider.GetComponent<Rigidbody2D>();
        if (rb == null || rb.isKinematic)
            return;

        MovableObject movable = collider.GetComponent<MovableObject>();
        if (movable == null || !movable.canBeMoved)
            return;

        Vector2 toObject = (Vector2)collider.transform.position - blobCenter;
        float distance = toObject.magnitude;
        float distanceFactor = Mathf.Clamp01(1f - (distance / currentBlobRadius));

        Vector2 pushAway = toObject.normalized;
        Vector2 pushDir;

        if (blobVelocity.magnitude > 0.5f)
        {
            pushDir = (blobVelocity.normalized * 0.7f + pushAway * 0.3f).normalized;
        }
        else
        {
            pushDir = pushAway;
        }

        float speedBoost = Mathf.Clamp(blobVelocity.magnitude * 0.5f, 1f, 3f);
        Vector2 force = pushDir * pushForce * distanceFactor * speedBoost;

        rb.AddForce(force, ForceMode2D.Force);
        movable.OnPushed(pushDir, force.magnitude);
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        switch (currentState)
        {
            case MatterState.Solid:
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                break;
            case MatterState.Gas:
                Gizmos.color = new Color(0.6f, 0.7f, 1f, 0.2f);
                break;
            default:
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                break;
        }

        DrawCircle(blobCenter, currentBlobRadius, 32);
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

    public MatterState GetCurrentState() => currentState;
    public bool GetIsSolid() => currentState == MatterState.Solid;
    public bool GetIsGas() => currentState == MatterState.Gas;
    public bool GetIsDead() => isDead;
    public float GetGasGravityMultiplier() => gasGravityMultiplier;
    public float GetGasMoveSpeedMultiplier() => gasMoveSpeedMultiplier;
}