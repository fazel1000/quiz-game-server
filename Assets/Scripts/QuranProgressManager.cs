using UnityEngine;

public static class QuranProgressManager
{
    public const int StarsPerVerse = 5;
    public const int StarsRequiredForNextVerse = 3;

    private const string KeyPrefix = "QuranKids.Progress.v1";

    public static void EnsureBismillahVerseMigration(
        QuranDatabase database)
    {
        if (database == null ||
            database.surahs == null ||
            database.verseNumberingVersion <
            QuranDatabase.BismillahAsVerseNumberingVersion)
        {
            return;
        }

        bool changed = false;

        for (int i = 0; i < database.surahs.Length; i++)
        {
            SurahData surah = database.surahs[i];

            if (surah == null ||
                surah.verses == null ||
                surah.number == 1 ||
                surah.number == 9)
            {
                continue;
            }

            string migrationKey =
                GetBismillahMigrationKey(surah.number);

            if (global::UnityEngine.PlayerPrefs.GetInt(
                    migrationKey,
                    0) == 1)
                continue;

            int highestVerseNumber = 0;

            for (int verseIndex = 0;
                 verseIndex < surah.verses.Length;
                 verseIndex++)
            {
                VerseData verse = surah.verses[verseIndex];

                if (verse != null)
                {
                    highestVerseNumber = global::UnityEngine.Mathf.Max(
                        highestVerseNumber,
                        verse.number);
                }
            }

            for (int oldVerseNumber = highestVerseNumber - 1;
                 oldVerseNumber >= 1;
                 oldVerseNumber--)
            {
                MoveProgress(
                    surah.number,
                    oldVerseNumber,
                    oldVerseNumber + 1);
            }

            global::UnityEngine.PlayerPrefs.DeleteKey(
                GetStarsKey(surah.number, 1));
            global::UnityEngine.PlayerPrefs.DeleteKey(
                GetScoreKey(surah.number, 1));
            global::UnityEngine.PlayerPrefs.SetInt(
                migrationKey,
                1);
            changed = true;
        }

        if (changed)
            global::UnityEngine.PlayerPrefs.Save();
    }

    public static int ScoreToStars(float score)
    {
        float clampedScore = global::UnityEngine.Mathf.Clamp(
            score,
            0f,
            100f);

        return global::UnityEngine.Mathf.Clamp(
            global::UnityEngine.Mathf.FloorToInt(
                clampedScore / 20f),
            0,
            StarsPerVerse);
    }

    public static int GetBestStars(
        int surahNumber,
        int verseNumber)
    {
        return global::UnityEngine.PlayerPrefs.GetInt(
            GetStarsKey(surahNumber, verseNumber),
            0);
    }

    public static float GetBestScore(
        int surahNumber,
        int verseNumber)
    {
        return global::UnityEngine.PlayerPrefs.GetFloat(
            GetScoreKey(surahNumber, verseNumber),
            0f);
    }

    public static bool SaveBestResult(
        int surahNumber,
        int verseNumber,
        float score)
    {
        float clampedScore = global::UnityEngine.Mathf.Clamp(
            score,
            0f,
            100f);
        float previousBestScore = GetBestScore(
            surahNumber,
            verseNumber);

        if (clampedScore <= previousBestScore)
            return false;

        global::UnityEngine.PlayerPrefs.SetFloat(
            GetScoreKey(surahNumber, verseNumber),
            clampedScore);

        global::UnityEngine.PlayerPrefs.SetInt(
            GetStarsKey(surahNumber, verseNumber),
            ScoreToStars(clampedScore));

        global::UnityEngine.PlayerPrefs.Save();
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

        return global::UnityEngine.Mathf.Max(
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

        for (int previousIndex = 0;
             previousIndex < verseIndex;
             previousIndex++)
        {
            VerseData previousVerse =
                surah.verses[previousIndex];

            if (previousVerse == null ||
                GetBestStars(
                    surah.number,
                    previousVerse.number) <
                StarsRequiredForNextVerse)
            {
                return false;
            }
        }

        return true;
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

    private static void MoveProgress(
        int surahNumber,
        int sourceVerseNumber,
        int destinationVerseNumber)
    {
        string sourceStarsKey =
            GetStarsKey(surahNumber, sourceVerseNumber);
        string destinationStarsKey =
            GetStarsKey(surahNumber, destinationVerseNumber);

        if (global::UnityEngine.PlayerPrefs.HasKey(sourceStarsKey))
        {
            global::UnityEngine.PlayerPrefs.SetInt(
                destinationStarsKey,
                global::UnityEngine.PlayerPrefs.GetInt(sourceStarsKey));
            global::UnityEngine.PlayerPrefs.DeleteKey(sourceStarsKey);
        }

        string sourceScoreKey =
            GetScoreKey(surahNumber, sourceVerseNumber);
        string destinationScoreKey =
            GetScoreKey(surahNumber, destinationVerseNumber);

        if (global::UnityEngine.PlayerPrefs.HasKey(sourceScoreKey))
        {
            global::UnityEngine.PlayerPrefs.SetFloat(
                destinationScoreKey,
                global::UnityEngine.PlayerPrefs.GetFloat(sourceScoreKey));
            global::UnityEngine.PlayerPrefs.DeleteKey(sourceScoreKey);
        }
    }

    private static string GetBismillahMigrationKey(int surahNumber)
    {
        return KeyPrefix +
               ".BismillahAsVerse.v2." +
               surahNumber;
    }
}