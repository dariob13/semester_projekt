using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ServerUI : MonoBehaviour
{
    [Header("UI References")]
    public Image progressBarFill;
    public TextMeshProUGUI statusText;
    public CanvasGroup canvasGroup;

    [Header("Colors")]
    public Color intactColor = new Color(0f, 1f, 0f, 1f);
    public Color damagingColor = new Color(1f, 0.5f, 0f, 1f);
    public Color destroyedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private Server[] allServers;
    private float targetAlpha = 0f;
    private float fadeSpeed = 5f;

    void Start()
    {
        allServers = FindObjectsOfType<Server>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        foreach (var server in allServers)
            server.OnServerDestroyed += OnServerDestroyed;
    }

    void Update()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

        // Find active server being targeted
        foreach (var server in allServers)
        {
            if (server.IsDestroyed()) continue;

            if (server.IsPlayerInRange())
            {
                float progress = server.GetDestroyProgress();

                if (progressBarFill != null)
                {
                    progressBarFill.fillAmount = progress;
                    progressBarFill.color = Color.Lerp(intactColor, damagingColor, progress);
                }

                if (statusText != null)
                {
                    float remaining = (1f - progress) * 5f;
                    statusText.text = $"Destroying Server... {remaining:F1}s";
                }

                targetAlpha = 1f;
                return;
            }
        }

        targetAlpha = 0f;
    }

    void OnServerDestroyed()
    {
        if (statusText != null)
        {
            statusText.text = "Server Destroyed!";
            statusText.color = destroyedColor;
        }

        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = 1f;
            progressBarFill.color = destroyedColor;
        }

        targetAlpha = 1f;
        StartCoroutine(HideAfterDelay(2f));
    }

    private System.Collections.IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        targetAlpha = 0f;
    }

    void OnDestroy()
    {
        if (allServers != null)
            foreach (var server in allServers)
                server.OnServerDestroyed -= OnServerDestroyed;
    }
}