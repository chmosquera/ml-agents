using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Stately;
using UnityEngine.Events;
using DG.Tweening;

[RequireComponent(typeof(CameraTouchControl))]
[RequireComponent(typeof(Camera))]
public class PlaytimeRecordableCamera : MonoBehaviour
{
    public UnityEvent OnPlaybackClipCompleted;

    public Transform filmCameraProxyTrans;
    public ToggleSwitch filmModeToggleSwitch;

    // state machine
    State rootState = new State("root");
    State idle = new State("idle");
    State filming = new State("filming");
    State playback = new State("playback");
    State filmTransition = new State("transition");
    const string idleSignal = "idle";
    const string filmSignal = "filmSignal";
    const string playbackSignal = "playback";
    const string filmTransitionSignal = "transition";
    
    // state machine debug
    public bool debugLogState;
    public bool IsRecordingCharacter { get; set; }
    [SerializeField]
    [ReadOnly]
    private string currentState;
    private string currentStateLast;

    // private
    CameraTouchControl cameraTouchControl;
    PlaytimeRecorderTransform playtimeRecorder;
    PlaytimePlayerTransform playtimePlayer;

    private PlaytimeClipHolder internalStateClipHolder;
    private StateClip internalStateTargetClip;
    private StateFrame editModeStateCache;
    private bool isRecording;
    private bool isPlaying;
    private Sequence filmModeTransitonTween;

    void Awake()
    {
        cameraTouchControl = GetComponent<CameraTouchControl>();
        playtimeRecorder = filmCameraProxyTrans.GetComponent<PlaytimeRecorderTransform>();
        playtimePlayer = filmCameraProxyTrans.GetComponent<PlaytimePlayerTransform>();

        DefineStateMachine();
        rootState.Start();
    }

    private void Start()
    {
        internalStateClipHolder = gameObject.AddComponent<PlaytimeClipHolder>();

        filmModeToggleSwitch.onValueChanged.AddListener(OnFilmToggleSwitched);

        PlaytimeController.onPlayUpdate += OnPlayUpdate;
    }

    private void OnDestroy()
    {
        filmModeToggleSwitch.onValueChanged.RemoveListener(OnFilmToggleSwitched);

        PlaytimeController.onPlayUpdate -= OnPlayUpdate;
    }

    private void OnPlayUpdate(float playheadTime)
    {
        if(isRecording)
        {
            internalStateTargetClip.AddFrame(cameraTouchControl.GetInternalState(), playheadTime);
            internalStateClipHolder.PurgeEnvelopedClips(internalStateTargetClip);
        }
        else if(filmModeToggleSwitch.IsOn)
        {
            StateClip clip = internalStateClipHolder.GetClipForTime<StateFrame, StateCurve>(playheadTime) as StateClip;
            if(clip != null)
            {
                StateFrame? frame = clip.GetFrame(playheadTime);
                if (frame.HasValue)
                {
                    cameraTouchControl.SetInternalState(frame.Value, !isPlaying);
                }

                if(clip != internalStateTargetClip || playheadTime > clip.StartTime + clip.ClipDuration)
                {
                    OnPlaybackClipCompleted?.Invoke();
                    internalStateTargetClip = clip;
                }
            }

            transform.SetPositionAndRotation(filmCameraProxyTrans.position, filmCameraProxyTrans.rotation);
        }
    }

    void Update()
    {
        rootState.Update(Time.deltaTime);
        DebugStateMachineUpdate();
    }

    void DebugStateMachineUpdate()
    {
        // state machine debug        
        currentState = rootState.CurrentStatePath;
        if (debugLogState)
        {
            if (currentState != currentStateLast)
                Debug.Log("RecordableCamera state: " + currentState);
        }
        currentStateLast = currentState;
    }

    private void OnFilmToggleSwitched(bool isOn)
    {
        if(isOn)
        {
            editModeStateCache = cameraTouchControl.GetInternalState();
            rootState.SendSignal(filmTransitionSignal);
        }
        else
        {
            cameraTouchControl.SetInternalState(editModeStateCache);
        }
    }

    void DefineStateMachine()
    {
        rootState.StartAt(idle);
        idle.OnEnter = delegate
        {
            cameraTouchControl.EnableTouchInput();
            playtimePlayer.armed = true;
            playtimeRecorder.armed = false;
        };
        idle.OnUpdate = (deltaTime) =>
        {
            if (filmModeToggleSwitch.IsOn)
            {
                filmCameraProxyTrans.SetPositionAndRotation(transform.position, transform.rotation);
            }
        };
        idle.ChangeTo(playback).IfSignalCaught(playbackSignal);
        idle.ChangeTo(filming).IfSignalCaught(filmSignal);
        idle.ChangeTo(filmTransition).IfSignalCaught(filmTransitionSignal);

        filmTransition.ChangeTo(idle).IfSignalCaught(idleSignal);
        filmTransition.OnEnter = () =>
        {
            cameraTouchControl.DisableTouchInput();
            cameraTouchControl.enabled = false;

            Vector3[] path = new Vector3[2];
            path[0] = filmCameraProxyTrans.position - filmCameraProxyTrans.forward;
            path[1] = filmCameraProxyTrans.position;

            Quaternion startRotation = transform.rotation;

            filmModeTransitonTween = DOTween.Sequence()
                .Append(transform.DOPath(path, 1.25f, PathType.CatmullRom, PathMode.Full3D).SetEase(Ease.OutQuad))
                .OnUpdate(() =>
                {
                    float t = filmModeTransitonTween.ElapsedPercentage();

                    Vector3 offsetLookPos = filmCameraProxyTrans.position + filmCameraProxyTrans.forward * 0.25f;
                    Quaternion lookAtRot = Quaternion.LookRotation(offsetLookPos - transform.position, Vector3.up);

                    if(t < 0.4f)
                    {
                        transform.rotation = Quaternion.Slerp(startRotation, lookAtRot, Mathf.Pow(t / 0.4f, 0.5f));
                    }
                    else if(t > 0.5f)
                    {
                        transform.rotation = Quaternion.Slerp(lookAtRot, filmCameraProxyTrans.rotation, Mathf.Pow((t - 0.5f) / 0.5f, 0.5f));
                    }
                    else
                    {
                        transform.rotation = lookAtRot;
                    }
                })
                .OnComplete(Idle);
        };
        filmTransition.OnExit = () =>
        {
            float playheadTime = PlaytimeController.instance.PlayheadTime;
            StateClip clip = internalStateClipHolder.GetClipForTime<StateFrame, StateCurve>(playheadTime) as StateClip;
            if (clip != null && clip.GetFrame(playheadTime) is StateFrame frame)
            {
                cameraTouchControl.SetInternalState(frame, true);
            }
            else
            {
                StateFrame fakeFrame = cameraTouchControl.GetInternalState();
                fakeFrame.camLookRotationVector = -filmCameraProxyTrans.forward;
                fakeFrame.lookPoint = filmCameraProxyTrans.position + filmCameraProxyTrans.forward * fakeFrame.camLookDistance;
                cameraTouchControl.SetInternalState(fakeFrame, true);
            }

            cameraTouchControl.enabled = true;
        };

        filming.ChangeTo(idle).IfSignalCaught(idleSignal);
        filming.OnEnter = delegate
        {
            // cameraTouchControl.SavePosition();
            cameraTouchControl.EnableTouchInput();
            playtimePlayer.armed = false;
            playtimeRecorder.armed = true;

            playtimeRecorder.StartRecording();

            float time = PlaytimeController.instance.PlayheadTime;
            internalStateTargetClip = new StateClip(time);
            internalStateClipHolder.InsertClipAtTime(time, internalStateTargetClip);

            isRecording = true;
        };
        filming.OnUpdate = (_) =>
        {
            if(filmModeToggleSwitch.IsOn)
            {
                filmCameraProxyTrans.position = transform.position;
                filmCameraProxyTrans.rotation = transform.rotation;
            }
        };
        filming.OnExit = delegate
        {
            playtimeRecorder.StopRecording();
            cameraTouchControl.ClearPreviousPositionData();
            isRecording = false;
            internalStateTargetClip = null;
        };

        playback.ChangeTo(idle).IfSignalCaught(idleSignal);
        playback.OnEnter = delegate
        {
            cameraTouchControl.DisableTouchInput();
            playtimePlayer.armed = true;
            playtimeRecorder.armed = false;
            isPlaying = true;
            internalStateTargetClip = internalStateClipHolder.GetClipForTime<StateClip>(PlaytimeController.instance.PlayheadTime);
        };
        playback.OnExit = () =>
        {
            cameraTouchControl.ClearPreviousPositionData();
            isPlaying = false;
            internalStateTargetClip = null;

            if (filmModeToggleSwitch.IsOn)
            {
                float playheadTime = PlaytimeController.instance.PlayheadTime;
                StateClip clip = internalStateClipHolder.GetClipForTime<StateFrame, StateCurve>(playheadTime) as StateClip;
                if (clip != null)
                {
                    StateFrame? frame = clip.GetFrame(playheadTime);
                    if (frame.HasValue)
                    {
                        cameraTouchControl.SetInternalState(frame.Value, true);
                    }
                }
            }
        };

    }
    
    public void Idle()
    {
        rootState.SendSignal(idleSignal);
    }

    public void Film()
    {
        rootState.SendSignal(filmSignal);
    }

    public void Playback()
    {
        if (IsRecordingCharacter)
        {
            rootState.SendSignal(idleSignal);
        }
        else
        {
            rootState.SendSignal(playbackSignal);
        }
    }

    #region Internal State Recording
    public struct StateFrame
    {
        public Vector3 lookPoint;
        public Vector3 camLookRotationVector;
        public float camLookDistance;
    }

    public class StateCurve
    {
        public Vector3AnimationCurve lookPointCurve = new Vector3AnimationCurve();
        public Vector3AnimationCurve camLookRotationVectorCurve = new Vector3AnimationCurve();
        public AnimationCurve camLookDistanceCurve = new AnimationCurve();
    }

    public class StateClip : PlaytimeClip<StateFrame, StateCurve>
    {
        private StateCurve data = new StateCurve();

        public StateClip(float startTime) : base(startTime) { }

        public override void AddFrame(StateFrame frame, float time)
        {
            time = ConvertGlobalToLocalTime(time);

            data.lookPointCurve.Add(frame.lookPoint, time);
            data.camLookRotationVectorCurve.Add(frame.camLookRotationVector, time);
            data.camLookDistanceCurve.AddKey(time, frame.camLookDistance);

            if (time > ClipDuration) ClipDuration = time;
        }

        public override StateCurve GetData()
        {
            return data;
        }

        public override StateFrame? GetFrame(float time)
        {
            time = ConvertGlobalToLocalTime(time) + startOffset;

            return new StateFrame()
            {
                lookPoint = data.lookPointCurve.Get(time),
                camLookRotationVector = data.camLookRotationVectorCurve.Get(time),
                camLookDistance = data.camLookDistanceCurve.Evaluate(time)
            };
        }

        public override void TrimEndToTime(float time)
        {
            time = ConvertGlobalToLocalTime(time);

            ClipDuration = Mathf.Min(ClipDuration, time);
        }
    }
    #endregion
}
