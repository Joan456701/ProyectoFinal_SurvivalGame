using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SurvivalVitalsHUD : MonoBehaviour
{
    private const string HudRootName = "GeneratedSurvivalVitalsHUD";

    [Header("References")]
    [SerializeField] private FirstPersonController _playerController;
    [SerializeField] private OxygenSystem _oxygenSystem;

    [Header("Style")]
    [SerializeField] private Color _healthColor = new Color(1f, 0.24f, 0.18f, 1f);
    [SerializeField] private Color _oxygenColor = new Color(0.13f, 0.9f, 1f, 1f);
    [SerializeField] private Color _warningColor = new Color(1f, 0.18f, 0.12f, 1f);
    [SerializeField] private Color _backgroundColor = new Color(0.01f, 0.08f, 0.12f, 0.58f);
    [SerializeField] private Color _frameColor = new Color(0.47f, 0.9f, 1f, 0.42f);
    [SerializeField] private Color _textColor = new Color(0.88f, 0.98f, 1f, 1f);

    private RectTransform _hudRoot;
    private Image _healthFill;
    private Image _oxygenFill;
    private Text _healthValueText;
    private Text _oxygenValueText;
    private Text _oxygenStatusText;
    private Font _font;
    private Sprite _uiSprite;

    private void Awake()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _uiSprite = CreateRuntimeSprite();
        BuildHud();
    }

    private void Start()
    {
        ResolveReferences();
        RefreshHud();
    }

    private void Update()
    {
        ResolveReferences();
        RefreshHud();
    }

    private void ResolveReferences()
    {
        if (_playerController == null)
        {
            _playerController = FindFirstObjectByType<FirstPersonController>();
        }

        if (_oxygenSystem == null)
        {
            _oxygenSystem = OxygenSystem.Instance != null ? OxygenSystem.Instance : FindFirstObjectByType<OxygenSystem>();
        }
    }

    private void RefreshHud()
    {
        RefreshHealth();
        RefreshOxygen();
    }

    private void RefreshHealth()
    {
        float currentHealth = _playerController != null ? _playerController.GetHealth() : 0f;
        float maxHealth = _playerController != null ? Mathf.Max(1f, _playerController.GetMaxHealth()) : 1f;
        float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);

        if (_healthFill != null)
        {
            _healthFill.fillAmount = healthPercent;
            _healthFill.color = healthPercent > 0.25f ? _healthColor : _warningColor;
        }

        if (_healthValueText != null)
        {
            _healthValueText.text = Mathf.RoundToInt(currentHealth).ToString();
        }
    }

    private void RefreshOxygen()
    {
        bool hasTank = _oxygenSystem != null && _oxygenSystem.HasOxygenTank;
        float currentOxygen = _oxygenSystem != null ? Mathf.Max(0f, _oxygenSystem.CurrentOxygenTime) : 0f;
        float maxOxygen = _oxygenSystem != null ? Mathf.Max(1f, _oxygenSystem.MaxOxygenTime) : 1f;
        float oxygenPercent = Mathf.Clamp01(currentOxygen / maxOxygen);

        if (_oxygenFill != null)
        {
            _oxygenFill.fillAmount = hasTank ? oxygenPercent : 0f;
            _oxygenFill.color = oxygenPercent > 0.25f ? _oxygenColor : _warningColor;
        }

        if (_oxygenValueText != null)
        {
            _oxygenValueText.text = hasTank ? Mathf.CeilToInt(currentOxygen).ToString() : "--";
        }

        if (_oxygenStatusText != null)
        {
            _oxygenStatusText.text = hasTank ? "O2" : "SIN TANQUE";
            _oxygenStatusText.color = hasTank ? _textColor : _warningColor;
        }
    }

    private void BuildHud()
    {
        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        Transform existingRoot = canvasRect.Find(HudRootName);
        _hudRoot = existingRoot != null ? existingRoot as RectTransform : null;

        if (_hudRoot == null)
        {
            GameObject hudRootObject = new GameObject(HudRootName, typeof(RectTransform));
            hudRootObject.transform.SetParent(canvasRect, false);
            _hudRoot = hudRootObject.GetComponent<RectTransform>();
        }

        _hudRoot.anchorMin = new Vector2(0f, 0f);
        _hudRoot.anchorMax = new Vector2(0f, 0f);
        _hudRoot.pivot = new Vector2(0f, 0f);
        _hudRoot.anchoredPosition = new Vector2(34f, 30f);
        _hudRoot.sizeDelta = new Vector2(310f, 150f);

        ClearChildren(_hudRoot);

        GameObject oxygenGauge = CreateGauge("OxygenGauge", _hudRoot, new Vector2(86f, 76f), 124f, _oxygenColor, out _oxygenFill, out _oxygenValueText);
        _oxygenStatusText = CreateText("Label", oxygenGauge.transform, "O2", 18, TextAnchor.MiddleCenter, FontStyle.Bold, _textColor);
        SetRect(_oxygenStatusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-54f, 10f), new Vector2(54f, 35f));

        CreateGauge("HealthGauge", _hudRoot, new Vector2(215f, 62f), 92f, _healthColor, out _healthFill, out _healthValueText);
        Text healthLabel = CreateText("Label", _healthFill.transform.parent, "VIDA", 15, TextAnchor.MiddleCenter, FontStyle.Bold, _textColor);
        SetRect(healthLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-44f, 7f), new Vector2(44f, 28f));
    }

    private GameObject CreateGauge(string name, Transform parent, Vector2 position, float size, Color fillColor, out Image fillImage, out Text valueText)
    {
        GameObject gaugeRoot = CreateUIObject(name, parent);
        RectTransform gaugeRect = gaugeRoot.GetComponent<RectTransform>();
        gaugeRect.anchorMin = new Vector2(0f, 0f);
        gaugeRect.anchorMax = new Vector2(0f, 0f);
        gaugeRect.pivot = new Vector2(0.5f, 0.5f);
        gaugeRect.anchoredPosition = position;
        gaugeRect.sizeDelta = new Vector2(size, size);

        Image backplate = gaugeRoot.AddComponent<Image>();
        backplate.sprite = _uiSprite;
        backplate.color = _backgroundColor;
        backplate.raycastTarget = false;

        GameObject frameObject = CreateUIObject("Frame", gaugeRoot.transform);
        Image frameImage = frameObject.AddComponent<Image>();
        frameImage.sprite = _uiSprite;
        frameImage.color = _frameColor;
        frameImage.raycastTarget = false;
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        StretchToParent(frameRect, 6f);

        GameObject fillObject = CreateUIObject("Fill", gaugeRoot.transform);
        fillImage = fillObject.AddComponent<Image>();
        fillImage.sprite = _uiSprite;
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Radial360;
        fillImage.fillOrigin = (int)Image.Origin360.Bottom;
        fillImage.fillAmount = 1f;
        fillImage.raycastTarget = false;
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        StretchToParent(fillRect, 13f);

        GameObject centerObject = CreateUIObject("Center", gaugeRoot.transform);
        Image centerImage = centerObject.AddComponent<Image>();
        centerImage.sprite = _uiSprite;
        centerImage.color = new Color(0f, 0.05f, 0.08f, 0.82f);
        centerImage.raycastTarget = false;
        RectTransform centerRect = centerObject.GetComponent<RectTransform>();
        StretchToParent(centerRect, 27f);

        valueText = CreateText("Value", gaugeRoot.transform, "0", Mathf.RoundToInt(size * 0.34f), TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
        SetRect(valueText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        return gaugeRoot;
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

    private static void StretchToParent(RectTransform rectTransform, float inset)
    {
        SetRect(rectTransform, Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
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
