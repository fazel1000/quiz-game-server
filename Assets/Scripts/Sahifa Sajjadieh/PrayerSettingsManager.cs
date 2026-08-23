using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrayerSettingsManager : MonoBehaviour
{
    [Header("Arabic Controls")]
    public TMP_Dropdown arabicFontDropdown;
    public Slider arabicFontSizeSlider;
    public Slider arabicLineSpacingSlider;
    public Slider arabicCharacterSpacingSlider;

    [Header("Persian Controls")]
    public TMP_Dropdown persianFontDropdown;
    public Slider persianFontSizeSlider;
    public Slider persianLineSpacingSlider;
    public Slider persianCharacterSpacingSlider;

    [Header("Arabic Fonts")]
    public TMP_FontAsset arabicFont1;
    public TMP_FontAsset arabicFont2;
    public TMP_FontAsset arabicFont3;

    [Header("Persian Fonts")]
    public TMP_FontAsset persianFont1;
    public TMP_FontAsset persianFont2;
    public TMP_FontAsset persianFont3;

    private const string ArabicFontKey = "ArabicFontIndex";
    private const string PersianFontKey = "PersianFontIndex";

    private const string ArabicSizeKey = "ArabicFontSize";
    private const string PersianSizeKey = "PersianFontSize";

    private const string ArabicLineSpacingKey = "ArabicLineSpacing";
    private const string PersianLineSpacingKey = "PersianLineSpacing";

    private const string ArabicCharacterSpacingKey =
        "ArabicCharacterSpacing";

    private const string PersianCharacterSpacingKey =
        "PersianCharacterSpacing";

    private const string ArabicColorKey = "ArabicTextColor";
    private const string PersianColorKey = "PersianTextColor";

    private const float DefaultArabicFontSize = 32f;
    private const float DefaultPersianFontSize = 26f;
    private const float DefaultLineSpacing = 0f;
    private const float DefaultCharacterSpacing = 0f;

    private static readonly Color DefaultArabicColor = Color.black;
    private static readonly Color DefaultPersianColor = Color.black;

    private void Start()
    {
        LoadSettings();
        AddListeners();
        ApplySettingsToAllParts();
    }

    private void AddListeners()
    {
        if (arabicFontDropdown != null)
        {
            arabicFontDropdown.onValueChanged.AddListener(
                SetArabicFont
            );
        }

        if (persianFontDropdown != null)
        {
            persianFontDropdown.onValueChanged.AddListener(
                SetPersianFont
            );
        }

        if (arabicFontSizeSlider != null)
        {
            arabicFontSizeSlider.onValueChanged.AddListener(
                SetArabicFontSize
            );
        }

        if (persianFontSizeSlider != null)
        {
            persianFontSizeSlider.onValueChanged.AddListener(
                SetPersianFontSize
            );
        }

        if (arabicLineSpacingSlider != null)
        {
            arabicLineSpacingSlider.onValueChanged.AddListener(
                SetArabicLineSpacing
            );
        }

        if (persianLineSpacingSlider != null)
        {
            persianLineSpacingSlider.onValueChanged.AddListener(
                SetPersianLineSpacing
            );
        }

        if (arabicCharacterSpacingSlider != null)
        {
            arabicCharacterSpacingSlider.onValueChanged.AddListener(
                SetArabicCharacterSpacing
            );
        }

        if (persianCharacterSpacingSlider != null)
        {
            persianCharacterSpacingSlider.onValueChanged.AddListener(
                SetPersianCharacterSpacing
            );
        }
    }

    private void LoadSettings()
    {
        if (arabicFontDropdown != null)
        {
            arabicFontDropdown.SetValueWithoutNotify(
                PlayerPrefs.GetInt(ArabicFontKey, 0)
            );
        }

        if (persianFontDropdown != null)
        {
            persianFontDropdown.SetValueWithoutNotify(
                PlayerPrefs.GetInt(PersianFontKey, 0)
            );
        }

        if (arabicFontSizeSlider != null)
        {
            arabicFontSizeSlider.SetValueWithoutNotify(
                PlayerPrefs.GetFloat(
                    ArabicSizeKey,
                    DefaultArabicFontSize
                )
            );
        }

        if (persianFontSizeSlider != null)
        {
            persianFontSizeSlider.SetValueWithoutNotify(
                PlayerPrefs.GetFloat(
                    PersianSizeKey,
                    DefaultPersianFontSize
                )
            );
        }

        if (arabicLineSpacingSlider != null)
        {
            arabicLineSpacingSlider.SetValueWithoutNotify(
                PlayerPrefs.GetFloat(
                    ArabicLineSpacingKey,
                    DefaultLineSpacing
                )
            );
        }

        if (persianLineSpacingSlider != null)
        {
            persianLineSpacingSlider.SetValueWithoutNotify(
                PlayerPrefs.GetFloat(
                    PersianLineSpacingKey,
                    DefaultLineSpacing
                )
            );
        }

        if (arabicCharacterSpacingSlider != null)
        {
            arabicCharacterSpacingSlider.SetValueWithoutNotify(
                PlayerPrefs.GetFloat(
                    ArabicCharacterSpacingKey,
                    DefaultCharacterSpacing
                )
            );
        }

        if (persianCharacterSpacingSlider != null)
        {
            persianCharacterSpacingSlider.SetValueWithoutNotify(
                PlayerPrefs.GetFloat(
                    PersianCharacterSpacingKey,
                    DefaultCharacterSpacing
                )
            );
        }
    }

    public void SetArabicFont(int index)
    {
        PlayerPrefs.SetInt(ArabicFontKey, index);
        SaveAndApply();
    }

    public void SetPersianFont(int index)
    {
        PlayerPrefs.SetInt(PersianFontKey, index);
        SaveAndApply();
    }

    public void SetArabicFontSize(float size)
    {
        PlayerPrefs.SetFloat(ArabicSizeKey, size);
        SaveAndApply();
    }

    public void SetPersianFontSize(float size)
    {
        PlayerPrefs.SetFloat(PersianSizeKey, size);
        SaveAndApply();
    }

    public void SetArabicLineSpacing(float spacing)
    {
        PlayerPrefs.SetFloat(ArabicLineSpacingKey, spacing);
        SaveAndApply();
    }

    public void SetPersianLineSpacing(float spacing)
    {
        PlayerPrefs.SetFloat(PersianLineSpacingKey, spacing);
        SaveAndApply();
    }

    public void SetArabicCharacterSpacing(float spacing)
    {
        PlayerPrefs.SetFloat(
            ArabicCharacterSpacingKey,
            spacing
        );

        SaveAndApply();
    }

    public void SetPersianCharacterSpacing(float spacing)
    {
        PlayerPrefs.SetFloat(
            PersianCharacterSpacingKey,
            spacing
        );

        SaveAndApply();
    }

    public void SetArabicColor(Color color)
    {
        string htmlColor =
            "#" + ColorUtility.ToHtmlStringRGBA(color);

        PlayerPrefs.SetString(ArabicColorKey, htmlColor);

        SaveAndApply();
    }

    public void SetPersianColor(Color color)
    {
        string htmlColor =
            "#" + ColorUtility.ToHtmlStringRGBA(color);

        PlayerPrefs.SetString(PersianColorKey, htmlColor);

        SaveAndApply();
    }

    public Color GetArabicColor()
    {
        return LoadColor(
            ArabicColorKey,
            DefaultArabicColor
        );
    }

    public Color GetPersianColor()
    {
        return LoadColor(
            PersianColorKey,
            DefaultPersianColor
        );
    }

    private Color LoadColor(
        string key,
        Color defaultColor
    )
    {
        string defaultHtml =
            "#" + ColorUtility.ToHtmlStringRGBA(defaultColor);

        string savedColor = PlayerPrefs.GetString(
            key,
            defaultHtml
        );

        if (ColorUtility.TryParseHtmlString(
                savedColor,
                out Color color))
        {
            return color;
        }

        return defaultColor;
    }

    private void SaveAndApply()
    {
        PlayerPrefs.Save();
        ApplySettingsToAllParts();
    }

    public void ApplySettingsToAllParts()
    {
        PrayerPartUI[] parts =
            FindObjectsByType<PrayerPartUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        TMP_FontAsset selectedArabicFont =
            GetArabicFont(
                arabicFontDropdown != null
                    ? arabicFontDropdown.value
                    : 0
            );

        TMP_FontAsset selectedPersianFont =
            GetPersianFont(
                persianFontDropdown != null
                    ? persianFontDropdown.value
                    : 0
            );

        float arabicFontSize =
            arabicFontSizeSlider != null
                ? arabicFontSizeSlider.value
                : DefaultArabicFontSize;

        float persianFontSize =
            persianFontSizeSlider != null
                ? persianFontSizeSlider.value
                : DefaultPersianFontSize;

        float arabicLineSpacing =
            arabicLineSpacingSlider != null
                ? arabicLineSpacingSlider.value
                : DefaultLineSpacing;

        float persianLineSpacing =
            persianLineSpacingSlider != null
                ? persianLineSpacingSlider.value
                : DefaultLineSpacing;

        float arabicCharacterSpacing =
            arabicCharacterSpacingSlider != null
                ? arabicCharacterSpacingSlider.value
                : DefaultCharacterSpacing;

        float persianCharacterSpacing =
            persianCharacterSpacingSlider != null
                ? persianCharacterSpacingSlider.value
                : DefaultCharacterSpacing;

        Color arabicColor = GetArabicColor();
        Color persianColor = GetPersianColor();

        foreach (PrayerPartUI part in parts)
        {
            if (part == null)
                continue;

            if (part.arabicText != null)
            {
                if (selectedArabicFont != null)
                {
                    part.arabicText.font =
                        selectedArabicFont;
                }

                part.arabicText.fontSize =
                    arabicFontSize;

                part.arabicText.lineSpacing =
                    arabicLineSpacing;

                part.arabicText.characterSpacing =
                    arabicCharacterSpacing;

                part.arabicText.color =
                    arabicColor;
            }

            if (part.persianText != null)
            {
                if (selectedPersianFont != null)
                {
                    part.persianText.font =
                        selectedPersianFont;
                }

                part.persianText.fontSize =
                    persianFontSize;

                part.persianText.lineSpacing =
                    persianLineSpacing;

                part.persianText.characterSpacing =
                    persianCharacterSpacing;

                part.persianText.color =
                    persianColor;
            }

            part.RefreshLayout();
        }
    }

    private TMP_FontAsset GetArabicFont(int index)
    {
        return index switch
        {
            1 => arabicFont2,
            2 => arabicFont3,
            _ => arabicFont1
        };
    }

    private TMP_FontAsset GetPersianFont(int index)
    {
        return index switch
        {
            1 => persianFont2,
            2 => persianFont3,
            _ => persianFont1
        };
    }
}