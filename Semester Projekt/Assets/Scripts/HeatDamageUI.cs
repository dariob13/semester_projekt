using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[DefaultExecutionOrder(-200)]
public class HeatDamageUI : MonoBehaviour
{
    [Header("UI References")]
    public Image heatBarFill;
    public Image heatBarBackground;
    public Image heatBarFrame;
    public Image heatBarGlow;
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI heatText;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI restartText;

    [Header("Colors")]
    public Color calmColor = new Color(0.2f, 0.85f, 1f, 1f);
    public Color warningColor = new Color(1f, 0.85f, 0.15f, 1f);
    public Color dangerColor = new Color(1f, 0.15f, 0.15f, 1f);

    [Header("Fade")]
    public float fadeSpeed = 7f;

    private float targetAlpha = 0f;
    private bool isDeathScreenActive = false;
    private RectTransform runtimeRoot;
    private Image deathBackdrop;

    public static HeatDamageUI Instance { get; private set; }

    public static HeatDamageUI GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        HeatDamageUI existing = FindObjectOfType<HeatDamageUI>();
        if (existing != null)
            return existing;

        GameObject uiObj = new GameObject("HeatDamageUI", typeof(RectTransform));
        return uiObj.AddComponent<HeatDamageUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureRuntimeUI();
    }

    void Start()
    {
        EnsureRuntimeUI();

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);

        if (restartText != null)
            restartText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // Smooth fade in/out
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }
    }

    public void UpdateHeatBar(float heatPercent)
    {
        EnsureRuntimeUI();
        heatPercent = Mathf.Clamp01(heatPercent);

        // heatPercent: 0 = safe, 1 = dead
        if (heatBarFill != null)
        {
            heatBarFill.fillAmount = heatPercent;

            Color barColor;
            if (heatPercent < 0.5f)
            {
                barColor = Color.Lerp(calmColor, warningColor, heatPercent / 0.5f);
            }
            else
            {
                barColor = Color.Lerp(warningColor, dangerColor, (heatPercent - 0.5f) / 0.5f);
            }

            heatBarFill.color = barColor;
        }

        if (heatBarGlow != null)
        {
            float pulse = 0.6f + Mathf.Sin(Time.time * 8f) * 0.4f;
            Color glowColor = Color.Lerp(calmColor, dangerColor, heatPercent);
            glowColor.a = Mathf.Lerp(0.08f, 0.45f, heatPercent) * pulse;
            heatBarGlow.color = glowColor;
        }

        if (heatText != null)
        {
            heatText.text = $"HEAT {Mathf.RoundToInt(heatPercent * 100f)}%";
        }

        // Show bar when taking heat damage, hide when safe
        if (!isDeathScreenActive)
            targetAlpha = heatPercent > 0.001f ? 1f : 0f;
    }

    public void ShowDeath()
    {
        EnsureRuntimeUI();

        if (heatBarFill != null)
        {
            heatBarFill.fillAmount = 1f;
            heatBarFill.color = dangerColor;
        }

        if (heatText != null)
            heatText.text = "HEAT 100%";

        if (deathBackdrop != null)
            deathBackdrop.color = new Color(0f, 0f, 0f, 0.65f);

        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);

        if (restartText != null)
            restartText.gameObject.SetActive(false);

        Death.GetOrCreate().ShowDeath();

        isDeathScreenActive = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        targetAlpha = 0f;
    }

    private void EnsureDeathTexts()
    {
        if (gameOverText != null && restartText != null)
            return;

        if (gameOverText == null)
        {
            gameOverText = CreateCenteredText("GameOverText", new Vector2(0f, 40f), 72f, FontStyles.Bold);
        }

        if (restartText == null)
        {
            restartText = CreateCenteredText("RestartText", new Vector2(0f, -20f), 36f, FontStyles.Bold);
        }
    }

    private TextMeshProUGUI CreateCenteredText(string objectName, Vector2 anchoredPos, float fontSize, FontStyles style)
    {
        Transform parent = runtimeRoot != null ? runtimeRoot : transform;

        GameObject textObj = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(900f, 120f);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.text = string.Empty;

        return tmp;
    }

    private void EnsureRuntimeUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HeatDamageCanvas");
            canvasObj.layer = LayerMask.NameToLayer("UI");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 6000;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (runtimeRoot == null)
        {
            Transform existingRoot = canvas.transform.Find("HeatDamageUIRoot");
            if (existingRoot != null)
                runtimeRoot = existingRoot as RectTransform;
            else
            {
                GameObject rootObj = new GameObject("HeatDamageUIRoot", typeof(RectTransform), typeof(CanvasGroup));
                rootObj.transform.SetParent(canvas.transform, false);
                runtimeRoot = rootObj.GetComponent<RectTransform>();
            }
        }

        if (runtimeRoot != null)
        {
            runtimeRoot.anchorMin = Vector2.zero;
            runtimeRoot.anchorMax = Vector2.one;
            runtimeRoot.offsetMin = Vector2.zero;
            runtimeRoot.offsetMax = Vector2.zero;

            if (canvasGroup == null)
                canvasGroup = runtimeRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = runtimeRoot.gameObject.AddComponent<CanvasGroup>();
        }

        if (deathBackdrop == null)
        {
            GameObject backdropObj = new GameObject("DeathBackdrop", typeof(RectTransform), typeof(Image));
            backdropObj.transform.SetParent(runtimeRoot, false);
            RectTransform rt = backdropObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            deathBackdrop = backdropObj.GetComponent<Image>();
            deathBackdrop.color = new Color(0f, 0f, 0f, 0f);
            deathBackdrop.raycastTarget = false;
        }

        if (heatBarBackground == null)
        {
            GameObject bgObj = new GameObject("HeatBarBg", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(runtimeRoot, false);

            heatBarBackground = bgObj.GetComponent<Image>();
            heatBarBackground.color = new Color(0.03f, 0.05f, 0.1f, 0.82f);
            heatBarBackground.raycastTarget = false;
        }

        RectTransform barBgRT = heatBarBackground.rectTransform;
        barBgRT.anchorMin = new Vector2(0f, 1f);
        barBgRT.anchorMax = new Vector2(0f, 1f);
        barBgRT.pivot = new Vector2(0f, 1f);
        barBgRT.sizeDelta = new Vector2(320f, 20f);
        barBgRT.anchoredPosition = new Vector2(24f, -24f);

        if (heatBarFill == null)
        {
            GameObject fillObj = new GameObject("HeatBarFill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(heatBarBackground.transform, false);

            heatBarFill = fillObj.GetComponent<Image>();
            heatBarFill.type = Image.Type.Filled;
            heatBarFill.fillMethod = Image.FillMethod.Horizontal;
            heatBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            heatBarFill.fillAmount = 0f;
            heatBarFill.color = calmColor;
            heatBarFill.raycastTarget = false;
        }

        RectTransform fillRT = heatBarFill.rectTransform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(3f, 3f);
        fillRT.offsetMax = new Vector2(-3f, -3f);

        if (heatBarFrame == null)
        {
            GameObject frameObj = new GameObject("HeatBarFrame", typeof(RectTransform), typeof(Image));
            frameObj.transform.SetParent(heatBarBackground.transform, false);
            heatBarFrame = frameObj.GetComponent<Image>();
            heatBarFrame.color = new Color(0.85f, 0.93f, 1f, 0.36f);
            heatBarFrame.raycastTarget = false;
        }

        RectTransform frameRT = heatBarFrame.rectTransform;
        frameRT.anchorMin = Vector2.zero;
        frameRT.anchorMax = Vector2.one;
        frameRT.offsetMin = new Vector2(-2f, -2f);
        frameRT.offsetMax = new Vector2(2f, 2f);

        if (heatBarGlow == null)
        {
            GameObject glowObj = new GameObject("HeatBarGlow", typeof(RectTransform), typeof(Image));
            glowObj.transform.SetParent(heatBarBackground.transform, false);
            heatBarGlow = glowObj.GetComponent<Image>();
            heatBarGlow.color = new Color(0.2f, 0.85f, 1f, 0.08f);
            heatBarGlow.raycastTarget = false;
        }

        RectTransform glowRT = heatBarGlow.rectTransform;
        glowRT.anchorMin = Vector2.zero;
        glowRT.anchorMax = Vector2.one;
        glowRT.offsetMin = new Vector2(-8f, -8f);
        glowRT.offsetMax = new Vector2(8f, 8f);

        if (heatText == null)
        {
            GameObject textObj = new GameObject("HeatBarText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(runtimeRoot, false);
            heatText = textObj.GetComponent<TextMeshProUGUI>();
            heatText.fontSize = 16f;
            heatText.fontStyle = FontStyles.Bold;
            heatText.alignment = TextAlignmentOptions.Left;
            heatText.color = new Color(0.85f, 0.95f, 1f, 1f);
            heatText.text = "HEAT 0%";
        }

        RectTransform textRT = heatText.rectTransform;
        textRT.anchorMin = new Vector2(0f, 1f);
        textRT.anchorMax = new Vector2(0f, 1f);
        textRT.pivot = new Vector2(0f, 1f);
        textRT.sizeDelta = new Vector2(320f, 28f);
        textRT.anchoredPosition = new Vector2(24f, -48f);

        EnsureDeathTexts();
    }
}