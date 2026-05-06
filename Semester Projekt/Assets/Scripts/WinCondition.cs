using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WinCondition : MonoBehaviour
{
    [Header("Server Settings")]
    public int targetServerCount = 3;

    [Header("UI Settings")]
    public bool buildRuntimeUI = true;
    public bool showCursorOnWin = true;
#if UNITY_EDITOR
    public SceneAsset mainMenuScene;
#endif
    public string mainMenuSceneName = "MainMenu";
    public int mainMenuSceneIndex = 0;

    [Header("UI References")]
    public Canvas winCanvas;
    public RectTransform root;
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI timeText;
    public Button mainMenuButton;

    [Header("Colors")]
    public Color accentColor = new Color(0.2f, 0.85f, 1f, 1f);
    public Color buttonHoverColor = new Color(0.35f, 0.9f, 1f, 1f);
    public Color buttonPressedColor = new Color(0.1f, 0.65f, 0.85f, 1f);
    public Color buttonTextColor = Color.white;
    public Color panelColor = new Color(0f, 0f, 0f, 0.8f);
    public Color backdropColor = new Color(0f, 0f, 0f, 0.6f);
    public Color textColor = Color.white;

    private Server[] servers;
    private int destroyedCount;
    private int requiredCount;
    private float startTime;
    private bool isComplete;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (mainMenuScene != null)
            mainMenuSceneName = mainMenuScene.name;
    }
#endif

    void Start()
    {
        startTime = Time.time;
        servers = FindObjectsOfType<Server>();
        requiredCount = Mathf.Clamp(targetServerCount, 1, Mathf.Max(1, servers.Length));

        foreach (var server in servers)
            server.OnServerDestroyed += OnServerDestroyed;

        if (buildRuntimeUI)
            EnsureRuntimeUI();

        HideWinUI();
    }

    void OnDestroy()
    {
        if (servers == null) return;
        foreach (var server in servers)
            server.OnServerDestroyed -= OnServerDestroyed;
    }

    private void OnServerDestroyed()
    {
        destroyedCount++;

        if (isComplete)
            return;

        if (destroyedCount >= requiredCount)
            CompleteGame();
    }

    private void CompleteGame()
    {
        isComplete = true;
        ShowWinUI();
        UpdateTimeText();

        if (showCursorOnWin)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void UpdateTimeText()
    {
        if (timeText == null)
            return;

        float elapsed = Time.time - startTime;
        var span = System.TimeSpan.FromSeconds(elapsed);
        int minutes = (int)span.TotalMinutes;
        int tenths = span.Milliseconds / 100;
        timeText.text = $"Time: {minutes:00}:{span.Seconds:00}.{tenths}";
    }

    private void ShowWinUI()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void HideWinUI()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void ReturnToMainMenu()
    {
        if (!string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        if (mainMenuSceneIndex >= 0 && mainMenuSceneIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(mainMenuSceneIndex);
    }

    private void EnsureRuntimeUI()
    {
        if (winCanvas == null)
        {
            GameObject canvasObj = new GameObject("WinCanvas");
            canvasObj.layer = LayerMask.NameToLayer("UI");
            winCanvas = canvasObj.AddComponent<Canvas>();
            winCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            winCanvas.sortingOrder = 2500;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (root == null)
        {
            GameObject rootObj = new GameObject("WinRoot", typeof(RectTransform), typeof(CanvasGroup));
            rootObj.transform.SetParent(winCanvas.transform, false);
            root = rootObj.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

        EnsureBackdrop();
        RectTransform panel = EnsurePanel();

        if (titleText == null)
            titleText = CreateText(panel, "Title", "CONGRATULATIONS", 42, FontStyles.Bold);
        titleText.color = accentColor;

        if (timeText == null)
            timeText = CreateText(panel, "TimeText", "Time: 00:00.0", 26, FontStyles.Normal);
        timeText.color = textColor;

        if (mainMenuButton == null)
            mainMenuButton = CreateButton(panel, "MainMenu", "Return to Main Menu", ReturnToMainMenu);

        EnsureEventSystem();
    }

    private void EnsureBackdrop()
    {
        Transform existing = root.Find("Backdrop");
        if (existing != null)
            return;

        GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(root, false);
        RectTransform rt = backdrop.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = backdropColor;
    }

    private RectTransform EnsurePanel()
    {
        Transform existing = root.Find("Panel");
        if (existing != null)
            return existing.GetComponent<RectTransform>();

        GameObject panelObj = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(Shadow));
        panelObj.transform.SetParent(root, false);
        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(620f, 320f);
        rt.anchoredPosition = Vector2.zero;

        Image image = panelObj.GetComponent<Image>();
        image.color = panelColor;

        Shadow shadow = panelObj.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(0f, -6f);

        VerticalLayoutGroup layout = panelObj.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 18f;
        layout.padding = new RectOffset(20, 20, 20, 20);

        panelObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rt;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, string text, int fontSize, FontStyles style)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform));
        textObj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;
        return tmp;
    }

    private Button CreateButton(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);
        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(320f, 56f);

        Image img = buttonObj.GetComponent<Image>();
        img.color = accentColor;

        Button button = buttonObj.GetComponent<Button>();
        button.onClick.AddListener(action);
        ColorBlock colors = button.colors;
        colors.normalColor = accentColor;
        colors.highlightedColor = buttonHoverColor;
        colors.selectedColor = buttonHoverColor;
        colors.pressedColor = buttonPressedColor;
        colors.disabledColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(buttonObj.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 22;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = buttonTextColor;

        return button;
    }

    private void EnsureEventSystem()
    {
        EventSystem existing = FindObjectOfType<EventSystem>();
        if (existing == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem", typeof(EventSystem));
            eventSystemObj.transform.SetParent(root, false);
            existing = eventSystemObj.GetComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (existing.GetComponent<InputSystemUIInputModule>() == null)
            existing.gameObject.AddComponent<InputSystemUIInputModule>();
#else
        if (existing.GetComponent<StandaloneInputModule>() == null)
            existing.gameObject.AddComponent<StandaloneInputModule>();
#endif
    }
}
