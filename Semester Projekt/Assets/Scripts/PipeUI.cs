using UnityEngine;
using TMPro;
using System.Collections;

public class PipeUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI promptText;
    public CanvasGroup canvasGroup;

    [Header("UI Settings")]
    public float fadeSpeed = 5f;
    public string promptMessage = "Press F to Enter Pipe";

    [Header("Text Style")]
    public bool useOutline = true;
    public Color outlineColor = Color.black;
    [Range(0f, 1f)]
    public float outlineWidth = 0.2f;

    private float targetAlpha = 0f;
    private Canvas canvas;
    private bool wasPromptVisible = false;

    void Start()
    {
        // Find Canvas
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("PipeUI: No Canvas parent found!");
            return;
        }

        // Find CanvasGroup
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                Debug.LogWarning("PipeUI: Creating CanvasGroup");
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Find TextMeshProUGUI
        if (promptText == null)
        {
            promptText = GetComponentInChildren<TextMeshProUGUI>();
            if (promptText == null)
            {
                Debug.LogError("PipeUI: No TextMeshProUGUI found!");
                return;
            }
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (promptText != null)
        {
            promptText.text = promptMessage;
            promptText.color = new Color(1f, 1f, 1f, 1f);
            if (useOutline)
            {
                promptText.outlineColor = outlineColor;
                promptText.outlineWidth = outlineWidth;
            }
            else
            {
                promptText.outlineWidth = 0f;
            }
        }

        Debug.Log("PipeUI initialized successfully");
    }

    void Update()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
    }

    public void ShowPrompt()
    {
        ShowPrompt(promptMessage);
    }

    public void ShowPrompt(string customMessage)
    {
        if (promptText != null)
            promptText.text = customMessage;

        if (!wasPromptVisible)
        {
            wasPromptVisible = true;
            Debug.Log("Prompt shown");
        }

        targetAlpha = 1f;
    }

    public void HidePrompt()
    {
        if (wasPromptVisible)
        {
            wasPromptVisible = false;
            Debug.Log("Prompt hidden");
        }

        targetAlpha = 0f;
    }

    public void ShowTraversalMessage()
    {
        StartCoroutine(ShowMessageTemporarily("Traversing pipe..."));
    }

    private IEnumerator ShowMessageTemporarily(string message)
    {
        if (promptText == null)
            yield break;

        string originalText = promptText.text;
        promptText.text = message;

        yield return new WaitForSeconds(0.5f);

        promptText.text = originalText;
    }
}