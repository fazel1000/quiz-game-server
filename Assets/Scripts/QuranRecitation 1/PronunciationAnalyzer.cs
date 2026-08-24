using UnityEngine;

public class PronunciationAnalyzer : MonoBehaviour
{
    private MFCCExtractor mfcc = new MFCCExtractor();
    private DTWCalculator dtw = new DTWCalculator();

    public float Analyze(AudioClip reference, AudioClip user)
    {
        var refData = mfcc.Extract(reference);
        var userData = mfcc.Extract(user);

        float distance = dtw.Calculate(refData,userData);

        float score = 100f - (distance * 100f);

        return Mathf.Clamp(score,0,100);
    }
}