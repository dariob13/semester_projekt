using UnityEngine;
using UnityEngine.UI;

public class HeatDamageUI : MonoBehaviour
{
    [Header("UI References")]
    public Image heatBarFill;
    public Image heatBarBackground;
    public CanvasGroup canvasGroup;

    [Header("Colors")]
    public Color safeColor = new Color(1f, 0.8f, 0f, 1f);
    public Color dangerColor = new Color(1f, 0.1f, 0f, 1f);

    private float fadeSpeed = 5f;
    private float targetAlpha = 0f;

    void Start()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
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
        if (heatBarFill != null)
        {
            heatBarFill.fillAmount = 1f;
            heatBarFill.color = dangerColor;
        }

        targetAlpha = 1f;
    }
}