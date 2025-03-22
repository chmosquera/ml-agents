using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Vector3AnimationCurve
{
    public AnimationCurve x;
    public AnimationCurve y;
    public AnimationCurve z;

    public Vector3AnimationCurve()
    
    {
        x = new AnimationCurve();
        y = new AnimationCurve();
        z = new AnimationCurve();
    }

    public void Add (Vector3 v, float time)
    {
        x.AddKey (time, v.x);
        y.AddKey (time, v.y);
        z.AddKey (time, v.z);
    }

    public Vector3 Get (float _time)
    {
        return new Vector3 (x.Evaluate (_time), y.Evaluate (_time), z.Evaluate (_time));
    }
}

[Serializable]
public class QuaternionAnimationCurve
{
    public AnimationCurve x;
    public AnimationCurve y;
    public AnimationCurve z;
    public AnimationCurve w;

    public QuaternionAnimationCurve()
    {
        x = new AnimationCurve();
        y = new AnimationCurve();
        z = new AnimationCurve();
        w = new AnimationCurve();
    }

    public void Add (Quaternion v, float time)
    {
        x.AddKey (time, v.x);
        y.AddKey (time, v.y);
        z.AddKey (time, v.z);
        w.AddKey (time, v.w);
    }

    public Quaternion Get (float _time)
    {
        return new Quaternion (x.Evaluate (_time), y.Evaluate (_time), z.Evaluate (_time), w.Evaluate (_time));
    }
}
