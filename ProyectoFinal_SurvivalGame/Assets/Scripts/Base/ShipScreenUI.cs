using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public class ShipScreenUI : MonoBehaviour
{
    private const string ScreenCanvasName = "GeneratedShipScreenCanvas";
    private const int SegmentCount = 16;

    [Header("References")]
    [SerializeField] private ShipStatusSystem _shipStatusSystem;
    [SerializeField] private Texture2D _screenBackgroundTexture;

    [Header("Screen Placement")]
    [SerializeField] private Vector2 _screenSize = new Vector2(1600f, 1024f);
    [SerializeField] private float _screenForwardOffset = 0.04f;
    [SerializeField] private bool _autoPositionOnObject = true;

    [Header("Dynamic Style")]
    [SerializeField] private Color _healthColor = new Color(1f, 0.05f, 0.04f, 0.94f);
    [SerializeField] private Color _energyColor = new Color(0f, 0.85f, 1f, 0.94f);
    [SerializeField] private Color _warningColor = new Color(1f, 0.08f, 0.03f, 0.94f);
    [SerializeField] private Color _hiddenSegmentColor = new Color(0.01f, 0.02f, 0.04f, 0.72f);
    [SerializeField] private Color _valueCoverColor = new Color(0.01f, 0.02f, 0.04f, 0.96f);
    [SerializeField] private Color _textColor = new Color(0.92f, 0.97f, 1f, 1f);

    private RectTransform _screenCanvasRect;
    private Image[] _healthSegments;
    private Image[] _energySegments;
    private Text _healthValueText;
    private Text _energyValueText;
    private Text _motorText;
    private Font _font;
    private Sprite _uiSprite;

    public void Initialize(ShipStatusSystem shipStatusSystem)
    {
        _shipStatusSystem = shipStatusSystem;
        EnsureScreen();
        Refresh();
    }

    private void Awake()
    {
        EnsureResources();
    }

    private void Start()
    {
        if (_shipStatusSystem == null)
        {
            _shipStatusSystem = ShipStatusSystem.Instance != null ? ShipStatusSystem.Instance : ShipStatusSystem.GetOrCreate();
        }

        EnsureScreen();
        Refresh();
    }

    private void Update()
    {
        if (_shipStatusSystem == null)
        {
            _shipStatusSystem = ShipStatusSystem.Instance;
        }

        EnsureScreen();
        Refresh();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureScreen();
        Refresh();
    }
#endif

    [ContextMenu("Rebuild Ship Screen UI")]
    public void RebuildScreen()
    {
        Transform existingCanvas = transform.Find(ScreenCanvasName);
        if (existingCanvas != null)
        {
            DestroyScreen(existingCanvas.gameObject);
        }

        BuildScreen();
        Refresh();
    }

    private void Refresh()
    {
        if (_shipStatusSystem == null)
        {
            return;
        }

        RefreshSegments(_healthSegments, _shipStatusSystem.CurrentShipHealth, _shipStatusSystem.MaxShipHealth, _healthColor);
        RefreshSegments(_energySegments, _shipStatusSystem.CurrentShipEnergy, _shipStatusSystem.MaxShipEnergy, _energyColor);

        if (_healthValueText != null)
        {
            _healthValueText.text = Mathf.RoundToInt(_shipStatusSystem.CurrentShipHealth) + " / " + Mathf.RoundToInt(_shipStatusSystem.MaxShipHealth);
            _healthValueText.color = GetValueColor(_shipStatusSystem.CurrentShipHealth, _shipStatusSystem.MaxShipHealth, _healthColor);
        }

        if (_energyValueText != null)
        {
            _energyValueText.text = Mathf.RoundToInt(_shipStatusSystem.CurrentShipEnergy) + " / " + Mathf.RoundToInt(_shipStatusSystem.MaxShipEnergy);
            _energyValueText.color = GetValueColor(_shipStatusSystem.CurrentShipEnergy, _shipStatusSystem.MaxShipEnergy, _energyColor);
        }

        if (_motorText != null)
        {
            int motorPercent = Mathf.RoundToInt(Mathf.Clamp01(_shipStatusSystem.CurrentShipEnergy / Mathf.Max(1f, _shipStatusSystem.MaxShipEnergy)) * 100f);
            _motorText.text = motorPercent + "%";
            _motorText.color = motorPercent > 25 ? _energyColor : _warningColor;
        }
    }

    private void RefreshSegments(Image[] segments, float currentValue, float maxValue, Color activeColor)
    {
        if (segments == null)
        {
            return;
        }

        float percent = Mathf.Clamp01(currentValue / Mathf.Max(1f, maxValue));
        int activeSegments = Mathf.CeilToInt(percent * segments.Length);
        Color finalActiveColor = percent > 0.25f ? activeColor : _warningColor;

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == null)
            {
                continue;
            }

            segments[i].color = i < activeSegments ? finalActiveColor : _hiddenSegmentColor;
        }
    }

    private Color GetValueColor(float currentValue, float maxValue, Color normalColor)
    {
        return currentValue / Mathf.Max(1f, maxValue) > 0.25f ? normalColor : _warningColor;
    }

    private void EnsureScreen()
    {
        EnsureResources();

        Transform existingCanvas = transform.Find(ScreenCanvasName);
        if (existingCanvas == null)
        {
            BuildScreen();
            return;
        }

        _screenCanvasRect = existingCanvas as RectTransform;
        if (_screenCanvasRect != null)
        {
            _screenCanvasRect.sizeDelta = _screenSize;
            if (_autoPositionOnObject)
            {
                PositionCanvasOnScreen(existingCanvas);
            }
        }

        BindScreenReferences(existingCanvas);
    }

    private void BuildScreen()
    {
        EnsureResources();

        GameObject canvasObject = new GameObject(ScreenCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        _screenCanvasRect = canvasObject.GetComponent<RectTransform>();
        _screenCanvasRect.sizeDelta = _screenSize;

        if (_autoPositionOnObject)
        {
            PositionCanvasOnScreen(canvasObject.transform);
        }

        RawImage background = canvasObject.AddComponent<RawImage>();
        background.texture = _screenBackgroundTexture;
        background.color = Color.white;
        background.raycastTarget = false;

        _healthSegments = CreateSegmentRow(canvasObject.transform, "HealthSegments", new Vector2(345f, -365f), new Vector2(53f, 74f), 7f, _healthColor);
        _energySegments = CreateSegmentRow(canvasObject.transform, "EnergySegments", new Vector2(345f, -622f), new Vector2(53f, 74f), 7f, _energyColor);

        CreateValueCover(canvasObject.transform, "HealthValueCover", new Vector2(1240f, -392f), new Vector2(250f, 94f));
        CreateValueCover(canvasObject.transform, "EnergyValueCover", new Vector2(1240f, -650f), new Vector2(250f, 94f));
        CreateValueCover(canvasObject.transform, "MotorValueCover", new Vector2(910f, -850f), new Vector2(120f, 36f));

        _healthValueText = CreateText("HealthValue", canvasObject.transform, "100 / 100", 48, TextAnchor.MiddleLeft, FontStyle.Bold, _healthColor);
        SetRect(_healthValueText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1265f, -395f), new Vector2(1515f, -310f));

        _energyValueText = CreateText("EnergyValue", canvasObject.transform, "100 / 100", 48, TextAnchor.MiddleLeft, FontStyle.Bold, _energyColor);
        SetRect(_energyValueText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1265f, -653f), new Vector2(1515f, -568f));

        _motorText = CreateText("MotorValue", canvasObject.transform, "100%", 22, TextAnchor.MiddleCenter, FontStyle.Bold, _energyColor);
        SetRect(_motorText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(910f, -850f), new Vector2(1030f, -814f));
    }

    private Image[] CreateSegmentRow(Transform parent, string rowName, Vector2 topLeft, Vector2 segmentSize, float spacing, Color activeColor)
    {
        GameObject row = CreateUIObject(rowName, parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        SetRect(rowRect, new Vector2(0f, 1f), new Vector2(0f, 1f), topLeft, topLeft + new Vector2((segmentSize.x + spacing) * SegmentCount, segmentSize.y));

        Image[] segments = new Image[SegmentCount];
        for (int i = 0; i < SegmentCount; i++)
        {
            GameObject segmentObject = CreateUIObject("Segment_" + i, row.transform);
            RectTransform segmentRect = segmentObject.GetComponent<RectTransform>();
            segmentRect.anchorMin = new Vector2(0f, 0.5f);
            segmentRect.anchorMax = new Vector2(0f, 0.5f);
            segmentRect.pivot = new Vector2(0.5f, 0.5f);
            segmentRect.anchoredPosition = new Vector2((segmentSize.x + spacing) * i + segmentSize.x * 0.5f, 0f);
            segmentRect.sizeDelta = segmentSize;
            segmentRect.localRotation = Quaternion.Euler(0f, 0f, -8f);

            Image segmentImage = segmentObject.AddComponent<Image>();
            segmentImage.sprite = _uiSprite;
            segmentImage.color = activeColor;
            segmentImage.raycastTarget = false;
            segments[i] = segmentImage;
        }

        return segments;
    }

    private void CreateValueCover(Transform parent, string name, Vector2 topLeft, Vector2 size)
    {
        GameObject coverObject = CreateUIObject(name, parent);
        RectTransform coverRect = coverObject.GetComponent<RectTransform>();
        SetRect(coverRect, new Vector2(0f, 1f), new Vector2(0f, 1f), topLeft, topLeft + size);

        Image coverImage = coverObject.AddComponent<Image>();
        coverImage.sprite = _uiSprite;
        coverImage.color = _valueCoverColor;
        coverImage.raycastTarget = false;
    }

    private void PositionCanvasOnScreen(Transform canvasTransform)
    {
        Renderer rendererComponent = GetComponent<Renderer>();
        if (rendererComponent == null)
        {
            canvasTransform.localPosition = Vector3.forward * 0.52f;
            canvasTransform.localRotation = Quaternion.identity;
            canvasTransform.localScale = Vector3.one * 0.002f;
            return;
        }

        Bounds bounds = rendererComponent.bounds;
        canvasTransform.position = bounds.center + transform.forward * (bounds.extents.z + _screenForwardOffset);
        canvasTransform.rotation = transform.rotation;

        float screenScale = Mathf.Min(bounds.size.x / _screenSize.x, bounds.size.y / _screenSize.y);
        float safeScale = Mathf.Max(0.001f, screenScale);
        Vector3 parentScale = transform.lossyScale;
        canvasTransform.localScale = new Vector3(
            safeScale / Mathf.Max(0.0001f, parentScale.x),
            safeScale / Mathf.Max(0.0001f, parentScale.y),
            safeScale / Mathf.Max(0.0001f, parentScale.z));
    }

    private void BindScreenReferences(Transform screenCanvas)
    {
        _healthSegments = BindSegmentRow(FindChildRecursive(screenCanvas, "HealthSegments"));
        _energySegments = BindSegmentRow(FindChildRecursive(screenCanvas, "EnergySegments"));
        _healthValueText = FindChildRecursive(screenCanvas, "HealthValue")?.GetComponent<Text>();
        _energyValueText = FindChildRecursive(screenCanvas, "EnergyValue")?.GetComponent<Text>();
        _motorText = FindChildRecursive(screenCanvas, "MotorValue")?.GetComponent<Text>();
    }

    private Image[] BindSegmentRow(Transform row)
    {
        if (row == null)
        {
            return null;
        }

        Image[] segments = new Image[SegmentCount];
        for (int i = 0; i < SegmentCount; i++)
        {
            segments[i] = FindChildRecursive(row, "Segment_" + i)?.GetComponent<Image>();
        }

        return segments;
    }

    private void EnsureResources()
    {
        if (_font == null)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        if (_uiSprite == null)
        {
            _uiSprite = CreateRuntimeSprite();
        }
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject uiObject = new GameObject(name, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor alignment, FontStyle style, Color color)
    {
        GameObject textObject = CreateUIObject(name, parent);
        Text text = textObject.AddComponent<Text>();
        text.font = _font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform match = FindChildRecursive(child, childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void DestroyScreen(GameObject screenObject)
    {
        if (Application.isPlaying)
        {
            Destroy(screenObject);
        }
        else
        {
            DestroyImmediate(screenObject);
        }
    }

    private static Sprite CreateRuntimeSprite()
    {
        Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[64];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
    }
}
