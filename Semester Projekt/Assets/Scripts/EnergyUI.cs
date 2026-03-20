using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays player energy as a simple fill bar. If no references are assigned, it builds a minimal UI at runtime.
/// </summary>
public class EnergyUI : MonoBehaviour
{
    [Header("UI References")]
    public Image barFill;
    public Image barBackground;
    public CanvasGroup canvasGroup;

    [Header("Visuals")]
    public Color fullColor = new Color(0.2f, 0.9f, 1f, 1f);
    public Color emptyColor = new Color(1f, 0.3f, 0.3f, 1f);
    public float fadeSpeed = 6f;

    private float targetAlpha = 1f;

    void Awake()
    {
        // Ensure this root is a RectTransform so anchoring works
        if (transform as RectTransform == null)
        {
            gameObject.AddComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (barFill == null)
        {
            BuildRuntimeUI();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
    }

    void Update()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }
    }

    /// <summary>
    /// Expects pct in [0,1].
    /// </summary>
    public void UpdateEnergy(float pct)
    {
        pct = Mathf.Clamp01(pct);
        if (barFill != null)
        {
            barFill.fillAmount = pct;
            barFill.color = Color.Lerp(emptyColor, fullColor, pct);
        }

        targetAlpha = 1f;
    }

    private void BuildRuntimeUI()
    {
        // Ensure there is a canvas parent
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            var canvasObj = new GameObject("EnergyCanvas");
            canvasObj.layer = LayerMask.NameToLayer("UI");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1200;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            transform.SetParent(canvas.transform, false);
        }
        else
        {
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 1200);
            transform.SetParent(canvas.transform, false);
        }

        // Stretch this holder over the full canvas so anchors work
        var holderRT = transform as RectTransform;
        if (holderRT != null)
        {
            holderRT.anchorMin = Vector2.zero;
            holderRT.anchorMax = Vector2.one;
            holderRT.offsetMin = Vector2.zero;
            holderRT.offsetMax = Vector2.zero;
        }

        // Background
        GameObject bgObj = new GameObject("EnergyBg", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(transform, false);
        var bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.80f, 0.93f);
        bgRT.anchorMax = new Vector2(0.96f, 0.97f);
        bgRT.offsetMin = new Vector2(-4f, -4f);
        bgRT.offsetMax = new Vector2(4f, 4f);
        barBackground = bgObj.GetComponent<Image>();
        barBackground.color = new Color(0f, 0f, 0f, 0.7f);

        // Fill
        GameObject fillObj = new GameObject("EnergyFill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(bgObj.transform, false);
        var fillRT = fillObj.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(4f, 4f);
        fillRT.offsetMax = new Vector2(-4f, -4f);

        barFill = fillObj.GetComponent<Image>();
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        barFill.color = fullColor;
    }
}
