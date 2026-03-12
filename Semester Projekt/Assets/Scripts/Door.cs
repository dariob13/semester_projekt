using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour
{
    [Header("Door Settings")]
    public bool startsOpen = false;
    public float slideDistance = 3f;
    public float slideSpeed = 4f;
    public DoorDirection slideDirection = DoorDirection.Up;

    [Header("Visual Settings")]
    public Color closedColor = new Color(0.5f, 0.3f, 0.1f, 1f);
    public Color openColor = new Color(0.3f, 0.6f, 0.3f, 0.4f);

    private bool isOpen = false;
    private bool isMoving = false;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D doorCollider;

    public enum DoorDirection { Up, Down, Left, Right }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = CreateDoorSprite();
        spriteRenderer.color = closedColor;
        spriteRenderer.sortingOrder = 1;

        doorCollider = GetComponent<BoxCollider2D>();
        if (doorCollider == null)
            doorCollider = gameObject.AddComponent<BoxCollider2D>();

        closedPosition = transform.position;
        openPosition = closedPosition + GetSlideVector() * slideDistance;

        if (startsOpen)
            Open();
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        StopAllCoroutines();
        StartCoroutine(SlideDoor(openPosition, openColor, false));
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        StopAllCoroutines();
        StartCoroutine(SlideDoor(closedPosition, closedColor, true));
    }

    private IEnumerator SlideDoor(Vector3 targetPos, Color targetColor, bool enableCollider)
    {
        isMoving = true;

        // Disable collider immediately when opening
        if (!enableCollider)
            doorCollider.enabled = false;

        Color startColor = spriteRenderer.color;
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        float duration = slideDistance / slideSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            spriteRenderer.color = Color.Lerp(startColor, targetColor, t);

            yield return null;
        }

        transform.position = targetPos;
        spriteRenderer.color = targetColor;

        // Re-enable collider after fully closed
        if (enableCollider)
            doorCollider.enabled = true;

        isMoving = false;
    }

    private Vector3 GetSlideVector()
    {
        switch (slideDirection)
        {
            case DoorDirection.Up:    return Vector3.up;
            case DoorDirection.Down:  return Vector3.down;
            case DoorDirection.Left:  return Vector3.left;
            case DoorDirection.Right: return Vector3.right;
            default:                  return Vector3.up;
        }
    }

    private Sprite CreateDoorSprite()
    {
        int w = 16;
        int h = 48;
        Texture2D texture = new Texture2D(w, h);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool border = (x == 0 || x == w - 1 || y == 0 || y == h - 1);
                bool panel1 = (x >= 2 && x <= 13 && y >= 3 && y <= 20);
                bool panel2 = (x >= 2 && x <= 13 && y >= 26 && y <= 44);
                bool handle = (x >= 10 && x <= 12 && y >= 22 && y <= 25);

                pixels[y * w + x] = (border || panel1 || panel2 || handle)
                    ? Color.white
                    : new Color(0.7f, 0.7f, 0.7f, 1f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
    }

    public bool IsOpen() => isOpen;
    public bool IsMoving() => isMoving;

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = isOpen
            ? new Color(0f, 1f, 0f, 0.3f)
            : new Color(1f, 0f, 0f, 0.3f);

        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 3f, 0.1f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + GetSlideVector() * slideDistance);
    }
}