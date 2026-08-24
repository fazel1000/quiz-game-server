#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class QuranDatabaseBuilder : EditorWindow
{
    private QuranDatabase database;
    private TextAsset orderedQuranPhonemesJson;

    private string surahNumbers = "1,112,113,114";
    private Vector2 scroll;
    private string status = string.Empty;

    [MenuItem("Quran Kids/Quran Database Builder")]
    public static void OpenWindow()
    {
        QuranDatabaseBuilder window =
            GetWindow<QuranDatabaseBuilder>("Quran Database Builder");

        window.minSize = new Vector2(480f, 520f);
        window.TryFindDatabase();
        window.TryFindJson();
    }

    [MenuItem("Quran Kids/Configure Four Required Surahs")]
    public static void ConfigureRequiredSurahs()
    {
        QuranDatabaseBuilder window =
            GetWindow<QuranDatabaseBuilder>("Quran Database Builder");

        window.minSize = new Vector2(480f, 520f);
        window.surahNumbers = "1,112,113,114";
        window.TryFindDatabase();
        window.TryFindJson();
        window.Import();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8f);

        EditorGUILayout.LabelField(
            "Quran Database Builder",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "This database stores only the enabled surahs, verse numbers, and audio references. " +
            "Arabic text and phoneme data remain in ordered_quran_phonemes.json.",
            MessageType.Info);

        EditorGUILayout.Space(6f);

        database = (QuranDatabase)EditorGUILayout.ObjectField(
            "Quran Database",
            database,
            typeof(QuranDatabase),
            false);

        orderedQuranPhonemesJson = (TextAsset)EditorGUILayout.ObjectField(
            "Ordered Quran JSON",
            orderedQuranPhonemesJson,
            typeof(TextAsset),
            false);

        EditorGUILayout.Space(6f);

        EditorGUILayout.LabelField(
            "Surah Numbers",
            EditorStyles.boldLabel);

        surahNumbers = EditorGUILayout.TextField(
            surahNumbers);

        EditorGUILayout.HelpBox(
            "Enter Quran surah numbers separated by commas. Example: 1,112,113,114. " +
            "You can later replace this with any surah numbers you want.",
            MessageType.None);

        EditorGUILayout.Space(6f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Find Assets", GUILayout.Height(30f)))
            {
                TryFindDatabase();
                TryFindJson();
            }

            if (GUILayout.Button("Import / Rebuild", GUILayout.Height(30f)))
            {
                Import();
            }
        }

        EditorGUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(database == null))
        {
            if (GUILayout.Button("Clear Database", GUILayout.Height(28f)))
            {
                ClearDatabase();
            }
        }

        EditorGUILayout.Space(8f);

        if (!string.IsNullOrEmpty(status))
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox(status, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space(8f);

        if (database != null)
        {
            DrawCurrentDatabaseSummary();
        }
    }

    private void DrawCurrentDatabaseSummary()
    {
        EditorGUILayout.LabelField(
            "Current Database",
            EditorStyles.boldLabel);

        if (database.surahs == null || database.surahs.Length == 0)
        {
            EditorGUILayout.LabelField("No surahs configured.");
            return;
        }

        for (int i = 0; i < database.surahs.Length; i++)
        {
            SurahData surah = database.surahs[i];

            if (surah == null)
                continue;

            int verseCount =
                surah.verses == null ? 0 : surah.verses.Length;

            EditorGUILayout.LabelField(
                surah.number + " - " + surah.nameArabic,
                verseCount + " verses");
        }
    }

    private void TryFindDatabase()
    {
        if (database != null)
            return;

        string[] guids =
            AssetDatabase.FindAssets("t:QuranDatabase");

        if (guids.Length == 0)
            return;

        string path =
            AssetDatabase.GUIDToAssetPath(guids[0]);

        database =
            AssetDatabase.LoadAssetAtPath<QuranDatabase>(path);
    }

    private void TryFindJson()
    {
        if (orderedQuranPhonemesJson != null)
            return;

        string[] guids =
            AssetDatabase.FindAssets("ordered_quran_phonemes t:TextAsset");

        if (guids.Length == 0)
            return;

        string path =
            AssetDatabase.GUIDToAssetPath(guids[0]);

        orderedQuranPhonemesJson =
            AssetDatabase.LoadAssetAtPath<TextAsset>(path);
    }

    private void Import()
    {
        status = string.Empty;

        if (database == null)
        {
            EditorUtility.DisplayDialog(
                "Quran Database",
                "QuranDatabase.asset was not found.",
                "OK");
            return;
        }

        if (orderedQuranPhonemesJson == null)
        {
            EditorUtility.DisplayDialog(
                "Quran JSON",
                "ordered_quran_phonemes.json was not found.",
                "OK");
            return;
        }

        List<int> selectedSurahs;

        try
        {
            selectedSurahs = ParseSurahNumbers(surahNumbers);
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog(
                "Invalid Surah Numbers",
                ex.Message,
                "OK");
            return;
        }

        if (selectedSurahs.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Invalid Surah Numbers",
                "Enter at least one surah number.",
                "OK");
            return;
        }

        try
        {
            Dictionary<string, QuranVerseJsonData> allVerses =
                QuranJsonParser.ParseVerses(
                    orderedQuranPhonemesJson.text);

            Dictionary<int, SurahData> oldSurahs =
                BuildExistingSurahMap(database.surahs);

            List<SurahData> newSurahs =
                new List<SurahData>();

            int totalVerses = 0;
            int missingVerses = 0;

            for (int i = 0; i < selectedSurahs.Count; i++)
            {
                int surahNumber = selectedSurahs[i];

                SurahData surah;

                if (!oldSurahs.TryGetValue(
                        surahNumber,
                        out surah) ||
                    surah == null)
                {
                    surah = CreateSurahData(surahNumber);
                }
                else
                {
                    surah.nameArabic = GetSurahArabicName(surahNumber);
                    surah.namePersian = GetSurahPersianName(surahNumber);
                }

                List<VerseData> newVerses =
                    new List<VerseData>();

                foreach (KeyValuePair<string, QuranVerseJsonData> pair in allVerses)
                {
                    int parsedSurah;
                    int parsedVerse;

                    if (!TryParseKey(
                            pair.Key,
                            out parsedSurah,
                            out parsedVerse))
                    {
                        continue;
                    }

                    if (parsedSurah != surahNumber)
                        continue;

                    VerseData existingVerse =
                        FindVerse(
                            surah.verses,
                            parsedVerse);

                    if (existingVerse == null)
                    {
                        existingVerse =
                            new VerseData
                            {
                                number = parsedVerse,
                                audio = null
                            };
                    }
                    else
                    {
                        existingVerse.number = parsedVerse;
                    }

                    newVerses.Add(existingVerse);
                    totalVerses++;
                }

                newVerses.Sort(
                    delegate (VerseData a, VerseData b)
                    {
                        return a.number.CompareTo(b.number);
                    });

                if (newVerses.Count == 0)
                {
                    missingVerses++;
                    continue;
                }

                surah.verses = newVerses.ToArray();
                newSurahs.Add(surah);
            }

            newSurahs.Sort(
                delegate (SurahData a, SurahData b)
                {
                    return a.number.CompareTo(b.number);
                });

            Undo.RecordObject(database, "Configure Quran Database");

            database.surahs = newSurahs.ToArray();

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            status =
                "Database updated successfully.\n\n" +
                "Surahs: " + string.Join(", ", selectedSurahs.Select(x => x.ToString()).ToArray()) + "\n" +
                "Total verses: " + totalVerses + "\n" +
                "Missing surahs in JSON: " + missingVerses + "\n\n" +
                "AudioClip fields were preserved when possible and are currently empty for new verses.";

            EditorUtility.DisplayDialog(
                "Quran Database Updated",
                status,
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

    private void ClearDatabase()
    {
        if (database == null)
            return;

        if (!EditorUtility.DisplayDialog(
                "Clear Quran Database",
                "This removes all configured surahs from the asset. Continue?",
                "Clear",
                "Cancel"))
        {
            return;
        }

        Undo.RecordObject(database, "Clear Quran Database");
        database.surahs = new SurahData[0];
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        status = "Database cleared.";
    }

    private static List<int> ParseSurahNumbers(string value)
    {
        List<int> result = new List<int>();

        if (string.IsNullOrWhiteSpace(value))
            return result;

        string[] parts =
            value.Split(
                new[] { ',', ' ', ';', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            int number;

            if (!int.TryParse(parts[i], out number))
            {
                throw new Exception(
                    "Invalid surah number: " + parts[i]);
            }

            if (number < 1 || number > 114)
            {
                throw new Exception(
                    "Surah number must be between 1 and 114: " + number);
            }

            if (!result.Contains(number))
                result.Add(number);
        }

        result.Sort();
        return result;
    }

    private static Dictionary<int, SurahData> BuildExistingSurahMap(
        SurahData[] surahs)
    {
        Dictionary<int, SurahData> result =
            new Dictionary<int, SurahData>();

        if (surahs == null)
            return result;

        for (int i = 0; i < surahs.Length; i++)
        {
            SurahData surah = surahs[i];

            if (surah == null)
                continue;

            if (!result.ContainsKey(surah.number))
                result.Add(surah.number, surah);
        }

        return result;
    }

    private static SurahData CreateSurahData(int number)
    {
        return new SurahData
        {
            number = number,
            nameArabic = GetSurahArabicName(number),
            namePersian = GetSurahPersianName(number),
            verses = new VerseData[0]
        };
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

    private static bool TryParseKey(
        string key,
        out int surah,
        out int verse)
    {
        surah = 0;
        verse = 0;

        if (string.IsNullOrWhiteSpace(key))
            return false;

        string[] parts = key.Split(':');

        if (parts.Length != 2)
            return false;

        return
            int.TryParse(parts[0], out surah) &&
            int.TryParse(parts[1], out verse);
    }

    private static string GetSurahArabicName(int number)
    {
        switch (number)
        {
            case 1: return "الفاتحة";
            case 112: return "الإخلاص";
            case 113: return "الفلق";
            case 114: return "الناس";
            default: return "سورة " + number;
        }
    }

    private static string GetSurahPersianName(int number)
    {
        switch (number)
        {
            case 1: return "سوره فاتحه";
            case 112: return "سوره اخلاص";
            case 113: return "سوره فلق";
            case 114: return "سوره ناس";
            default: return "سوره " + number;
        }
    }
}

#endif