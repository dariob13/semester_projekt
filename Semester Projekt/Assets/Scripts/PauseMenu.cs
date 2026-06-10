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

public class PauseMenu : MonoBehaviour
{
    [Header("UI Settings")]
    public bool buildRuntimeUI = true;

    [Header("UI References")]
    public Canvas pauseCanvas;
    public RectTransform root;
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI titleText;
    public Button continueButton;
    public Button restartButton;
    public Button quitButton;

    [Header("Colors")]
    public Color accentColor = new Color(0.2f, 0.85f, 1f, 1f);
    public Color buttonHoverColor = new Color(0.35f, 0.9f, 1f, 1f);
    public Color buttonPressedColor = new Color(0.1f, 0.65f, 0.85f, 1f);
    public Color buttonTextColor = Color.white;
    public Color panelColor = new Color(0f, 0f, 0f, 0.8f);
    public Color backdropColor = new Color(0f, 0f, 0f, 0.6f);
    public Color textColor = Color.white;

    private bool isPaused;

    void Start()
    {
        if (buildRuntimeUI)
            EnsureRuntimeUI();

        HidePauseUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        ShowPauseUI();
    }

    private void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        HidePauseUI();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowPauseUI()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (continueButton != null)
            continueButton.Select();
    }

    private void HidePauseUI()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void EnsureRuntimeUI()
    {
        if (pauseCanvas == null)
        {
            GameObject canvasObj = new GameObject("PauseCanvas");
            canvasObj.layer = LayerMask.NameToLayer("UI");
            pauseCanvas = canvasObj.AddComponent<Canvas>();
            pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            pauseCanvas.sortingOrder = 2000;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (root == null)
        {
            GameObject rootObj = new GameObject("PauseRoot", typeof(RectTransform), typeof(CanvasGroup));
            rootObj.transform.SetParent(pauseCanvas.transform, false);
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
            titleText = CreateText(panel, "Title", "PAUSED", 42, FontStyles.Bold);
        titleText.color = accentColor;

        if (continueButton == null)
            continueButton = CreateButton(panel, "Continue", "Continue", Resume);

        if (restartButton == null)
            restartButton = CreateButton(panel, "Restart", "Restart Level", RestartLevel);

        if (quitButton == null)
            quitButton = CreateButton(panel, "Quit", "Quit Game", QuitGame);

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
        rt.sizeDelta = new Vector2(620f, 360f);
        rt.anchoredPosition = Vector2.zero;

        Image image = panelObj.GetComponent<Image>();
        image.color = panelColor;

        Shadow shadow = panelObj.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(0f, -6f);

        VerticalLayoutGroup layout = panelObj.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 40f;
        layout.padding = new RectOffset(20, 20, 40, 40);
        layout.childForceExpandHeight = false;

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
        rt.sizeDelta = new Vector2(320f, 60f);

        LayoutElement layoutElement = buttonObj.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 320f;
        layoutElement.preferredHeight = 60f;

        Image img = buttonObj.GetComponent<Image>();
        img.color = accentColor;

        Button button = buttonObj.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
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
