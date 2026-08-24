#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class QuranDatabaseBuilder
{
    [MenuItem("Quran Kids/Create Empty Quran Database")]
    private static void CreateDatabase()
    {
        QuranDatabase asset =
            ScriptableObject.CreateInstance<QuranDatabase>();

        string folder = "Assets/QuranKids";

        if (!AssetDatabase.IsValidFolder("Assets/QuranKids"))
            AssetDatabase.CreateFolder("Assets", "QuranKids");

        string path =
            AssetDatabase.GenerateUniqueAssetPath(
                folder + "/QuranDatabase.asset");

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    [MenuItem("Quran Kids/Import Ordered Quran Data")]
    private static void ImportOrderedQuranData()
    {
        string jsonPath =
            EditorUtility.OpenFilePanel(
                "Select ordered_quran_phonemes.json",
                Application.dataPath,
                "json");

        if (string.IsNullOrEmpty(jsonPath))
            return;

        QuranDatabase database =
            Selection.activeObject as QuranDatabase;

        if (database == null)
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    "t:QuranDatabase");

            if (guids.Length == 1)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(
                        guids[0]);

                database =
                    AssetDatabase.LoadAssetAtPath<QuranDatabase>(
                        path);
            }
        }

        if (database == null)
        {
            EditorUtility.DisplayDialog(
                "Quran Database",
                "Select a QuranDatabase asset first.",
                "OK");

            return;
        }

        try
        {
            string json =
                System.IO.File.ReadAllText(jsonPath);

            Dictionary<string, QuranVerseJsonData> verses =
                QuranJsonParser.ParseVerses(json);

            if (verses.Count == 0)
                throw new InvalidOperationException(
                    "No Quran verses were found in ordered_quran_phonemes.json.");

            Dictionary<int, SurahData> existingSurahs =
                new Dictionary<int, SurahData>();

            if (database.surahs != null)
            {
                for (int i = 0; i < database.surahs.Length; i++)
                {
                    SurahData surah =
                        database.surahs[i];

                    if (surah == null)
                        continue;

                    existingSurahs[surah.number] =
                        surah;
                }
            }

            Dictionary<int, List<int>> verseNumbers =
                new Dictionary<int, List<int>>();

            foreach (string key in verses.Keys)
            {
                string[] parts =
                    key.Split(':');

                if (parts.Length != 2)
                    continue;

                int surahNumber;
                int verseNumber;

                if (!int.TryParse(
                        parts[0],
                        out surahNumber))
                {
                    continue;
                }

                if (!int.TryParse(
                        parts[1],
                        out verseNumber))
                {
                    continue;
                }

                List<int> list;

                if (!verseNumbers.TryGetValue(
                        surahNumber,
                        out list))
                {
                    list = new List<int>();
                    verseNumbers[surahNumber] = list;
                }

                if (!list.Contains(verseNumber))
                    list.Add(verseNumber);
            }

            List<int> surahNumbers =
                new List<int>(verseNumbers.Keys);

            surahNumbers.Sort();

            SurahData[] newSurahs =
                new SurahData[surahNumbers.Count];

            for (int i = 0; i < surahNumbers.Count; i++)
            {
                int surahNumber =
                    surahNumbers[i];

                SurahData surah;

                if (existingSurahs.TryGetValue(
                        surahNumber,
                        out surah) &&
                    surah != null)
                {
                    if (string.IsNullOrWhiteSpace(
                        surah.nameArabic))
                    {
                        surah.nameArabic =
                            $"سوره {surahNumber}";
                    }

                    if (string.IsNullOrWhiteSpace(
                        surah.namePersian))
                    {
                        surah.namePersian =
                            $"سوره {surahNumber}";
                    }
                }
                else
                {
                    surah = new SurahData
                    {
                        number = surahNumber,
                        nameArabic = $"سوره {surahNumber}",
                        namePersian = $"سوره {surahNumber}"
                    };
                }

                List<int> numbers =
                    verseNumbers[surahNumber];

                numbers.Sort();

                VerseData[] verseArray =
                    new VerseData[numbers.Count];

                for (int v = 0; v < numbers.Count; v++)
                {
                    int verseNumber =
                        numbers[v];

                    VerseData existingVerse =
                        FindVerse(
                            surah.verses,
                            verseNumber);

                    if (existingVerse == null)
                    {
                        existingVerse =
                            new VerseData
                            {
                                number = verseNumber
                            };
                    }

                    verseArray[v] = existingVerse;
                }

                surah.verses = verseArray;
                newSurahs[i] = surah;
            }

            database.surahs = newSurahs;

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Quran Database Updated",
                $"Loaded {verses.Count} verses from the official JSON source.\n\n" +
                "The database now stores verse numbers/audio only.\n" +
                "Arabic text and phoneme data are read from ordered_quran_phonemes.json at runtime.",
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            EditorUtility.DisplayDialog(
                "Import Failed",
                ex.Message,
                "OK");
        }
    }

    [MenuItem("Quran Kids/Validate Quran JSON")]
    private static void ValidateQuranJson()
    {
        string jsonPath =
            EditorUtility.OpenFilePanel(
                "Select ordered_quran_phonemes.json",
                Application.dataPath,
                "json");

        if (string.IsNullOrEmpty(jsonPath))
            return;

        try
        {
            string json =
                System.IO.File.ReadAllText(jsonPath);

            Dictionary<string, QuranVerseJsonData> verses =
                QuranJsonParser.ParseVerses(json);

            int missingText = 0;
            int missingPhonemes = 0;
            int invalidList = 0;

            foreach (KeyValuePair<string, QuranVerseJsonData> pair in verses)
            {
                QuranVerseJsonData verse =
                    pair.Value;

                if (string.IsNullOrWhiteSpace(
                    verse.ayaText))
                {
                    missingText++;
                }

                if (string.IsNullOrWhiteSpace(
                    verse.AyaPhoneme))
                {
                    missingPhonemes++;
                }

                if (verse.ayaPhonemesList == null ||
                    verse.ayaPhonemesList.Count == 0)
                {
                    invalidList++;
                }
            }

            EditorUtility.DisplayDialog(
                "Quran JSON Validation",
                $"Verses: {verses.Count}\n" +
                $"Missing text: {missingText}\n" +
                $"Missing phoneme: {missingPhonemes}\n" +
                $"Invalid phoneme list: {invalidList}",
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            EditorUtility.DisplayDialog(
                "Validation Failed",
                ex.Message,
                "OK");
        }
    }

    [MenuItem("Quran Kids/Validate Quran Database")]
    private static void ValidateDatabase()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:QuranDatabase");

        int databases = 0;
        int surahs = 0;
        int verses = 0;
        int invalidVerses = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[i]);

            QuranDatabase db =
                AssetDatabase.LoadAssetAtPath<QuranDatabase>(
                    path);

            if (db == null)
                continue;

            databases++;

            if (db.surahs == null)
                continue;

            for (int s = 0; s < db.surahs.Length; s++)
            {
                SurahData surah =
                    db.surahs[s];

                if (surah == null)
                    continue;

                surahs++;

                if (surah.verses == null)
                    continue;

                for (int v = 0; v < surah.verses.Length; v++)
                {
                    VerseData verse =
                        surah.verses[v];

                    if (verse == null ||
                        verse.number <= 0)
                    {
                        invalidVerses++;
                        continue;
                    }

                    verses++;
                }
            }
        }

        Debug.Log(
            $"Quran database validation | " +
            $"Databases={databases} | " +
            $"Surahs={surahs} | " +
            $"Verses={verses} | " +
            $"Invalid={invalidVerses}");
    }

    private static VerseData FindVerse(
        VerseData[] verses,
        int number)
    {
        if (verses == null)
            return null;

        for (int i = 0; i < verses.Length; i++)
        {
            if (verses[i] != null &&
                verses[i].number == number)
            {
                return verses[i];
            }
        }

        return null;
    }
}

#endif