using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeAnimator : MonoBehaviour
{
    public struct Frame
    {
        public Vector2 eyeLook;
        public float blink;
    }

    [SerializeField] private Transform pupilTransform = default;
    [SerializeField] private Vector2 pupilMovementExtents = default;
    [SerializeField] private Transform upperLidPivotTransform = default;
    [SerializeField] private Vector3 upperLidOpenScale = default;
    [SerializeField] private Vector3 upperLidClosedScale = default;
    [SerializeField] private Transform lowerLidPivotTransform = default;
    [SerializeField] private Vector3 lowerLidOpenScale = default;
    [SerializeField] private Vector3 lowerLidClosedScale = default;

    public void ApplyFrame(Frame frame)
    {
        Vector3 pupilPosition = frame.eyeLook * pupilMovementExtents;
        pupilPosition.z = pupilTransform.localPosition.z;
        pupilTransform.localPosition = pupilPosition;

        upperLidPivotTransform.localScale = Vector3.Lerp(upperLidOpenScale, upperLidClosedScale, frame.blink);
        lowerLidPivotTransform.localScale = Vector3.Lerp(lowerLidOpenScale, lowerLidClosedScale, frame.blink);
    }
}
