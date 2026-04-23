using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AlertUI : MonoBehaviour
{
    [Header("UI References")]
    public Image alertBarFill;
    public Image alertBarBackground;
    public Image alertBarFrame;
    public Image alertBarGlow;
    public Image screenTint;
    public TextMeshProUGUI alertText;
    public CanvasGroup canvasGroup;

    [Header("Colors")]
    public Color calmColor = new Color(0.2f, 0.85f, 1f, 1f);
    public Color warningColor = new Color(1f, 0.85f, 0.15f, 1f);
    public Color dangerColor = new Color(1f, 0.15f, 0.15f, 1f);

    [Header("Behavior")]
    public float idleAlpha = 0.5f;
    public float activeAlpha = 1f;
    public float fadeSpeed = 7f;
    public float loseThreshold = 0.999f;

    private PatrolAI[] allAIs;
    private CCTVCamera[] allCameras;
    private LiquidSolidForm player;
    private float targetAlpha = 0f;
    private Image[] edgeTints;
    private RectTransform runtimeRoot;
    private bool hasTriggeredLose;

    void Start()
    {
        EnsureRuntimeOverlay();
        CacheActors();
        SubscribeToDetectors();

        if (canvasGroup != null)
            canvasGroup.alpha = idleAlpha;

        targetAlpha = idleAlpha;
        hasTriggeredLose = false;
    }

    void Update()
    {
        if (screenTint == null || alertBarFill == null || alertText == null)
            EnsureRuntimeOverlay();

        if (player == null)
            player = FindObjectOfType<LiquidSolidForm>();

        RefreshDetectorsIfNeeded();

        if (canvasGroup != null)
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

        bool anyAlerting = false;
        float highestProgress = 0f;

        // Guards
        if (allAIs != null)
        {
            foreach (var ai in allAIs)
            {
                if (ai != null && ai.IsAlerting())
                {
                    anyAlerting = true;
                    highestProgress = Mathf.Max(highestProgress, ai.GetAlertProgress());
                }
            }
        }

        // Cameras
        if (allCameras != null)
        {
            foreach (var cameraAI in allCameras)
            {
                if (cameraAI != null && cameraAI.IsAlerting())
                {
                    anyAlerting = true;
                    highestProgress = Mathf.Max(highestProgress, cameraAI.GetAlertProgress());
                }
            }
        }

        UpdateDetectionBar(anyAlerting, highestProgress);
        UpdateScreenEffects(anyAlerting, highestProgress);

        if (anyAlerting && highestProgress >= loseThreshold && !hasTriggeredLose)
        {
            hasTriggeredLose = true;

            if (alertText != null)
                alertText.text = "CAUGHT!";

            if (player != null && !player.GetIsDead())
                player.ForceKill();
        }
        else if (!anyAlerting)
        {
            hasTriggeredLose = false;
        }
    }

    void OnAIAlerted()
    {
        if (alertText != null)
            alertText.color = Color.white;
    }

    void OnAILostPlayer()
    {
        if (alertText != null)
            alertText.color = new Color(0.85f, 0.95f, 1f, 1f);
    }

    private void UpdateDetectionBar(bool anyAlerting, float progress)
    {
        targetAlpha = anyAlerting ? activeAlpha : idleAlpha;

        if (alertBarFill != null)
        {
            alertBarFill.fillAmount = progress;

            Color gradientColor;
            if (progress < 0.5f)
            {
                gradientColor = Color.Lerp(calmColor, warningColor, progress / 0.5f);
            }
            else
            {
                gradientColor = Color.Lerp(warningColor, dangerColor, (progress - 0.5f) / 0.5f);
            }

            alertBarFill.color = gradientColor;
        }

        if (alertBarGlow != null)
        {
            float pulse = 0.6f + Mathf.Sin(Time.time * 8f) * 0.4f;
            Color glowColor = Color.Lerp(calmColor, dangerColor, progress);
            glowColor.a = Mathf.Lerp(0.08f, 0.45f, progress) * pulse;
            alertBarGlow.color = glowColor;
        }

        if (alertText != null)
        {
            if (anyAlerting)
            {
                alertText.text = $"DETECTED {Mathf.RoundToInt(progress * 100f)}%";
            }
            else
            {
                alertText.text = "Stealth Stable";
            }
        }
    }

    private void UpdateScreenEffects(bool anyAlerting, float highestProgress)
    {
        if (screenTint != null)
        {
            Color tint = screenTint.color;
            float targetTintAlpha = anyAlerting ? Mathf.Lerp(0.04f, 0.24f, highestProgress) : 0f;
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

    private void CacheActors()
    {
        allAIs = FindObjectsOfType<PatrolAI>();
        allCameras = FindObjectsOfType<CCTVCamera>();
        player = FindObjectOfType<LiquidSolidForm>();
    }

    private void RefreshDetectorsIfNeeded()
    {
        if (allAIs == null || allAIs.Length == 0)
            allAIs = FindObjectsOfType<PatrolAI>();

        if (allCameras == null || allCameras.Length == 0)
            allCameras = FindObjectsOfType<CCTVCamera>();
    }

    private void SubscribeToDetectors()
    {
        if (allAIs != null)
        {
            foreach (var ai in allAIs)
            {
                if (ai == null) continue;
                ai.OnPlayerDetected += OnAIAlerted;
                ai.OnPlayerLost += OnAILostPlayer;
            }
        }

        if (allCameras != null)
        {
            foreach (var cam in allCameras)
            {
                if (cam == null) continue;
                cam.OnPlayerDetected += OnAIAlerted;
                cam.OnPlayerLost += OnAILostPlayer;
            }
        }
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

        if (allCameras != null)
        {
            foreach (var cam in allCameras)
            {
                if (cam == null) continue;
                cam.OnPlayerDetected -= OnAIAlerted;
                cam.OnPlayerLost -= OnAILostPlayer;
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

        if (runtimeRoot == null)
        {
            Transform root = canvasObj.transform.Find("AlertUIRoot");
            if (root == null)
            {
                GameObject rootObj = new GameObject("AlertUIRoot", typeof(RectTransform), typeof(CanvasGroup));
                rootObj.transform.SetParent(canvasObj.transform, false);
                runtimeRoot = rootObj.GetComponent<RectTransform>();
            }
            else
            {
                runtimeRoot = root as RectTransform;
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

        EnsureCenterBarUI();

        if (screenTint == null)
        {
            GameObject tintObj = new GameObject("AlertScreenTint", typeof(RectTransform), typeof(Image));
            tintObj.transform.SetParent(runtimeRoot != null ? runtimeRoot : canvasObj.transform, false);
            screenTint = tintObj.GetComponent<Image>();
        }

        if (screenTint.transform.parent != (runtimeRoot != null ? runtimeRoot : canvasObj.transform))
            screenTint.transform.SetParent(runtimeRoot != null ? runtimeRoot : canvasObj.transform, false);

        if (edgeTints == null || edgeTints.Length != 4)
        {
            edgeTints = new Image[4];
            Transform parent = runtimeRoot != null ? runtimeRoot : canvasObj.transform;
            edgeTints[0] = CreateEdgeImage(parent, "TintTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, -140f));
            edgeTints[1] = CreateEdgeImage(parent, "TintBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 140f), new Vector2(0f, 0f));
            edgeTints[2] = CreateEdgeImage(parent, "TintLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 140f), new Vector2(140f, -140f));
            edgeTints[3] = CreateEdgeImage(parent, "TintRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-140f, 140f), new Vector2(0f, -140f));
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

    private void EnsureCenterBarUI()
    {
        if (runtimeRoot == null)
            return;

        if (alertBarBackground == null)
        {
            Transform existing = runtimeRoot.Find("DetectionBarBg");
            if (existing != null)
                alertBarBackground = existing.GetComponent<Image>();
        }

        if (alertBarBackground == null)
        {
            GameObject bgObj = new GameObject("DetectionBarBg", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(runtimeRoot, false);

            RectTransform bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(1f, 1f);
            bgRT.anchorMax = new Vector2(1f, 1f);
            bgRT.pivot = new Vector2(1f, 1f);
            bgRT.sizeDelta = new Vector2(320f, 20f);
            bgRT.anchoredPosition = new Vector2(-24f, -24f);

            alertBarBackground = bgObj.GetComponent<Image>();
            alertBarBackground.color = new Color(0.03f, 0.05f, 0.1f, 0.82f);
            alertBarBackground.raycastTarget = false;
        }

        RectTransform barBgRT = alertBarBackground.rectTransform;
        barBgRT.anchorMin = new Vector2(1f, 1f);
        barBgRT.anchorMax = new Vector2(1f, 1f);
        barBgRT.pivot = new Vector2(1f, 1f);
        barBgRT.sizeDelta = new Vector2(320f, 20f);
        barBgRT.anchoredPosition = new Vector2(-24f, -24f);

        if (alertBarFill == null)
        {
            Transform existingFill = alertBarBackground.transform.Find("DetectionBarFill");
            if (existingFill != null)
                alertBarFill = existingFill.GetComponent<Image>();
        }

        if (alertBarFill == null)
        {
            GameObject fillObj = new GameObject("DetectionBarFill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(alertBarBackground.transform, false);

            RectTransform fillRT = fillObj.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = new Vector2(4f, 4f);
            fillRT.offsetMax = new Vector2(-4f, -4f);

            alertBarFill = fillObj.GetComponent<Image>();
            alertBarFill.type = Image.Type.Filled;
            alertBarFill.fillMethod = Image.FillMethod.Horizontal;
            alertBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            alertBarFill.fillAmount = 0f;
            alertBarFill.color = calmColor;
            alertBarFill.raycastTarget = false;
        }

        RectTransform fillRuntimeRT = alertBarFill.rectTransform;
        fillRuntimeRT.anchorMin = Vector2.zero;
        fillRuntimeRT.anchorMax = Vector2.one;
        fillRuntimeRT.offsetMin = new Vector2(3f, 3f);
        fillRuntimeRT.offsetMax = new Vector2(-3f, -3f);

        if (alertBarFrame == null)
        {
            GameObject frameObj = new GameObject("DetectionBarFrame", typeof(RectTransform), typeof(Image));
            frameObj.transform.SetParent(alertBarBackground.transform, false);

            RectTransform frameRT = frameObj.GetComponent<RectTransform>();
            frameRT.anchorMin = Vector2.zero;
            frameRT.anchorMax = Vector2.one;
            frameRT.offsetMin = new Vector2(-2f, -2f);
            frameRT.offsetMax = new Vector2(2f, 2f);

            alertBarFrame = frameObj.GetComponent<Image>();
            alertBarFrame.color = new Color(0.85f, 0.93f, 1f, 0.36f);
            alertBarFrame.raycastTarget = false;
        }

        if (alertBarGlow == null)
        {
            GameObject glowObj = new GameObject("DetectionBarGlow", typeof(RectTransform), typeof(Image));
            glowObj.transform.SetParent(alertBarBackground.transform, false);

            RectTransform glowRT = glowObj.GetComponent<RectTransform>();
            glowRT.anchorMin = Vector2.zero;
            glowRT.anchorMax = Vector2.one;
            glowRT.offsetMin = new Vector2(-8f, -8f);
            glowRT.offsetMax = new Vector2(8f, 8f);

            alertBarGlow = glowObj.GetComponent<Image>();
            alertBarGlow.color = new Color(0.2f, 0.85f, 1f, 0.08f);
            alertBarGlow.raycastTarget = false;
        }

        if (alertText == null)
        {
            Transform existingText = runtimeRoot.Find("DetectionBarText");
            if (existingText != null)
                alertText = existingText.GetComponent<TextMeshProUGUI>();
        }

        if (alertText == null)
        {
            GameObject textObj = new GameObject("DetectionBarText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(runtimeRoot, false);

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(1f, 1f);
            textRT.anchorMax = new Vector2(1f, 1f);
            textRT.pivot = new Vector2(1f, 1f);
            textRT.sizeDelta = new Vector2(320f, 28f);
            textRT.anchoredPosition = new Vector2(-24f, -48f);

            alertText = textObj.GetComponent<TextMeshProUGUI>();
            alertText.fontSize = 16f;
            alertText.fontStyle = FontStyles.Bold;
            alertText.alignment = TextAlignmentOptions.Right;
            alertText.color = new Color(0.85f, 0.95f, 1f, 1f);
            alertText.text = "Stealth Stable";
        }

        RectTransform textRuntimeRT = alertText.rectTransform;
        textRuntimeRT.anchorMin = new Vector2(1f, 1f);
        textRuntimeRT.anchorMax = new Vector2(1f, 1f);
        textRuntimeRT.pivot = new Vector2(1f, 1f);
        textRuntimeRT.sizeDelta = new Vector2(320f, 28f);
        textRuntimeRT.anchoredPosition = new Vector2(-24f, -48f);
        alertText.fontSize = 16f;
        alertText.alignment = TextAlignmentOptions.Right;
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