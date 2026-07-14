using System.Collections;
using System.Collections.Generic;
using RTLTMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrayerPageUI : MonoBehaviour
{
    [Header("References")]
    public Transform content;
    public PrayerPartUI prayerPartPrefab;
    public PrayerAudioManager audioManager;
    public ScrollRect prayerScrollRect;
    public RTLTextMeshPro prayerTitleText;

    [Header("Temporary Test Audio")]
    public AudioClip testArabicClip;

    private readonly List<PrayerPartUI> prayerParts = new();
    private int currentPartIndex = -1;

    private void OnEnable()
    {
        if (audioManager != null)
            audioManager.PlaybackFinished += OnPlaybackFinished;
    }

    private void OnDisable()
    {
        if (audioManager != null)
            audioManager.PlaybackFinished -= OnPlaybackFinished;
    }

    private void Start()
    {
        LoadPrayer(1);
    }

    public void LoadPrayer(int prayerId)
    {
        ClearCurrentPrayer();

        string resourceName = $"prayer_{prayerId:00}";
        TextAsset jsonFile = Resources.Load<TextAsset>(resourceName);

        if (jsonFile == null)
        {
            Debug.LogError($"فایل {resourceName}.json پیدا نشد.");
            return;
        }

        PrayerData prayerData =
            JsonUtility.FromJson<PrayerData>(jsonFile.text);

        if (prayerData == null || prayerData.parts == null)
        {
            Debug.LogError("اطلاعات JSON معتبر نیست.");
            return;
        }

        if (prayerTitleText != null)
            prayerTitleText.text = prayerData.title;

        foreach (PrayerPartData partData in prayerData.parts)
        {
            CreatePart(partData);
        }

        StartCoroutine(RefreshContentLayout());
    }

    private void CreatePart(PrayerPartData partData)
    {
        PrayerPartUI item =
            Instantiate(prayerPartPrefab, content);

        item.SetAudioManager(audioManager);
        item.SetTexts(partData.arabic, partData.persian);

        // موقتاً برای آزمایش صوت
        item.arabicClip = testArabicClip;

        item.Clicked += OnPartClicked;
        prayerParts.Add(item);
    }

    private void OnPartClicked(PrayerPartUI clickedPart)
    {
        currentPartIndex = prayerParts.IndexOf(clickedPart);
    }

    private void OnPlaybackFinished()
    {
        if (currentPartIndex < 0 ||
            currentPartIndex >= prayerParts.Count - 1)
            return;

        currentPartIndex++;

        PrayerPartUI nextPart = prayerParts[currentPartIndex];
        nextPart.SelectAndPlay();

        StartCoroutine(ScrollToPart(nextPart));
    }

    public void PlayPreviousPart()
    {
        if (prayerParts.Count == 0)
            return;

        currentPartIndex = Mathf.Max(0, currentPartIndex - 1);

        PrayerPartUI part = prayerParts[currentPartIndex];
        part.SelectAndPlay();

        StartCoroutine(ScrollToPart(part));
    }

    public void PlayNextPart()
    {
        if (prayerParts.Count == 0)
            return;

        if (currentPartIndex < 0)
            currentPartIndex = 0;
        else
            currentPartIndex =
                Mathf.Min(currentPartIndex + 1, prayerParts.Count - 1);

        PrayerPartUI part = prayerParts[currentPartIndex];
        part.SelectAndPlay();

        StartCoroutine(ScrollToPart(part));
    }

    private IEnumerator RefreshContentLayout()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (content is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        prayerScrollRect.verticalNormalizedPosition = 1f;
    }

    private IEnumerator ScrollToPart(PrayerPartUI part)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        RectTransform contentRect = content as RectTransform;
        RectTransform viewportRect = prayerScrollRect.viewport;
        RectTransform partRect = part.GetComponent<RectTransform>();

        float scrollableHeight =
            contentRect.rect.height - viewportRect.rect.height;

        if (scrollableHeight <= 0)
            yield break;

        float partCenterFromTop =
            -partRect.anchoredPosition.y +
            partRect.rect.height * 0.5f;

        float desiredOffset =
            partCenterFromTop -
            viewportRect.rect.height * 0.5f;

        float normalizedPosition =
            1f - desiredOffset / scrollableHeight;

        prayerScrollRect.verticalNormalizedPosition =
            Mathf.Clamp01(normalizedPosition);
    }

    private void ClearCurrentPrayer()
    {
        foreach (PrayerPartUI item in prayerParts)
        {
            if (item == null)
                continue;

            item.Clicked -= OnPartClicked;
            Destroy(item.gameObject);
        }

        prayerParts.Clear();
        currentPartIndex = -1;

        if (audioManager != null)
            audioManager.StopAll();
    }
}