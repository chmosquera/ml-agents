using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Stately;
using UnityEngine.Events;

[RequireComponent(typeof(PlaytimeClipHolder))]
public class PlaytimeRecorderTransform : MonoBehaviour
{
    public bool armed = true;

    private PlaytimeClipHolder clipHolder;
    private PlaytimeClipTransform targetClip;

    // state machine
    State rootState = new State("root");
    State stopped = new State("stopped");
    State recording = new State("recording");
    const string startRecordingSignal = "record";
    const string stopRecordingSignal = "stop";

    // events
    public UnityEvent onStartRecording;
    public UnityEvent onStopRecording;

    void Awake()
    {
        clipHolder = GetComponent<PlaytimeClipHolder>();
        DefineStateMachine();
        rootState.Start();
    }

    void Update()
    {
        rootState.Update(Time.deltaTime);
    }

    void DefineStateMachine()
    {
        rootState.StartAt(stopped);
        rootState.ChangeToSubState(stopped).IfSignalCaught(stopRecordingSignal);
        rootState.ChangeToSubState(recording).IfSignalCaught(startRecordingSignal);

        recording.OnEnter = delegate
        {
            float time = PlaytimeController.instance.PlayheadTime;
            targetClip = new PlaytimeClipTransform(time);
            clipHolder.InsertClipAtTime(time, targetClip);
            onStartRecording.Invoke();
        };
        recording.OnUpdate = delegate 
        {
            float recordingTime = PlaytimeController.instance.PlayheadTime;
            targetClip.AddFrame(new TransformFrame(transform.position, transform.rotation), recordingTime);
            clipHolder.PurgeEnvelopedClips(targetClip);
        };
        recording.OnExit = delegate
        {
            onStopRecording.Invoke();
            targetClip = null;
        };
    }

    [InspectorButton("StartRecording")]
    public bool startRecording;
    public void StartRecording()
    {
        rootState.SendSignal(startRecordingSignal);
    }

    [InspectorButton("StopRecording")]
    public bool stopRecording;
    public void StopRecording()
    {
        rootState.SendSignal(stopRecordingSignal);
    }
}