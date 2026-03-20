using UnityEngine;
using UnityEngine.Events;

public class WireConnectStation : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRadius = 2f;

    [Header("Rewards")]
    public Door[] doorsToOpenOnSuccess;
    public UnityEvent onMinigameComplete;

    public bool IsCompleted { get; private set; }
    public bool IsRunning { get; private set; }

    private WireConnectMinigameUI activeUI;

    public bool IsPlayerNearby(LiquidSolidForm player)
    {
        if (player == null) return false;
        Vector2 center = GetBlobCenter(player);
        return Vector2.Distance(transform.position, center) <= interactRadius;
    }

    public bool TryStart(LiquidSolidForm player, WireConnectMinigameUI ui)
    {
        if (IsCompleted || IsRunning) return false;
        if (!IsPlayerNearby(player)) return false;
        if (ui == null)
        {
            Debug.LogWarning("[WireConnect] No UI instance found; cannot start minigame.");
            return false;
        }

        IsRunning = true;
        activeUI = ui;
        activeUI.Show(this);
        return true;
    }

    public void Complete()
    {
        if (IsCompleted) return;

        IsCompleted = true;
        IsRunning = false;

        // Rewards
        if (doorsToOpenOnSuccess != null)
        {
            for (int i = 0; i < doorsToOpenOnSuccess.Length; i++)
            {
                if (doorsToOpenOnSuccess[i] != null)
                    doorsToOpenOnSuccess[i].Open();
            }
        }

        onMinigameComplete?.Invoke();
        if (activeUI != null)
        {
            activeUI.Hide();
            activeUI = null;
        }
    }

    public void Stop()
    {
        IsRunning = false;
        if (activeUI != null)
        {
            activeUI.Hide();
            activeUI = null;
        }
    }

    private Vector2 GetBlobCenter(LiquidSolidForm player)
    {
        LiquidParticle[] particles = player.GetComponentsInChildren<LiquidParticle>();
        if (particles.Length == 0) return player.transform.position;

        Vector2 center = Vector2.zero;
        foreach (var p in particles)
            center += (Vector2)p.transform.position;

        return center / particles.Length;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = IsCompleted ? new Color(0f, 1f, 0f, 0.25f) : new Color(1f, 0.8f, 0f, 0.25f);
        DrawCircle(transform.position, interactRadius, 24);
    }

    private void DrawCircle(Vector2 center, float radius, int segments)
    {
        float step = 360f / segments;
        Vector3 prev = center + new Vector2(radius, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * step * Mathf.Deg2Rad;
            Vector3 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
