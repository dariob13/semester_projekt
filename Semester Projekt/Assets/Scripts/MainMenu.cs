using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Runtime Build")]
    public bool buildRuntimeUI = true;
    public bool showOnStart = true;
    public bool allowEscapeToggle = false;
    public bool manageCursor = true;

    [Header("UI References")]
    public Canvas menuCanvas;
    public RectTransform root;
    public CanvasGroup canvasGroup;
    public GameObject mainPanel;
    public GameObject levelSelectPanel;
    public GameObject creditsPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI creditsText;

    [Header("Colors")]
    public Color accentColor = new Color(0.2f, 0.85f, 1f, 1f);
    public Color panelColor = new Color(0f, 0f, 0f, 0.7f);
    public Color backdropColor = new Color(0f, 0f, 0f, 0.45f);
    public Color buttonColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    public Color buttonHoverColor = new Color(0.15f, 0.22f, 0.28f, 0.95f);
    public Color buttonPressedColor = new Color(0.18f, 0.35f, 0.45f, 1f);
    public Color textColor = Color.white;

    private bool isVisible;
    private readonly List<Button> levelButtons = new List<Button>();

    void Awake()
    {
        if (buildRuntimeUI)
            EnsureRuntimeUI();

        if (showOnStart)
            ShowMenu();
        else
            HideMenu();
    }

    void Update()
    {
        if (allowEscapeToggle && Input.GetKeyDown(KeyCode.Escape))
        {
            if (isVisible)
                HideMenu();
            else
                ShowMenu();
        }
    }

    public void ShowMenu()
    {
        if (canvasGroup == null)
            EnsureRuntimeUI();

        isVisible = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        ShowPanel(mainPanel);

        if (manageCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void HideMenu()
    {
        isVisible = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (manageCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void OpenLevelSelect()
    {
        BuildLevelButtons();
        ShowPanel(levelSelectPanel);
    }

    public void OpenCredits()
    {
        ShowPanel(creditsPanel);
    }

    public void OpenMain()
    {
        ShowPanel(mainPanel);
    }

    public void LoadLevelByIndex(int buildIndex)
    {
        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
            return;

        SceneManager.LoadScene(buildIndex);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("QuitGame called (editor).");
#else
        Application.Quit();
#endif
    }

    private void ShowPanel(GameObject panel)
    {
        if (mainPanel != null) mainPanel.SetActive(panel == mainPanel);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(panel == levelSelectPanel);
        if (creditsPanel != null) creditsPanel.SetActive(panel == creditsPanel);
    }

    private void EnsureRuntimeUI()
    {
        if (menuCanvas == null)
        {
            GameObject canvasObj = new GameObject("MainMenuCanvas");
            canvasObj.layer = LayerMask.NameToLayer("UI");
            menuCanvas = canvasObj.AddComponent<Canvas>();
            menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            menuCanvas.sortingOrder = 2000;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (root == null)
        {
            GameObject rootObj = new GameObject("MainMenuRoot", typeof(RectTransform), typeof(CanvasGroup));
            rootObj.transform.SetParent(menuCanvas.transform, false);
            root = rootObj.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
        }

        if (canvasGroup == null)
            canvasGroup = root.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

        EnsureBackdrop();

        RectTransform window = EnsureWindow();
        EnsureTitle(window);

        if (mainPanel == null)
            mainPanel = BuildMainPanel(window);

        if (levelSelectPanel == null)
            levelSelectPanel = BuildLevelSelectPanel(window);

        if (creditsPanel == null)
            creditsPanel = BuildCreditsPanel(window);

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

        Image img = backdrop.GetComponent<Image>();
        img.color = backdropColor;
        img.raycastTarget = false;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(esObj);
    }

    private RectTransform EnsureWindow()
    {
        Transform existing = root.Find("Window");
        if (existing != null)
            return existing as RectTransform;

        GameObject windowObj = new GameObject("Window", typeof(RectTransform), typeof(Image));
        windowObj.transform.SetParent(root, false);

        RectTransform rt = windowObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(600f, 520f);

        Image panel = windowObj.GetComponent<Image>();
        panel.color = panelColor;

        Outline outline = windowObj.AddComponent<Outline>();
        outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);

        return rt;
    }

    private void EnsureTitle(RectTransform window)
    {
        if (titleText != null)
            return;

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(window, false);
        RectTransform rt = titleObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -30f);
        rt.sizeDelta = new Vector2(520f, 60f);

        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "MAIN MENU";
        titleText.fontSize = 42;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = accentColor;
        titleText.fontStyle = FontStyles.Bold;
    }

    private GameObject BuildMainPanel(RectTransform window)
    {
        GameObject panel = CreatePanel("MainPanel", window, new Vector2(30f, 90f), new Vector2(30f, 30f));
        RectTransform list = CreateVerticalList(panel.transform, "MainButtons");

        CreateButton(list, "Level Select", OpenLevelSelect);
        CreateButton(list, "Credits", OpenCredits);
        CreateButton(list, "Quit", QuitGame);

        return panel;
    }

    private GameObject BuildLevelSelectPanel(RectTransform window)
    {
        GameObject panel = CreatePanel("LevelSelectPanel", window, new Vector2(30f, 90f), new Vector2(30f, 30f));
        RectTransform list = CreateVerticalList(panel.transform, "LevelButtons");

        TextMeshProUGUI header = CreateText(list, "Header", "SELECT LEVEL", 28, FontStyles.Bold, TextAlignmentOptions.Center);
        header.color = accentColor;
        AddLayoutSpacing(list, 6f);

        RectTransform levelList = CreateVerticalList(list, "LevelList");
        BuildLevelButtons(levelList);

        AddLayoutSpacing(list, 6f);
        CreateButton(list, "Back", OpenMain);

        return panel;
    }

    private GameObject BuildCreditsPanel(RectTransform window)
    {
        GameObject panel = CreatePanel("CreditsPanel", window, new Vector2(30f, 90f), new Vector2(30f, 30f));
        RectTransform list = CreateVerticalList(panel.transform, "CreditsContent");

        TextMeshProUGUI header = CreateText(list, "Header", "CREDITS", 28, FontStyles.Bold, TextAlignmentOptions.Center);
        header.color = accentColor;

        creditsText = CreateText(list, "CreditsText",
            "Dario Butnariu\nArt • Design • Code\nSpecial thanks to everyone who played",
            20, FontStyles.Normal, TextAlignmentOptions.Center);
        creditsText.color = textColor;

        AddLayoutSpacing(list, 8f);
        CreateButton(list, "Back", OpenMain);

        return panel;
    }

    private GameObject CreatePanel(string name, RectTransform window, Vector2 paddingTopLeft, Vector2 paddingBottomRight)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(window, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(paddingTopLeft.x, paddingBottomRight.y);
        rt.offsetMax = new Vector2(-paddingBottomRight.x, -paddingTopLeft.y);
        return panel;
    }

    private RectTransform CreateVerticalList(Transform parent, string name)
    {
        GameObject listObj = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        listObj.transform.SetParent(parent, false);

        RectTransform rt = listObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = listObj.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = listObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        return rt;
    }

    private void AddLayoutSpacing(RectTransform list, float height)
    {
        GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(list, false);
        LayoutElement le = spacer.GetComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleHeight = 0f;
    }

    private void BuildLevelButtons()
    {
        if (levelSelectPanel == null)
            return;

        Transform list = levelSelectPanel.transform.Find("LevelButtons/LevelList");
        if (list == null)
            return;

        foreach (Transform child in list)
            Destroy(child.gameObject);
        levelButtons.Clear();

        int sceneCount = SceneManager.sceneCountInBuildSettings;
        if (sceneCount == 0)
        {
            TextMeshProUGUI none = CreateText(list, "NoScenes", "No scenes in Build Settings", 18, FontStyles.Italic, TextAlignmentOptions.Center);
            none.color = new Color(1f, 0.75f, 0.4f, 1f);
            return;
        }

        for (int i = 0; i < sceneCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = string.IsNullOrEmpty(path) ? $"Scene {i}" : Path.GetFileNameWithoutExtension(path);
            int index = i;

            Button button = CreateButton(list, sceneName, () => LoadLevelByIndex(index));
            levelButtons.Add(button);
        }
    }

    private void BuildLevelButtons(RectTransform listOverride)
    {
        if (listOverride == null)
            return;

        foreach (Transform child in listOverride)
            Destroy(child.gameObject);
        levelButtons.Clear();

        int sceneCount = SceneManager.sceneCountInBuildSettings;
        if (sceneCount == 0)
        {
            TextMeshProUGUI none = CreateText(listOverride, "NoScenes", "No scenes in Build Settings", 18, FontStyles.Italic, TextAlignmentOptions.Center);
            none.color = new Color(1f, 0.75f, 0.4f, 1f);
            return;
        }

        for (int i = 0; i < sceneCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = string.IsNullOrEmpty(path) ? $"Scene {i}" : Path.GetFileNameWithoutExtension(path);
            int index = i;

            Button button = CreateButton(listOverride, sceneName, () => LoadLevelByIndex(index));
            levelButtons.Add(button);
        }
    }

    private Button CreateButton(Transform parent, string label, UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);

        RectTransform rt = buttonObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 48f);

        Image image = buttonObj.GetComponent<Image>();
        image.color = buttonColor;

        Button button = buttonObj.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonHoverColor;
        colors.pressedColor = buttonPressedColor;
        colors.selectedColor = buttonHoverColor;
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Outline outline = buttonObj.AddComponent<Outline>();
        outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.6f);
        outline.effectDistance = new Vector2(1f, -1f);

        TextMeshProUGUI text = CreateText(buttonObj.transform, "Label", label, 22, FontStyles.Bold, TextAlignmentOptions.Center);
        text.color = textColor;

        LayoutElement le = buttonObj.AddComponent<LayoutElement>();
        le.preferredHeight = 48f;
        le.minHeight = 48f;
        le.flexibleHeight = 0f;

        return button;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text, int size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform));
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.color = textColor;
        tmp.raycastTarget = false;

        return tmp;
    }
}
