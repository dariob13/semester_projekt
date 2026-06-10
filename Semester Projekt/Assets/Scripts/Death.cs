using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-150)]
public class Death : MonoBehaviour
{
    [Header("UI References")]
    public Canvas deathCanvas;
    public RectTransform root;
    public CanvasGroup canvasGroup;
    public Image backdrop;
    public Image panel;
    public Image accentStrip;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI messageText;
    public Button restartButton;
    public Button quitButton;

    [Header("Colors")]
    public Color backdropColor = new Color(0f, 0f, 0f, 0.78f);
    public Color panelColor = new Color(0.06f, 0.07f, 0.09f, 0.96f);
    public Color accentColor = new Color(1f, 0.22f, 0.2f, 1f);
    public Color buttonColor = new Color(0.14f, 0.14f, 0.16f, 1f);
    public Color buttonHoverColor = new Color(0.22f, 0.22f, 0.26f, 1f);
    public Color buttonPressedColor = new Color(0.32f, 0.12f, 0.12f, 1f);
    public Color textColor = Color.white;

    public LiquidSolidForm player;

    private static Death instance;
    private float previousTimeScale = 1f;
    private bool timeScaleCaptured;
    private bool isVisible;
    private bool playerGameplayDisabled;
    private bool playerWasActive = true;

    public static Death Instance => instance;

    public static Death GetOrCreate()
    {
        if (instance != null)
            return instance;

        Death existing = FindObjectOfType<Death>();
        if (existing != null)
            return existing;

        GameObject deathObject = new GameObject("DeathUI", typeof(RectTransform));
        return deathObject.AddComponent<Death>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureRuntimeUI();
        HideImmediate();
    }

    private void Start()
    {
        ResolvePlayer();
        SubscribeToPlayer();

        if (player != null && player.GetIsDead())
            ShowDeath();
    }

    private void Update()
    {
        if (!isVisible)
        {
            if (player == null)
                ResolvePlayer();

            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }
        else if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromPlayer();

        if (instance == this)
            instance = null;

        RestoreTimeScale();
    }

    public void ShowDeath()
    {
        EnsureRuntimeUI();

        if (isVisible)
            return;

        ResolvePlayer();
        SubscribeToPlayer();
        DisablePlayerGameplay();

        isVisible = true;

        if (!timeScaleCaptured)
        {
            previousTimeScale = Time.timeScale;
            timeScaleCaptured = true;
        }

        Time.timeScale = 0f;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (titleText != null)
            titleText.text = "GAME OVER";

        if (messageText != null)
            messageText.text = "Press R to restart the level or Q to quit.";

        if (restartButton != null)
            SetButtonLabel(restartButton, "RESTART");

        if (quitButton != null)
            SetButtonLabel(quitButton, "QUIT");

        if (restartButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(restartButton.gameObject);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void HideDeath()
    {
        isVisible = false;
        RestorePlayerGameplay();
        HideImmediate();
        RestoreTimeScale();
    }

    public void RestartLevel()
    {
        RestoreTimeScale();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        RestoreTimeScale();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void RestoreTimeScale()
    {
        if (!timeScaleCaptured)
            return;

        Time.timeScale = previousTimeScale;
        timeScaleCaptured = false;
    }

    private void DisablePlayerGameplay()
    {
        if (player == null || playerGameplayDisabled)
            return;

        playerWasActive = player.gameObject.activeSelf;

        if (playerWasActive)
            player.gameObject.SetActive(false);

        playerGameplayDisabled = true;
    }

    private void RestorePlayerGameplay()
    {
        if (player == null || !playerGameplayDisabled)
            return;

        if (playerWasActive)
            player.gameObject.SetActive(true);

        playerGameplayDisabled = false;
    }

    private void ResolvePlayer()
    {
        if (player != null)
            return;

        player = FindObjectOfType<LiquidSolidForm>();
    }

    private void SubscribeToPlayer()
    {
        if (player == null)
            return;

        player.OnPlayerDied -= HandlePlayerDied;
        player.OnPlayerDied += HandlePlayerDied;
    }

    private void UnsubscribeFromPlayer()
    {
        if (player == null)
            return;

        player.OnPlayerDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        ShowDeath();
    }

    private void EnsureRuntimeUI()
    {
        Canvas canvas = deathCanvas;
        if (canvas == null)
        {
            GameObject canvasObject = GameObject.Find("DeathCanvas");
            if (canvasObject == null)
            {
                canvasObject = new GameObject("DeathCanvas");
                canvasObject.layer = LayerMask.NameToLayer("UI");
            }

            canvas = canvasObject.GetComponent<Canvas>();
            if (canvas == null)
                canvas = canvasObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 8000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvasObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasObject.GetComponent<GraphicRaycaster>() == null)
                canvasObject.AddComponent<GraphicRaycaster>();

            deathCanvas = canvas;
        }

        if (root == null)
        {
            Transform existingRoot = deathCanvas.transform.Find("DeathRoot");
            if (existingRoot != null)
            {
                root = existingRoot as RectTransform;
            }
            else
            {
                GameObject rootObject = new GameObject("DeathRoot", typeof(RectTransform), typeof(CanvasGroup));
                rootObject.transform.SetParent(deathCanvas.transform, false);
                root = rootObject.GetComponent<RectTransform>();
            }
        }

        if (root != null)
        {
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            if (canvasGroup == null)
                canvasGroup = root.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
        }

        if (backdrop == null)
        {
            GameObject backdropObject = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdropObject.transform.SetParent(root, false);
            RectTransform backdropRect = backdropObject.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;

            backdrop = backdropObject.GetComponent<Image>();
            backdrop.color = backdropColor;
            backdrop.raycastTarget = false;
        }

        if (panel == null)
        {
            GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(root, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(920f, 500f);
            panelRect.anchoredPosition = Vector2.zero;

            panel = panelObject.GetComponent<Image>();
            panel.color = panelColor;

            GameObject frameObject = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frameObject.transform.SetParent(panelObject.transform, false);
            RectTransform frameRect = frameObject.GetComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0f, 0f);
            frameRect.anchorMax = new Vector2(1f, 1f);
            frameRect.offsetMin = new Vector2(-10f, -10f);
            frameRect.offsetMax = new Vector2(10f, 10f);

            Image frameImage = frameObject.GetComponent<Image>();
            frameImage.color = accentColor;
            frameImage.raycastTarget = false;
        }

        if (accentStrip == null)
        {
            GameObject accentObject = new GameObject("AccentStrip", typeof(RectTransform), typeof(Image));
            accentObject.transform.SetParent(panel != null ? panel.transform : root, false);
            RectTransform accentRect = accentObject.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.sizeDelta = new Vector2(0f, 12f);
            accentRect.anchoredPosition = Vector2.zero;

            accentStrip = accentObject.GetComponent<Image>();
            accentStrip.color = accentColor;
            accentStrip.raycastTarget = false;
        }

        if (titleText == null)
            titleText = CreateText("TitleText", panel != null ? panel.transform : root, new Vector2(0f, 120f), 72f, FontStyles.Bold, TextAlignmentOptions.Center);

        if (messageText == null)
            messageText = CreateText("MessageText", panel != null ? panel.transform : root, new Vector2(0f, 35f), 30f, FontStyles.Normal, TextAlignmentOptions.Center);

        if (restartButton == null)
            restartButton = CreateButton("RestartButton", panel != null ? panel.transform : root, new Vector2(0f, -90f), "RESTART", RestartLevel);

        if (quitButton == null)
            quitButton = CreateButton("QuitButton", panel != null ? panel.transform : root, new Vector2(0f, -165f), "QUIT", QuitGame);

        if (EventSystem.current == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemObject.layer = LayerMask.NameToLayer("UI");
        }
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, Vector2 anchoredPosition, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = anchoredPosition;
        textRect.sizeDelta = new Vector2(840f, 120f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.color = textColor;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.raycastTarget = false;

        return text;
    }

    private Button CreateButton(string objectName, Transform parent, Vector2 anchoredPosition, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(280f, 58f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = buttonColor;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = buttonPressedColor;
        colors.selectedColor = buttonHoverColor;
        colors.disabledColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 28f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        labelText.raycastTarget = false;

        return button;
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null)
            return;

        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText != null)
            labelText.text = label;
    }
}
