using System;
using UnityEngine;

[CreateAssetMenu(fileName = "QuranDatabase", menuName = "Quran Kids/Quran Database")]
public class QuranDatabase : ScriptableObject
{
    public const int BismillahAsVerseNumberingVersion = 2;

    public SurahData[] surahs;

    [HideInInspector]
    public int verseNumberingVersion;
}

[Serializable]
public class SurahData
{
    public int number;
    public string nameArabic;
    public string namePersian;
    public VerseData[] verses;
}

[Serializable]
public class VerseData
{
    public int number;

    [global::UnityEngine.Tooltip("Audio for the first reciter. This is the existing verse audio.")]
    public AudioClip audio;

    [global::UnityEngine.Tooltip("Audio for the second reciter.")]
    public AudioClip secondReciterAudio;
}
