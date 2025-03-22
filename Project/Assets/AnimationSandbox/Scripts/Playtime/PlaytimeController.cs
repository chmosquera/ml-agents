using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Stately;
using Edwon.Tools;
using UnityEngine.UI;

public class PlaytimeController : MonoBehaviour
{
    public static PlaytimeController instance;

    // Time
    public float PlayheadTime { get; private set; }
    private float playtimeTotal;
    
    // ui
    public Toggle loopToggle;

    // state machine
    State rootState = new State("root");
    State stoppedState = new State("stopped");
    State playingState = new State("playing");
    State loopState = new State("loop");
    State backToStartState = new State("backToStart");
    const string playSignal = "play";
    const string stopSignal = "stop";
    const string backToStartSignal = "backToStart";

    // state machine debug
    public bool debugLogState;
    [SerializeField]
    [ReadOnly]
    private string currentState;
    private string currentStateLast;

    // inspector variables
    private bool loop = false;
    private bool Loop
    {
        get {return loop;}
        set
        {
            loopToggle.isOn = value;
            loop = value;
        }
    }
    [SerializeField]
    [ReadOnly]
    private int loopCount = 0;
    public List<PlaytimeClipHolder> clipHolders = new List<PlaytimeClipHolder>();

    private bool animationHasBeenRecorded;
    public bool AnimationHasBeenRecorded
    {
        get
        {
            if (animationHasBeenRecorded) return true;

            for(int i = 0; i < clipHolders.Count; ++i)
            {
                if(clipHolders[i].clips.Count > 0)
                {
                    animationHasBeenRecorded = true;
                    return true;
                }
            }

            return false;
        }
    }

    // events
    public delegate void OnFirstPlayStart();
    public static event OnFirstPlayStart onFirstPlayStart;
    public delegate void OnPlayStart();
    public static event OnPlayStart onPlayStart;
    public delegate void OnPlayUpdate(float playheadTime);
    public static event OnPlayUpdate onPlayUpdate;
    public delegate void OnPlayStop();
    public static event OnPlayStop onPlayStop;
    public delegate void OnPlayReset();
    public static event OnPlayReset onPlayReset; // for resetting the stage, physics, character animation etc...
    public delegate void OnPlaytimeTotalChanged(float totalTime);
    public static event OnPlaytimeTotalChanged onPlaytimeTotalChanged;

    void Awake()
    {
        instance = this;
        DefineStateMachine();
        rootState.Start();
    }

    void DefineStateMachine()
    {
        rootState.StartAt(stoppedState);
        rootState.ChangeToSubState(stoppedState).IfSignalCaught(stopSignal);
        rootState.ChangeToSubState(playingState).IfSignalCaught(playSignal);
        rootState.ChangeToSubState(backToStartState).IfSignalCaught(backToStartSignal);
        stoppedState.OnEnter = delegate
        {
            loopCount = 0;
            if (onPlayStop != null)
                onPlayStop();
        };
        playingState.OnEnter = delegate
        {
            if (PlayheadTime == 0 && loopCount == 0)
                if (onFirstPlayStart != null)
                    onFirstPlayStart();

            if (onPlayStart != null)
                onPlayStart();
        };
        playingState.OnUpdate = (deltaTime) =>
        {
            if (onPlayUpdate != null)
                onPlayUpdate(PlayheadTime);

            PlayheadTime += deltaTime;
        };
        playingState.ChangeTo(loopState).If(()=> Loop && PlayheadTime > playtimeTotal);
        playingState.ChangeTo(backToStartState).If(() => !Loop && PlayheadTime > playtimeTotal);
        loopState.OnEnter = delegate
        {
            if (onPlayReset != null)
                onPlayReset();
            loopCount += 1;
            PlayheadTime = 0;
        };
        loopState.ChangeTo(playingState).AfterOneFrame();
        backToStartState.OnEnter = delegate
        {
            loopCount = 0;
            PlayheadTime = 0;
            if (onPlayUpdate != null)
                onPlayUpdate(PlayheadTime);
            if (onPlayReset != null)
                onPlayReset();
        };
        backToStartState.ChangeTo(stoppedState).AfterOneFrame();
    }

    void Update()
    {
        UpdateLongestClipLength();
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
                Debug.Log("PlaytimeController state: " + currentState);
        }
        currentStateLast = currentState;
    }

    void UpdateLongestClipLength()
    {
        float longestTotalTime = 0;
        foreach (PlaytimeClipHolder clipHolder in clipHolders)
        {
            float animLength = clipHolder.GetClipLengthSum();
            if (animLength > longestTotalTime)
                longestTotalTime = animLength;
        }
        if(playtimeTotal != longestTotalTime)
        {
            playtimeTotal = longestTotalTime;
            onPlaytimeTotalChanged?.Invoke(playtimeTotal);
        }
    }

    [InspectorButton("Play")]
    public bool play;
    public void Play()
    {
        rootState.SendSignal(playSignal);
    }

    [InspectorButton("Stop")]
    public bool stop;
    public void Stop()
    {
        rootState.SendSignal(stopSignal);
    }

    [InspectorButton("BackToStart")]
    public bool backToStart;
    public void BackToStart()
    {
        rootState.SendSignal(backToStartSignal);
    }

    public void OnPlayToggle(bool toggle)
    {
        if (toggle)
        {
            Play();
        }
        else
        {
            Stop();
            // BackToStart();
        }
    }

    public void OnFilmToggle(bool toggle)
    {
        if (toggle)
        {
            Loop = false;
            Play();
        }
        else
        {
            Stop();
        }
    }

    public void OnLoopToggle(bool toggle)
    {
        if (toggle)
        {
            Loop = true;
        }
        else
        {
            Loop = false;
        }
    }

    public void OnTimelineSlider(float value)
    {
        PlayheadTime = value;
        onPlayUpdate?.Invoke(PlayheadTime);
    }

    public void OnTimelineBackgroundPressed()
    {
        if(rootState.CurrentState == stoppedState)
        {
            BackToStart();
        }
    }
}