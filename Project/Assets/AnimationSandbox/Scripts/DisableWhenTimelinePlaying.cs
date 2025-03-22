using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableWhenTimelinePlaying : MonoBehaviour
{
    private void Start()
    {
        PlaytimeController.onPlayStart += OnPlayStart;
        PlaytimeController.onPlayStop += OnPlayStop;
    }

    private void OnDestroy()
    {
        PlaytimeController.onPlayStart -= OnPlayStart;
        PlaytimeController.onPlayStop -= OnPlayStop;
    }

    private void OnPlayStart()
    {
        gameObject.SetActive(false);
    }

    private void OnPlayStop()
    {
        gameObject.SetActive(true);
    }
}
