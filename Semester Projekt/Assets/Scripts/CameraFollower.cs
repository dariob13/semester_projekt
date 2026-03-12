using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public float followSpeed = 8f;
    public bool followX = true;
    public bool followY = true;

    [Header("Offset")]
    public float xOffset = 0f;
    public float yOffset = 1f;

    [Header("Bounds (Optional)")]
    public bool useBounds = false;
    public float minX = -20f;
    public float maxX = 20f;
    public float minY = -10f;
    public float maxY = 10f;

    private LiquidSolidForm player;
    private LiquidParticle[] particles;

    void Start()
    {
        player = FindObjectOfType<LiquidSolidForm>();

        if (player == null)
        {
            Debug.LogError("CameraFollower: LiquidSolidForm not found!");
            return;
        }

        particles = player.GetComponentsInChildren<LiquidParticle>();
        Debug.Log($"CameraFollower: Found {particles.Length} particles to track");

        // Snap to blob immediately on start
        Vector2 startBlobCenter = GetBlobCenter();
        transform.position = new Vector3(
            startBlobCenter.x + xOffset,
            startBlobCenter.y + yOffset,
            -10f
        );
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Refresh particles in case they changed
        if (particles == null || particles.Length == 0)
        {
            particles = player.GetComponentsInChildren<LiquidParticle>();
        }

        Vector2 blobCenter = GetBlobCenter();

        Vector3 targetPos = transform.position;

        if (followX)
        {
            targetPos.x = blobCenter.x + xOffset;
        }

        if (followY)
        {
            targetPos.y = blobCenter.y + yOffset;
        }

        targetPos.z = -10f;

        // Apply bounds if enabled
        if (useBounds)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
        }

        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
    }

    private Vector2 GetBlobCenter()
    {
        if (particles == null || particles.Length == 0)
            return player.transform.position;

        Vector2 center = Vector2.zero;
        foreach (var particle in particles)
        {
            center += (Vector2)particle.transform.position;
        }

        return center / particles.Length;
    }
}