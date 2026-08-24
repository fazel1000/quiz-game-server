using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Eitan.Sherpa.Onnx.Unity.Mono.Components;
using UnityEngine;

public class QuranAssessmentEngine : MonoBehaviour
{
    [Header("Quran Data")]
    [SerializeField] private QuranJsonRepository quranRepository;

    [Header("SherpaONNXUnity (EitanWong)")]
    [SerializeField] private RealtimeSpeechRecognizerComponent realtimeSpeechRecognizer;

    [Header("Scoring")]
    [SerializeField, Range(0f, 100f)] private float excellentThreshold = 90f;
    [SerializeField, Range(0f, 100f)] private float goodThreshold = 75f;
    [SerializeField, Min(1f)] private float initializationTimeoutSeconds = 15f;

    public bool IsReady
    {
        get
        {
            return quranRepository != null &&
                   quranRepository.IsReady &&
                   realtimeSpeechRecognizer != null &&
                   realtimeSpeechRecognizer.IsInitialized;
        }
    }

    public async Task<QuranAssessmentResult> AssessAsync(
        int surahNumber,
        int verseNumber,
        AudioClip recording)
    {
        if (recording == null)
            return QuranAssessmentResult.Failed("Recording is missing.");

        if (quranRepository == null)
            return QuranAssessmentResult.Failed("QuranJsonRepository is not assigned.");

        if (!quranRepository.IsReady)
        {
            quranRepository.Initialize();

            if (!quranRepository.IsReady)
                return QuranAssessmentResult.Failed("Quran JSON repository is not ready.");
        }

        List<string> expected;

        try
        {
            expected = quranRepository.GetExpectedTokens(
                surahNumber,
                verseNumber);
        }
        catch (Exception ex)
        {
            return QuranAssessmentResult.Failed(
                "Expected phoneme tokenization failed: " + ex.Message);
        }

        if (expected.Count == 0)
        {
            return QuranAssessmentResult.Failed(
                $"Expected phoneme tokens are missing for {surahNumber}:{verseNumber}.");
        }

        if (realtimeSpeechRecognizer == null)
        {
            return QuranAssessmentResult.Failed(
                "RealtimeSpeechRecognizerComponent is not assigned.");
        }

        if (!realtimeSpeechRecognizer.IsInitialized)
            await InitializeRecognizerAsync();

        if (!realtimeSpeechRecognizer.IsInitialized)
        {
            return QuranAssessmentResult.Failed(
                "Quran phoneme model is not initialized.");
        }

        try
        {
            string recognizedText =
                await realtimeSpeechRecognizer.TranscribeClipAsync(recording);

            List<string> actual = TokenizeRecognizedText(recognizedText);

            if (actual.Count == 0)
            {
                return QuranAssessmentResult.Failed(
                    "The model returned no usable phoneme tokens.");
            }

            Alignment alignment = Align(expected, actual);

            float score = CalculateScore(expected, alignment);

            return new QuranAssessmentResult
            {
                success = true,
                score = Mathf.Clamp(score, 0f, 100f),
                expectedPhonemes = string.Join(" ", expected),
                recognizedPhonemes = string.Join(" ", actual),
                matches = alignment.matches,
                substitutions = alignment.substitutions,
                deletions = alignment.deletions,
                insertions = alignment.insertions,
                grade = GetGrade(score)
            };
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, this);
            return QuranAssessmentResult.Failed(ex.Message);
        }
    }

    private async Task InitializeRecognizerAsync()
    {
        using (CancellationTokenSource timeout =
               new CancellationTokenSource(
                   TimeSpan.FromSeconds(initializationTimeoutSeconds)))
        {
            try
            {
                await realtimeSpeechRecognizer
                    .StartRecognizerAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                // The readiness check below returns the user-facing error.
            }
        }
    }

    private void AddNormalizedToken(
        string raw,
        List<string> destination)
    {
        string token = raw.Trim();

        if (string.IsNullOrEmpty(token))
            return;

        int id;

        if (int.TryParse(token, out id))
        {
            string vocabularyToken;

            if (quranRepository.TryGetTokenById(id, out vocabularyToken))
            {
                if (!string.Equals(vocabularyToken, "<blank>", StringComparison.Ordinal))
                    destination.Add(vocabularyToken);
            }

            return;
        }

        if (token == "<blank>")
            return;

        if (quranRepository.IsVocabularyToken(token))
        {
            destination.Add(token);
            return;
        }

        List<string> subTokens =
            quranRepository.TokenizePhonemeSequence(token);

        destination.AddRange(subTokens);
    }

    private List<string> TokenizeRecognizedText(string text)
    {
        List<string> result = new List<string>();

        if (string.IsNullOrWhiteSpace(text))
            return result;

        string[] parts = text
            .Trim()
            .Split(new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
            AddNormalizedToken(parts[i], result);

        return result;
    }

    private static Alignment Align(
        List<string> expected,
        List<string> actual)
    {
        int n = expected.Count;
        int m = actual.Count;

        float[,] distance = new float[n + 1, m + 1];

        for (int i = 1; i <= n; i++)
            distance[i, 0] =
                distance[i - 1, 0] +
                TokenWeight(expected[i - 1]);

        for (int j = 1; j <= m; j++)
            distance[0, j] += 0.5f * TokenWeight(actual[j - 1]);

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                float expectedWeight =
                    TokenWeight(expected[i - 1]);

                float substitutionCost =
                    expected[i - 1] == actual[j - 1]
                        ? 0f
                        : expectedWeight;

                float substitution =
                    distance[i - 1, j - 1] +
                    substitutionCost;

                float deletion =
                    distance[i - 1, j] +
                    expectedWeight;

                float insertion =
                    distance[i, j - 1] +
                    0.5f * TokenWeight(actual[j - 1]);

                distance[i, j] =
                    Mathf.Min(
                        substitution,
                        Mathf.Min(deletion, insertion));
            }
        }

        Alignment result = new Alignment();

        int x = n;
        int y = m;

        while (x > 0 || y > 0)
        {
            if (x > 0 && y > 0)
            {
                float expectedWeight =
                    TokenWeight(expected[x - 1]);

                float diagonalCost =
                    expected[x - 1] == actual[y - 1]
                        ? 0f
                        : expectedWeight;

                float diagonal =
                    distance[x - 1, y - 1] +
                    diagonalCost;

                if (ApproximatelyEqual(
                    distance[x, y],
                    diagonal))
                {
                    if (expected[x - 1] == actual[y - 1])
                        result.matches++;
                    else
                        result.substitutions++;

                    x--;
                    y--;
                    continue;
                }
            }

            if (x > 0)
            {
                float deletion =
                    distance[x - 1, y] +
                    TokenWeight(expected[x - 1]);

                if (ApproximatelyEqual(
                    distance[x, y],
                    deletion))
                {
                    result.deletions++;
                    x--;
                    continue;
                }
            }

            if (y > 0)
            {
                result.insertions++;
                y--;
                continue;
            }
        }

        result.totalExpectedWeight = 0f;

        for (int i = 0; i < expected.Count; i++)
            result.totalExpectedWeight += TokenWeight(expected[i]);

        return result;
    }

    private static float CalculateScore(
        List<string> expected,
        Alignment alignment)
    {
        if (expected == null || expected.Count == 0)
            return 0f;

        float totalWeight = alignment.totalExpectedWeight;

        if (totalWeight <= 0f)
            return 0f;

        float errorWeight =
            0f;

        errorWeight +=
            WeightedSubstitutionCost(
                expected,
                alignment.substitutions);

        errorWeight +=
            WeightedDeletionCost(
                expected,
                alignment.deletions);

        errorWeight +=
            alignment.insertions * 0.5f;

        float score =
            100f *
            (1f - errorWeight / totalWeight);

        return Mathf.Clamp(score, 0f, 100f);
    }

    private static float WeightedSubstitutionCost(
        List<string> expected,
        int count)
    {
        if (count <= 0 || expected == null || expected.Count == 0)
            return 0f;

        float average =
            AverageTokenWeight(expected);

        return count * average;
    }

    private static float WeightedDeletionCost(
        List<string> expected,
        int count)
    {
        if (count <= 0 || expected == null || expected.Count == 0)
            return 0f;

        float average =
            AverageTokenWeight(expected);

        return count * average;
    }

    private static float AverageTokenWeight(List<string> tokens)
    {
        if (tokens == null || tokens.Count == 0)
            return 1f;

        float sum = 0f;

        for (int i = 0; i < tokens.Count; i++)
            sum += TokenWeight(tokens[i]);

        return sum / tokens.Count;
    }

    private static float TokenWeight(string token)
    {
        if (string.IsNullOrEmpty(token))
            return 1f;

        bool hasArabicMark = false;

        for (int i = 0; i < token.Length; i++)
        {
            char c = token[i];

            if ((c >= '\u064B' && c <= '\u065F') ||
                (c >= '\u0670' && c <= '\u06ED'))
            {
                hasArabicMark = true;
                break;
            }
        }

        // Tajweed/harakah-related encoded tokens stay significant,
        // but small transcription marks do not dominate the score.
        return hasArabicMark ? 1.15f : 1f;
    }

    private static bool ApproximatelyEqual(float a, float b)
    {
        return Mathf.Abs(a - b) <= 0.0001f;
    }

    private string GetGrade(float score)
    {
        if (score >= excellentThreshold)
            return "Excellent";

        if (score >= goodThreshold)
            return "Good";

        return "TryAgain";
    }

    [Serializable]
    private sealed class Alignment
    {
        public int matches;
        public int substitutions;
        public int deletions;
        public int insertions;
        public float totalExpectedWeight;
    }
}

[Serializable]
public sealed class QuranAssessmentResult
{
    public bool success;
    public float score;
    public string expectedPhonemes;
    public string recognizedPhonemes;
    public int matches;
    public int substitutions;
    public int deletions;
    public int insertions;
    public string grade;
    public string error;

    public static QuranAssessmentResult Failed(string message)
    {
        return new QuranAssessmentResult
        {
            success = false,
            score = 0f,
            error = message,
            grade = "Failed"
        };
    }
}
