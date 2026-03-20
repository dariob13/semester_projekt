using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaterHeaterMinigameUI : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public Image gaugeFill;
    public Image safeZone;
    public Image needle;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI hintText;

    [Header("Visual Settings")]
    public Color coldColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color warmColor = new Color(1f, 0.7f, 0.2f, 1f);
    public Color hotColor = new Color(1f, 0.2f, 0.1f, 1f);
    public Color needleColor = new Color(1f, 1f, 1f, 1f);

    private float targetAlpha = 0f;

    void Start()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (gaugeFill == null || titleText == null || hintText == null)
            BuildRuntimeUI();

        canvasGroup.alpha = 0f;
    }

    void Update()
    {
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * 8f);
    }

    public void Show()
    {
        targetAlpha = 1f;
        if (titleText != null)
            titleText.text = "Heat Control";
        if (hintText != null)
            hintText.text = "Hold LMB to heat • Keep arrow in green zone";

        if (titleText != null)
            titleText.color = Color.white;
    }

    public void Hide()
    {
        targetAlpha = 0f;
    }

    public void SetProgress(float value01, float minSafe, float maxSafe, float stableTime, float requiredStableTime)
    {
        float clamped = Mathf.Clamp01(value01);

        if (gaugeFill != null)
        {
            // Background thermal state feel
            gaugeFill.fillAmount = clamped;

            if (clamped < minSafe)
                gaugeFill.color = coldColor;
            else if (clamped > maxSafe)
                gaugeFill.color = hotColor;
            else
                gaugeFill.color = warmColor;
        }

        if (safeZone != null)
        {
            RectTransform rt = safeZone.rectTransform;
            rt.anchorMin = new Vector2(minSafe, 0f);
            rt.anchorMax = new Vector2(maxSafe, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // Needle/arrow that must stay in threshold
        if (needle != null)
        {
            RectTransform nrt = needle.rectTransform;
            nrt.anchorMin = new Vector2(clamped, 0f);
            nrt.anchorMax = new Vector2(clamped, 1f);
            nrt.offsetMin = new Vector2(-4f, -8f);
            nrt.offsetMax = new Vector2(4f, 8f);

            if (clamped < minSafe || clamped > maxSafe)
                needle.color = hotColor;
            else
                needle.color = needleColor;
        }

        if (titleText != null)
            titleText.text = $"Heat Control ({stableTime:F1}/{requiredStableTime:F1}s)";
    }

    public void ShowResult(string message, Color color, float duration)
    {
        if (titleText != null)
        {
            titleText.text = message;
            titleText.color = color;
        }

        if (hintText != null)
            hintText.text = string.Empty;

        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), duration);
    }

    private void BuildRuntimeUI()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            var canvasObj = new GameObject("WaterMinigameCanvas");
            canvasObj.layer = LayerMask.NameToLayer("UI");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
            transform.SetParent(canvas.transform, false);
        }

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 1f);
        panelRT.anchorMax = new Vector2(0.5f, 1f);
        panelRT.pivot = new Vector2(0.5f, 1f);
        panelRT.anchoredPosition = new Vector2(0f, -40f);
        panelRT.sizeDelta = new Vector2(460f, 150f);
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        titleText = CreateText(panel.transform, "Title", new Vector2(0f, -18f), 28, FontStyles.Bold);
        hintText = CreateText(panel.transform, "Hint", new Vector2(0f, -48f), 18, FontStyles.Normal);

        var barBg = new GameObject("BarBg", typeof(RectTransform), typeof(Image));
        barBg.transform.SetParent(panel.transform, false);
        var barBgRT = barBg.GetComponent<RectTransform>();
        barBgRT.anchorMin = new Vector2(0.1f, 0.1f);
        barBgRT.anchorMax = new Vector2(0.9f, 0.32f);
        barBgRT.offsetMin = Vector2.zero;
        barBgRT.offsetMax = Vector2.zero;
        barBg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 1f);

        var safeObj = new GameObject("SafeZone", typeof(RectTransform), typeof(Image));
        safeObj.transform.SetParent(barBg.transform, false);
        safeZone = safeObj.GetComponent<Image>();
        safeZone.color = new Color(0.2f, 1f, 0.2f, 0.25f);

        var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(barBg.transform, false);
        gaugeFill = fillObj.GetComponent<Image>();
        gaugeFill.type = Image.Type.Filled;
        gaugeFill.fillMethod = Image.FillMethod.Horizontal;
        gaugeFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        gaugeFill.fillAmount = 0.5f;
        gaugeFill.color = warmColor;

        var fillRT = fillObj.GetComponent<RectTransform>();
        // Keep the fill slightly inside the bar
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        var needleObj = new GameObject("Needle", typeof(RectTransform), typeof(Image));
        needleObj.transform.SetParent(barBg.transform, false);
        needle = needleObj.GetComponent<Image>();
        needle.color = needleColor;
        needle.raycastTarget = false;
        
        RectTransform nRT = needle.GetComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0.5f, 0f);
        nRT.anchorMax = new Vector2(0.5f, 1f);
        // Give it a thicker width and make it slightly taller than the gauge bar
        nRT.offsetMin = new Vector2(-4f, -8f);
        nRT.offsetMax = new Vector2(4f, 8f);
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, Vector2 anchoredPos, int size, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(430f, 32f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        return tmp;
    }
}
