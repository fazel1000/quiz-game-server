using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SimpleColorPicker : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField]
    private PrayerSettingsManager settingsManager;

    [Header("Color Controls")]
    [SerializeField]
    private RawImage saturationValueArea;

    [SerializeField]
    private RectTransform selectionPointer;

    [SerializeField]
    private Slider hueSlider;

    [SerializeField]
    private Image hueBackground;

    [SerializeField]
    private Image currentColorPreview;

    [Header("Texts")]
    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text arabicPreviewText;

    [SerializeField]
    private TMP_Text persianPreviewText;

    private enum TargetLanguage
    {
        Arabic,
        Persian
    }

    private TargetLanguage targetLanguage;

    private Texture2D saturationValueTexture;
    private Texture2D hueTexture;

    private float saturation = 1f;
    private float brightness = 1f;

    private Color selectedColor = Color.black;
    private Color originalColor = Color.black;

    private bool initialized;

    private void OnEnable()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        if (hueSlider == null ||
            saturationValueArea == null)
        {
            Debug.LogError(
                "SimpleColorPicker references are incomplete.",
                this
            );

            return;
        }

        initialized = true;

        hueSlider.minValue = 0f;
        hueSlider.maxValue = 1f;
        hueSlider.wholeNumbers = false;

        hueSlider.onValueChanged.AddListener(
            OnHueChanged
        );

        CreateHueTexture();

        CreateSaturationValueTexture(
            hueSlider.value
        );

        ColorPickerPointerHandler pointerHandler =
            saturationValueArea.GetComponent
                <ColorPickerPointerHandler>();

        if (pointerHandler == null)
        {
            pointerHandler =
                saturationValueArea.gameObject.AddComponent
                    <ColorPickerPointerHandler>();
        }

        pointerHandler.Initialize(this);
    }

    public void OpenArabic()
    {
        gameObject.SetActive(true);

        Initialize();

        targetLanguage =
            TargetLanguage.Arabic;

        if (titleText != null)
        {
            titleText.text =
                "رنگ متن عربی";
        }

        originalColor =
            settingsManager != null
                ? settingsManager.GetArabicColor()
                : Color.black;

        SetPickerColor(originalColor);
    }

    public void OpenPersian()
    {
        gameObject.SetActive(true);

        Initialize();

        targetLanguage =
            TargetLanguage.Persian;

        if (titleText != null)
        {
            titleText.text =
                "رنگ متن فارسی";
        }

        originalColor =
            settingsManager != null
                ? settingsManager.GetPersianColor()
                : Color.black;

        SetPickerColor(originalColor);
    }

    public void ConfirmColor()
    {
        if (settingsManager == null)
        {
            Debug.LogError(
                "PrayerSettingsManager is not assigned.",
                this
            );

            return;
        }

        if (targetLanguage ==
            TargetLanguage.Arabic)
        {
            settingsManager.SetArabicColor(
                selectedColor
            );
        }
        else
        {
            settingsManager.SetPersianColor(
                selectedColor
            );
        }

        gameObject.SetActive(false);
    }

    public void CancelColor()
    {
        UpdatePreview(originalColor);
        gameObject.SetActive(false);
    }

    private void OnHueChanged(float hue)
    {
        CreateSaturationValueTexture(hue);
        UpdateSelectedColor();
    }

    public void SelectColor(
        PointerEventData eventData
    )
    {
        RectTransform areaRect =
            saturationValueArea.rectTransform;

        bool converted =
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    areaRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint
                );

        if (!converted)
            return;

        Rect rect = areaRect.rect;

        saturation = Mathf.Clamp01(
            Mathf.InverseLerp(
                rect.xMin,
                rect.xMax,
                localPoint.x
            )
        );

        brightness = Mathf.Clamp01(
            Mathf.InverseLerp(
                rect.yMin,
                rect.yMax,
                localPoint.y
            )
        );

        MovePointer();
        UpdateSelectedColor();
    }

    private void UpdateSelectedColor()
    {
        selectedColor = Color.HSVToRGB(
            hueSlider.value,
            saturation,
            brightness
        );

        if (currentColorPreview != null)
        {
            currentColorPreview.color =
                selectedColor;
        }

        UpdatePreview(selectedColor);
    }

    private void UpdatePreview(Color color)
    {
        if (targetLanguage ==
            TargetLanguage.Arabic)
        {
            if (arabicPreviewText != null)
            {
                arabicPreviewText.color =
                    color;
            }
        }
        else
        {
            if (persianPreviewText != null)
            {
                persianPreviewText.color =
                    color;
            }
        }
    }

    private void SetPickerColor(Color color)
    {
        Color.RGBToHSV(
            color,
            out float hue,
            out saturation,
            out brightness
        );

        hueSlider.SetValueWithoutNotify(hue);

        CreateSaturationValueTexture(hue);
        MovePointer();

        selectedColor = color;

        if (currentColorPreview != null)
        {
            currentColorPreview.color =
                color;
        }

        UpdatePreview(color);
    }

    private void MovePointer()
    {
        if (selectionPointer == null)
            return;

        Rect rect =
            saturationValueArea
                .rectTransform.rect;

        float x = Mathf.Lerp(
            rect.xMin,
            rect.xMax,
            saturation
        );

        float y = Mathf.Lerp(
            rect.yMin,
            rect.yMax,
            brightness
        );

        selectionPointer.anchoredPosition =
            new Vector2(x, y);
    }

    private void CreateHueTexture()
    {
        if (hueBackground == null)
            return;

        hueTexture = new Texture2D(
            256,
            1,
            TextureFormat.RGB24,
            false
        );

        hueTexture.wrapMode =
            TextureWrapMode.Clamp;

        hueTexture.filterMode =
            FilterMode.Bilinear;

        for (int x = 0;
             x < hueTexture.width;
             x++)
        {
            float hue =
                x /
                (float)(hueTexture.width - 1);

            Color color =
                Color.HSVToRGB(
                    hue,
                    1f,
                    1f
                );

            hueTexture.SetPixel(
                x,
                0,
                color
            );
        }

        hueTexture.Apply();

        hueBackground.sprite =
            Sprite.Create(
                hueTexture,
                new Rect(
                    0,
                    0,
                    hueTexture.width,
                    hueTexture.height
                ),
                new Vector2(0.5f, 0.5f)
            );

        hueBackground.type =
            Image.Type.Simple;
    }

    private void CreateSaturationValueTexture(
        float hue
    )
    {
        const int textureSize = 128;

        if (saturationValueTexture == null)
        {
            saturationValueTexture =
                new Texture2D(
                    textureSize,
                    textureSize,
                    TextureFormat.RGB24,
                    false
                );

            saturationValueTexture.wrapMode =
                TextureWrapMode.Clamp;

            saturationValueTexture.filterMode =
                FilterMode.Bilinear;
        }

        for (int y = 0;
             y < textureSize;
             y++)
        {
            float value =
                y /
                (float)(textureSize - 1);

            for (int x = 0;
                 x < textureSize;
                 x++)
            {
                float sat =
                    x /
                    (float)(textureSize - 1);

                Color color =
                    Color.HSVToRGB(
                        hue,
                        sat,
                        value
                    );

                saturationValueTexture.SetPixel(
                    x,
                    y,
                    color
                );
            }
        }

        saturationValueTexture.Apply();

        saturationValueArea.texture =
            saturationValueTexture;
    }
}

public class ColorPickerPointerHandler :
    MonoBehaviour,
    IPointerDownHandler,
    IDragHandler
{
    private SimpleColorPicker colorPicker;

    public void Initialize(
        SimpleColorPicker picker
    )
    {
        colorPicker = picker;
    }

    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        if (colorPicker != null)
        {
            colorPicker.SelectColor(
                eventData
            );
        }
    }

    public void OnDrag(
        PointerEventData eventData
    )
    {
        if (colorPicker != null)
        {
            colorPicker.SelectColor(
                eventData
            );
        }
    }
}