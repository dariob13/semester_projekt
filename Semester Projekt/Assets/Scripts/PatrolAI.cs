using UnityEngine;

public class PatrolAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform leftPoint;
    public Transform rightPoint;
    public float patrolSpeed = 3f;
    public bool startMovingRight = true;

    [Header("Detection Settings")]
    public float detectionDistance = 50f;
    public float detectionAngle = 60f;
    public float alertDuration = 3f;
    public LayerMask obstacleLayer;

    [Header("Control Settings")]
    public float controlDuration = 5f;
    public float knockoutDuration = 15f;

    [Header("Lockdown Settings")]
    public float lockdownDuration = 10f;
    public float lockdownSpeedMultiplier = 2f;
    public float lockdownConeMultiplier = 1.5f;
    public float lockdownAlertDuration = 1.5f;

    [Header("Visual Settings")]
    public Color normalColor = new Color(1f, 0.5f, 0f, 0.8f);
    public Color alertColor = new Color(1f, 0f, 0f, 1f);
    public Color controlledColor = new Color(0f, 1f, 1f, 1f);
    public Color knockoutColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    public Color lockdownColor = new Color(1f, 0f, 1f, 1f);

    [Header("Cone Visual Settings")]
    public Color coneNormalColor = new Color(1f, 1f, 0f, 0.15f);
    public Color coneAlertColor = new Color(1f, 0f, 0f, 0.25f);
    public Color coneLockdownColor = new Color(1f, 0f, 1f, 0.2f);
    public int coneSegments = 30;

    private Vector2 currentDirection = Vector2.right;
    private bool isMovingRight = true;
    private SpriteRenderer spriteRenderer;
    private bool hasDetected = false;
    private float alertTimer = 0f;
    private float spawnGraceTime = 2f;

    // State management
    private AIState currentState = AIState.Patrol;
    private float stateTimer = 0f;

    // Control
    private bool isBeingControlled = false;
    private Vector2 controlInput = Vector2.zero;

    // Lockdown
    private bool isInLockdown = false;

    // Cone visualization
    private GameObject coneObject;
    private MeshFilter coneMeshFilter;
    private MeshRenderer coneMeshRenderer;
    private Mesh coneMesh;

    public event System.Action OnPlayerDetected;
    public event System.Action OnPlayerLost;
    public event System.Action OnGuardKnockedOut;
    public event System.Action OnLockdownTriggered;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;

        isMovingRight = startMovingRight;
        currentDirection = isMovingRight ? Vector2.right : Vector2.left;

        if (leftPoint == null || rightPoint == null)
            Debug.LogError($"AI at {transform.position} is missing patrol points!");

        SetupConeMesh();
    }

    void SetupConeMesh()
    {
        coneObject = new GameObject("DetectionCone");
        coneObject.transform.SetParent(transform);
        coneObject.transform.localPosition = Vector3.zero;

        coneMeshFilter = coneObject.AddComponent<MeshFilter>();
        coneMeshRenderer = coneObject.AddComponent<MeshRenderer>();

        Material coneMaterial = new Material(Shader.Find("Sprites/Default"));
        coneMaterial.color = coneNormalColor;
        coneMeshRenderer.material = coneMaterial;
        coneMeshRenderer.sortingOrder = -1;

        coneMesh = new Mesh();
        coneMeshFilter.mesh = coneMesh;

        UpdateConeMesh();
    }

    void UpdateConeMesh()
    {
        if (coneMesh == null) return;

        Vector2 origin = transform.position;
        Vector2 detectionDir = isMovingRight ? Vector2.right : Vector2.left;

        float currentDetectionDist = detectionDistance;
        float currentDetectionAngle = detectionAngle;

        if (isInLockdown)
        {
            currentDetectionDist *= lockdownConeMultiplier;
            currentDetectionAngle *= lockdownConeMultiplier;
        }

        int vertexCount = coneSegments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[coneSegments * 3];

        // Origin vertex (local space = zero)
        vertices[0] = Vector3.zero;

        float halfAngle = currentDetectionAngle / 2f;
        float angleStep = currentDetectionAngle / coneSegments;

        for (int i = 0; i <= coneSegments; i++)
        {
            float angle = -halfAngle + angleStep * i;
            Vector2 rayDir = RotateVector(detectionDir, angle);

            // Cast ray - stop at obstacles
            RaycastHit2D hit = Physics2D.Raycast(origin, rayDir, currentDetectionDist, obstacleLayer);

            float rayLength = hit.collider != null ? hit.distance : currentDetectionDist;

            // Convert to local space
            Vector2 worldPoint = origin + rayDir * rayLength;
            Vector2 localPoint = worldPoint - origin;
            vertices[i + 1] = new Vector3(localPoint.x, localPoint.y, 0);
        }

        for (int i = 0; i < coneSegments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        coneMesh.Clear();
        coneMesh.vertices = vertices;
        coneMesh.triangles = triangles;
        coneMesh.RecalculateNormals();

        // Update color based on state
        if (coneMeshRenderer != null)
        {
            Color targetColor;
            if (hasDetected)
                targetColor = coneAlertColor;
            else if (isInLockdown)
                targetColor = coneLockdownColor;
            else if (currentState == AIState.KnockedOut)
                targetColor = Color.clear;
            else
                targetColor = coneNormalColor;

            coneMeshRenderer.material.color = Color.Lerp(
                coneMeshRenderer.material.color,
                targetColor,
                Time.deltaTime * 5f
            );
        }
    }

    void Update()
    {
        if (spawnGraceTime > 0)
            spawnGraceTime -= Time.deltaTime;

        UpdateState();
        UpdateColors();
        UpdateConeMesh();
    }

    void UpdateState()
    {
        switch (currentState)
        {
            case AIState.Patrol:
                UpdatePatrol();
                DetectPlayer();
                DetectKnockedOutGuards();
                break;

            case AIState.Controlled:
                UpdateControlled();
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0)
                    EnterKnockedOut();
                break;

            case AIState.KnockedOut:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0)
                    ReturnToPatrol();
                break;

            case AIState.Lockdown:
                UpdatePatrol();
                DetectPlayer();
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0)
                    ReturnToPatrol();
                break;
        }
    }

    void UpdatePatrol()
    {
        if (leftPoint == null || rightPoint == null) return;

        Vector3 targetPosition = isMovingRight ? rightPoint.position : leftPoint.position;
        Vector3 moveDirection = (targetPosition - transform.position).normalized;

        float currentSpeed = patrolSpeed;
        if (isInLockdown)
            currentSpeed *= lockdownSpeedMultiplier;

        transform.position += moveDirection * currentSpeed * Time.deltaTime;

        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget < 0.5f)
        {
            transform.position = targetPosition;
            isMovingRight = !isMovingRight;
        }

        if (spriteRenderer != null)
            spriteRenderer.flipX = !isMovingRight;
    }

    void UpdateControlled()
    {
        Vector2 moveDir = new Vector2(controlInput.x, 0).normalized;
        if (Mathf.Abs(moveDir.x) > 0.1f)
        {
            transform.position += new Vector3(moveDir.x * patrolSpeed * 1.5f * Time.deltaTime, 0, 0);

            if (spriteRenderer != null)
                spriteRenderer.flipX = moveDir.x < 0;
        }
    }

    void DetectPlayer()
    {
        if (spawnGraceTime > 0)
            return;

        LiquidSolidForm player = FindObjectOfType<LiquidSolidForm>();
        if (player == null)
            return;

        Vector2 playerPos = GetBlobCenter(player);
        Vector2 origin = transform.position;
        Vector2 detectionDir = isMovingRight ? Vector2.right : Vector2.left;
        Vector2 toPlayer = playerPos - origin;
        float distToPlayer = toPlayer.magnitude;

        if (distToPlayer == 0) return;

        toPlayer = toPlayer.normalized;
        float angleToPlayer = Vector2.Angle(detectionDir, toPlayer);

        float currentDetectionDistance = detectionDistance;
        float currentDetectionAngle = detectionAngle;

        if (isInLockdown)
        {
            currentDetectionDistance *= lockdownConeMultiplier;
            currentDetectionAngle *= lockdownConeMultiplier;
        }

        // Check if player is within angle and distance
        bool inAngleAndRange = (angleToPlayer < currentDetectionAngle / 2f) && (distToPlayer < currentDetectionDistance);

        // Check line of sight - blocked by obstacles?
        bool hasLineOfSight = false;
        if (inAngleAndRange)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, toPlayer, distToPlayer, obstacleLayer);
            hasLineOfSight = hit.collider == null; // No obstacle between guard and player
        }

        bool playerInCone = inAngleAndRange && hasLineOfSight;

        if (playerInCone && !hasDetected)
        {
            hasDetected = true;
            alertTimer = 0f;
            OnPlayerDetected?.Invoke();
            Debug.Log($"*** {gameObject.name} SPOTTED PLAYER! ***");

            if (spriteRenderer != null)
                spriteRenderer.color = alertColor;
        }
        else if (!playerInCone && hasDetected)
        {
            hasDetected = false;
            alertTimer = 0f;
            OnPlayerLost?.Invoke();
            Debug.Log($"*** {gameObject.name} lost player ***");

            if (spriteRenderer != null)
                spriteRenderer.color = normalColor;
        }

        if (hasDetected)
            UpdateAlertTimer();
    }

    private Vector2 GetBlobCenter(LiquidSolidForm player)
    {
        LiquidParticle[] particles = player.GetComponentsInChildren<LiquidParticle>();

        if (particles.Length == 0)
            return player.transform.position;

        Vector2 center = Vector2.zero;
        foreach (var particle in particles)
            center += (Vector2)particle.transform.position;

        return center / particles.Length;
    }

    void DetectKnockedOutGuards()
    {
        PatrolAI[] allGuards = FindObjectsOfType<PatrolAI>();

        foreach (var guard in allGuards)
        {
            if (guard == this) continue;
            if (guard.currentState != AIState.KnockedOut) continue;

            Vector2 toGuard = ((Vector2)guard.transform.position - (Vector2)transform.position);
            float distToGuard = toGuard.magnitude;

            if (distToGuard < detectionDistance)
            {
                // Check line of sight to knocked out guard
                RaycastHit2D hit = Physics2D.Raycast(transform.position, toGuard.normalized, distToGuard, obstacleLayer);
                if (hit.collider == null)
                {
                    Debug.Log($"*** {gameObject.name} SPOTTED KNOCKED OUT GUARD! LOCKDOWN! ***");
                    TriggerLockdown();
                }
            }
        }
    }

    void UpdateAlertTimer()
    {
        alertTimer += Time.deltaTime;
        float alertDur = isInLockdown ? lockdownAlertDuration : alertDuration;

        if (alertTimer >= alertDur)
        {
            hasDetected = false;
            LiquidSolidForm player = FindObjectOfType<LiquidSolidForm>();
            if (player != null && !player.GetIsDead())
                player.ForceKill();
        }
    }

    public void TakeControl(Vector2 input)
    {
        if (currentState == AIState.KnockedOut || currentState == AIState.Lockdown)
            return;

        if (currentState != AIState.Controlled)
        {
            currentState = AIState.Controlled;
            stateTimer = controlDuration;
            Debug.Log($"*** CONTROLLING {gameObject.name}! ***");
        }

        controlInput = input;
    }

    void EnterKnockedOut()
    {
        currentState = AIState.KnockedOut;
        stateTimer = knockoutDuration;
        hasDetected = false;

        Debug.Log($"*** {gameObject.name} IS KNOCKED OUT! ***");
        OnGuardKnockedOut?.Invoke();

        if (spriteRenderer != null)
            spriteRenderer.color = knockoutColor;
    }

    void TriggerLockdown()
    {
        if (currentState == AIState.KnockedOut)
            return;

        currentState = AIState.Lockdown;
        stateTimer = lockdownDuration;
        isInLockdown = true;

        OnLockdownTriggered?.Invoke();

        if (spriteRenderer != null)
            spriteRenderer.color = lockdownColor;

        PatrolAI[] allGuards = FindObjectsOfType<PatrolAI>();
        foreach (var guard in allGuards)
        {
            if (guard.currentState != AIState.KnockedOut)
            {
                guard.currentState = AIState.Lockdown;
                guard.stateTimer = lockdownDuration;
                guard.isInLockdown = true;

                if (guard.spriteRenderer != null)
                    guard.spriteRenderer.color = lockdownColor;
            }
        }
    }

    void ReturnToPatrol()
    {
        currentState = AIState.Patrol;
        isInLockdown = false;
        hasDetected = false;
        alertTimer = 0f;

        Debug.Log($"*** {gameObject.name} RETURNED TO PATROL ***");

        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;
    }

    void UpdateColors()
    {
        if (currentState == AIState.Controlled && spriteRenderer != null)
            spriteRenderer.color = controlledColor;
    }

    private Vector2 RotateVector(Vector2 vector, float angleDegrees)
    {
        float angleRad = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angleRad);
        float sin = Mathf.Sin(angleRad);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    public AIState GetCurrentState() => currentState;
    public bool IsControlled() => currentState == AIState.Controlled;
    public bool IsKnockedOut() => currentState == AIState.KnockedOut;
    public bool IsAlerting() => hasDetected;

    public float GetAlertProgress()
    {
        if (!hasDetected) return 0f;
        float currentAlertDuration = isInLockdown ? lockdownAlertDuration : alertDuration;
        return Mathf.Clamp01(alertTimer / currentAlertDuration);
    }
    
    void OnDrawGizmos()
    {
        if (leftPoint == null || rightPoint == null) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(leftPoint.position, 0.3f);
        Gizmos.DrawSphere(rightPoint.position, 0.3f);
        Gizmos.DrawLine(leftPoint.position, rightPoint.position);
    }
}