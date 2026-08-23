using UnityEngine;

public class PronunciationScore : MonoBehaviour
{
    // مرحله اول: فقط رابط امتیازدهی.
    // در مرحله بعدی موتور تشخیص گفتار عربی به این کلاس متصل می‌شود.
    public float Compare(string referenceText, string recognizedText)
    {
        if (string.IsNullOrWhiteSpace(referenceText) ||
            string.IsNullOrWhiteSpace(recognizedText))
            return 0f;

        string[] reference = Normalize(referenceText).Split(' ');
        string[] recognized = Normalize(recognizedText).Split(' ');

        int[,] dp = new int[reference.Length + 1, recognized.Length + 1];

        for (int i = 0; i <= reference.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= recognized.Length; j++) dp[0, j] = j;

        for (int i = 1; i <= reference.Length; i++)
        {
            for (int j = 1; j <= recognized.Length; j++)
            {
                int cost = reference[i - 1] == recognized[j - 1] ? 0 : 1;

                dp[i, j] = Mathf.Min(
                    Mathf.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        int maxLength = Mathf.Max(reference.Length, recognized.Length);
        if (maxLength == 0) return 0f;

        return Mathf.Clamp01(1f - (float)dp[reference.Length, recognized.Length] / maxLength) * 100f;
    }

    private string Normalize(string value)
    {
        return value
            .Replace("َ", "")
            .Replace("ِ", "")
            .Replace("ُ", "")
            .Replace("ّ", "")
            .Replace("ْ", "")
            .Replace("ً", "")
            .Replace("ٍ", "")
            .Replace("ٌ", "")
            .Replace("ـ", "")
            .Trim();
    }
}
