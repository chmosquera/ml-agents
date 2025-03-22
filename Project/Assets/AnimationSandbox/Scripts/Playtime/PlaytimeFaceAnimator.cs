using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_IOS
using UnityEngine.XR.ARKit;
#endif

public class PlaytimeFaceAnimator : MonoBehaviour
{
    [SerializeField] private EyeAnimator leftEye = default;
    [SerializeField] private EyeAnimator rightEye = default;
    [SerializeField] private BrowAnimator leftBrow = default;
    [SerializeField] private BrowAnimator rightBrow = default;
    [SerializeField] private MouthSpriteAnimator mouth = default;
    [SerializeField] private TongueAnimator tongue = default;
    [SerializeField] private PlaytimeAudioSource audioSource = default;

    private PlaytimeClipHolder clipHolder;
    private bool isRecording;
    private PlaytimeClipFace recordingClip;
    private bool interpolateMouth;

    private void Start()
    {
        clipHolder = gameObject.AddComponent<PlaytimeClipHolder>();

        PlaytimeController.onPlayUpdate += OnPlayheadTimeUpdate;
    }

    private void OnDestroy()
    {
        PlaytimeController.onPlayUpdate -= OnPlayheadTimeUpdate;
    }

    public void StartRecording()
    {
        isRecording = true;

        float time = PlaytimeController.instance.PlayheadTime;
        recordingClip = new PlaytimeClipFace(time);
        clipHolder.InsertClipAtTime(time, recordingClip);
    }

    public void StopRecording()
    {
        recordingClip = null;
        isRecording = false;
    }

    private void OnPlayheadTimeUpdate(float time)
    {
        if(isRecording)
        {
            Frame frame = GetCurrentCaptureData();
            recordingClip.AddFrame(frame, time);
            clipHolder.PurgeEnvelopedClips(recordingClip);
            ApplyFrameToAnimators(frame);
        }
        else
        {
            PlaytimeClipFace clip = clipHolder.GetClipForTime<PlaytimeClipFace>(time);
            if(clip != null)
            {
                ApplyFrameToAnimators(clip.GetFrame(time).Value);
            }
            else
            {
                ApplyFrameToAnimators(default);
            }
        }
    }

    private void ApplyFrameToAnimators(Frame frame)
    {
        leftEye.ApplyFrame(frame.leftEye);
        rightEye.ApplyFrame(frame.rightEye);
        leftBrow.ApplyFrame(frame.leftBrow);
        rightBrow.ApplyFrame(frame.rightBrow);
        mouth.SetMouthOpenFactor(frame.mouthOpen, interpolateMouth);
        tongue.ApplyFrame(frame.tongueOut);
    }

    private Frame GetCurrentCaptureData()
    {
        Frame frame = new Frame();

#if UNITY_IOS
        ARFacialCapture source = ARFacialCapture.Instance;

        frame.leftEye = new EyeAnimator.Frame()
        {
            eyeLook = new Vector2()
            {
                x = source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.EyeLookInLeft) - source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.EyeLookOutLeft),
                y = source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.EyeLookUpLeft) - source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.EyeLookDownLeft),
            },
            blink = source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.EyeBlinkLeft),
        };

        frame.rightEye = new EyeAnimator.Frame()
        {
            eyeLook = new Vector2()
            {
                x = source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.EyeLookOutRight) - source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.EyeLookInRight),
                y = source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.EyeLookUpRight) - source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.EyeLookDownRight),
            },
            blink = source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.EyeBlinkRight),
        };

        frame.leftBrow = new BrowAnimator.Frame()
        {
            innerUpDown = source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.BrowInnerUp),
            outerUpDown = source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.BrowOuterUpLeft) - source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.BrowDownLeft),
        };

        frame.rightBrow = new BrowAnimator.Frame()
        {
            innerUpDown = source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.BrowInnerUp),
            outerUpDown = source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.BrowOuterUpRight) - source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.BrowDownRight),
        };

        frame.mouthOpen = source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.JawOpen) - source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.MouthClose);

        frame.tongueOut = source.GetBlendShapeCoefficient(ARKitBlendShapeLocation.TongueOut);
#else
        interpolateMouth = true;
        frame.mouthOpen = audioSource.CurrentNormalizedAmplitude;
#endif
        return frame;
    }

    #region Clip Classes
    public struct Frame
    {
        public EyeAnimator.Frame leftEye;
        public EyeAnimator.Frame rightEye;
        public BrowAnimator.Frame leftBrow;
        public BrowAnimator.Frame rightBrow;
        public float mouthOpen;
        public float tongueOut;
    }

    public class EyeCurve
    {
        public AnimationCurve x = new AnimationCurve();
        public AnimationCurve y = new AnimationCurve();
        public AnimationCurve blink = new AnimationCurve();

        public EyeAnimator.Frame GetFrame(float time)
        {
            return new EyeAnimator.Frame()
            {
                eyeLook = new Vector2()
                {
                    x = this.x.Evaluate(time),
                    y = this.y.Evaluate(time)
                },
                blink = this.blink.Evaluate(time),
            };
        }

        public void RecordFrame(EyeAnimator.Frame frame, float time)
        {
            x.AddKey(time, frame.eyeLook.x);
            y.AddKey(time, frame.eyeLook.y);
            blink.AddKey(time, frame.blink);
        }
    }

    public class BrowCurve
    {
        public AnimationCurve inner = new AnimationCurve();
        public AnimationCurve outer = new AnimationCurve();

        public BrowAnimator.Frame GetFrame(float time)
        {
            return new BrowAnimator.Frame()
            {
                innerUpDown = inner.Evaluate(time),
                outerUpDown = outer.Evaluate(time),
            };
        }

        public void RecordFrame(BrowAnimator.Frame frame, float time)
        {
            inner.AddKey(time, frame.innerUpDown);
            outer.AddKey(time, frame.outerUpDown);
        }
    }

    public class Curve 
    {
        public EyeCurve leftEye = new EyeCurve();
        public EyeCurve rightEye = new EyeCurve();
        public BrowCurve leftBrow = new BrowCurve();
        public BrowCurve rightBrow = new BrowCurve();
        public AnimationCurve mouthOpen = new AnimationCurve();
        public AnimationCurve tongueOut = new AnimationCurve();

        public Frame GetFrame(float time)
        {
            return new Frame()
            {
                leftEye = this.leftEye.GetFrame(time),
                rightEye = this.rightEye.GetFrame(time),
                leftBrow = this.leftBrow.GetFrame(time),
                rightBrow = this.rightBrow.GetFrame(time),
                mouthOpen = this.mouthOpen.Evaluate(time),
                tongueOut = this.tongueOut.Evaluate(time),
            };
        }

        public void RecordFrame(Frame frame, float time)
        {
            leftEye.RecordFrame(frame.leftEye, time);
            rightEye.RecordFrame(frame.rightEye, time);
            leftBrow.RecordFrame(frame.leftBrow, time);
            rightBrow.RecordFrame(frame.rightBrow, time);
            mouthOpen.AddKey(time, frame.mouthOpen);
            tongueOut.AddKey(time, frame.tongueOut);
        }
    }

    public class PlaytimeClipFace : PlaytimeClip<Frame, Curve>
    {
        private Curve data;

        public PlaytimeClipFace(float startTime) : base (startTime)
        {
            data = new Curve();
        }

        public override void AddFrame(Frame frame, float time)
        {
            time = ConvertGlobalToLocalTime(time);

            data.RecordFrame(frame, time);

            if (time > ClipDuration) ClipDuration = time;
        }

        public override Frame? GetFrame(float time)
        {
            time = ConvertGlobalToLocalTime(time) + startOffset;

            return data.GetFrame(time);
        }

        public override Curve GetData() => data;

        public override void TrimEndToTime(float time)
        {
            time = ConvertGlobalToLocalTime(time);

            ClipDuration = Mathf.Min(ClipDuration, time);
        }
    }
    #endregion
}
