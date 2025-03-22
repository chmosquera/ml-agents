using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Edwon.Tools;

public class PlaytimeTimelineSliderUI : MonoBehaviour
{
    public static PlaytimeTimelineSliderUI Instance { get; private set; }

    public Slider slider;

    [SerializeField] private float minTimelineLength = 5f;
    [SerializeField] private float timelineSoftBoundarySize = 1f;
    [SerializeField] private float timelineSmoothTime = 1f;
    [SerializeField] private float timelineTickmarkInterval = 5f;
    [SerializeField] private float timelineTickmarkBoldInterval = 10f;
    [SerializeField] private float timelineTickWidth = 3f;
    [SerializeField] private float timelineTickBoldWidth = 8f;

    [SerializeField] private RectTransform clipViewsRect = default;
    [SerializeField] private PlaytimeClipView clipViewPrefab = default;
    [SerializeField] private RectTransform markersRect = default;
    [SerializeField] private RectTransform markersPrefab = default;
    [SerializeField] private RectTransform timeTicksRect = default;
    [SerializeField] private RectTransform timeTicksPrefab = default;
    [SerializeField] private PlaytimeClipHolder clipSource = default;

    private float timelineLengthTarget;
    private float currentTimelineVelocity;
    private List<PlaytimeClipView> clipViews;
    private List<RectTransform> timeTicks;
    private Dictionary<PlaytimeClipHolder, RectTransform> timelineMarkers;

    void Awake()
    {
        Instance = this;

        slider.maxValue = minTimelineLength;
        timelineLengthTarget = minTimelineLength;

        clipViews = new List<PlaytimeClipView>(10);
        timelineMarkers = new Dictionary<PlaytimeClipHolder, RectTransform>(10);
        timeTicks = new List<RectTransform>(50);

        SetEnabledClipViewCount(5); // Pool 5 clip views on start to avoid instantiation at run time
        SetEnabledClipViewCount(0); // Disable clip views in pool by default

        UpdateTickMarks();
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    private void OnRectTransformDimensionsChange()
    {
        if (Instance == null) return;

        UpdateAllMarkers();
        UpdateClipViewsUI();
        UpdateTickMarks();
    }

    private void Update()
    {
        if(!Mathf.Approximately(timelineLengthTarget, slider.maxValue))
        {
            slider.maxValue = Mathf.SmoothDamp(slider.maxValue, timelineLengthTarget, ref currentTimelineVelocity, timelineSmoothTime);

            UpdateAllMarkers();
            UpdateClipViewsUI();
            UpdateTickMarks();
        }
    }

    void OnFirstPlayStart()
    {

    }

    void OnPlayStart()
    {

    }

    void OnPlayUpdate(float time)
    {
        slider.SetValueWithoutNotify(time);

        // TODO only need to do this when clip source object is recording
        UpdateClipViewsUI();
    }

    void OnPlayStop()
    {

    }

    void OnPlayReset()
    {

    }

    void OnPlaytimeTotalChanged(float newPlaytimeTotal)
    {
        timelineLengthTarget = Mathf.Max(newPlaytimeTotal + timelineSoftBoundarySize, minTimelineLength);
    }

    public void SetClipSource(PlaytimeClipHolder holder)
    {
        clipSource = holder;
        UpdateClipViewsUI();
    }

    public void ClearClipSource()
    {
        clipSource = null;
        UpdateClipViewsUI();
    }

    public void SetMarker(PlaytimeClipHolder clipHolder, Color markerColor)
    {
        if(!timelineMarkers.TryGetValue(clipHolder, out RectTransform marker))
        {
            marker = Instantiate<RectTransform>(markersPrefab, markersRect);

            Graphic[] markerGraphics = marker.GetComponentsInChildren<Graphic>();
            for(int i = 0; i < markerGraphics.Length; ++i)
            {
                markerGraphics[i].color = markerColor;
            }

            timelineMarkers.Add(clipHolder, marker);
        }

        SetMarkerPositionOnTimeline(marker, markersRect, clipHolder.GetClipLengthSum(), slider.maxValue);

        marker.SetAsLastSibling();
    }

    private void SetEnabledClipViewCount(int count)
    {
        for(int i = 0; i < count; ++i)
        {
            if (i < clipViews.Count)
            {
                clipViews[i].gameObject.SetActive(true);
            }
            else
            {
                PlaytimeClipView newClip = Instantiate<PlaytimeClipView>(clipViewPrefab, clipViewsRect); 
                clipViews.Add(newClip);

                newClip.OnPointerClick += OnClipTapped;
            }
        }

        for(int i = count; i < clipViews.Count; ++i)
        {
            clipViews[i].gameObject.SetActive(false);
        }
    }

    private void UpdateClipViewsUI()
    {
        if(clipSource == null)
        {
            SetEnabledClipViewCount(0);
            return;
        }

        int numClips = clipSource.clips.Count;
        SetEnabledClipViewCount(numClips);

        float rectWidth = clipViewsRect.rect.width;
        float rectHeight = clipViewsRect.rect.height;
        float timelineLength = slider.maxValue;

        for(int i = 0; i < clipSource.clips.Count; ++i)
        {
            RectTransform view = clipViews[i].RectTransform;
            PlaytimeClip clip = clipSource.clips[i];

            float xPos = clip.StartTime / timelineLength * rectWidth;
            float width = clip.ClipDuration / timelineLength * rectWidth;

            view.offsetMin = new Vector2(xPos, view.offsetMin.y);
            view.offsetMax = new Vector2(xPos, view.offsetMax.y);

            view.sizeDelta = new Vector2(width, rectHeight);
        }
    }

    private void UpdateAllMarkers()
    {
        float timelineLength = slider.maxValue;
        foreach(KeyValuePair<PlaytimeClipHolder, RectTransform> markerPair in timelineMarkers)
        {
            float time = markerPair.Key.GetClipLengthSum();
            SetMarkerPositionOnTimeline(markerPair.Value, markersRect, time, timelineLength);
        }
    }

    private void UpdateTickMarks()
    {
        float totalTime = slider.maxValue;
        int numTicks = Mathf.FloorToInt(totalTime / timelineTickmarkInterval) + 1;
        for(int i = 0; i < numTicks; ++i)
        {
            RectTransform tick;
            if (i >= timeTicks.Count)
            {
                tick = Instantiate<RectTransform>(timeTicksPrefab, timeTicksRect);
                timeTicks.Add(tick);
            }
            else
            {
                tick = timeTicks[i];
            }

            float time = i * timelineTickmarkInterval;
            float widthOverride = Mathf.Approximately(time % timelineTickmarkBoldInterval, 0f) ? timelineTickBoldWidth : timelineTickWidth;

            tick.gameObject.SetActive(true);
            SetMarkerPositionOnTimeline(tick, timeTicksRect, time, totalTime, widthOverride);
        }
        for(int i = numTicks; i < timeTicks.Count; ++i)
        {
            timeTicks[i].gameObject.SetActive(false);
        }
    }

    private void SetMarkerPositionOnTimeline(RectTransform marker, RectTransform container, float time, float timelineLength, float? widthOverride = null)
    {
        float posX = time / timelineLength * container.rect.width;
        float width = widthOverride.HasValue ? widthOverride.Value : (marker.offsetMax.x - marker.offsetMin.x);

        marker.offsetMin = new Vector2(posX, marker.offsetMin.y);
        marker.offsetMax = new Vector2(posX + width, marker.offsetMin.y + container.rect.height);
    }

    private void OnClipTapped(PlaytimeClipView view)
    {
        int clipIndex = view.transform.GetSiblingIndex();

        if (clipSource == null || clipIndex >= clipSource.clips.Count) return;

        slider.value = clipSource.clips[clipIndex].StartTime;
    }

    void OnEnable()
    {
        PlaytimeController.onFirstPlayStart += OnFirstPlayStart;
        PlaytimeController.onPlayStart += OnPlayStart;
        PlaytimeController.onPlayUpdate += OnPlayUpdate;
        PlaytimeController.onPlayStop += OnPlayStop;
        PlaytimeController.onPlayReset += OnPlayReset;
        PlaytimeController.onPlaytimeTotalChanged += OnPlaytimeTotalChanged;
    }

    void OnDisable()
    {
        PlaytimeController.onFirstPlayStart -= OnFirstPlayStart;
        PlaytimeController.onPlayStart -= OnPlayStart;
        PlaytimeController.onPlayUpdate -= OnPlayUpdate;
        PlaytimeController.onPlayStop -= OnPlayStop;
        PlaytimeController.onPlayReset -= OnPlayReset;
        PlaytimeController.onPlaytimeTotalChanged -= OnPlaytimeTotalChanged;
    }
}
