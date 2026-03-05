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
    public LayerMask playerLayer;
    public float alertDuration = 3f;

    [Header("Visual Settings")]
    public Color normalColor = new Color(1f, 0.5f, 0f, 0.8f);
    public Color alertColor = new Color(1f, 0f, 0f, 1f);

    private Vector2 currentDirection = Vector2.right;
    private bool isMovingRight = true;
    private LiquidSolidForm detectedPlayer;
    private SpriteRenderer spriteRenderer;
    private bool hasDetected = false;
    private float alertTimer = 0f;

    public event System.Action OnPlayerDetected;
    public event System.Action OnPlayerLost;

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
        Patrol();
        DetectPlayer();
        UpdateAlertTimer();
    }

    void Patrol()
    {
        if (leftPoint == null || rightPoint == null) return;

        Vector3 targetPosition = isMovingRight ? rightPoint.position : leftPoint.position;

        // Move towards target
        Vector3 moveDirection = (targetPosition - transform.position).normalized;
        transform.position += moveDirection * patrolSpeed * Time.deltaTime;

        // Check if reached waypoint
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget < 0.5f)
        {
            transform.position = targetPosition;
            isMovingRight = !isMovingRight;
            currentDirection = isMovingRight ? Vector2.right : Vector2.left;
        }

        // Flip sprite based on direction
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !isMovingRight;
        }
    }

    void DetectPlayer()
    {
        LiquidSolidForm player = FindObjectOfType<LiquidSolidForm>();
        
        if (player == null)
            return;
        
        Vector2 detectionDir = isMovingRight ? Vector2.right : Vector2.left;
        Vector2 toPlayer = ((Vector2)player.transform.position - (Vector2)transform.position);
        float distToPlayer = toPlayer.magnitude;
        
        if (distToPlayer == 0) return;
        
        toPlayer = toPlayer.normalized;
        float angleToPlayer = Vector2.Angle(detectionDir, toPlayer);
        
        bool playerInCone = (angleToPlayer < detectionAngle / 2f) && (distToPlayer < detectionDistance);
        
        if (playerInCone && !hasDetected)
        {
            hasDetected = true;
            alertTimer = 0f;
            OnPlayerDetected?.Invoke();
            Debug.Log("*** PLAYER SPOTTED BY AI! 3 SECOND WARNING! ***");

            if (spriteRenderer != null)
            {
                spriteRenderer.color = alertColor;
            }
        }
        else if (!playerInCone && hasDetected)
        {
            hasDetected = false;
            alertTimer = 0f;
            OnPlayerLost?.Invoke();
            Debug.Log("*** Player escaped! Alert cancelled! ***");

            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
        }
    }

    void UpdateAlertTimer()
    {
        if (!hasDetected) return;

        alertTimer += Time.deltaTime;
        float remainingTime = alertDuration - alertTimer;

        Debug.Log($"[ALERT TIMER] {remainingTime:F1}s remaining");

        if (alertTimer >= alertDuration)
        {
            // Time's up - player loses
            hasDetected = false;
            Debug.Log("*** TIME'S UP - PLAYER CAUGHT BY AI! ***");
            
            LiquidSolidForm player = FindObjectOfType<LiquidSolidForm>();
            if (player != null && !player.GetIsDead())
            {
                player.ForceKill();
            }
        }
    }

    public float GetAlertProgress()
    {
        if (!hasDetected) return 0f;
        return Mathf.Clamp01(alertTimer / alertDuration);
    }

    public bool IsAlerting()
    {
        return hasDetected;
    }

    void OnDrawGizmos()
    {
        // Draw patrol points
        if (leftPoint != null && rightPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(leftPoint.position, 0.3f);
            Gizmos.DrawSphere(rightPoint.position, 0.3f);
            Gizmos.DrawLine(leftPoint.position, rightPoint.position);
        }

        // Draw detection cone
        Gizmos.color = hasDetected ? new Color(1f, 0f, 0f, 0.3f) : new Color(1f, 1f, 0f, 0.2f);
        Vector2 detectionDir = (isMovingRight || !Application.isPlaying) ? Vector2.right : Vector2.left;

        // Draw cone lines
        float leftAngle = detectionAngle / 2f;
        float rightAngle = -detectionAngle / 2f;

        Vector2 leftRay = RotateVector(detectionDir, leftAngle) * detectionDistance;
        Vector2 rightRay = RotateVector(detectionDir, rightAngle) * detectionDistance;
        Vector2 centerRay = detectionDir * detectionDistance;

        Gizmos.DrawLine(transform.position, transform.position + (Vector3)leftRay);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)rightRay);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)centerRay);

        // Draw cone arc
        for (int i = 0; i < 5; i++)
        {
            float angle = -detectionAngle / 2f + (detectionAngle / 4f) * i;
            Vector2 dir = RotateVector(detectionDir, angle) * detectionDistance;
            Gizmos.DrawSphere(transform.position + (Vector3)dir, 0.1f);
        }
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
}