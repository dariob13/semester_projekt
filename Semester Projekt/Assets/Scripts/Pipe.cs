using UnityEngine;
using System.Collections;

public class Pipe : MonoBehaviour
{
    [Header("Pipe Settings")]
    public Transform exitPoint;
    public float detectionRadius = 3f;
    public float traversalTime = 0.8f;

    [Header("Smoke FX Settings")]
    public Color smokeColor = new Color(0.4f, 0.7f, 1f, 0.8f);
    public int smokeParticleCount = 20;
    public float smokeSpread = 1.5f;
    public float smokeFadeTime = 0.6f;

    private bool isOccupied = false;
    private LiquidSolidForm cachedPlayer;

    void Start()
    {
        if (exitPoint == null)
            Debug.LogError($"Pipe at {transform.position} has no exit point assigned!");

        cachedPlayer = FindObjectOfType<LiquidSolidForm>();
    }

    public bool IsPlayerNearby()
    {
        if (cachedPlayer == null)
        {
            cachedPlayer = FindObjectOfType<LiquidSolidForm>();
            if (cachedPlayer == null) return false;
        }

        // Use blob center for accurate proximity check
        Vector2 blobCenter = GetBlobCenter(cachedPlayer);
        float dist = Vector2.Distance(transform.position, blobCenter);
        return dist <= detectionRadius;
    }

    public bool TryEnterPipe(LiquidSolidForm player)
    {
        if (isOccupied) return false;
        if (player.GetCurrentState() != MatterState.Liquid) return false;

        isOccupied = true;
        StartCoroutine(TraversePipe(player));
        return true;
    }

    private IEnumerator TraversePipe(LiquidSolidForm player)
    {
        // Play smoke at entry
        SpawnSmokeEffect(transform.position, smokeColor);

        // Hide player
        SetPlayerVisibility(player, false);

        // Wait for traversal
        yield return new WaitForSeconds(traversalTime);

        // Teleport all particles to exit
        if (exitPoint != null)
        {
            MovePlayerToExit(player);
        }

        // Play smoke at exit
        SpawnSmokeEffect(exitPoint.position, smokeColor);

        // Small delay before showing player
        yield return new WaitForSeconds(0.1f);

        // Show player
        SetPlayerVisibility(player, true);

        isOccupied = false;
    }

    private void MovePlayerToExit(LiquidSolidForm player)
    {
        LiquidParticle[] particles = player.GetComponentsInChildren<LiquidParticle>();
        Vector2 blobCenter = GetBlobCenter(player);
        Vector2 offset = (Vector2)exitPoint.position - blobCenter;

        foreach (var particle in particles)
        {
            particle.transform.position += (Vector3)offset;
            particle.velocity = Vector2.zero;
        }
    }

    private void SpawnSmokeEffect(Vector2 position, Color color)
    {
        for (int i = 0; i < smokeParticleCount; i++)
        {
            GameObject smokeParticle = new GameObject("SmokeParticle");
            smokeParticle.transform.position = position + Random.insideUnitCircle * 0.3f;

            SpriteRenderer sr = smokeParticle.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = new Color(color.r, color.g, color.b, 1f);
            sr.sortingOrder = 5;

            float scale = Random.Range(0.4f, 0.9f);
            smokeParticle.transform.localScale = Vector3.one * scale;

            // Random rotation for variety
            smokeParticle.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 90f));

            StartCoroutine(AnimateSmokeParticle(smokeParticle, sr));
        }
    }

    private IEnumerator AnimateSmokeParticle(GameObject particle, SpriteRenderer sr)
    {
        Vector2 velocity = Random.insideUnitCircle * smokeSpread;
        float elapsed = 0f;
        Color startColor = sr.color;
        float startScale = particle.transform.localScale.x;

        // Random spin direction
        float spinSpeed = Random.Range(-180f, 180f);

        while (elapsed < smokeFadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / smokeFadeTime;

            // Move outward
            particle.transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity *= 0.92f;

            // Float upward
            particle.transform.position += Vector3.up * Time.deltaTime * 1.5f;

            // Spin the square
            particle.transform.Rotate(0, 0, spinSpeed * Time.deltaTime);

            // Fade out and scale up
            sr.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, t));
            float scale = Mathf.Lerp(startScale, startScale * 3.5f, t);
            particle.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        Destroy(particle);
    }

    private void SetPlayerVisibility(LiquidSolidForm player, bool visible)
    {
        LiquidParticle[] particles = player.GetComponentsInChildren<LiquidParticle>();
        foreach (var particle in particles)
        {
            SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.enabled = visible;
        }
    }

    private Vector2 GetBlobCenter(LiquidSolidForm player)
    {
        LiquidParticle[] particles = player.GetComponentsInChildren<LiquidParticle>();

        if (particles.Length == 0)
            return player.transform.position;

        Vector2 center = Vector2.zero;
        foreach (var particle in particles)
            center += (Vector2)particle.transform.position;

        return center / particles.Length;
    }

    private Sprite CreateSquareSprite()
    {
        int size = 16;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }

    private Sprite CreateCircleSprite()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - dist / radius);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
        DrawCircle(transform.position, detectionRadius, 32);

        if (exitPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, exitPoint.position);
            Gizmos.DrawWireSphere(exitPoint.position, 0.4f);
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