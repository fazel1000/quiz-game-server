using System;
using UnityEngine;

public class PrayerAudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource arabicSource;
    public AudioSource persianSource;

    public event Action PlaybackFinished;

    private AudioSource activeSource;
    private float segmentStartTime;
    private float segmentEndTime;

    private bool isPaused;
    private bool isSegmentPlaying;

    private void Update()
    {
        if (activeSource == null || !isSegmentPlaying || isPaused)
            return;

        bool reachedEndTime =
            activeSource.time >= segmentEndTime;

        bool clipStopped =
            !activeSource.isPlaying;

        if (reachedEndTime || clipStopped)
            FinishSegment();
    }

    public void PlayArabicSegment(
        AudioClip clip,
        float startTime,
        float endTime)
    {
        PlaySegment(arabicSource, clip, startTime, endTime);
    }

    public void PlayPersianSegment(
        AudioClip clip,
        float startTime,
        float endTime)
    {
        PlaySegment(persianSource, clip, startTime, endTime);
    }

    private void PlaySegment(
        AudioSource source,
        AudioClip clip,
        float startTime,
        float endTime)
    {
        if (source == null || clip == null)
            return;

        if (endTime <= startTime)
        {
            Debug.LogWarning("زمان پایان فراز باید از زمان شروع بیشتر باشد.");
            return;
        }

        StopAll();

        activeSource = source;
        activeSource.clip = clip;

        segmentStartTime =
            Mathf.Clamp(startTime, 0f, clip.length);

        segmentEndTime =
            Mathf.Clamp(endTime, segmentStartTime, clip.length);

        activeSource.time = segmentStartTime;
        activeSource.Play();

        isPaused = false;
        isSegmentPlaying = true;
    }

    public void TogglePlayPause()
    {
        if (activeSource == null || activeSource.clip == null)
            return;

        if (activeSource.isPlaying)
        {
            activeSource.Pause();
            isPaused = true;
        }
        else if (isPaused)
        {
            activeSource.UnPause();
            isPaused = false;
        }
    }

    public void StopAll()
    {
        isSegmentPlaying = false;
        isPaused = false;

        if (arabicSource != null)
            arabicSource.Stop();

        if (persianSource != null)
            persianSource.Stop();

        activeSource = null;
    }

    private void FinishSegment()
    {
        isSegmentPlaying = false;

        if (activeSource != null)
            activeSource.Stop();

        activeSource = null;
        isPaused = false;

        PlaybackFinished?.Invoke();
    }
}