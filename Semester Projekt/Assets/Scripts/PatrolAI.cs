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

    public event System.Action OnPlayerDetected;
    public event System.Action OnPlayerLost;
    public event System.Action OnGuardKnockedOut;
    public event System.Action OnLockdownTriggered;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }

        isMovingRight = startMovingRight;
        currentDirection = isMovingRight ? Vector2.right : Vector2.left;

        if (leftPoint == null || rightPoint == null)
        {
            Debug.LogError($"AI at {transform.position} is missing patrol points!");
        }
    }

    void Update()
    {
        // Decrease grace time
        if (spawnGraceTime > 0)
        {
            spawnGraceTime -= Time.deltaTime;
        }

        UpdateState();
        UpdateColors();
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
                {
                    EnterKnockedOut();
                }
                break;

            case AIState.KnockedOut:
                // Do nothing, just stay in place
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0)
                {
                    ReturnToPatrol();
                }
                break;

            case AIState.Lockdown:
                UpdatePatrol(); // Continue patrolling but faster
                DetectPlayer(); // With enhanced detection
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0)
                {
                    ReturnToPatrol();
                }
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
        {
            currentSpeed *= lockdownSpeedMultiplier;
        }

        transform.position += moveDirection * currentSpeed * Time.deltaTime;

        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget < 0.5f)
        {
            transform.position = targetPosition;
            isMovingRight = !isMovingRight;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !isMovingRight;
        }
    }

    void UpdateControlled()
    {
        // Move based on player input (from LiquidController) - X AXIS ONLY
        Vector2 moveDir = new Vector2(controlInput.x, 0).normalized;
        if (Mathf.Abs(moveDir.x) > 0.1f)
        {
            transform.position += new Vector3(moveDir.x * patrolSpeed * 1.5f * Time.deltaTime, 0, 0);

            // Face direction
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = moveDir.x < 0;
            }
        }
    }

    void DetectPlayer()
    {
        if (spawnGraceTime > 0)
            return;

        LiquidSolidForm player = FindObjectOfType<LiquidSolidForm>();
        if (player == null)
            return;

        Vector2 detectionDir = isMovingRight ? Vector2.right : Vector2.left;
        Vector2 toPlayer = ((Vector2)player.transform.position - (Vector2)transform.position);
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

        bool playerInCone = (angleToPlayer < currentDetectionAngle / 2f) && (distToPlayer < currentDetectionDistance);

        if (playerInCone && !hasDetected)
        {
            hasDetected = true;
            alertTimer = 0f;
            OnPlayerDetected?.Invoke();
            Debug.Log($"*** {gameObject.name} SPOTTED PLAYER! ***");

            if (spriteRenderer != null)
            {
                spriteRenderer.color = alertColor;
            }
        }
        else if (!playerInCone && hasDetected)
        {
            hasDetected = false;
            OnPlayerLost?.Invoke();
            Debug.Log($"*** {gameObject.name} lost player ***");
        }

        if (hasDetected)
        {
            UpdateAlertTimer();
        }
    }

    void DetectKnockedOutGuards()
    {
        PatrolAI[] allGuards = FindObjectsOfType<PatrolAI>();

        foreach (var guard in allGuards)
        {
            if (guard == this) continue;
            if (guard.currentState != AIState.KnockedOut) continue;

            Vector2 detectionDir = isMovingRight ? Vector2.right : Vector2.left;
            Vector2 toGuard = ((Vector2)guard.transform.position - (Vector2)transform.position);
            float distToGuard = toGuard.magnitude;

            if (distToGuard < detectionDistance)
            {
                Debug.Log($"*** {gameObject.name} SPOTTED KNOCKED OUT GUARD! LOCKDOWN TRIGGERED! ***");
                TriggerLockdown();
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
            {
                player.ForceKill();
            }
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
        {
            spriteRenderer.color = knockoutColor;
        }
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
        {
            spriteRenderer.color = lockdownColor;
        }

        // Trigger lockdown on all guards
        PatrolAI[] allGuards = FindObjectsOfType<PatrolAI>();
        foreach (var guard in allGuards)
        {
            if (guard.currentState != AIState.KnockedOut)
            {
                guard.currentState = AIState.Lockdown;
                guard.stateTimer = lockdownDuration;
                guard.isInLockdown = true;

                if (guard.spriteRenderer != null)
                {
                    guard.spriteRenderer.color = lockdownColor;
                }
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
        {
            spriteRenderer.color = normalColor;
        }
    }

    void UpdateColors()
    {
        switch (currentState)
        {
            case AIState.Controlled:
                if (spriteRenderer != null && spriteRenderer.color != controlledColor)
                {
                    spriteRenderer.color = controlledColor;
                }
                break;
        }
    }

    public AIState GetCurrentState() => currentState;
    public bool IsControlled() => currentState == AIState.Controlled;
    public bool IsKnockedOut() => currentState == AIState.KnockedOut;

    public bool IsAlerting()
    {
        return hasDetected;
    }

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