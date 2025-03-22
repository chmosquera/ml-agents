using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlaytimeControllerEventListener : MonoBehaviour
{
    public UnityEvent onFirstPlayStart;
    public UnityEvent onPlayStart;
    public UnityEvent<float> onPlayUpdate;
    public UnityEvent onPlayStop;
    public UnityEvent onPlayReset;
    
    void OnFirstPlayStart()
    {
        onFirstPlayStart.Invoke();
    }

    void OnPlayStart()
    {
        onPlayStart.Invoke();
    }

    void OnPlayUpdate(float time)
    {
        onPlayUpdate.Invoke(time);
    }

    void OnPlayStop()
    {
        onPlayStop.Invoke();
    }

    void OnPlayReset()
    {
        onPlayReset.Invoke();
    }

    void OnEnable()
    {
        PlaytimeController.onFirstPlayStart += OnFirstPlayStart;
        PlaytimeController.onPlayStart += OnPlayStart;
        PlaytimeController.onPlayUpdate += OnPlayUpdate;
        PlaytimeController.onPlayStop += OnPlayStop;
        PlaytimeController.onPlayReset += OnPlayReset;
    }

    void OnDisable()
    {
        PlaytimeController.onFirstPlayStart -= OnFirstPlayStart;
        PlaytimeController.onPlayStart -= OnPlayStart;
        PlaytimeController.onPlayUpdate -= OnPlayUpdate;
        PlaytimeController.onPlayStop -= OnPlayStop;
        PlaytimeController.onPlayReset -= OnPlayReset;
    }
}
