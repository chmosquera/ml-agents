using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaytimeTimelineMarkerSource : MonoBehaviour
{
    [SerializeField] private PlaytimeClipHolder clipHolder = default;
    [SerializeField] private Renderer characterColorRenderer = default;
    [SerializeField] private Color characterColor = default;
    [SerializeField] private bool randomizeColorOnStart = default;

    private Material materialInstance;

    private void Awake()
    {
        if(randomizeColorOnStart)
        {
            characterColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.75f, 1f);
        }

        if(characterColorRenderer != null)
        {
            materialInstance = characterColorRenderer.material;
            materialInstance.color = characterColor;
        }

        this.enabled = false;
    }

    private void OnDestroy()
    {
        Destroy(materialInstance);
    }

    private void Update()
    {
        SendMarkerUpdateToTimeline();
    }

    public void SendMarkerUpdateToTimeline()
    {
        PlaytimeTimelineSliderUI.Instance.SetMarker(clipHolder, characterColor);
    }
}
