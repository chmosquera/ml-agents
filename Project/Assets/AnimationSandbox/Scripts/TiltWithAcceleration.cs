using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TiltWithAcceleration : MonoBehaviour
{
    public Transform targetTransform;
    public Transform accelRefTransform;
    public float accelSmoothTime = 1;
    public float tiltAmount = 1f;
    public bool debugDraw;
    Vector3 velocity = Vector3.zero;
    Vector3 accel = Vector3.zero;
    Vector3 accelSmoothed = Vector3.zero;
    Vector3 accelSmoothVelocity = Vector3.zero;
    Vector3 velocityLast = Vector3.zero;
    Vector3 positionLast = Vector3.zero;

    void Update()
    {
        velocity = transform.position - positionLast;
        accel = velocity - velocityLast / Time.deltaTime;
        accelSmoothed = Vector3.SmoothDamp(accelSmoothed, accel, ref accelSmoothVelocity, accelSmoothTime);
        Vector3 tiltAxis = Vector3.Cross(accelSmoothed, Vector3.up);
        tiltAxis.y = 0;
        if (debugDraw)
            Debug.DrawRay(accelRefTransform.position, tiltAxis, Color.red);
        Quaternion targetRotation = Quaternion.AngleAxis(accelSmoothed.magnitude * tiltAmount, tiltAxis) * accelRefTransform.rotation;
        targetTransform.rotation = targetRotation;

        positionLast = transform.position;
        velocityLast = velocity;
    }
}