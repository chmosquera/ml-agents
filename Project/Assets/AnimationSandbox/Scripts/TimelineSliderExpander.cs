using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TimelineSliderExpander : MonoBehaviour
{
    [SerializeField] private RectTransform timelineTransform = default;
    [SerializeField] private float expandAnimDuration = 0.15f;
    [SerializeField] private Ease expandAnimEase = default;
    [SerializeField] private float contractAnimDuration = 0.15f;
    [SerializeField] private Ease contractAnimEase = default;

    private Vector2 expandedMinOffset;
    private Vector3 defaultMinOffset;

    private Tween currentTween;

    private void Awake()
    {
        defaultMinOffset = timelineTransform.offsetMin;
        expandedMinOffset = defaultMinOffset;
        expandedMinOffset.x = -timelineTransform.offsetMax.x;

        timelineTransform.offsetMin = expandedMinOffset;
        this.enabled = false;
    }

    private void OnEnable()
    {
        TweenOffset(defaultMinOffset, contractAnimDuration, contractAnimEase);
    }

    private void OnDisable()
    {
        TweenOffset(expandedMinOffset, expandAnimDuration, expandAnimEase);
    }

    private void OnDestroy()
    {
        if(currentTween != null)
        {
            currentTween.Kill();
            currentTween = null;
        }
    }

    private void TweenOffset(Vector2 offset, float duration, Ease ease)
    {
        if(currentTween != null)
        {
            currentTween.Kill();
        }

        currentTween = DOTween.To(() => timelineTransform.offsetMin, (newOffset) => timelineTransform.offsetMin = newOffset, offset, duration)
            .SetEase(ease)
            .OnComplete(() => currentTween = null);
    }
}
