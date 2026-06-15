using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDInitializer : MonoBehaviour
{
    public static HUDInitializer Instance { get; private set; }

    private void Awake()
    {
        Initialize(this);
    }

    public static void Initialize(HUDInitializer instance)
    {
        if (Instance == null) Instance = instance;
    }

    public Canvas EnsureCanvas()
    {
        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("HUD_Canvas");
            canvasGO.transform.SetParent(this.transform, false);
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        return canvas;
    }

    public Slider CreateSlider(string name, Vector2 anchoredPos, Color color, Transform parent)
    {
        return CreateSlider(name, anchoredPos, color, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
    }

    public Slider CreateSlider(string name, Vector2 anchoredPos, Color color, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        GameObject sliderGO = new GameObject(name);
        sliderGO.transform.SetParent(parent, false);
        
        RectTransform rt = sliderGO.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(250, 24); // Slightly wider and taller for readability
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;

        Slider slider = sliderGO.AddComponent<Slider>();

        // Background
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.75f); // Darker, more contrast
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        // Fill Area
        GameObject fillAreaGO = new GameObject("Fill Area");
        fillAreaGO.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRT = fillAreaGO.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.sizeDelta = new Vector2(-10, -4);

        // Fill
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAreaGO.transform, false);
        Image fillImage = fillGO.AddComponent<Image>();
        fillImage.color = color;
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(1, 1);
        fillRT.sizeDelta = Vector2.zero;

        slider.fillRect = fillRT;
        slider.targetGraphic = fillImage;
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.value = 100;

        return slider;
    }

    public TextMeshProUGUI CreateText(string name, string defaultText, Vector2 anchoredPos, int fontSize, Transform parent, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
    {
        return CreateText(name, defaultText, anchoredPos, fontSize, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), alignment);
    }

    public TextMeshProUGUI CreateText(string name, string defaultText, Vector2 anchoredPos, int fontSize, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);
        
        RectTransform rt = textGO.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(450, 50); // Slightly larger box to fit all text
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white; // Ensure default readability

        return tmp;
    }

    public Image CreateOverlay(string name, Color color, Transform parent)
    {
        GameObject overlayGO = new GameObject(name);
        overlayGO.transform.SetParent(parent, false);
        
        RectTransform rt = overlayGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image img = overlayGO.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        return img;
    }
}
