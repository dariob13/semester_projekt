using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ServerStars : MonoBehaviour
{
    private const int MaxStars = 3;
    private const int StarTextureSize = 64;
    private const float StarOuterRadius = 0.46f;
    private const float StarInnerRadius = 0.2f;
    private const float StarSize = 48f;
    private const float StarSpacing = 10f;
    private const float Padding = 16f;
    private const float PopDuration = 0.35f;
    private const float SettleDuration = 0.18f;
    private const float GlowDuration = 0.35f;

    private readonly Color starGray = new Color(0.5f, 0.5f, 0.5f, 1f);
    private readonly Color starGold = new Color(1f, 0.85f, 0.2f, 1f);
    private readonly Color glowColor = new Color(1f, 0.95f, 0.5f, 0.8f);

    private readonly List<Image> stars = new List<Image>(MaxStars);
    private Server[] allServers;
    private int currentStars = 0;
    private Sprite starSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureExists()
    {
        if (FindObjectOfType<ServerStars>() != null)
            return;

        GameObject go = new GameObject("ServerStarsController");
        go.AddComponent<ServerStars>();
    }

    void Start()
    {
        starSprite = CreateStarSprite(StarTextureSize, StarOuterRadius, StarInnerRadius);
        SetupUI();

        allServers = FindObjectsOfType<Server>();
        foreach (var server in allServers)
        {
            if (server.IsDestroyed() && currentStars < MaxStars)
            {
                stars[currentStars].color = starGold;
                currentStars++;
            }

            server.OnServerDestroyed += HandleServerDestroyed;
        }
    }

    private void SetupUI()
    {
        Canvas canvas = null;
        GameObject existingCanvas = GameObject.Find("ServerStarsCanvas");
        if (existingCanvas != null)
            canvas = existingCanvas.GetComponent<Canvas>();

        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("ServerStarsCanvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvasGo.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        GameObject containerGo = new GameObject("ServerStars");
        containerGo.transform.SetParent(canvas.transform, false);

        RectTransform container = containerGo.AddComponent<RectTransform>();
        container.anchorMin = new Vector2(0f, 1f);
        container.anchorMax = new Vector2(0f, 1f);
        container.pivot = new Vector2(0f, 1f);
        container.anchoredPosition = new Vector2(Padding, -Padding);

        HorizontalLayoutGroup layout = containerGo.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = StarSpacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        ContentSizeFitter fitter = containerGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < MaxStars; i++)
        {
            GameObject starGo = new GameObject($"Star_{i + 1}");
            starGo.transform.SetParent(container, false);

            Image starImage = starGo.AddComponent<Image>();
            starImage.sprite = starSprite;
            starImage.color = starGray;
            starImage.preserveAspect = true;
            starImage.raycastTarget = false;

            LayoutElement layoutElement = starGo.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = StarSize;
            layoutElement.preferredHeight = StarSize;

            RectTransform starRect = starGo.GetComponent<RectTransform>();
            starRect.sizeDelta = new Vector2(StarSize, StarSize);
            starRect.localScale = Vector3.one;

            stars.Add(starImage);
        }
    }

    private void HandleServerDestroyed()
    {
        if (currentStars >= MaxStars)
            return;

        StartCoroutine(AnimateStarUnlock(stars[currentStars]));
        currentStars++;
    }

    private IEnumerator AnimateStarUnlock(Image star)
    {
        if (star == null)
            yield break;

        RectTransform rect = star.rectTransform;
        rect.localScale = Vector3.one;

        GameObject glowGo = new GameObject("StarGlow");
        glowGo.transform.SetParent(rect, false);
        Image glow = glowGo.AddComponent<Image>();
        glow.sprite = starSprite;
        glow.color = glowColor;
        glow.raycastTarget = false;
        RectTransform glowRect = glowGo.GetComponent<RectTransform>();
        glowRect.sizeDelta = rect.sizeDelta;
        glowRect.localScale = Vector3.one * 1.6f;
        glowRect.SetAsFirstSibling();

        float elapsed = 0f;
        while (elapsed < PopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PopDuration);
            float pop = Mathf.SmoothStep(1f, 1.35f, t);
            rect.localScale = Vector3.one * pop;
            star.color = Color.Lerp(starGray, starGold, t);
            glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, Mathf.Lerp(glowColor.a, 0f, t));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < SettleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / SettleDuration);
            float scale = Mathf.SmoothStep(1.35f, 1f, t);
            rect.localScale = Vector3.one * scale;
            yield return null;
        }

        star.color = starGold;
        rect.localScale = Vector3.one;
        Destroy(glowGo, GlowDuration);
    }

    private Sprite CreateStarSprite(int size, float outerRadius, float innerRadius)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float outer = size * outerRadius;
        float inner = size * innerRadius;

        Vector2[] points = BuildStarPoints(outer, inner);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                bool inside = IsPointInPolygon(p, points);
                pixels[y * size + x] = inside ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private Vector2[] BuildStarPoints(float outer, float inner)
    {
        Vector2[] points = new Vector2[10];
        float angleStep = 36f;
        float startAngle = 90f;

        for (int i = 0; i < 10; i++)
        {
            float angle = (startAngle + angleStep * i) * Mathf.Deg2Rad;
            float radius = (i % 2 == 0) ? outer : inner;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        return points;
    }

    private bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int count = polygon.Length;
        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];

            bool intersect = ((pi.y > point.y) != (pj.y > point.y)) &&
                             (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y + 0.0001f) + pi.x);
            if (intersect)
                inside = !inside;
        }

        return inside;
    }

    void OnDestroy()
    {
        if (allServers == null)
            return;

        foreach (var server in allServers)
            server.OnServerDestroyed -= HandleServerDestroyed;
    }
}
