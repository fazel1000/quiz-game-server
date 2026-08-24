using UnityEngine;
using RTLTMPro;

public class PronunciationScore : MonoBehaviour
{
    [SerializeField] private PronunciationAnalyzer analyzer;
    [SerializeField] private RTLTextMeshPro percentText;


    public float CompareAudio(AudioClip referenceClip, AudioClip recordedClip)
    {
        if (referenceClip == null || recordedClip == null)
        {
            Debug.LogWarning("Audio clip is missing");
            return 0f;
        }

        if (analyzer == null)
        {
            Debug.LogWarning("Pronunciation Analyzer is missing");
            return 0f;
        }

        float score = analyzer.Analyze(referenceClip, recordedClip);

        ShowScore(score);

        return score;
    }


    private void ShowScore(float score)
    {
        score = Mathf.Clamp(score, 0, 100);

        if (percentText != null)
            percentText.text = Mathf.RoundToInt(score) + "%";
    }
}