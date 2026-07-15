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
            Debug.LogError("اطلاعات فایل JSON معتبر نیست.");
            return;
        }

        if (prayerTitleText != null)
            prayerTitleText.text = prayerData.title;

        PrayerAudioTrackData arabicTrack =
            FindTrack(prayerData.audioTracks, "arabic");

        PrayerAudioTrackData persianTrack =
            FindTrack(prayerData.audioTracks, "persian");

        AudioClip arabicClip = LoadAudioClip(arabicTrack);
        AudioClip persianClip = LoadAudioClip(persianTrack);

        foreach (PrayerPartData partData in prayerData.parts)
        {
            CreatePart(
                partData,
                arabicTrack,
                arabicClip,
                persianTrack,
                persianClip
            );
        }

        StartCoroutine(RefreshContentLayout());
    }

    private void CreatePart(
        PrayerPartData partData,
        PrayerAudioTrackData arabicTrack,
        AudioClip arabicClip,
        PrayerAudioTrackData persianTrack,
        AudioClip persianClip)
    {
        PrayerPartUI item =
            Instantiate(prayerPartPrefab, content);

        item.SetAudioManager(audioManager);
        item.SetTexts(partData.arabic, partData.persian);

        PrayerAudioSegmentData arabicSegment =
            FindSegment(arabicTrack, partData.id);

        if (arabicSegment != null)
        {
            item.arabicClip = arabicClip;
            item.arabicStartTime = arabicSegment.startTime;
            item.arabicEndTime = arabicSegment.endTime;
        }

        PrayerAudioSegmentData persianSegment =
            FindSegment(persianTrack, partData.id);

        if (persianSegment != null)
        {
            item.persianClip = persianClip;
            item.persianStartTime = persianSegment.startTime;
            item.persianEndTime = persianSegment.endTime;
        }

        item.Clicked += OnPartClicked;
        prayerParts.Add(item);
    }

    private PrayerAudioTrackData FindTrack(
        PrayerAudioTrackData[] tracks,
        string language)
    {
        if (tracks == null)
            return null;

        foreach (PrayerAudioTrackData track in tracks)
        {
            if (track != null &&
                string.Equals(
                    track.language,
                    language,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                return track;
            }
        }

        return null;
    }

    private PrayerAudioSegmentData FindSegment(
        PrayerAudioTrackData track,
        int partId)
    {
        if (track == null || track.segments == null)
            return null;

        foreach (PrayerAudioSegmentData segment in track.segments)
        {
            if (segment != null && segment.partId == partId)
                return segment;
        }

        return null;
    }

    private AudioClip LoadAudioClip(PrayerAudioTrackData track)
    {
        if (track == null || string.IsNullOrWhiteSpace(track.audioPath))
            return null;

        AudioClip clip =
            Resources.Load<AudioClip>(track.audioPath);

        if (clip == null)
        {
            Debug.LogError(
                $"فایل صوتی در مسیر {track.audioPath} پیدا نشد."
            );
        }

        return clip;
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

        if (currentPartIndex <= 0)
            currentPartIndex = 0;
        else
            currentPartIndex--;

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

        if (prayerScrollRect != null)
            prayerScrollRect.verticalNormalizedPosition = 1f;
    }

    private IEnumerator ScrollToPart(PrayerPartUI part)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (prayerScrollRect == null ||
            prayerScrollRect.viewport == null)
            yield break;

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
        if (audioManager != null)
            audioManager.StopAll();

        foreach (PrayerPartUI item in prayerParts)
        {
            if (item == null)
                continue;

            item.Clicked -= OnPartClicked;
            Destroy(item.gameObject);
        }

        prayerParts.Clear();
        currentPartIndex = -1;
    }
}