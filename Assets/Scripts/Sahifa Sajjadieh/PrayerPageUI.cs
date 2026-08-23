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

    [Header("Smooth Scroll")]
    [SerializeField] private float scrollDuration = 1f;

    [SerializeField]
    private float scrollTopPadding = 20f;

    [SerializeField]
    private AnimationCurve scrollCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly List<PrayerPartUI> prayerParts = new();

    private int currentPartIndex = -1;
    private Coroutine scrollCoroutine;

    private void OnEnable()
    {
        if (audioManager != null)
        {
            audioManager.PlaybackFinished +=
                OnPlaybackFinished;
        }
    }

    private void OnDisable()
    {
        if (audioManager != null)
        {
            audioManager.PlaybackFinished -=
                OnPlaybackFinished;
        }

        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
        }
    }

    public void LoadPrayer(int prayerId)
    {
        ClearCurrentPrayer();

        string resourceName =
            $"prayer_{prayerId:00}";

        TextAsset jsonFile =
            Resources.Load<TextAsset>(
                resourceName
            );

        if (jsonFile == null)
        {
            Debug.LogError(
                $"فایل {resourceName}.json پیدا نشد."
            );

            return;
        }

        PrayerData prayerData =
            JsonUtility.FromJson<PrayerData>(
                jsonFile.text
            );

        if (prayerData == null ||
            prayerData.parts == null)
        {
            Debug.LogError(
                "اطلاعات فایل JSON معتبر نیست."
            );

            return;
        }

        if (prayerTitleText != null)
        {
            prayerTitleText.text =
                prayerData.title;
        }

        PrayerAudioTrackData arabicTrack =
            FindTrack(
                prayerData.audioTracks,
                "arabic"
            );

        PrayerAudioTrackData persianTrack =
            FindTrack(
                prayerData.audioTracks,
                "persian"
            );

        AudioClip arabicClip =
            LoadAudioClip(arabicTrack);

        AudioClip persianClip =
            LoadAudioClip(persianTrack);

        foreach (PrayerPartData partData
                 in prayerData.parts)
        {
            CreatePart(
                partData,
                arabicTrack,
                arabicClip,
                persianTrack,
                persianClip
            );
        }

        StartCoroutine(
            RefreshContentLayout()
        );
    }

    private void CreatePart(
        PrayerPartData partData,
        PrayerAudioTrackData arabicTrack,
        AudioClip arabicClip,
        PrayerAudioTrackData persianTrack,
        AudioClip persianClip)
    {
        PrayerPartUI item =
            Instantiate(
                prayerPartPrefab,
                content
            );

        item.SetAudioManager(audioManager);

        item.SetTexts(
            partData.arabic,
            partData.persian
        );

        PrayerAudioSegmentData arabicSegment =
            FindSegment(
                arabicTrack,
                partData.id
            );

        if (arabicSegment != null)
        {
            item.arabicClip =
                arabicClip;

            item.arabicStartTime =
                arabicSegment.startTime;

            item.arabicEndTime =
                arabicSegment.endTime;
        }

        PrayerAudioSegmentData persianSegment =
            FindSegment(
                persianTrack,
                partData.id
            );

        if (persianSegment != null)
        {
            item.persianClip =
                persianClip;

            item.persianStartTime =
                persianSegment.startTime;

            item.persianEndTime =
                persianSegment.endTime;
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

        foreach (PrayerAudioTrackData track
                 in tracks)
        {
            if (track != null &&
                string.Equals(
                    track.language,
                    language,
                    System.StringComparison
                        .OrdinalIgnoreCase))
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
        if (track == null ||
            track.segments == null)
        {
            return null;
        }

        foreach (PrayerAudioSegmentData segment
                 in track.segments)
        {
            if (segment != null &&
                segment.partId == partId)
            {
                return segment;
            }
        }

        return null;
    }

    private AudioClip LoadAudioClip(
        PrayerAudioTrackData track)
    {
        if (track == null ||
            string.IsNullOrWhiteSpace(
                track.audioPath))
        {
            return null;
        }

        AudioClip clip =
            Resources.Load<AudioClip>(
                track.audioPath
            );

        if (clip == null)
        {
            Debug.LogError(
                $"فایل صوتی در مسیر " +
                $"{track.audioPath} پیدا نشد."
            );
        }

        return clip;
    }

    private void OnPartClicked(
        PrayerPartUI clickedPart)
    {
        currentPartIndex =
            prayerParts.IndexOf(
                clickedPart
            );
    }

    private void OnPlaybackFinished()
    {
        if (currentPartIndex < 0 ||
            currentPartIndex >=
            prayerParts.Count - 1)
        {
            return;
        }

        currentPartIndex++;

        PrayerPartUI nextPart =
            prayerParts[currentPartIndex];

        nextPart.SelectAndPlay();

        StartSmoothScroll(nextPart);
    }

    public void PlayPreviousPart()
    {
        if (prayerParts.Count == 0)
            return;

        if (currentPartIndex <= 0)
        {
            currentPartIndex = 0;
        }
        else
        {
            currentPartIndex--;
        }

        PrayerPartUI part =
            prayerParts[currentPartIndex];

        part.SelectAndPlay();

        StartSmoothScroll(part);
    }

    public void PlayNextPart()
    {
        if (prayerParts.Count == 0)
            return;

        if (currentPartIndex < 0)
        {
            currentPartIndex = 0;
        }
        else
        {
            currentPartIndex =
                Mathf.Min(
                    currentPartIndex + 1,
                    prayerParts.Count - 1
                );
        }

        PrayerPartUI part =
            prayerParts[currentPartIndex];

        part.SelectAndPlay();

        StartSmoothScroll(part);
    }

    private void StartSmoothScroll(
        PrayerPartUI part)
    {
        if (part == null)
            return;

        if (scrollCoroutine != null)
        {
            StopCoroutine(
                scrollCoroutine
            );
        }

        scrollCoroutine =
            StartCoroutine(
                ScrollToPart(part)
            );
    }

    private IEnumerator RefreshContentLayout()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (content is RectTransform contentRect)
        {
            LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    contentRect
                );
        }

        if (prayerScrollRect != null)
        {
            prayerScrollRect
                .verticalNormalizedPosition = 1f;
        }
    }

    private IEnumerator ScrollToPart(
        PrayerPartUI part)
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (prayerScrollRect == null ||
            prayerScrollRect.viewport == null ||
            content is not RectTransform contentRect)
        {
            scrollCoroutine = null;
            yield break;
        }

        RectTransform partRect =
            part.GetComponent<RectTransform>();

        if (partRect == null)
        {
            scrollCoroutine = null;
            yield break;
        }

        RectTransform viewportRect =
            prayerScrollRect.viewport;

        LayoutRebuilder
            .ForceRebuildLayoutImmediate(
                contentRect
            );

        Canvas.ForceUpdateCanvases();

        float scrollableHeight =
            contentRect.rect.height -
            viewportRect.rect.height;

        if (scrollableHeight <= 0f)
        {
            scrollCoroutine = null;
            yield break;
        }

        Bounds partBounds =
            RectTransformUtility
                .CalculateRelativeRectTransformBounds(
                    contentRect,
                    partRect
                );

        float partTopFromContentTop =
            contentRect.rect.yMax -
            partBounds.max.y;

        float desiredScrollOffset =
            partTopFromContentTop -
            scrollTopPadding;

        desiredScrollOffset =
            Mathf.Clamp(
                desiredScrollOffset,
                0f,
                scrollableHeight
            );

        float targetPosition =
            1f -
            desiredScrollOffset /
            scrollableHeight;

        targetPosition =
            Mathf.Clamp01(
                targetPosition
            );

        float startPosition =
            prayerScrollRect
                .verticalNormalizedPosition;

        prayerScrollRect.StopMovement();

        if (scrollDuration <= 0f)
        {
            prayerScrollRect
                .verticalNormalizedPosition =
                targetPosition;

            scrollCoroutine = null;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime <
               scrollDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime /
                    scrollDuration
                );

            float smoothProgress =
                scrollCurve != null
                    ? scrollCurve.Evaluate(
                        progress
                    )
                    : Mathf.SmoothStep(
                        0f,
                        1f,
                        progress
                    );

            prayerScrollRect.StopMovement();

            prayerScrollRect
                .verticalNormalizedPosition =
                Mathf.Lerp(
                    startPosition,
                    targetPosition,
                    smoothProgress
                );

            yield return null;
        }

        prayerScrollRect.StopMovement();

        prayerScrollRect
            .verticalNormalizedPosition =
            targetPosition;

        scrollCoroutine = null;
    }

    private void ClearCurrentPrayer()
    {
        if (scrollCoroutine != null)
        {
            StopCoroutine(
                scrollCoroutine
            );

            scrollCoroutine = null;
        }

        if (audioManager != null)
        {
            audioManager.StopAll();
        }

        foreach (PrayerPartUI item
                 in prayerParts)
        {
            if (item == null)
                continue;

            item.Clicked -=
                OnPartClicked;

            Destroy(item.gameObject);
        }

        prayerParts.Clear();
        currentPartIndex = -1;
    }
}