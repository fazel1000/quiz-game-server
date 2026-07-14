using System;
using UnityEngine;

public class PrayerAudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource arabicSource;
    public AudioSource persianSource;

    public event Action PlaybackFinished;

    private AudioSource activeSource;
    private bool isPaused;
    private bool wasPlaying;

    private void Update()
    {
        if (activeSource == null || !wasPlaying || isPaused)
            return;

        if (!activeSource.isPlaying)
        {
            wasPlaying = false;
            PlaybackFinished?.Invoke();
        }
    }

    public void PlayArabic(AudioClip clip)
    {
        if (clip == null)
            return;

        StopAll();

        activeSource = arabicSource;
        activeSource.clip = clip;
        activeSource.Play();

        isPaused = false;
        wasPlaying = true;
    }

    public void PlayPersian(AudioClip clip)
    {
        if (clip == null)
            return;

        StopAll();

        activeSource = persianSource;
        activeSource.clip = clip;
        activeSource.Play();

        isPaused = false;
        wasPlaying = true;
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
            wasPlaying = true;
        }
        else
        {
            activeSource.Play();
            isPaused = false;
            wasPlaying = true;
        }
    }

    public void StopAll()
    {
        arabicSource.Stop();
        persianSource.Stop();

        activeSource = null;
        isPaused = false;
        wasPlaying = false;
    }
}