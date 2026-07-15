using System;

[Serializable]
public class PrayerData
{
    public int id;
    public string title;

    public PrayerPartData[] parts;
    public PrayerAudioTrackData[] audioTracks;
}

[Serializable]
public class PrayerPartData
{
    public int id;
    public string arabic;
    public string persian;
}

[Serializable]
public class PrayerAudioTrackData
{
    public string speakerId;
    public string language;
    public string audioPath;

    public PrayerAudioSegmentData[] segments;
}

[Serializable]
public class PrayerAudioSegmentData
{
    public int partId;
    public float startTime;
    public float endTime;
}