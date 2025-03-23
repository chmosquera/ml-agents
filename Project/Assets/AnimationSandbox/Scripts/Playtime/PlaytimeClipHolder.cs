using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlaytimeClipHolder : MonoBehaviour
{
    public List<PlaytimeClip> clips = new List<PlaytimeClip>();

    void OnEnable()
    {
        // PlaytimeController.instance.clipHolders.Add(this);
    }

    void OnDisable()
    {
        // PlaytimeController.instance.clipHolders.Remove(this);
    }

    public PlaytimeClip<TFrameDataType, TCurveType> GetClipForTime<TFrameDataType, TCurveType>(float time)
        where TFrameDataType : struct
    {
        if (clips == null || clips.Count == 0)
        {
            return null;
        }

        for(int i = 0; i < clips.Count - 1; ++i)
        {
            PlaytimeClip clip = clips[i];
            if(clip.StartTime <= time && clip.StartTime + clip.ClipDuration >= time)
            {
                return clip as PlaytimeClip<TFrameDataType, TCurveType>;
            }
        }
        return clips[clips.Count - 1] as PlaytimeClip<TFrameDataType, TCurveType>;
    }

    public TClipType GetClipForTime<TClipType>(float time)
        where TClipType : PlaytimeClip
    {
        if (clips == null || clips.Count == 0)
        {
            return null;
        }

        for(int i = 0; i < clips.Count - 1; ++i)
        {
            PlaytimeClip clip = clips[i];
            if(clip.StartTime <= time && clip.StartTime + clip.ClipDuration >= time)
            {
                return clip as TClipType;
            }
        }
        return clips[clips.Count - 1] as TClipType;
    }

    public float GetClipLengthSum()
    {
        if(clips != null && clips.Count == 0)
        {
            return 0f;
        }

        float max = 0f;
        for(int i = 0; i < clips.Count; ++i)
        {
            max = Mathf.Max(max, clips[i].StartTime + clips[i].ClipDuration);
        }

        return max;
    }

    public void InsertClipAtTime(float time, PlaytimeClip clip)
    {
        if(clips.Count > 0 && clips[0].StartTime >= time)
        {
            clips.Insert(0, clip);
            return;
        }

        for(int i = 0; i < clips.Count; ++i)
        {
            PlaytimeClip existing = clips[i];
            if(existing.StartTime < time && existing.StartTime + existing.ClipDuration > time)
            {
                PlaytimeClip.Split(existing, time, out PlaytimeClip first, out PlaytimeClip last);

                clips[i] = last;
                clips.Insert(i, clip);
                clips.Insert(i, first);
                return;
            }
        }

        clips.Add(clip);
    }

    public void PurgeEnvelopedClips(PlaytimeClip target)
    {
        int targetIndex = clips.IndexOf(target);

        float timelineTime = target.StartTime + target.ClipDuration;

        for(int i = targetIndex + 1; i < clips.Count; ++i)
        {
            if(clips[i].StartTime < timelineTime)
            {
                clips[i].OffsetStartToTime(timelineTime);
            }
            else { break; }
        }

        while(targetIndex < clips.Count - 2 &&
            (clips[targetIndex + 1].ClipDuration < 0f || timelineTime >= clips[targetIndex + 1].StartTime + clips[targetIndex + 1].ClipDuration))
        {
            clips.RemoveAt(targetIndex + 1);
        }
    }
}
