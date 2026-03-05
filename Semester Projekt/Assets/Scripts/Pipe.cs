using UnityEngine;

public class Pipe : MonoBehaviour
{
    [Header("Pipe Settings")]
    public Transform exitPoint;
    public float detectionRadius = 2f;
    public float traversalTime = 1f;
    public LayerMask playerLayer;

    [Header("Visual Settings")]
    public Color pipeColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);

    private bool isOccupied = false;

    void Start()
    {
        if (exitPoint == null)
        {
            Debug.LogError($"Pipe at {transform.position} has no exit point assigned!");
        }

        // Visual setup
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = pipeColor;
        }
    }

    public bool TryEnterPipe(LiquidSolidForm player)
    {
        if (isOccupied)
            return false;

        // Only liquid form can enter pipes
        if (player.GetCurrentState() != MatterState.Liquid)
        {
            Debug.Log("Can only enter pipes in liquid form!");
            return false;
        }

        isOccupied = true;
        StartCoroutine(TraversePipe(player));
        return true;
    }

    private System.Collections.IEnumerator TraversePipe(LiquidSolidForm player)
    {
        // Hide player during traversal
        SetPlayerVisibility(player, false);

        // Wait for traversal time
        yield return new WaitForSeconds(traversalTime);

        // Teleport to exit
        if (exitPoint != null)
        {
            player.transform.position = exitPoint.position;
        }

        // Show player again
        SetPlayerVisibility(player, true);

        isOccupied = false;
    }

    private void SetPlayerVisibility(LiquidSolidForm player, bool visible)
    {
        LiquidParticle[] particles = player.GetComponentsInChildren<LiquidParticle>();
        foreach (var particle in particles)
        {
            SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = visible;
            }
        }
    }

    public bool IsPlayerNearby()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius);

        foreach (var col in colliders)
        {
            if (col.GetComponent<LiquidSolidForm>() != null)
                return true;
        }

        return false;
    }

    void OnDrawGizmos()
    {
        // Draw detection radius
        Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
        DrawCircle(transform.position, detectionRadius, 32);

        // Draw exit point connection
        if (exitPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, exitPoint.position);
            Gizmos.DrawCube(exitPoint.position, Vector3.one * 0.3f);
        }
    }

    private void DrawCircle(Vector2 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector2(radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}