using System;
using UnityEngine;

[CreateAssetMenu(fileName = "QuranDatabase", menuName = "Quran Kids/Quran Database")]
public class QuranDatabase : ScriptableObject
{
    public SurahData[] surahs;
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
    public AudioClip audio;
}