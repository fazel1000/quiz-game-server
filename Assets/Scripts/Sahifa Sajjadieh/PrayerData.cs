using System;

[Serializable]
public class PrayerData
{
    public int id;
    public string title;
    public PrayerPartData[] parts;
}

[Serializable]
public class PrayerPartData
{
    public int id;
    public string arabic;
    public string persian;

    public PrayerAudioData[] arabicAudios;
    public PrayerAudioData[] persianAudios;
}

[Serializable]
public class PrayerAudioData
{
    public string speakerId;
    public string audioPath;
}