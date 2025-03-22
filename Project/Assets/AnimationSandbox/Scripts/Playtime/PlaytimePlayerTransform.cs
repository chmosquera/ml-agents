using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Stately;
using Edwon.Tools;
using UnityEngine.Events;

[RequireComponent(typeof(PlaytimeClipHolder))]
public class PlaytimePlayerTransform : MonoBehaviour
{
    public bool armed = true;

    PlaytimeClipHolder clipHolder;
    public UnityEvent onPlayStart;
    public UnityEvent onPlayStop;
    
    void Awake()
    {
        clipHolder = GetComponent<PlaytimeClipHolder>();
    }

    void OnDestroy()
    {
        
    }

    void OnPlayStartEvent()
    {
        onPlayStart.Invoke();
    }

    void OnPlayUpdateEvent(float playtime)
    {
        if (!armed) return;

        TransformFrame? currentFrame = clipHolder.GetClipForTime<TransformFrame, TransformCurve>(playtime)?.GetFrame(playtime);
        if (currentFrame != null)
        {
            transform.position = currentFrame.Value.position;
            transform.rotation = currentFrame.Value.rotation;
        }
    }

    void OnPlayStopEvent()
    {
        onPlayStop.Invoke();
    }

    void OnEnable()
    {
        PlaytimeController.onPlayUpdate += OnPlayUpdateEvent;
        PlaytimeController.onPlayStart += OnPlayStartEvent;
        PlaytimeController.onPlayStop += OnPlayStopEvent;
    }

    void OnDisable()
    {
        PlaytimeController.onPlayUpdate -= OnPlayUpdateEvent;
        PlaytimeController.onPlayStart -= OnPlayStartEvent;
        PlaytimeController.onPlayStop -= OnPlayStopEvent;
    }
}
