using UnityEngine;

public class PulleyMinigame : MonoBehaviour
{
    [Header("Pulley Platforms")]
    public Rigidbody2D loadPlatform;
    public Rigidbody2D counterPlatform;

    [Header("Load Detection")]
    public LayerMask boxLayer;
    public Vector2 loadCheckSize = new Vector2(1.6f, 0.7f);
    public Vector2 loadCheckOffset = new Vector2(0f, 0.7f);
    public bool requireMovableObjectComponent = true;

    [Header("Activation Requirements")]
    public int minimumBoxesRequired = 1;
    public float minimumTotalWeight = 1f;
    public float extraForcePerExtraBox = 12f;
    public float extraForcePerExtraWeight = 10f;

    [Header("Physics Motion")]
    public float baseMotorForce = 28f;
    public float returnToStartForce = 20f;
    public float returnDamping = 6f;
    public float maxVerticalSpeed = 3.5f;
    public float maxTravelDistance = 3f;
    public bool returnToStartWhenInactive = true;

    [Header("Visual Rope + Wheel")]
    public bool buildVisualsAtRuntime = true;
    public Transform pulleyWheel;
    public Transform ropePivot;
    public Transform loadRopeAttach;
    public Transform counterRopeAttach;
    public LineRenderer ropeRenderer;
    public float wheelRadius = 0.35f;
    public float wheelSpinMultiplier = 1f;
    public float ropeWidth = 0.07f;
    public Color ropeColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    [Header("Debug State")]
    [SerializeField] private int currentBoxCount;
    [SerializeField] private float currentTotalWeight;
    [SerializeField] private bool isActive;

    private float loadStartX;
    private float counterStartX;
    private float loadStartY;
    private float counterStartY;
    private float previousLoadY;

    private void Start()
    {
        if (loadPlatform == null || counterPlatform == null)
        {
            Debug.LogError("PulleyMinigame: Assign both loadPlatform and counterPlatform Rigidbody2D references.");
            enabled = false;
            return;
        }

        loadStartX = loadPlatform.position.x;
        counterStartX = counterPlatform.position.x;
        loadStartY = loadPlatform.position.y;
        counterStartY = counterPlatform.position.y;

        // Platforms should move only up/down and stay upright for stable pulley behavior.
        loadPlatform.constraints |= RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        counterPlatform.constraints |= RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

        EnsureVisualReferences();
        previousLoadY = loadPlatform.position.y;
        UpdateRopeVisual();
    }

    private void FixedUpdate()
    {
        CountLoad();

        isActive = currentBoxCount >= minimumBoxesRequired && currentTotalWeight >= minimumTotalWeight;

        if (isActive)
        {
            DrivePulleyFromLoad();
        }
        else if (returnToStartWhenInactive)
        {
            ReturnPlatformsToStart();
        }

        ClampTravelAndSpeed();

        UpdateRopeVisual();
        RotateWheelFromRopeTravel();
    }

    private void EnsureVisualReferences()
    {
        if (loadRopeAttach == null)
            loadRopeAttach = loadPlatform.transform;

        if (counterRopeAttach == null)
            counterRopeAttach = counterPlatform.transform;

        if (pulleyWheel == null)
            pulleyWheel = transform;

        if (ropePivot == null)
            ropePivot = pulleyWheel;

        if (ropeRenderer == null && buildVisualsAtRuntime)
        {
            GameObject ropeObject = new GameObject("PulleyRope", typeof(LineRenderer));
            ropeObject.transform.SetParent(transform, false);
            ropeRenderer = ropeObject.GetComponent<LineRenderer>();
        }

        if (ropeRenderer != null)
        {
            if (ropeRenderer.material == null)
                ropeRenderer.material = new Material(Shader.Find("Sprites/Default"));

            ropeRenderer.positionCount = 3;
            ropeRenderer.startWidth = ropeWidth;
            ropeRenderer.endWidth = ropeWidth;
            ropeRenderer.startColor = ropeColor;
            ropeRenderer.endColor = ropeColor;
            ropeRenderer.useWorldSpace = true;
            ropeRenderer.sortingOrder = 10;
        }
    }

    private void UpdateRopeVisual()
    {
        if (ropeRenderer == null)
            return;

        Vector3 leftPoint = loadRopeAttach != null ? loadRopeAttach.position : loadPlatform.transform.position;
        Vector3 pivotPoint = ropePivot != null ? ropePivot.position : transform.position;
        Vector3 rightPoint = counterRopeAttach != null ? counterRopeAttach.position : counterPlatform.transform.position;

        ropeRenderer.SetPosition(0, leftPoint);
        ropeRenderer.SetPosition(1, pivotPoint);
        ropeRenderer.SetPosition(2, rightPoint);
    }

    private void RotateWheelFromRopeTravel()
    {
        if (pulleyWheel == null)
            return;

        float currentLoadY = loadPlatform.position.y;
        float frameTravel = currentLoadY - previousLoadY;
        previousLoadY = currentLoadY;

        float ropeDistance = Mathf.Abs(frameTravel);

        float effectiveRadius = Mathf.Max(0.01f, wheelRadius);
        float circumference = Mathf.PI * 2f * effectiveRadius;
        float turns = ropeDistance / circumference;
        float degrees = turns * 360f * wheelSpinMultiplier;

        bool loadDescending = frameTravel < 0f;
        float signedDegrees = loadDescending ? -degrees : degrees;
        pulleyWheel.Rotate(0f, 0f, signedDegrees);
    }

    private void CountLoad()
    {
        currentBoxCount = 0;
        currentTotalWeight = 0f;

        Vector2 center = loadPlatform.position + loadCheckOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, loadCheckSize, 0f, boxLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;

            Rigidbody2D rb = hit.attachedRigidbody;
            if (rb == null) continue;

            float topY = loadPlatform.position.y + (loadCheckSize.y * 0.5f) + loadCheckOffset.y;
            if (rb.worldCenterOfMass.y < topY - loadCheckSize.y)
                continue;

            float itemWeight = rb.mass;
            MovableObject movable = rb.GetComponent<MovableObject>();

            if (requireMovableObjectComponent && movable == null)
                continue;

            if (movable != null)
                itemWeight = Mathf.Max(0.01f, movable.weightMultiplier);

            currentBoxCount++;
            currentTotalWeight += itemWeight;
        }
    }

    private void DrivePulleyFromLoad()
    {
        float loadTravel = loadPlatform.position.y - loadStartY;
        float counterTravel = counterPlatform.position.y - counterStartY;

        bool canGoDown = loadTravel > -maxTravelDistance + 0.01f;
        bool canCounterGoUp = counterTravel < maxTravelDistance - 0.01f;

        if (!canGoDown || !canCounterGoUp)
            return;

        int extraBoxes = Mathf.Max(0, currentBoxCount - minimumBoxesRequired);
        float extraWeight = Mathf.Max(0f, currentTotalWeight - minimumTotalWeight);

        float totalForce = baseMotorForce + (extraBoxes * extraForcePerExtraBox) + (extraWeight * extraForcePerExtraWeight);

        loadPlatform.AddForce(Vector2.down * totalForce, ForceMode2D.Force);
        counterPlatform.AddForce(Vector2.up * totalForce, ForceMode2D.Force);
    }

    private void ReturnPlatformsToStart()
    {
        float loadError = loadStartY - loadPlatform.position.y;
        float counterError = counterStartY - counterPlatform.position.y;

        float loadForce = (loadError * returnToStartForce) - (loadPlatform.linearVelocity.y * returnDamping);
        float counterForce = (counterError * returnToStartForce) - (counterPlatform.linearVelocity.y * returnDamping);

        loadPlatform.AddForce(Vector2.up * loadForce, ForceMode2D.Force);
        counterPlatform.AddForce(Vector2.up * counterForce, ForceMode2D.Force);
    }

    private void ClampTravelAndSpeed()
    {
        Vector2 loadPos = loadPlatform.position;
        Vector2 counterPos = counterPlatform.position;

        loadPos.x = loadStartX;
        counterPos.x = counterStartX;

        loadPos.y = Mathf.Clamp(loadPos.y, loadStartY - maxTravelDistance, loadStartY + maxTravelDistance);
        counterPos.y = Mathf.Clamp(counterPos.y, counterStartY - maxTravelDistance, counterStartY + maxTravelDistance);

        loadPlatform.position = loadPos;
        counterPlatform.position = counterPos;

        Vector2 loadVel = loadPlatform.linearVelocity;
        Vector2 counterVel = counterPlatform.linearVelocity;

        loadVel.x = 0f;
        counterVel.x = 0f;
        loadVel.y = Mathf.Clamp(loadVel.y, -maxVerticalSpeed, maxVerticalSpeed);
        counterVel.y = Mathf.Clamp(counterVel.y, -maxVerticalSpeed, maxVerticalSpeed);

        loadPlatform.linearVelocity = loadVel;
        counterPlatform.linearVelocity = counterVel;
    }

    public int GetCurrentBoxCount() => currentBoxCount;
    public float GetCurrentTotalWeight() => currentTotalWeight;
    public bool IsPulleyActive() => isActive;

    private void OnDrawGizmosSelected()
    {
        Rigidbody2D reference = loadPlatform != null ? loadPlatform : GetComponent<Rigidbody2D>();
        if (reference == null)
            return;

        Vector3 center = (Vector3)reference.position + (Vector3)loadCheckOffset;
        Gizmos.color = isActive
            ? new Color(0.2f, 1f, 0.3f, 0.35f)
            : new Color(1f, 0.75f, 0.1f, 0.25f);
        Gizmos.DrawCube(center, loadCheckSize);

        if (ropePivot != null)
        {
            Gizmos.color = new Color(0.8f, 0.95f, 1f, 0.6f);
            Gizmos.DrawWireSphere(ropePivot.position, Mathf.Max(0.05f, wheelRadius));
        }
    }
}
