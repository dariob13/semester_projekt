using UnityEngine;

public class WaterHeatingMinigameStation : MonoBehaviour
{
    [Header("Interaction")]
    public float interactionRadius = 2f;

    [Header("Temperature Rules")]
    [Range(0f, 1f)] public float startTemperature = 0.35f;
    [Range(0f, 1f)] public float minSafeThreshold = 0.45f;
    [Range(0f, 1f)] public float maxSafeThreshold = 0.65f;
    public float heatRatePerSecond = 0.35f;
    public float coolRatePerSecond = 0.25f;
    public float successStableTime = 5f;
    public float failOutsideTime = 1.5f;

    [Header("Door Rewards")]
    public Door[] doorsToOpenOnSuccess;

    [Header("Visual States")]
    public Color idleColor = new Color(0.3f, 0.5f, 1f, 1f);
    public Color activeColor = new Color(1f, 0.8f, 0.2f, 1f);
    public Color successColor = new Color(0.2f, 1f, 0.3f, 1f);
    public Color failColor = new Color(1f, 0.2f, 0.2f, 1f);

    private float temperature;
    private float stableTimer;
    private float aboveTimer;
    private float belowTimer;

    private bool isRunning;
    private bool isCompleted;
    private bool isHeatingInput;

    private WaterHeaterMinigameUI currentUI;
    private SpriteRenderer spriteRenderer;

    public bool IsRunning => isRunning;
    public bool IsCompleted => isCompleted;

    void Start()
    {
        temperature = Mathf.Clamp01(startTemperature);

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = CreateStationSprite();

        spriteRenderer.color = idleColor;
    }

    void Update()
    {
        if (!isRunning)
            return;

        float dt = Time.deltaTime;

        // Temperature dynamics
        if (isHeatingInput)
            temperature += heatRatePerSecond * dt;
        else
            temperature -= coolRatePerSecond * dt;

        temperature = Mathf.Clamp01(temperature);

        bool inSafe = temperature >= minSafeThreshold && temperature <= maxSafeThreshold;

        if (inSafe)
        {
            stableTimer += dt;
            aboveTimer = 0f;
            belowTimer = 0f;
        }
        else if (temperature > maxSafeThreshold)
        {
            aboveTimer += dt;
            stableTimer = Mathf.Max(0f, stableTimer - dt * 0.5f);
            belowTimer = 0f;

            if (aboveTimer >= failOutsideTime)
            {
                Fail("Too hot! Turned to steam.");
                return;
            }
        }
        else
        {
            belowTimer += dt;
            stableTimer = Mathf.Max(0f, stableTimer - dt * 0.5f);
            aboveTimer = 0f;

            if (belowTimer >= failOutsideTime)
            {
                Fail("Too cold! Water froze.");
                return;
            }
        }

        if (currentUI != null)
            currentUI.SetProgress(temperature, minSafeThreshold, maxSafeThreshold, stableTimer, successStableTime);

        if (stableTimer >= successStableTime)
            Succeed();
    }

    public bool IsPlayerNearby(LiquidSolidForm player)
    {
        if (player == null) return false;

        Vector2 center = GetBlobCenter(player);
        return Vector2.Distance(transform.position, center) <= interactionRadius;
    }

    public bool TryStart(LiquidSolidForm player, WaterHeaterMinigameUI ui)
    {
        if (isCompleted || isRunning) return false;
        if (!IsPlayerNearby(player)) return false;

        isRunning = true;
        currentUI = ui;

        temperature = Mathf.Clamp01(startTemperature);
        stableTimer = 0f;
        aboveTimer = 0f;
        belowTimer = 0f;
        isHeatingInput = false;

        if (spriteRenderer != null)
            spriteRenderer.color = activeColor;

        currentUI?.Show();
        return true;
    }

    public void SetHeatingInput(bool heating)
    {
        isHeatingInput = heating;
    }

    private void Succeed()
    {
        isRunning = false;
        isCompleted = true;

        if (spriteRenderer != null)
            spriteRenderer.color = successColor;

        for (int i = 0; i < doorsToOpenOnSuccess.Length; i++)
        {
            if (doorsToOpenOnSuccess[i] != null)
                doorsToOpenOnSuccess[i].Open();
        }

        currentUI?.ShowResult("Success! Doors unlocked.", successColor, 2f);
        currentUI = null;
    }

    private void Fail(string reason)
    {
        isRunning = false;

        if (spriteRenderer != null)
            spriteRenderer.color = failColor;

        currentUI?.ShowResult(reason, failColor, 2f);
        currentUI = null;

        // Reset visual after short delay so player can retry
        Invoke(nameof(ResetToIdle), 2f);
    }

    private void ResetToIdle()
    {
        if (!isCompleted && spriteRenderer != null)
            spriteRenderer.color = idleColor;
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

    private Sprite CreateStationSprite()
    {
        int w = 16;
        int h = 16;
        Texture2D texture = new Texture2D(w, h);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool border = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                bool tank = x >= 3 && x <= 12 && y >= 4 && y <= 12;
                bool heater = x >= 5 && x <= 10 && y >= 1 && y <= 3;
                bool gauge = x >= 6 && x <= 9 && y >= 13 && y <= 14;

                pixels[y * w + x] = (border || tank || heater || gauge)
                    ? Color.white
                    : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isCompleted
            ? new Color(0f, 1f, 0f, 0.25f)
            : new Color(1f, 0.8f, 0f, 0.25f);

        DrawCircle(transform.position, interactionRadius, 32);
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
