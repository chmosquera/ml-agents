using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouthSpriteAnimator : MonoBehaviour
{
    [SerializeField] private Transform spriteTransform = default;
    [SerializeField] private Vector3 mouthClosedScale = default;
    [SerializeField] private Vector3 mouthOpenScale = default;
    [SerializeField] private AnimationCurve interpolationCurve = default;
    [SerializeField] private float ascendingSmoothTime = 0.05f;
    [SerializeField] private float descendingSmoothTime = 0.05f;

    private float interpolationFactor;
    private float interpolationVelocity;

    private void Start()
    {
        spriteTransform.localScale = mouthClosedScale;
    }

    public void SetMouthOpenFactor(float open, bool interpolate = true)
    {
        if (interpolate)
        {
            float t = interpolationCurve.Evaluate(open);
            float smoothTime = t > interpolationFactor ? ascendingSmoothTime : descendingSmoothTime;
            interpolationFactor = Mathf.SmoothDamp(interpolationFactor, t, ref interpolationVelocity, smoothTime);
            spriteTransform.localScale = Vector3.Lerp(mouthClosedScale, mouthOpenScale, interpolationFactor);
        }
        else
        {
            spriteTransform.localScale = Vector3.Lerp(mouthClosedScale, mouthOpenScale, open);
        }
    }
}
