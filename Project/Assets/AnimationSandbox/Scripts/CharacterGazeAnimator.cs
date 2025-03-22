using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterGazeAnimator : MonoBehaviour
{
    [SerializeField] private CharacterEmotion emotion = default;
    [SerializeField] private Transform pivotTransform = default;
    [SerializeField] private Vector3 pivotAxis = default;
    [SerializeField] private float happyPitch = default;
    [SerializeField] private float sadPitch = default;
    [SerializeField] private Vector3 happyPosition = default;
    [SerializeField] private Vector3 sadPosition = default;

    private Quaternion neutralRotation;

    private void Awake()
    {
        neutralRotation = pivotTransform.localRotation;
    }

    private void LateUpdate()
    {
        float pitch = Mathf.Lerp(sadPitch, happyPitch, emotion.happiness);
        pivotTransform.localRotation = Quaternion.AngleAxis(pitch, pivotAxis) * neutralRotation;
        pivotTransform.localPosition = Vector3.Lerp(sadPosition, happyPosition, emotion.happiness);
    }
}
