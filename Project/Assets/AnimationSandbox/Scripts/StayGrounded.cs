using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StayGrounded : MonoBehaviour
{
    public float raycastFromHeight = 2f;
    public LayerMask layerMaskGround;

    void FixedUpdate()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + new Vector3(0, raycastFromHeight, 0), Vector3.down, out hit, 100, layerMaskGround))
        {
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }
    }
}
