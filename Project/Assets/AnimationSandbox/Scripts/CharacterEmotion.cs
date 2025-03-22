using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterEmotion : MonoBehaviour
{
    [Range(0f, 1f)]
    public float happiness;

    private PlaytimeClipHolder clipHolder;
    private bool isActive;
    private EmotionClip targetClip;

    private static Slider uiSlider;

    private void Start()
    {
        clipHolder = gameObject.AddComponent<PlaytimeClipHolder>();
        if(uiSlider == null)
        {
            uiSlider = GameObject.Find("EmotionSlider").GetComponent<Slider>();
            uiSlider.gameObject.SetActive(false);
        }

        PlaytimeController.onPlayUpdate += OnPlaytimeUpdate;
    }

    public void Activate()
    {
        uiSlider.SetValueWithoutNotify(happiness);
        uiSlider.onValueChanged.AddListener(OnValueChanged);
        uiSlider.gameObject.SetActive(true);

        isActive = true;

        float time = PlaytimeController.instance.PlayheadTime;
        targetClip = new EmotionClip(time);
        clipHolder.InsertClipAtTime(time, targetClip);
    }

    public void Deactivate()
    {
        uiSlider.onValueChanged.RemoveListener(OnValueChanged);
        uiSlider.gameObject.SetActive(false);
        isActive = false;
    }

    private void OnValueChanged(float value)
    {
        happiness = value;
    }

    private void OnPlaytimeUpdate(float playheadTime)
    {
        if(isActive)
        {
            targetClip.AddFrame(happiness, playheadTime);
            clipHolder.PurgeEnvelopedClips(targetClip);
        }
        else
        {
            EmotionClip clip = clipHolder.GetClipForTime<EmotionClip>(playheadTime);
            if(clip != null)
            {
                happiness = clip.GetFrame(playheadTime).Value;
            }
        }
    }

    public class EmotionClip : PlaytimeClip<float, AnimationCurve>
    {
        private AnimationCurve data = new AnimationCurve();

        public EmotionClip(float startTime) : base(startTime) { }

        public override void AddFrame(float frame, float time)
        {
            time = ConvertGlobalToLocalTime(time);

            data.AddKey(time, frame);

            if (time > ClipDuration) ClipDuration = time;
        }

        public override AnimationCurve GetData() => data;

        public override float? GetFrame(float time)
        {
            time = ConvertGlobalToLocalTime(time) + startOffset;

            return data.Evaluate(time);
        }

        public override void TrimEndToTime(float time)
        {
            time = ConvertGlobalToLocalTime(time);

            ClipDuration = Mathf.Min(ClipDuration, time);
        }
    }
}

