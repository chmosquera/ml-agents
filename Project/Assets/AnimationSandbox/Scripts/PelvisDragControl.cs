using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PelvisDragControl : MonoBehaviour
{
    public Transform springTarget;
    public float gravity = 9.8f;
    public float mass = 1f;
    public float spring = 15f;
    float minimumYPosLocal;
    float simulatedYPosLocal = 0;
    float velocity = 0;
    public float debugForce = 1;
    float debugForceActual = 0;
    public bool debugDraw;

    void Awake()
    {
        minimumYPosLocal = transform.localPosition.y;
        simulatedYPosLocal = 0;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.D))
        {
            debugForceActual = debugForce;
        }
        else
        {
            debugForceActual = 0;
        }

        float forces = -gravity + debugForceActual;
        float acceleration = forces / mass;
        velocity += acceleration * Time.deltaTime;
        simulatedYPosLocal += velocity * Time.deltaTime; 
        if (simulatedYPosLocal <= minimumYPosLocal)
        {
            simulatedYPosLocal = minimumYPosLocal;
            velocity = 0;
        }
        // Debug.Log("forces: " + forces + " velocity: " + velocity + " simulatedY: " + simulatedY);
    }

    void OnDrawGizmos()
    {
        if (!debugDraw)
            return;
            
        Vector3 simulatedPosGlobal = transform.parent.TransformPoint(new Vector3(0, simulatedYPosLocal, 0));
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(simulatedPosGlobal, .25f);
    }
}
