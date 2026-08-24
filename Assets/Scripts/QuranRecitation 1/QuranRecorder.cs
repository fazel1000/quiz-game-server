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

    public bool IsRecording => isRecording;
    public AudioClip LastRecording => recordingClip;
    public event Action<AudioClip> RecordingFinished;

    public void StartRecording()
    {
        if (isRecording || Microphone.devices.Length == 0) return;
        microphoneDevice = Microphone.devices[0];
        recordingClip = Microphone.Start(microphoneDevice, false, maxRecordSeconds, microphoneFrequency);
        isRecording = true;
        silenceCoroutine = StartCoroutine(CheckSilence());
    }

    private IEnumerator CheckSilence()
    {
        float silentTime = 0f;
        while(isRecording)
        {
            yield return new WaitForSeconds(0.2f);
            int pos = Microphone.GetPosition(microphoneDevice);
            if(pos <= 0 || recordingClip == null) continue;
            float[] data = new float[256];
            int start = Mathf.Max(0, pos-256);
            recordingClip.GetData(data, start);
            float max = 0f;
            foreach(float s in data) max = Mathf.Max(max, Mathf.Abs(s));
            if(max < silenceThreshold) silentTime += 0.2f;
            else silentTime = 0f;
            if(silentTime >= silenceSecondsToStop) StopRecording();
        }
    }

    public AudioClip StopRecording()
    {
        if(!isRecording) return recordingClip;
        if(silenceCoroutine != null) StopCoroutine(silenceCoroutine);
        int position = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);
        isRecording=false;
        if(recordingClip != null && position>0)
        {
            float[] samples=new float[position*recordingClip.channels];
            recordingClip.GetData(samples,0);
            AudioClip trimmed=AudioClip.Create("PlayerRecording",position,recordingClip.channels,recordingClip.frequency,false);
            trimmed.SetData(samples,0);
            recordingClip=trimmed;
        }
        RecordingFinished?.Invoke(recordingClip);
        return recordingClip;
    }
}
