using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public float followSpeed = 10f;
    public bool followX = true;
    public bool followY = false;

    [Header("Offset")]
    public float xOffset = 0f;
    public float yOffset = 2f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraFollower: No Camera component found!");
            return;
        }

        if (target == null)
        {
            LiquidSolidForm player = FindObjectOfType<LiquidSolidForm>();
            if (player != null)
            {
                target = player.transform;
                Debug.Log($"CameraFollower: Found player at {target.position}!");
            }
        }

        // Set initial camera position
        if (target != null)
        {
            Vector3 startPos = target.position;
            startPos.z = -10f;
            transform.position = startPos;
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 newPos = transform.position;

        // Direct follow (no lerp)
        if (followX)
        {
            newPos.x = target.position.x + xOffset;
        }

        if (followY)
        {
            newPos.y = target.position.y + yOffset;
        }

        newPos.z = -10f;

        // Apply immediately
        transform.position = newPos;

        Debug.Log($"[Camera] Camera: {transform.position}, Player: {target.position}");
    }
}