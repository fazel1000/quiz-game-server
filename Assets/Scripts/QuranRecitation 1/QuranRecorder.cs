using System;
using UnityEngine;

public class QuranRecorder : MonoBehaviour
{
    [SerializeField] private int microphoneFrequency = 16000;
    [SerializeField] private int maxRecordSeconds = 15;

    private AudioClip recordingClip;
    private string microphoneDevice;
    private bool isRecording;

    public bool IsRecording => isRecording;
    public AudioClip LastRecording => recordingClip;

    public void StartRecording()
    {
        if (isRecording) return;

        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("No microphone found.");
            return;
        }

        microphoneDevice = Microphone.devices[0];
        recordingClip = Microphone.Start(
            microphoneDevice,
            false,
            maxRecordSeconds,
            microphoneFrequency);

        isRecording = true;
    }

    public AudioClip StopRecording()
    {
        if (!isRecording) return recordingClip;

        int position = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);
        isRecording = false;

        if (recordingClip == null || position <= 0)
            return recordingClip;

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

        return recordingClip;
    }

    public void ToggleRecording()
    {
        if (isRecording) StopRecording();
        else StartRecording();
    }
}
