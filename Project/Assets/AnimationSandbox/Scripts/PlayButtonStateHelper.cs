using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayButtonStateHelper : MonoBehaviour
{
    [SerializeField] private SpriteSwapToggleButton button = default;
    [SerializeField] private Canvas canvas = default;
    [SerializeField] private CanvasGroup group = default;

    private void Start()
    {
        canvas.enabled = group.interactable = false;

        PlaytimeController.onPlayStart += OnPlayStart;
        PlaytimeController.onPlayStop += OnPlayStop;
    }

    private void OnDestroy()
    {
        PlaytimeController.onPlayStart -= OnPlayStart;
        PlaytimeController.onPlayStop -= OnPlayStop;
    }

    private void OnPlayStop()
    {
        button.SetIsOnWithoutNotify(false);
    }

    private void OnPlayStart()
    {
        button.SetIsOnWithoutNotify(true);
    }
}
