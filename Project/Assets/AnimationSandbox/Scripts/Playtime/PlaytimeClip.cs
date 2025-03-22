using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class PlaytimeClip
{
    protected float startTime;
    protected float startOffset;

    public float StartTime => startTime + startOffset;
    public float ClipDuration { get; protected set; }

    public PlaytimeClip(float startTime)
    {
        this.startTime = startTime;
    }

    public abstract void TrimEndToTime(float time);

    public virtual void OffsetStartToTime(float time)
    {
        float newOffset = time - startTime;
        ClipDuration -= newOffset - startOffset;
        startOffset = newOffset;
    }

    protected virtual float ConvertGlobalToLocalTime(float time)
    {
        return time - StartTime;
    }

    public static void Split(PlaytimeClip source, float time, out PlaytimeClip first, out PlaytimeClip second)
    {
        float splitDuration = time - source.StartTime;

        first = source;
        second = source.MemberwiseClone() as PlaytimeClip;

        first.ClipDuration = splitDuration;

        second.startOffset = splitDuration;
        second.ClipDuration = second.ClipDuration - splitDuration;
    }
}

public abstract class PlaytimeClip<TFrameDataType, TCurveType> : PlaytimeClip
    where TFrameDataType : struct
{
    public PlaytimeClip(float startTime) : base(startTime) { }

    public abstract void AddFrame(TFrameDataType frame, float time);
    public abstract TFrameDataType? GetFrame(float time);
    public abstract TCurveType GetData();
}
