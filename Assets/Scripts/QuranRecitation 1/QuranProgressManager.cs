using UnityEngine;

public static class QuranProgressManager
{
    public const int StarsPerVerse = 5;
    public const int StarsRequiredForNextVerse = 3;

    private const string KeyPrefix = "QuranKids.Progress.v1";

    public static int ScoreToStars(float score)
    {
        float clampedScore = Mathf.Clamp(score, 0f, 100f);

        return Mathf.Clamp(
            Mathf.FloorToInt(clampedScore / 20f),
            0,
            StarsPerVerse);
    }

    public static int GetBestStars(
        int surahNumber,
        int verseNumber)
    {
        return PlayerPrefs.GetInt(
            GetStarsKey(surahNumber, verseNumber),
            0);
    }

    public static float GetBestScore(
        int surahNumber,
        int verseNumber)
    {
        return PlayerPrefs.GetFloat(
            GetScoreKey(surahNumber, verseNumber),
            0f);
    }

    public static bool SaveBestResult(
        int surahNumber,
        int verseNumber,
        float score)
    {
        float clampedScore = Mathf.Clamp(score, 0f, 100f);
        float previousBestScore = GetBestScore(
            surahNumber,
            verseNumber);

        if (clampedScore <= previousBestScore)
            return false;

        PlayerPrefs.SetFloat(
            GetScoreKey(surahNumber, verseNumber),
            clampedScore);

        PlayerPrefs.SetInt(
            GetStarsKey(surahNumber, verseNumber),
            ScoreToStars(clampedScore));

        PlayerPrefs.Save();
        return true;
    }

    public static int GetEarnedStars(SurahData surah)
    {
        if (surah == null || surah.verses == null)
            return 0;

        int total = 0;

        for (int i = 0; i < surah.verses.Length; i++)
        {
            VerseData verse = surah.verses[i];

            if (verse == null)
                continue;

            total += GetBestStars(
                surah.number,
                verse.number);
        }

        return total;
    }

    public static int GetMaximumStars(SurahData surah)
    {
        if (surah == null || surah.verses == null)
            return 0;

        return surah.verses.Length * StarsPerVerse;
    }

    public static int GetRequiredStarsForNextSurah(
        SurahData surah)
    {
        if (surah == null || surah.verses == null)
            return 0;

        return Mathf.Max(
            0,
            GetMaximumStars(surah) - surah.verses.Length);
    }

    public static bool IsVerseUnlocked(
        SurahData surah,
        int verseIndex)
    {
        if (surah == null ||
            surah.verses == null ||
            verseIndex < 0 ||
            verseIndex >= surah.verses.Length)
        {
            return false;
        }

        if (verseIndex == 0)
            return true;

        VerseData previousVerse = surah.verses[verseIndex - 1];

        if (previousVerse == null)
            return false;

        return GetBestStars(
                   surah.number,
                   previousVerse.number) >=
               StarsRequiredForNextVerse;
    }

    public static bool IsSurahUnlocked(
        QuranDatabase database,
        int surahIndex)
    {
        if (database == null ||
            database.surahs == null ||
            surahIndex < 0 ||
            surahIndex >= database.surahs.Length)
        {
            return false;
        }

        if (surahIndex == 0)
            return true;

        SurahData previousSurah =
            database.surahs[surahIndex - 1];

        if (previousSurah == null)
            return false;

        return GetEarnedStars(previousSurah) >=
               GetRequiredStarsForNextSurah(previousSurah);
    }

    private static string GetStarsKey(
        int surahNumber,
        int verseNumber)
    {
        return KeyPrefix + "." +
               surahNumber + "." +
               verseNumber + ".Stars";
    }

    private static string GetScoreKey(
        int surahNumber,
        int verseNumber)
    {
        return KeyPrefix + "." +
               surahNumber + "." +
               verseNumber + ".Score";
    }
}