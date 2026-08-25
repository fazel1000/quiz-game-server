using System;
using System.Collections;
using UnityEngine;

public class QuranRecorder : MonoBehaviour
{
    [SerializeField] private int microphoneFrequency = 16000;
    [SerializeField] private int maxRecordSeconds = 15;
    [SerializeField] private float silenceSecondsToStop = 3f;
    [SerializeField] private float silenceThreshold = 0.015f;

    private AudioClip recordingClip;
    private string microphoneDevice;
    private bool isRecording;
    private Coroutine silenceCoroutine;
    private float recordingStartedAt;

    public bool IsRecording => isRecording;
    public AudioClip LastRecording => recordingClip;
    public event Action<AudioClip> RecordingFinished;

    public void StartRecording()
    {
        if (isRecording || Microphone.devices.Length == 0) return;

        microphoneDevice = Microphone.devices[0];
        recordingClip = Microphone.Start(microphoneDevice, false, maxRecordSeconds, microphoneFrequency);

        if (recordingClip == null) return;

        isRecording = true;
        recordingStartedAt = Time.realtimeSinceStartup;
        silenceCoroutine = StartCoroutine(CheckSilence());
    }

    private IEnumerator CheckSilence()
    {
        float silentTime = 0f;
        bool speechDetected = false;

        while (isRecording)
        {
            yield return new WaitForSecondsRealtime(0.2f);

            if (Time.realtimeSinceStartup - recordingStartedAt >= maxRecordSeconds)
            {
                silenceCoroutine = null;
                StopRecording();
                yield break;
            }

            int pos = Microphone.GetPosition(microphoneDevice);

            if (pos <= 0 || recordingClip == null) continue;

            float[] data = new float[256];
            int start = Mathf.Max(0, pos - data.Length);
            recordingClip.GetData(data, start);

            float max = 0f;

            foreach (float sample in data)
                max = Mathf.Max(max, Mathf.Abs(sample));

            if (max >= silenceThreshold)
            {
                speechDetected = true;
                silentTime = 0f;
            }
            else if (speechDetected)
            {
                silentTime += 0.2f;
            }

            if (speechDetected && silentTime >= silenceSecondsToStop)
            {
                silenceCoroutine = null;
                StopRecording();
                yield break;
            }
        }
    }

    public AudioClip StopRecording()
    {
        if (!isRecording) return recordingClip;

        if (silenceCoroutine != null)
        {
            Coroutine runningCoroutine = silenceCoroutine;
            silenceCoroutine = null;
            StopCoroutine(runningCoroutine);
        }

        int position = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);
        isRecording = false;

        if (recordingClip != null && position > 0)
        {
            float[] samples = new float[position * recordingClip.channels];
            recordingClip.GetData(samples, 0);

            AudioClip trimmed = AudioClip.Create(
                "PlayerRecording",
                position,
                recordingClip.channels,
                recordingClip.frequency,
                false);

            trimmed.SetData(samples, 0);
            recordingClip = trimmed;
        }

        RecordingFinished?.Invoke(recordingClip);
        return recordingClip;
    }
}
