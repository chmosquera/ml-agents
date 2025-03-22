using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmPoseAnimator : MonoBehaviour
{
    public CharacterArmPose[] poses = default;
    public float interpolationTime = 0.15f;

    private int currentPose = 0;
    private float currentWeight = 0f;
    private bool isActive = false;
    private bool isRecording = false;
    private PosePanel uiPanel;

    private PlaytimeClipHolder clipHolder;
    private Clip targetClip;

    private void Start()
    {
        uiPanel = FindObjectOfType<PosePanel>();
        clipHolder = gameObject.AddComponent<PlaytimeClipHolder>();

        PlaytimeController.onPlayUpdate += OnPlaytimeUpdate;
    }

    private void OnDestroy()
    {
        PlaytimeController.onPlayUpdate -= OnPlaytimeUpdate;
    }

    public void SetPose(int index)
    {
        currentPose = index;
        isActive = true;
    }

    public void ReleasePose()
    {
        isActive = false;
    }

    public float GetCurrentPoseAndWeight(out CharacterArmPose pose)
    {
        if (currentPose < poses.Length)
        {
            pose = poses[currentPose];
        }
        else
        {
            pose = null;
        }
        return currentWeight / interpolationTime;
    }

    public void Activate()
    {
        uiPanel.SetCharacterTarget(this);

        float time = PlaytimeController.instance.PlayheadTime;
        targetClip = new Clip(time);
        clipHolder.InsertClipAtTime(time, targetClip);
        isRecording = true;
    }

    public void Deactivate()
    {
        uiPanel.SetCharacterTarget(null);
        targetClip = null;
        isRecording = false;
    }

    private void OnPlaytimeUpdate(float time)
    {
        if (isRecording)
        {
            if (isActive)
            {
                currentWeight += Time.deltaTime;
            }
            else
            {
                currentWeight -= Time.deltaTime;
            }

            currentWeight = Mathf.Clamp(currentWeight, 0f, interpolationTime);
            targetClip.AddFrame(new Frame() { index = currentPose, weight = currentWeight }, time);
            clipHolder.PurgeEnvelopedClips(targetClip);
        }
        else
        {
            Clip clip = clipHolder.GetClipForTime<Clip>(time);
            if(clip != null)
            {
                Frame frame = clip.GetFrame(time).Value;
                currentPose = frame.index;
                currentWeight = frame.weight;
            }
        }
    }

    public struct Frame
    {
        public float weight;
        public int index;
    }

    public class Curve
    {
        public AnimationCurve weight = new AnimationCurve();
        public AnimationCurve index = new AnimationCurve();
    }

    public class Clip : PlaytimeClip<Frame, Curve>
    {
        private Curve data = new Curve();

        public Clip(float startTime) : base(startTime) { }

        public override void AddFrame(Frame frame, float time)
        {
            time = ConvertGlobalToLocalTime(time);

            data.weight.AddKey(time, frame.weight);
            data.index.AddKey(time, frame.index);

            if (time > ClipDuration) ClipDuration = time;
        }

        public override Curve GetData() => data;

        public override Frame? GetFrame(float time)
        {
            time = ConvertGlobalToLocalTime(time) + startOffset;

            return new Frame()
            {
                weight = data.weight.Evaluate(time),
                index = Mathf.RoundToInt(data.index.Evaluate(time))
            };
        }

        public override void TrimEndToTime(float time)
        {
            time = ConvertGlobalToLocalTime(time);

            ClipDuration = Mathf.Min(ClipDuration, time);
        }
    }
}
