using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmWalkAnimator : MonoBehaviour
{
    [SerializeField] private Transform armTarget = default;
    [SerializeField] private Transform legTarget = default;
    [SerializeField] private Transform happyNeutralHandTransform = default;
    [SerializeField] private Transform sadNeutralHandTransform = default;
    [SerializeField] private CharacterEmotion emotion = default;
    [SerializeField] private Vector3 happyMultiplier = default;
    [SerializeField] private Vector3 sadMultiplier = default;
    [SerializeField] private ArmPoseAnimator poseAnimator = default;
    [SerializeField] private bool isRightSide = default;

    private Vector3 neutralLegPos;

    private Vector3 armVelocity;

    private void Start()
    {
        neutralLegPos = legTarget.localPosition;
    }

    private void Update()
    {
        Vector3 legDelta = legTarget.localPosition - neutralLegPos;

        Vector3 multiplier = Vector3.Lerp(sadMultiplier, happyMultiplier, emotion.happiness);
        legDelta.Scale(multiplier);

        Vector3 neutral = Vector3.Lerp(sadNeutralHandTransform.localPosition, happyNeutralHandTransform.localPosition, emotion.happiness);

        Vector3 position = Vector3.SmoothDamp(armTarget.localPosition, neutral - legDelta, ref armVelocity, 0.1f);
        Quaternion rotation = Quaternion.Slerp(sadNeutralHandTransform.localRotation, happyNeutralHandTransform.localRotation, emotion.happiness);

        float poseWeight = poseAnimator.GetCurrentPoseAndWeight(out CharacterArmPose pose);
        if(pose != null && poseWeight > 0f)
        {
            Transform poseTransform = isRightSide ? pose.right : pose.left;

            position = Vector3.Lerp(position, poseTransform.localPosition, poseWeight);
            rotation = Quaternion.Slerp(rotation, poseTransform.localRotation, poseWeight);
        }

        armTarget.localPosition = position;
        armTarget.localRotation = rotation;
    }
}
