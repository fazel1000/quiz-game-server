using UnityEngine;
using System.Collections.Generic;

public class MFCCExtractor
{
    // Unity lightweight MFCC placeholder pipeline.
    // Frame extraction and feature calculation are isolated here
    // so it can be tuned for Quran recitation audio.

    public List<float[]> Extract(AudioClip clip)
    {
        List<float[]> result = new List<float[]>();

        if (clip == null)
            return result;

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        int frameSize = 400;   // 25ms at 16kHz
        int hopSize = 160;     // 10ms at 16kHz

        for (int i = 0; i + frameSize < samples.Length; i += hopSize)
        {
            float[] feature = new float[13];

            for (int j = 0; j < 13; j++)
            {
                float sum = 0;

                for (int k = j; k < frameSize; k += 13)
                    sum += Mathf.Abs(samples[i + k]);

                feature[j] = sum / frameSize;
            }

            result.Add(feature);
        }

        return result;
    }
}