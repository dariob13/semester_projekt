using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AlertUI : MonoBehaviour
{
    [Header("UI References")]
    public Image alertBarFill;
    public Image alertBarBackground;
    public Image screenTint;
    public TextMeshProUGUI alertText;
    public CanvasGroup canvasGroup;

    [Header("Colors")]
    public Color safeColor = new Color(1f, 1f, 0f, 1f);
    public Color dangerColor = new Color(1f, 0f, 0f, 1f);

    private PatrolAI[] allAIs;
    private float fadeSpeed = 5f;
    private float targetAlpha = 0f;
    private Image[] edgeTints;

    void Start()
    {
        allAIs = FindObjectsOfType<PatrolAI>();

        EnsureRuntimeOverlay();

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
        if (screenTint == null || edgeTints == null || edgeTints.Length != 4)
            EnsureRuntimeOverlay();

        if (allAIs == null || allAIs.Length == 0)
            allAIs = FindObjectsOfType<PatrolAI>();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }

        bool anyAlerting = false;
        float highestProgress = 0f;

        // Update alert bars + gather strongest detection progress
        foreach (var ai in allAIs)
        {
            if (ai.IsAlerting())
            {
                anyAlerting = true;
                float progress = ai.GetAlertProgress();
                if (progress > highestProgress)
                    highestProgress = progress;
                
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
            }
        }

        if (!anyAlerting)
            targetAlpha = 0f;

        // Full-screen red tint: starts visible on detection and grows stronger over continuous detection.
        if (screenTint != null)
        {
            Color tint = screenTint.color;
            float targetTintAlpha = anyAlerting ? Mathf.Lerp(0.05f, 0.35f, highestProgress) : 0f;
            tint.a = Mathf.Lerp(tint.a, targetTintAlpha, Time.deltaTime * 8f);
            screenTint.color = tint;
        }

        if (edgeTints != null)
        {
            float targetEdgeAlpha = anyAlerting ? Mathf.Lerp(0.2f, 0.8f, highestProgress) : 0f;
            for (int i = 0; i < edgeTints.Length; i++)
            {
                if (edgeTints[i] == null) continue;
                Color c = edgeTints[i].color;
                c.a = Mathf.Lerp(c.a, targetEdgeAlpha, Time.deltaTime * 8f);
                edgeTints[i].color = c;
            }
        }
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

    private void EnsureRuntimeOverlay()
    {
        GameObject canvasObj = GameObject.Find("AlertScreenTintCanvas");
        Canvas tintCanvas = null;

        if (canvasObj == null)
        {
            canvasObj = new GameObject("AlertScreenTintCanvas");
            canvasObj.layer = LayerMask.NameToLayer("UI");

            tintCanvas = canvasObj.AddComponent<Canvas>();
            tintCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tintCanvas.sortingOrder = 5000;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            tintCanvas = canvasObj.GetComponent<Canvas>();
            if (tintCanvas == null)
                tintCanvas = canvasObj.AddComponent<Canvas>();
            tintCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tintCanvas.sortingOrder = Mathf.Max(tintCanvas.sortingOrder, 5000);

            if (canvasObj.GetComponent<CanvasScaler>() == null)
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            if (canvasObj.GetComponent<GraphicRaycaster>() == null)
                canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (screenTint == null)
        {
            GameObject tintObj = new GameObject("AlertScreenTint", typeof(RectTransform), typeof(Image));
            tintObj.transform.SetParent(canvasObj.transform, false);
            screenTint = tintObj.GetComponent<Image>();
        }

        if (screenTint.transform.parent != canvasObj.transform)
            screenTint.transform.SetParent(canvasObj.transform, false);

        if (edgeTints == null || edgeTints.Length != 4)
        {
            edgeTints = new Image[4];
            edgeTints[0] = CreateEdgeImage(canvasObj.transform, "TintTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -140f));
            edgeTints[1] = CreateEdgeImage(canvasObj.transform, "TintBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 140f), new Vector2(0f, 0f));
            edgeTints[2] = CreateEdgeImage(canvasObj.transform, "TintLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 140f), new Vector2(140f, -140f));
            edgeTints[3] = CreateEdgeImage(canvasObj.transform, "TintRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-140f, 140f), new Vector2(0f, -140f));
        }

        RectTransform rt = screenTint.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        screenTint.color = new Color(1f, 0f, 0f, 0f);
        screenTint.raycastTarget = false;

        if (edgeTints != null)
        {
            for (int i = 0; i < edgeTints.Length; i++)
            {
                if (edgeTints[i] == null) continue;
                edgeTints[i].color = new Color(1f, 0f, 0f, 0f);
                edgeTints[i].raycastTarget = false;
            }
        }
    }

    private Image CreateEdgeImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        Image img = go.GetComponent<Image>();
        img.color = new Color(1f, 0f, 0f, 0f);
        return img;
    }
}