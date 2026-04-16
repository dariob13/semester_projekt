using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class HeatDamageUI : MonoBehaviour
{
    [Header("UI References")]
    public Image heatBarFill;
    public Image heatBarBackground;
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI restartText;

    [Header("Colors")]
    public Color safeColor = new Color(1f, 0.8f, 0f, 1f);
    public Color dangerColor = new Color(1f, 0.1f, 0f, 1f);

    private float fadeSpeed = 5f;
    private float targetAlpha = 0f;
    private bool isDeathScreenActive = false;
    private RectTransform runtimeRoot;
    private Image deathBackdrop;

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

    void Update()
    {
        // Smooth fade in/out
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }

        if (isDeathScreenActive && Input.GetKeyDown(KeyCode.G))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void UpdateHeatBar(float heatPercent)
    {
        EnsureRuntimeUI();

        // heatPercent: 0 = safe, 1 = dead
        if (heatBarFill != null)
        {
            heatBarFill.fillAmount = heatPercent;
            heatBarFill.color = Color.Lerp(safeColor, dangerColor, heatPercent);
        }

        // Show bar when taking heat damage, hide when safe
        targetAlpha = heatPercent > 0.01f ? 1f : 0f;
    }

    public void ShowDeath()
    {
        EnsureRuntimeUI();

        if (heatBarFill != null)
        {
            heatBarFill.fillAmount = 1f;
            heatBarFill.color = dangerColor;
        }

        if (deathBackdrop != null)
            deathBackdrop.color = new Color(0f, 0f, 0f, 0.65f);

        EnsureDeathTexts();

        if (gameOverText != null)
        {
            gameOverText.text = "GAME OVER";
            gameOverText.gameObject.SetActive(true);
        }

        if (restartText != null)
        {
            restartText.text = "PRESS G TO RESTART";
            restartText.gameObject.SetActive(true);
        }

        isDeathScreenActive = true;
        targetAlpha = 1f;
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
        if (canvasGroup != null && heatBarFill != null && heatBarBackground != null)
            return;

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
            GameObject rootObj = new GameObject("HeatDamageUIRoot", typeof(RectTransform), typeof(CanvasGroup));
            rootObj.transform.SetParent(canvas.transform, false);
            runtimeRoot = rootObj.GetComponent<RectTransform>();
            runtimeRoot.anchorMin = Vector2.zero;
            runtimeRoot.anchorMax = Vector2.one;
            runtimeRoot.offsetMin = Vector2.zero;
            runtimeRoot.offsetMax = Vector2.zero;

            canvasGroup = rootObj.GetComponent<CanvasGroup>();
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
            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.35f, 0.9f);
            bgRT.anchorMax = new Vector2(0.65f, 0.95f);
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            heatBarBackground = bgObj.GetComponent<Image>();
            heatBarBackground.color = new Color(0f, 0f, 0f, 0.65f);
        }

        if (heatBarFill == null)
        {
            GameObject fillObj = new GameObject("HeatBarFill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(heatBarBackground.transform, false);
            RectTransform fillRT = fillObj.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = new Vector2(3f, 3f);
            fillRT.offsetMax = new Vector2(-3f, -3f);

            heatBarFill = fillObj.GetComponent<Image>();
            heatBarFill.type = Image.Type.Filled;
            heatBarFill.fillMethod = Image.FillMethod.Horizontal;
            heatBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            heatBarFill.fillAmount = 0f;
            heatBarFill.color = safeColor;
        }

        EnsureDeathTexts();
    }
}