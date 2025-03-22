using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TongueAnimator : MonoBehaviour
{
    [SerializeField] private Transform tongueTransform = default;
    [SerializeField] private Vector3 tongueInPosition = default;
    [SerializeField] private Vector3 tongueOutPosition = default;

    public void ApplyFrame(float tongueOut)
    {
        tongueTransform.localPosition = Vector3.Lerp(tongueInPosition, tongueOutPosition, tongueOut);
    }
}
