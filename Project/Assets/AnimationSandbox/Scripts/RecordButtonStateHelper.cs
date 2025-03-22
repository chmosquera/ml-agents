using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecordButtonStateHelper : MonoBehaviour
{
    [SerializeField] private PlaytimeRecorderTransform cameraRecorder = default;
    [SerializeField] private ToggleSwitch filmModeSwitch = default;
    [SerializeField] private Canvas canvas = default;
    [SerializeField] private CanvasGroup group = default;

    private void Start()
    {
        canvas.enabled = group.interactable = false;
        filmModeSwitch.onValueChanged.AddListener(OnFilmSwitchChanged);

        PlaytimeController.onPlayStart += OnPlayStart;
        PlaytimeController.onPlayStop += OnPlayStop;
    }

    private void OnDestroy()
    {
        filmModeSwitch.onValueChanged.RemoveListener(OnFilmSwitchChanged);
        PlaytimeController.onPlayStart -= OnPlayStart;
        PlaytimeController.onPlayStop -= OnPlayStop;
    }

    private void OnFilmSwitchChanged(bool isOn)
    {
        SetEnabled(isOn);
    }

    private void OnPlayStop()
    {
        SetEnabled(PlaytimeController.instance.AnimationHasBeenRecorded && filmModeSwitch.IsOn);
    }

    private void OnPlayStart()
    {
        SetEnabled(cameraRecorder.armed);
    }

    private void SetEnabled(bool enabled)
    {
        canvas.enabled = group.interactable = enabled;
    }
}
