using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Stately;

public class IKTargetAutoStep : MonoBehaviour
{
    public IKTargetAutoStep oppositeLimbTarget;
    public Transform stepTarget;
    public Transform velocityRefTransform;

    [SerializeField] public float stepSpeed = 1f;
    public float stepDistanceMax = 0.4f;
    public float stepHeightMax = 0.3f;
    public float stepDistanceForMaxHeight = 1f;

    [SerializeField]
    [ReadOnly]
    float stepDistance;
    Vector3 stepPosCurrent;
    Vector3 stepPosOld;
    Vector3 stepPosNew;
    float lerp;

    public State rootState = new State("root");
    const string groundedStateName = "grounded";
    const string steppingStateName = "stepping";
    State grounded = new State(groundedStateName);
    State stepping = new State(steppingStateName);
    [SerializeField]
    [ReadOnly]
    string currentStateString;
    string currentStateStringLast;
    public bool debugLogState = false;
    public bool debugDraw = false;

    private Vector3 smoothedVelocity;
    private Vector3 smoothedVelocityVelocity;
    private Vector3 prevRefPos;
    private Vector3 velocityAdjustedTargetPos;
    
    void Awake()
    {
        stepPosCurrent = transform.position;
        stepPosOld = stepPosCurrent;
        stepPosNew = stepPosCurrent;

        prevRefPos = velocityRefTransform.position;
        velocityAdjustedTargetPos = transform.position;

        DefineStateMachine();
        rootState.Start();
    }

    void DefineStateMachine()
    {
        rootState.StartAt(grounded);
        
        grounded.OnEnter = delegate
        {
            lerp = 0;
        };
        grounded.ChangeTo(stepping).If(
        ()=> 
            stepDistance > stepDistanceMax && 
            oppositeLimbTarget.rootState.CurrentState.Name == groundedStateName
        );

        stepping.OnEnter = delegate
        {
            stepPosOld = transform.position;
            stepPosNew = velocityAdjustedTargetPos;
        };
        stepping.OnUpdate = (deltaTime) =>
        {
            float t = lerp / stepSpeed;
            Vector3 footPosition = Vector3.Lerp(stepPosOld, velocityAdjustedTargetPos, t);
            float stepDistance = (stepPosOld - velocityAdjustedTargetPos).magnitude;
            footPosition.y += Mathf.Sin(t * Mathf.PI) * stepHeightMax * Mathf.Clamp01(stepDistance / stepDistanceForMaxHeight);
            stepPosCurrent = footPosition;
            lerp += deltaTime;
        };
        stepping.OnExit = delegate
        {
            //stepPosCurrent = stepPosNew;
        };
        stepping.ChangeTo(grounded).If(()=> lerp > stepSpeed);
    }

    void Update()
    {
        Vector3 velocity = (velocityRefTransform.position - prevRefPos) / Time.deltaTime;
        prevRefPos = velocityRefTransform.position;

        smoothedVelocity = Vector3.SmoothDamp(smoothedVelocity, velocity, ref smoothedVelocityVelocity, 0.05f);

        velocityAdjustedTargetPos = stepTarget.position + smoothedVelocity * stepSpeed * 0.5f;

        stepDistance = Vector3.Distance(stepPosCurrent, velocityAdjustedTargetPos);
        rootState.Update(Time.deltaTime);
        currentStateString = rootState.CurrentStatePath;
        transform.position = stepPosCurrent;
        if (debugDraw)
        {
            Debug.DrawLine(stepPosCurrent, stepTarget.position, Color.red);
            Debug.DrawLine(velocityAdjustedTargetPos, velocityAdjustedTargetPos + Vector3.up, Color.white);
        }

        if (debugLogState)
            if (currentStateString != currentStateStringLast)
                Debug.Log(currentStateString);

        currentStateStringLast = currentStateString;
    }

    void OnDrawGizmos()
    {
        if (!debugDraw)
            return;
            
        Gizmos.color = Color.red;
        Gizmos.DrawCube(stepPosCurrent, new Vector3(0.1f, 0.1f, 0.1f));
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(stepTarget.position, new Vector3(0.1f, 0.1f, 0.1f));
    }
}
