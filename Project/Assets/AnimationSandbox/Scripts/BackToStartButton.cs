using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackToStartButton : MonoBehaviour
{
    [SerializeField] private CanvasGroup group = default;
    [SerializeField] private Button button = default;

    private bool isPlaying;

    private void Start()
    {
        PlaytimeController.onPlayUpdate += OnPlayheadUpdate;
        PlaytimeController.onPlayStart += OnPlayStart;
        PlaytimeController.onPlayStop += OnPlayStop;

        button.onClick.AddListener(OnButtonPressed);

        OnPlayheadUpdate(0f);
    }

    private void OnDestroy()
    {
        PlaytimeController.onPlayUpdate -= OnPlayheadUpdate;
        PlaytimeController.onPlayStart -= OnPlayStart;
        PlaytimeController.onPlayStop -= OnPlayStop;

        button.onClick.RemoveListener(OnButtonPressed);
    }

    private void OnButtonPressed()
    {
        PlaytimeController.instance.OnTimelineSlider(0f);
    }

    private void OnPlayheadUpdate(float playheadTime)
    {
        UpdateButtonState(playheadTime);
    }

    private void OnPlayStart()
    {
        isPlaying = true;

        UpdateButtonState(PlaytimeController.instance.PlayheadTime);
    }

    private void OnPlayStop()
    {
        isPlaying = false;

        UpdateButtonState(PlaytimeController.instance.PlayheadTime);
    }

    private void UpdateButtonState(float playheadTime)
    {
        bool buttonEnabled = !isPlaying && playheadTime > 0f;

        if(group.interactable != buttonEnabled)
        {
            group.interactable = buttonEnabled;
            group.alpha = buttonEnabled ? 1f : 0f;
        }
    }
}
