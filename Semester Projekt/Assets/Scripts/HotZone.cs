using UnityEngine;

public class HotZone : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color zoneColor = new Color(1f, 0.3f, 0f, 0.3f);

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // Ensure there is a collider set as trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }

        // Optional: tint the sprite to show the hot zone
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = zoneColor;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.DrawCube(transform.position, box.size * transform.localScale);
        }
        else
        {
            Gizmos.DrawCube(transform.position, transform.localScale);
        }
    }
}