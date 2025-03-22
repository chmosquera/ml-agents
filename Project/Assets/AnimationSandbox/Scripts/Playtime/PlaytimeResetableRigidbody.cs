using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlaytimeResetableRigidbody : MonoBehaviour
{
    Rigidbody rb;

    [SerializeField]
    [ReadOnly]
    Vector3 localPositionCached;
    [SerializeField]
    [ReadOnly]
    Quaternion localRotationCached;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void CacheRigidbody()
    {
        localPositionCached = transform.localPosition;
        localRotationCached = transform.localRotation;
    }

    public void ResetRigidbody()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 worldPosition = transform.parent.TransformPoint(localPositionCached);
        rb.MovePosition(worldPosition);
        Quaternion worldRotation = transform.parent.rotation * localRotationCached;
        rb.MoveRotation(worldRotation);
    }
}
