using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class PressurePad : MonoBehaviour
{
    [Header("Pressure Pad Settings")]
    public Door linkedDoor;
    public float detectionHeight = 0.3f;
    public float activationDelay = 0f;

    [Header("Visual Settings")]
    public Color inactiveColor = new Color(0.6f, 0.4f, 0.1f, 1f);
    public Color activeColor = new Color(0.1f, 1f, 0.3f, 1f);

    [SerializeField] private SpriteRenderer spriteRenderer;

    private bool isActivated = false;
    private BoxCollider2D padCollider;

    void OnValidate()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            if (spriteRenderer.sprite == null)
                Debug.LogWarning("PressurePad is missing a Sprite on its SpriteRenderer.", this);

            spriteRenderer.color = inactiveColor;
            spriteRenderer.sortingOrder = 0;
        }

        padCollider = GetComponent<BoxCollider2D>();

        padCollider.isTrigger = true;
        padCollider.size = new Vector2(1f, detectionHeight);
        padCollider.offset = new Vector2(0f, detectionHeight / 2f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isActivated) return;

        MovableObject box = other.GetComponent<MovableObject>();
        if (box == null || !box.canBeMoved) return;

        isActivated = true;
        UpdateVisual();

        if (activationDelay > 0f)
            Invoke(nameof(OpenLinkedDoor), activationDelay);
        else
            OpenLinkedDoor();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        MovableObject box = other.GetComponent<MovableObject>();
        if (box == null) return;

        // Check if any other box is still on the pad
        if (!IsBoxStillOnPad())
        {
            isActivated = false;
            UpdateVisual();
            CancelInvoke(nameof(OpenLinkedDoor));
            CloseLinkedDoor();
        }
    }

    bool IsBoxStillOnPad()
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            (Vector2)transform.position + padCollider.offset,
            padCollider.size,
            0f
        );

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (hit.GetComponent<MovableObject>() != null)
                return true;
        }

        return false;
    }

    void OpenLinkedDoor()
    {
        if (linkedDoor != null)
            linkedDoor.Open();
    }

    void CloseLinkedDoor()
    {
        if (linkedDoor != null)
            linkedDoor.Close();
    }

    void UpdateVisual()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = isActivated ? activeColor : inactiveColor;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isActivated
            ? new Color(0.1f, 1f, 0.3f, 0.4f)
            : new Color(1f, 0.6f, 0f, 0.3f);

        Vector3 center = transform.position + new Vector3(0, detectionHeight / 2f, 0);
        Gizmos.DrawCube(center, new Vector3(1f, detectionHeight, 0.1f));

        if (linkedDoor != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, linkedDoor.transform.position);
        }
    }
}