using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaytimeClipTransform : PlaytimeClip<TransformFrame, TransformCurve>
{
    bool hasData = false;

    private TransformCurve data = new TransformCurve();

    public PlaytimeClipTransform(float startTime)
        : base (startTime)
    {
    }

    public override void AddFrame(TransformFrame frame, float time)
    {
        time = ConvertGlobalToLocalTime(time);

        hasData = true;
        data.positionCurve.Add(frame.position, time);
        data.rotationCurve.Add(frame.rotation, time);

        if(time > ClipDuration)
        {
            ClipDuration = time;
        }
    }

    public override TransformFrame? GetFrame(float time) 
    {
        if (!hasData)
            return null;

        time = ConvertGlobalToLocalTime(time) + startOffset;

        return new TransformFrame(data.positionCurve.Get(time), data.rotationCurve.Get(time));
    }

    public override TransformCurve GetData()
    {
        return data;
    }

    public override void TrimEndToTime(float time)
    {
        time = ConvertGlobalToLocalTime(time);

        ClipDuration = Mathf.Min(ClipDuration, time);
    }
}

public struct TransformFrame
{
    public Vector3 position;
    public Quaternion rotation;

    public TransformFrame(Vector3 position, Quaternion rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }
}

public class TransformCurve
{
    public Vector3AnimationCurve positionCurve = new Vector3AnimationCurve();
    public QuaternionAnimationCurve rotationCurve = new QuaternionAnimationCurve();
}
