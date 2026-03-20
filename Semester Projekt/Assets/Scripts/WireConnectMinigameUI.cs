using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Simple "connect the wires" UI. Builds a panel and allows dragging from left nodes to matching right nodes.
/// </summary>
public class WireConnectMinigameUI : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private WireConnectStation currentStation;

    private RectTransform panelRT;
    private RectTransform wiresLayer;

    private class Node
    {
        public RectTransform rt;
        public Color color;
        public bool connected;
    }

    private readonly List<Node> leftNodes = new();
    private readonly List<Node> rightNodes = new();
    private readonly List<Image> wires = new();

    private int draggingIndex = -1;
    private float targetAlpha = 0f;

    private static readonly Color[] baseColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.yellow,
        Color.green
    };

    void Awake()
    {
        if (transform as RectTransform == null)
            gameObject.AddComponent<RectTransform>();
    }

    void Start()
    {
        EnsureCanvasGroup();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Show(WireConnectStation station)
    {
        currentStation = station;
        BuildUI();
        EnsureCanvasGroup();

        targetAlpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        targetAlpha = 0f;
        EnsureCanvasGroup();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        currentStation = null;
        draggingIndex = -1;
    }

    void Update()
    {
        EnsureCanvasGroup();
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * 8f);

        if (currentStation == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            for (int i = 0; i < leftNodes.Count; i++)
            {
                if (!leftNodes[i].connected && RectTransformUtility.RectangleContainsScreenPoint(leftNodes[i].rt, Input.mousePosition, null))
                {
                    draggingIndex = i;
                    var wire = wires[i];
                    wire.gameObject.SetActive(true);
                    wire.color = leftNodes[i].color;
                    break;
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (draggingIndex != -1)
            {
                bool matched = false;
                for (int i = 0; i < rightNodes.Count; i++)
                {
                    if (!rightNodes[i].connected &&
                        rightNodes[i].color == leftNodes[draggingIndex].color &&
                        RectTransformUtility.RectangleContainsScreenPoint(rightNodes[i].rt, Input.mousePosition, null))
                    {
                        rightNodes[i].connected = true;
                        leftNodes[draggingIndex].connected = true;
                        UpdateWire(wires[draggingIndex], leftNodes[draggingIndex].rt, rightNodes[i].rt.position, true);
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    wires[draggingIndex].gameObject.SetActive(false);
                }

                draggingIndex = -1;
                CheckWin();
            }
        }

        if (draggingIndex != -1)
        {
            UpdateWire(wires[draggingIndex], leftNodes[draggingIndex].rt, Input.mousePosition, false);
        }
    }

    private void CheckWin()
    {
        for (int i = 0; i < leftNodes.Count; i++)
        {
            if (!leftNodes[i].connected)
                return;
        }

        currentStation?.Complete();
    }

    private void UpdateWire(Image wire, RectTransform startNode, Vector3 target, bool targetIsWorld)
    {
        if (wire == null || panelRT == null) return;

        RectTransform rt = wire.rectTransform;

        Vector2 localStart = panelRT.InverseTransformPoint(startNode.position);

        Vector3 worldEnd;
        if (targetIsWorld)
        {
            worldEnd = target;
        }
        else
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(panelRT, target, null, out worldEnd);
        }

        Vector2 localEnd = panelRT.InverseTransformPoint(worldEnd);

        rt.SetParent(wiresLayer, false);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = localStart;

        Vector2 dir = localEnd - localStart;
        float dist = dir.magnitude;

        rt.sizeDelta = new Vector2(dist, 10f);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void BuildUI()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        leftNodes.Clear();
        rightNodes.Clear();
        wires.Clear();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            var canvasObj = new GameObject("WireMinigameCanvas");
            canvasObj.layer = LayerMask.NameToLayer("UI");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 1000);
        }

        transform.SetParent(canvas.transform, false);
        transform.SetAsLastSibling();

        GameObject overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(transform, false);
        var overlayRT = overlay.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

        GameObject panelObj = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(transform, false);
        panelRT = panelObj.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = new Vector2(720f, 520f);
        panelObj.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);

        CreateText(panelRT, "Title", "Connect the Wires", new Vector2(0f, 200f), 34, FontStyles.Bold);
        CreateText(panelRT, "Hint", "Drag from left nodes to matching right nodes", new Vector2(0f, 170f), 20, FontStyles.Italic);

        GameObject wiresObj = new GameObject("Wires", typeof(RectTransform));
        wiresObj.transform.SetParent(panelRT, false);
        wiresLayer = wiresObj.GetComponent<RectTransform>();
        wiresLayer.anchorMin = Vector2.zero;
        wiresLayer.anchorMax = Vector2.one;
        wiresLayer.offsetMin = Vector2.zero;
        wiresLayer.offsetMax = Vector2.zero;

        List<Color> leftCol = new List<Color>(baseColors);
        List<Color> rightCol = new List<Color>(baseColors);
        Shuffle(leftCol);
        Shuffle(rightCol);

        float startY = 120f;
        float spacingY = -100f;
        float leftX = -220f;
        float rightX = 220f;

        for (int i = 0; i < baseColors.Length; i++)
        {
            GameObject wireObj = new GameObject($"Wire_{i}", typeof(RectTransform), typeof(Image));
            wireObj.transform.SetParent(wiresLayer, false);
            var wireImg = wireObj.GetComponent<Image>();
            wireImg.raycastTarget = false;
            wireObj.SetActive(false);
            wires.Add(wireImg);

            RectTransform lrt = CreateNode(panelRT, new Vector2(leftX, startY + i * spacingY), leftCol[i]);
            leftNodes.Add(new Node { rt = lrt, color = leftCol[i], connected = false });

            RectTransform rrt = CreateNode(panelRT, new Vector2(rightX, startY + i * spacingY), rightCol[i]);
            rightNodes.Add(new Node { rt = rrt, color = rightCol[i], connected = false });
        }
    }

    private void Shuffle(List<Color> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private RectTransform CreateNode(Transform parent, Vector2 pos, Color c)
    {
        var go = new GameObject("Node", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(48f, 48f);

        var img = go.GetComponent<Image>();
        img.color = c;
        img.raycastTarget = true;
        return rt;
    }

    private void CreateText(RectTransform parent, string name, string text, Vector2 anchoredPos, int size, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(460f, 32f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = Color.white;
    }
}
