using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AlertUI : MonoBehaviour
{
    [Header("UI References")]
    public Image alertBarFill;
    public Image alertBarBackground;
    public TextMeshProUGUI alertText;
    public CanvasGroup canvasGroup;

    [Header("Colors")]
    public Color safeColor = new Color(1f, 1f, 0f, 1f);
    public Color dangerColor = new Color(1f, 0f, 0f, 1f);

    private PatrolAI[] allAIs;
    private float fadeSpeed = 5f;
    private float targetAlpha = 0f;

    void Start()
    {
        allAIs = FindObjectsOfType<PatrolAI>();

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        // Subscribe to all AI events
        foreach (var ai in allAIs)
        {
            ai.OnPlayerDetected += OnAIAlerted;
            ai.OnPlayerLost += OnAILostPlayer;
        }
    }

    void Update()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }

        // Update alert bars
        foreach (var ai in allAIs)
        {
            if (ai.IsAlerting())
            {
                float progress = ai.GetAlertProgress();
                
                if (alertBarFill != null)
                {
                    alertBarFill.fillAmount = progress;
                    alertBarFill.color = Color.Lerp(safeColor, dangerColor, progress);
                }

                if (alertText != null)
                {
                    float remainingTime = (1f - progress) * 3f;
                    alertText.text = $"ESCAPE! {remainingTime:F1}s";
                }

                targetAlpha = 1f;
                return;
            }
        }

        targetAlpha = 0f;
    }

    void OnAIAlerted()
    {
        if (alertText != null)
        {
            alertText.color = new Color(1f, 0f, 0f, 1f);
        }
    }

    void OnAILostPlayer()
    {
        if (alertText != null)
        {
            alertText.text = "Safe!";
            alertText.color = new Color(0f, 1f, 0f, 1f);
        }

        // Keep visible briefly
        StartCoroutine(HideAfterDelay(1f));
    }

    private System.Collections.IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        targetAlpha = 0f;
    }

    void OnDestroy()
    {
        if (allAIs != null)
        {
            foreach (var ai in allAIs)
            {
                ai.OnPlayerDetected -= OnAIAlerted;
                ai.OnPlayerLost -= OnAILostPlayer;
            }
        }
    }
}