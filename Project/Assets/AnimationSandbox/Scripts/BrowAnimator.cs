using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrowAnimator : MonoBehaviour
{
    public struct Frame
    {
        public float innerUpDown;
        public float outerUpDown;
    }

    [Header("Facial Capture Animation")]
    [SerializeField] private Transform innerTransform = default;
    [SerializeField] private float innerLowY = default;
    [SerializeField] private float innerHighY = default;
    [SerializeField] private Transform outerTransform = default;
    [SerializeField] private float outerLowY = default;
    [SerializeField] private float outerHighY = default;

    [Header("Emotion Animation")]
    [SerializeField] private CharacterEmotion emotion = default;
    [SerializeField] private Transform emotionTransform = default;
    [SerializeField] private Vector3 happyPosition = default;
    [SerializeField] private Vector3 sadPosition = default;
    [SerializeField] private Vector3 rotationAxis = default;
    [SerializeField] private float happyRotation = default;
    [SerializeField] private float sadRotation = default;

    private Quaternion emotionNeutralRotation;

    private void Awake()
    {
        emotionNeutralRotation = emotionTransform.localRotation;
    }

    public void ApplyFrame(Frame frame)
    {
        float innerT = (frame.innerUpDown + 1f) * 0.5f;
        float outerT = (frame.outerUpDown + 1f) * 0.5f;

        float innerY = Mathf.Lerp(innerLowY, innerHighY, innerT);
        float outerY = Mathf.Lerp(outerLowY, outerHighY, outerT);

        Vector3 innerPos = innerTransform.localPosition;
        Vector3 outerPos = outerTransform.localPosition;

        innerPos.y = innerY;
        outerPos.y = outerY;

        innerTransform.localPosition = innerPos;
        outerTransform.localPosition = outerPos;
    }

    private void LateUpdate()
    {
        emotionTransform.localPosition = Vector3.Lerp(sadPosition, happyPosition, emotion.happiness);
        emotionTransform.localRotation = Quaternion.AngleAxis(Mathf.Lerp(sadRotation, happyRotation, emotion.happiness), rotationAxis) * emotionNeutralRotation;
    }
}
