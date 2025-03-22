using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlaytimeAudioSource : MonoBehaviour
{
    [SerializeField] private AudioSource playbackSource = default;
    [SerializeField] private int maxClipLength = 30;
    [SerializeField] private float amplitudeSampleInterval = 0.1f;
    [SerializeField] private float amplitudeNormalizationRange = 0.1f;

    private string micDeviceName;
    private int micFreq;
    private float[] amplitudeSampleBuffer;
    private float lastSampledTimeSeconds;

    private bool isRecording;
    private bool isPlaying;
    private PlaytimeClipHolder clipHolder;
    private PlaytimeClipAudio targetClip;

    public float CurrentNormalizedAmplitude { get; private set; }

    private void Start()
    {
        clipHolder = gameObject.AddComponent<PlaytimeClipHolder>();

        micDeviceName = Microphone.devices[0];
        Microphone.GetDeviceCaps(micDeviceName, out int minFreq, out int maxFreq);
        micFreq = maxFreq;

        if (micFreq == 0) micFreq = Mathf.Max(44100, minFreq);

        int sampleWindowSize = Mathf.RoundToInt(micFreq * amplitudeSampleInterval);
        amplitudeSampleBuffer = new float[sampleWindowSize];
        lastSampledTimeSeconds = float.MinValue;

        PlaytimeController.onPlayStart += OnPlayStart;
        PlaytimeController.onPlayStop += OnPlayStop;
        PlaytimeController.onPlayUpdate += OnPlayUpdate;
    }

    private void OnDestroy()
    {
        PlaytimeController.onPlayStart -= OnPlayStart;
        PlaytimeController.onPlayStop -= OnPlayStop;
        PlaytimeController.onPlayUpdate -= OnPlayUpdate;
    }

    public void StartRecording()
    {
        float time = PlaytimeController.instance.PlayheadTime;


#if UNITY_IOS
        iPhoneSpeaker.ForceToEarpiece();
#endif

        AudioClip input = Microphone.Start(micDeviceName, false, maxClipLength, micFreq);

        targetClip = new PlaytimeClipAudio(time, input);

        clipHolder.InsertClipAtTime(time, targetClip);
        isRecording = true;
    }

    public void StopRecording()
    {
        Microphone.End(micDeviceName);
        isRecording = false;
        targetClip = null;

#if UNITY_IOS
        iPhoneSpeaker.ForceToSpeaker();
#endif
    }

    private void OnPlayStart()
    {
        if (isRecording) return;

        isPlaying = true;

        float time = PlaytimeController.instance.PlayheadTime;
        PlaytimeClipAudio clip = clipHolder.GetClipForTime<PlaytimeClipAudio>(time);
        if(clip != null)
        {
            Frame? frame = clip.GetFrame(time);
            if(frame.HasValue)
            {
                playbackSource.clip = frame.Value.clip;
                playbackSource.Play();
                playbackSource.time = Mathf.Clamp(frame.Value.localClipTime, 0f, frame.Value.clip.length);
            }
        }
    }

    private void OnPlayStop()
    {
        if (isRecording) return;

        isPlaying = false;

        playbackSource.Stop();
    }

    private void OnPlayUpdate(float playheadTime)
    {
        PlaytimeClipAudio clip = clipHolder.GetClipForTime<PlaytimeClipAudio>(playheadTime);
        if (clip == null)
        {
            CurrentNormalizedAmplitude = 0f;
            lastSampledTimeSeconds = playheadTime;
            return;
        }

        if (isRecording)
        {
            AudioClip audioClip = targetClip.GetData().clip;
            targetClip.AddFrame(new Frame() { clip = audioClip }, playheadTime);
            clipHolder.PurgeEnvelopedClips(targetClip);
            if (Mathf.Abs(playheadTime - lastSampledTimeSeconds) > amplitudeSampleInterval)
            {
                CalculateAmplitudeFromMicrophoneInput(audioClip, Microphone.GetPosition(micDeviceName));
                lastSampledTimeSeconds = playheadTime;
            }
            return;
        }

        Frame? frame = clip.GetFrame(playheadTime);
        if (frame.HasValue && Mathf.Abs(playheadTime - lastSampledTimeSeconds) > amplitudeSampleInterval)
        {
            CalculateAmplitudeFromPlayback(frame.Value.clip, frame.Value.localClipTime);
        }

        if (isPlaying)
        {
            if (frame.HasValue && frame.Value.clip != null)
            {
                if (frame.Value.clip != playbackSource.clip)
                {
                    playbackSource.clip = frame.Value.clip;
                    playbackSource.Play();
                    playbackSource.time = Mathf.Clamp(frame.Value.localClipTime, 0f, frame.Value.clip.length);
                }
                else if (Mathf.Abs(playbackSource.time - frame.Value.localClipTime) > 0.1f)
                {
                    playbackSource.time = Mathf.Clamp(frame.Value.localClipTime, 0f, frame.Value.clip.length);
                }
            }
        }
    }

    private void CalculateAmplitudeFromMicrophoneInput(AudioClip clip, int timeSamples)
    {
        if (clip == null)
        {
            CurrentNormalizedAmplitude = 0f;
            return;
        }
        int startSample = Mathf.Clamp(timeSamples - amplitudeSampleBuffer.Length + 1, 0, clip.samples - amplitudeSampleBuffer.Length);
        CalculateAmplitude(clip, startSample);
    }

    private void CalculateAmplitudeFromPlayback(AudioClip clip, float timeSeconds)
    {
        if (clip == null)
        {
            CurrentNormalizedAmplitude = 0f;
            return;
        }
        int startSample = Mathf.RoundToInt(timeSeconds * clip.frequency);
        CalculateAmplitude(clip, startSample);
    }

    private void CalculateAmplitude(AudioClip clip, int startSample)
    {
        clip.GetData(amplitudeSampleBuffer, startSample);

        float maxAmplitude = 0f;
        for(int i = 0; i < amplitudeSampleBuffer.Length; ++i)
        {
            maxAmplitude = Mathf.Max(maxAmplitude, Mathf.Abs(amplitudeSampleBuffer[i]));
        }

        amplitudeNormalizationRange = Mathf.Max(maxAmplitude, amplitudeNormalizationRange);

        CurrentNormalizedAmplitude = maxAmplitude / amplitudeNormalizationRange;
    }

    #region Clip Structs/Classes
    public struct Frame 
    {
        public AudioClip clip;
        public float localClipTime;
    }

    public struct Curve
    {
        public AudioClip clip;
    }

    public class PlaytimeClipAudio : PlaytimeClip<Frame, Curve>
    {
        private AudioClip data;

        public PlaytimeClipAudio(float startTime, AudioClip data) : base (startTime) 
        {
            this.data = data;
        }

        public override void AddFrame(Frame frame, float time)
        {
            time = ConvertGlobalToLocalTime(time);

            if (frame.clip != data) data = frame.clip;

            if (time > ClipDuration) ClipDuration = time;
        }

        public override Curve GetData()
        {
            return new Curve() { clip = data };
        }

        public override Frame? GetFrame(float time)
        {
            time = ConvertGlobalToLocalTime(time);
            return new Frame() { clip = data, localClipTime = time + startOffset };
        }

        public override void TrimEndToTime(float time)
        {
            time = ConvertGlobalToLocalTime(time);

            ClipDuration = Mathf.Min(time, ClipDuration);
        }
        #endregion
    }
}
