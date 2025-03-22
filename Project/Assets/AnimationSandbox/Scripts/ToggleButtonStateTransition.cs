using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButtonStateTransition : MonoBehaviour
{
    [SerializeField] private Toggle toggle = default;
    [SerializeField] private CanvasGroup canvasGroup = default;
    [SerializeField] private Graphic enabledGraphic = default;
    [SerializeField] private float enabledCanvasAlpha = 1f;
    [SerializeField] private float disabledCanvasAlpha = 0.5f;

    private void OnEnable()
    {
        OnToggled(toggle.isOn);
    }

    public void OnToggled(bool enabled)
    {
        canvasGroup.alpha = enabled ? enabledCanvasAlpha : disabledCanvasAlpha;
        enabledGraphic.enabled = enabled;
    }
}
