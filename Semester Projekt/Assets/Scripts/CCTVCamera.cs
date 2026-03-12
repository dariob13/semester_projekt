using UnityEngine;

public class CCTVCamera : MonoBehaviour
{
    [Header("Sweep Settings")]
    public float sweepAngleMin = -60f;
    public float sweepAngleMax = 60f;
    public float sweepSpeed = 30f;
    public float pauseAtEnd = 0.5f;

    [Header("Detection Settings")]
    public float detectionDistance = 10f;
    public float detectionAngle = 40f;
    public float alertDuration = 3f;
    public LayerMask obstacleLayer;

    [Header("Lockdown Settings")]
    public float lockdownDetectionMultiplier = 1.5f;
    public float lockdownAlertDuration = 1.5f;
    public float lockdownSweepMultiplier = 2f;

    [Header("Visual Settings")]
    public Color bodyColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color coneNormalColor = new Color(1f, 1f, 0f, 0.15f);
    public Color coneAlertColor = new Color(1f, 0f, 0f, 0.3f);
    public Color coneLockdownColor = new Color(1f, 0f, 1f, 0.25f);
    public int coneSegments = 30;

    private float currentAngle = 0f;
    private float sweepDirection = 1f;
    private float pauseTimer = 0f;
    private bool isPausing = false;

    private bool hasDetected = false;
    private float alertTimer = 0f;
    private bool isInLockdown = false;

    private SpriteRenderer spriteRenderer;
    private GameObject coneObject;
    private MeshFilter coneMeshFilter;
    private MeshRenderer coneMeshRenderer;
    private Mesh coneMesh;

    private LiquidSolidForm cachedPlayer;

    public event System.Action OnPlayerDetected;
    public event System.Action OnPlayerLost;

    void Start()
    {
        cachedPlayer = FindObjectOfType<LiquidSolidForm>();

        SetupSprite();
        SetupConeMesh();

        currentAngle = sweepAngleMin;
    }

    void SetupSprite()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = CreateCameraSprite();
        spriteRenderer.color = bodyColor;
        spriteRenderer.sortingOrder = 2;
    }

    void SetupConeMesh()
    {
        coneObject = new GameObject("CCTVCone");
        coneObject.transform.SetParent(transform);
        coneObject.transform.localPosition = Vector3.zero;

        coneMeshFilter = coneObject.AddComponent<MeshFilter>();
        coneMeshRenderer = coneObject.AddComponent<MeshRenderer>();

        Material coneMat = new Material(Shader.Find("Sprites/Default"));
        coneMat.color = coneNormalColor;
        coneMeshRenderer.material = coneMat;
        coneMeshRenderer.sortingOrder = 1;

        coneMesh = new Mesh();
        coneMeshFilter.mesh = coneMesh;
    }

    void Update()
    {
        UpdateSweep();
        DetectPlayer();
        UpdateConeMesh();
    }

    void UpdateSweep()
    {
        if (isPausing)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
                isPausing = false;
            return;
        }

        float speed = sweepSpeed;
        if (isInLockdown)
            speed *= lockdownSweepMultiplier;

        currentAngle += sweepDirection * speed * Time.deltaTime;

        // Reached end of sweep - pause and reverse
        if (currentAngle >= sweepAngleMax)
        {
            currentAngle = sweepAngleMax;
            sweepDirection = -1f;
            isPausing = true;
            pauseTimer = pauseAtEnd;
        }
        else if (currentAngle <= sweepAngleMin)
        {
            currentAngle = sweepAngleMin;
            sweepDirection = 1f;
            isPausing = true;
            pauseTimer = pauseAtEnd;
        }
    }

    void DetectPlayer()
    {
        if (cachedPlayer == null)
        {
            cachedPlayer = FindObjectOfType<LiquidSolidForm>();
            if (cachedPlayer == null) return;
        }

        // Gas state is invisible to cameras
        if (cachedPlayer.GetCurrentState() == MatterState.Gas)
        {
            // Reset detection if player switches to gas mid-detection
            if (hasDetected)
            {
                hasDetected = false;
                alertTimer = 0f;
                OnPlayerLost?.Invoke();
            }
            return;
        }

        Vector2 playerPos = GetBlobCenter(cachedPlayer);
        Vector2 origin = transform.position;
        Vector2 viewDir = GetCurrentViewDirection();

        Vector2 toPlayer = playerPos - origin;
        float distToPlayer = toPlayer.magnitude;

        if (distToPlayer == 0) return;

        toPlayer = toPlayer.normalized;
        float angleToPlayer = Vector2.Angle(viewDir, toPlayer);

        float currentDetectionDist = detectionDistance;
        float currentDetectionAngle = detectionAngle;

        if (isInLockdown)
        {
            currentDetectionDist *= lockdownDetectionMultiplier;
            currentDetectionAngle *= lockdownDetectionMultiplier;
        }

        bool inAngleAndRange = (angleToPlayer < currentDetectionAngle / 2f) && (distToPlayer < currentDetectionDist);

        bool hasLineOfSight = false;
        if (inAngleAndRange)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, toPlayer, distToPlayer, obstacleLayer);
            hasLineOfSight = hit.collider == null;
        }

        bool playerInCone = inAngleAndRange && hasLineOfSight;

        if (playerInCone && !hasDetected)
        {
            hasDetected = true;
            alertTimer = 0f;
            OnPlayerDetected?.Invoke();
            Debug.Log($"*** CCTV at {transform.position} SPOTTED PLAYER! ***");
        }
        else if (!playerInCone && hasDetected)
        {
            hasDetected = false;
            alertTimer = 0f;
            OnPlayerLost?.Invoke();
            Debug.Log($"*** CCTV lost player ***");
        }

        if (hasDetected)
            UpdateAlertTimer();
    }

    void UpdateAlertTimer()
    {
        alertTimer += Time.deltaTime;
        float alertDur = isInLockdown ? lockdownAlertDuration : alertDuration;

        if (alertTimer >= alertDur)
        {
            hasDetected = false;
            if (cachedPlayer != null && !cachedPlayer.GetIsDead())
                cachedPlayer.ForceKill();
        }
    }

    void UpdateConeMesh()
    {
        if (coneMesh == null) return;

        Vector2 origin = transform.position;
        Vector2 viewDir = GetCurrentViewDirection();

        float currentDetectionDist = detectionDistance;
        float currentDetectionAngle = detectionAngle;

        if (isInLockdown)
        {
            currentDetectionDist *= lockdownDetectionMultiplier;
            currentDetectionAngle *= lockdownDetectionMultiplier;
        }

        Vector3[] vertices = new Vector3[coneSegments + 2];
        int[] triangles = new int[coneSegments * 3];

        vertices[0] = Vector3.zero;

        float halfAngle = currentDetectionAngle / 2f;
        float angleStep = currentDetectionAngle / coneSegments;

        for (int i = 0; i <= coneSegments; i++)
        {
            float angle = -halfAngle + angleStep * i;
            Vector2 rayDir = RotateVector(viewDir, angle);

            RaycastHit2D hit = Physics2D.Raycast(origin, rayDir, currentDetectionDist, obstacleLayer);
            float rayLength = hit.collider != null ? hit.distance : currentDetectionDist;

            Vector2 localPoint = rayDir * rayLength;
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

        if (coneMeshRenderer != null)
        {
            Color targetColor = hasDetected ? coneAlertColor :
                                isInLockdown ? coneLockdownColor :
                                coneNormalColor;

            coneMeshRenderer.material.color = Color.Lerp(
                coneMeshRenderer.material.color,
                targetColor,
                Time.deltaTime * 8f
            );
        }
    }

    public void EnterLockdown(float duration)
    {
        isInLockdown = true;
        Invoke(nameof(ExitLockdown), duration);
    }

    void ExitLockdown()
    {
        isInLockdown = false;
    }

    private Vector2 GetCurrentViewDirection()
    {
        float radians = currentAngle * Mathf.Deg2Rad;
        // Base direction is downward for ceiling-mounted camera
        return RotateVector(Vector2.down, currentAngle);
    }

    private Vector2 GetBlobCenter(LiquidSolidForm player)
    {
        LiquidParticle[] particles = player.GetComponentsInChildren<LiquidParticle>();

        if (particles.Length == 0)
            return player.transform.position;

        Vector2 center = Vector2.zero;
        foreach (var p in particles)
            center += (Vector2)p.transform.position;

        return center / particles.Length;
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

    private Sprite CreateCameraSprite()
    {
        int size = 16;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Camera body shape
                bool inBody = (x >= 2 && x <= 13 && y >= 5 && y <= 10);
                // Lens
                bool inLens = (x >= 11 && x <= 15 && y >= 6 && y <= 9);
                // Mount bracket
                bool inMount = (x >= 5 && x <= 10 && y >= 11 && y <= 15);

                pixels[y * size + x] = (inBody || inLens || inMount)
                    ? Color.white
                    : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }

    public bool IsAlerting() => hasDetected;
    public float GetAlertProgress()
    {
        if (!hasDetected) return 0f;
        float dur = isInLockdown ? lockdownAlertDuration : alertDuration;
        return Mathf.Clamp01(alertTimer / dur);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}