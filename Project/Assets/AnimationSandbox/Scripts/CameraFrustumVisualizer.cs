using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFrustumVisualizer : MonoBehaviour
{
    [SerializeField] private new Camera camera = default;
    [SerializeField] private float nearClipPlane = default;
    [SerializeField] private float farClipPlane = default;
    [SerializeField] private LineRenderer[] edgeLines = default;
    [SerializeField] private LineRenderer nearClipLine = default;
    [SerializeField] private LineRenderer farClipLine = default;

    private Vector3[] nearCorners = new Vector3[4];
    private Vector3[] farCorners = new Vector3[4];

    [ContextMenu("Update Frustum")]
    private void UpdateFrustum()
    {
        if(camera == null)
        {
            for(int i = 0; i < edgeLines.Length; ++i)
            {
                edgeLines[i].positionCount = 0;
            }
            nearClipLine.positionCount = 0;
            farClipLine.positionCount = 0;
            return;
        }

        Rect wholeRect = new Rect(Vector2.zero, Vector2.one);

        camera.CalculateFrustumCorners(wholeRect, nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, nearCorners);
        camera.CalculateFrustumCorners(wholeRect, farClipPlane, Camera.MonoOrStereoscopicEye.Mono, farCorners);

        nearClipLine.positionCount = 5;
        farClipLine.positionCount = 5;

        for(int i = 0; i < edgeLines.Length; ++i)
        {
            Vector3 nearPoint = nearCorners[i];
            Vector3 farPoint = farCorners[i];

            edgeLines[i].SetPosition(0, nearPoint);
            edgeLines[i].SetPosition(1, farPoint);

            nearClipLine.SetPosition(i, nearPoint);
            farClipLine.SetPosition(i, farPoint);
        }

        nearClipLine.SetPosition(4, nearCorners[0]);
        farClipLine.SetPosition(4, farCorners[0]);
    }
}
