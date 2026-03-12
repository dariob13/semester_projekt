using UnityEngine;

public class PressurePad : MonoBehaviour
{
    [Header("Pressure Pad Settings")]
    public Door linkedDoor;
    public float detectionHeight = 0.3f;
    public float activationDelay = 0f;

    [Header("Visual Settings")]
    public Color inactiveColor = new Color(0.6f, 0.4f, 0.1f, 1f);
    public Color activeColor = new Color(0.1f, 1f, 0.3f, 1f);

    private bool isActivated = false;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D padCollider;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = CreatePadSprite();
        spriteRenderer.color = inactiveColor;
        spriteRenderer.sortingOrder = 0;

        padCollider = GetComponent<BoxCollider2D>();
        if (padCollider == null)
            padCollider = gameObject.AddComponent<BoxCollider2D>();

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

    private Sprite CreatePadSprite()
    {
        int w = 32;
        int h = 8;
        Texture2D texture = new Texture2D(w, h);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Outer border
                bool border = (x == 0 || x == w - 1 || y == 0 || y == h - 1);
                // Arrow markers in center
                bool marker = (y >= 2 && y <= 5 && (x == 10 || x == 15 || x == 21));

                pixels[y * w + x] = (border || marker) ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
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