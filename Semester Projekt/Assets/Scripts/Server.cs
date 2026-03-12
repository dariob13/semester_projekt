using UnityEngine;
using System.Collections;

public class Server : MonoBehaviour
{
    [Header("Server Settings")]
    public float destroyTime = 5f;
    public float detectionRadius = 2f;

    [Header("Visual Settings")]
    public Color intactColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color damagedColor = new Color(1f, 0.5f, 0f, 1f);
    public Color destroyedColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [Header("FX Settings")]
    public int smokeParticleCount = 25;
    public int explosionParticleCount = 40;
    public Color smokeColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color explosionColor = new Color(1f, 0.4f, 0f, 1f);
    public Color sparkColor = new Color(1f, 1f, 0.2f, 1f);

    private bool isDestroyed = false;
    private float destroyTimer = 0f;
    private bool playerInRange = false;

    private SpriteRenderer spriteRenderer;
    private LiquidSolidForm cachedPlayer;
    private Coroutine smokeLoopCoroutine;

    public event System.Action OnServerDestroyed;

    void Start()
    {
        cachedPlayer = FindObjectOfType<LiquidSolidForm>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = CreateServerSprite(false);
        spriteRenderer.color = intactColor;
        spriteRenderer.sortingOrder = 1;
    }

    void Update()
    {
        if (isDestroyed) return;
        if (cachedPlayer == null)
        {
            cachedPlayer = FindObjectOfType<LiquidSolidForm>();
            return;
        }

        CheckPlayerInRange();
        UpdateDestroyProgress();
    }

    void CheckPlayerInRange()
    {
        // Only gas state can destroy servers
        if (cachedPlayer.GetCurrentState() != MatterState.Gas)
        {
            playerInRange = false;
            return;
        }

        Vector2 blobCenter = GetBlobCenter(cachedPlayer);
        float dist = Vector2.Distance(transform.position, blobCenter);
        playerInRange = dist <= detectionRadius;
    }

    void UpdateDestroyProgress()
    {
        if (playerInRange)
        {
            destroyTimer += Time.deltaTime;

            // Visual feedback as timer progresses
            float progress = destroyTimer / destroyTime;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(intactColor, damagedColor, progress);
            }

            // Start smoke loop when damage begins
            if (destroyTimer > 0.1f && smokeLoopCoroutine == null)
            {
                smokeLoopCoroutine = StartCoroutine(SmokeLoop());
            }

            if (destroyTimer >= destroyTime)
            {
                DestroyServer();
            }
        }
        else
        {
            // Cool down when player leaves
            destroyTimer = Mathf.Max(0f, destroyTimer - Time.deltaTime * 1.5f);

            float progress = destroyTimer / destroyTime;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(intactColor, damagedColor, progress);
            }

            if (destroyTimer <= 0f && smokeLoopCoroutine != null)
            {
                StopCoroutine(smokeLoopCoroutine);
                smokeLoopCoroutine = null;
            }
        }
    }

    void DestroyServer()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (smokeLoopCoroutine != null)
        {
            StopCoroutine(smokeLoopCoroutine);
            smokeLoopCoroutine = null;
        }

        // Switch to destroyed sprite and color
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = CreateServerSprite(true);
            spriteRenderer.color = destroyedColor;
        }

        // Spawn FX
        StartCoroutine(PlayDestroyFX());

        OnServerDestroyed?.Invoke();
        Debug.Log($"*** SERVER at {transform.position} DESTROYED! ***");
    }

    private IEnumerator PlayDestroyFX()
    {
        // Big explosion first
        SpawnExplosion(transform.position);

        // Shockwave of sparks
        SpawnSparks(transform.position);

        yield return new WaitForSeconds(0.15f);

        // Secondary smaller explosions
        for (int i = 0; i < 3; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 0.4f;
            SpawnExplosion((Vector2)transform.position + offset);
            yield return new WaitForSeconds(0.1f);
        }

        // Long smoke trail after explosion
        for (int i = 0; i < 5; i++)
        {
            SpawnSmokeCloud(transform.position);
            yield return new WaitForSeconds(0.08f);
        }
    }

    private IEnumerator SmokeLoop()
    {
        while (!isDestroyed && destroyTimer > 0f)
        {
            // Light smoke while being damaged
            SpawnSingleSmoke(transform.position, smokeColor * 0.5f, 0.15f);
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void SpawnExplosion(Vector2 position)
    {
        for (int i = 0; i < explosionParticleCount; i++)
        {
            GameObject p = new GameObject("ExplosionParticle");
            p.transform.position = position;

            SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = (i % 3 == 0) ? sparkColor : explosionColor;
            sr.sortingOrder = 10;

            float scale = Random.Range(0.05f, 0.25f);
            p.transform.localScale = Vector3.one * scale;
            p.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            StartCoroutine(AnimateExplosionParticle(p, sr));
        }
    }

    private IEnumerator AnimateExplosionParticle(GameObject particle, SpriteRenderer sr)
    {
        float speed = Random.Range(3f, 8f);
        Vector2 dir = Random.insideUnitCircle.normalized;
        Vector2 velocity = dir * speed;
        float lifetime = Random.Range(0.3f, 0.7f);
        float elapsed = 0f;
        Color startColor = sr.color;
        float startScale = particle.transform.localScale.x;
        float spinSpeed = Random.Range(-360f, 360f);

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;

            // Move outward and decelerate
            particle.transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity *= 0.88f;
            velocity.y -= 2f * Time.deltaTime; // Gravity on sparks

            // Spin
            particle.transform.Rotate(0, 0, spinSpeed * Time.deltaTime);

            // Fade and shrink
            sr.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, t));
            float scale = Mathf.Lerp(startScale, 0f, t * t);
            particle.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        Destroy(particle);
    }

    private void SpawnSparks(Vector2 position)
    {
        for (int i = 0; i < 15; i++)
        {
            GameObject p = new GameObject("SparkParticle");
            float angle = (360f / 15f) * i;
            p.transform.position = position;

            SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = sparkColor;
            sr.sortingOrder = 11;
            p.transform.localScale = Vector3.one * 0.08f;

            StartCoroutine(AnimateSparkParticle(p, sr, angle));
        }
    }

    private IEnumerator AnimateSparkParticle(GameObject particle, SpriteRenderer sr, float angleDeg)
    {
        float angleRad = angleDeg * Mathf.Deg2Rad;
        Vector2 velocity = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * 5f;
        float lifetime = 0.5f;
        float elapsed = 0f;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;

            particle.transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity.y -= 5f * Time.deltaTime;
            velocity *= 0.95f;

            sr.color = new Color(1f, 1f, 0.2f, Mathf.Lerp(1f, 0f, t));
            float scale = Mathf.Lerp(0.08f, 0.02f, t);
            particle.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        Destroy(particle);
    }

    private void SpawnSmokeCloud(Vector2 position)
    {
        for (int i = 0; i < smokeParticleCount; i++)
        {
            SpawnSingleSmoke(position, smokeColor, 0.8f);
        }
    }

    private void SpawnSingleSmoke(Vector2 position, Color color, float maxScale)
    {
        GameObject p = new GameObject("SmokeParticle");
        p.transform.position = position + Random.insideUnitCircle * 0.2f;

        SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = new Color(color.r, color.g, color.b, 0.9f);
        sr.sortingOrder = 8;

        float scale = Random.Range(0.1f, 0.3f);
        p.transform.localScale = Vector3.one * scale;
        p.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 90f));

        StartCoroutine(AnimateSmokeParticle(p, sr, maxScale));
    }

    private IEnumerator AnimateSmokeParticle(GameObject particle, SpriteRenderer sr, float maxScale)
    {
        Vector2 velocity = Random.insideUnitCircle * 1.5f;
        float lifetime = Random.Range(0.5f, 1.2f);
        float elapsed = 0f;
        float startScale = particle.transform.localScale.x;
        float spinSpeed = Random.Range(-90f, 90f);
        Color startColor = sr.color;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;

            particle.transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity *= 0.96f;
            particle.transform.position += Vector3.up * Time.deltaTime * 1.2f;
            particle.transform.Rotate(0, 0, spinSpeed * Time.deltaTime);

            sr.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));
            float scale = Mathf.Lerp(startScale, startScale * maxScale * 4f, t);
            particle.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        Destroy(particle);
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

    private Sprite CreateServerSprite(bool destroyed)
    {
        int size = 16;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Server body
                bool inBody = (x >= 1 && x <= 14 && y >= 1 && y <= 14);
                // Rack lines
                bool inRack1 = (y == 4 || y == 8 || y == 12) && inBody;
                // Status lights
                bool inLight1 = (x >= 11 && x <= 12 && y >= 5 && y <= 6);
                bool inLight2 = (x >= 11 && x <= 12 && y >= 9 && y <= 10);
                // Drive bays
                bool inBay = (x >= 2 && x <= 9 && (y >= 2 && y <= 3 || y >= 5 && y <= 7 || y >= 9 && y <= 11));
                // Damage cracks if destroyed
                bool inCrack = destroyed && (
                    (x == 5 && y >= 3 && y <= 8) ||
                    (x == 6 && y >= 5 && y <= 10) ||
                    (x == 10 && y >= 2 && y <= 7)
                );

                if (inCrack)
                    pixels[y * size + x] = Color.clear;
                else if (inRack1 || inBody)
                    pixels[y * size + x] = Color.white;
                else
                    pixels[y * size + x] = Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }

    private Sprite CreateSquareSprite()
    {
        int size = 8;
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 8f);
    }

    public bool IsDestroyed() => isDestroyed;
    public float GetDestroyProgress() => Mathf.Clamp01(destroyTimer / destroyTime);
    public bool IsPlayerInRange() => playerInRange;

    void OnDrawGizmos()
    {
        Gizmos.color = isDestroyed
            ? new Color(0.5f, 0.5f, 0.5f, 0.3f)
            : new Color(0f, 1f, 0f, 0.3f);

        DrawCircle(transform.position, detectionRadius, 32);
    }

    private void DrawCircle(Vector2 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prev = center + new Vector2(radius, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}