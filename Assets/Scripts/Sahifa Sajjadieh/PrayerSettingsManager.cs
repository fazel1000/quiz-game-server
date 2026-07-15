using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrayerSettingsManager : MonoBehaviour
{
    [Header("Arabic Controls")]
    public TMP_Dropdown arabicFontDropdown;
    public Slider arabicFontSizeSlider;

    [Header("Persian Controls")]
    public TMP_Dropdown persianFontDropdown;
    public Slider persianFontSizeSlider;

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

    private void Start()
    {
        LoadSettings();

        arabicFontDropdown.onValueChanged.AddListener(SetArabicFont);
        persianFontDropdown.onValueChanged.AddListener(SetPersianFont);

        arabicFontSizeSlider.onValueChanged.AddListener(SetArabicFontSize);
        persianFontSizeSlider.onValueChanged.AddListener(SetPersianFontSize);

        ApplySettingsToAllParts();
    }

    private void LoadSettings()
    {
        arabicFontDropdown.SetValueWithoutNotify(
            PlayerPrefs.GetInt(ArabicFontKey, 0)
        );

        persianFontDropdown.SetValueWithoutNotify(
            PlayerPrefs.GetInt(PersianFontKey, 0)
        );

        arabicFontSizeSlider.SetValueWithoutNotify(
            PlayerPrefs.GetFloat(ArabicSizeKey, 32f)
        );

        persianFontSizeSlider.SetValueWithoutNotify(
            PlayerPrefs.GetFloat(PersianSizeKey, 26f)
        );
    }

    public void SetArabicFont(int index)
    {
        PlayerPrefs.SetInt(ArabicFontKey, index);
        PlayerPrefs.Save();
        ApplySettingsToAllParts();
    }

    public void SetPersianFont(int index)
    {
        PlayerPrefs.SetInt(PersianFontKey, index);
        PlayerPrefs.Save();
        ApplySettingsToAllParts();
    }

    public void SetArabicFontSize(float size)
    {
        PlayerPrefs.SetFloat(ArabicSizeKey, size);
        PlayerPrefs.Save();
        ApplySettingsToAllParts();
    }

    public void SetPersianFontSize(float size)
    {
        PlayerPrefs.SetFloat(PersianSizeKey, size);
        PlayerPrefs.Save();
        ApplySettingsToAllParts();
    }

    public void ApplySettingsToAllParts()
    {
        PrayerPartUI[] parts = FindObjectsByType<PrayerPartUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        TMP_FontAsset arabicFont =
            GetArabicFont(arabicFontDropdown.value);

        TMP_FontAsset persianFont =
            GetPersianFont(persianFontDropdown.value);

        foreach (PrayerPartUI part in parts)
        {
            if (part == null)
                continue;

            if (arabicFont != null)
                part.arabicText.font = arabicFont;

            if (persianFont != null)
                part.persianText.font = persianFont;

            part.arabicText.fontSize = arabicFontSizeSlider.value;
            part.persianText.fontSize = persianFontSizeSlider.value;

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