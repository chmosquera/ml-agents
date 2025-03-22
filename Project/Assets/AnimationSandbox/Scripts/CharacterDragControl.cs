using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Edwon.Tools;
using Lean.Touch;
using Stately;

public class CharacterDragControl : MonoBehaviour, ICharacterFeatureToggleContainer
{
    // public 
    public Transform rootTransform;
    public Transform pelvisTransform;
    public LayerMask groundLayerMask;
    [Range(0,0.15f)]
    public float movementFollowSmooth;
    [Range(0,0.3f)]
    public float movementFacingSmooth;

    public CharacterFeatureToggle elevationToolToggle;
    
    // debug
    [Header("Debug")]
    public bool debugLog;
    public bool debugDraw;
    public RectTransform selectingFingerDebugUI;
    public RectTransform secondFingerDebugUI;

    // private
    [SerializeField]
    [ReadOnly]
    bool isDragged;
    public bool IsDragged {get{ return isDragged;}}
    LeanFinger selectingFinger = null;
    LeanFinger secondFinger = null;
    Camera mainCam;
    Vector3 positionTarget;
    Vector3 positionLast;
    Vector3 positionVelocity;
    Vector3 movementDelta;
    Vector3 movementDeltaNormalized;
    Vector3 movementDeltaVelocity;
    private Vector2 rootPositionScreenSpaceTouchOffset;

    // state machine debug
    public bool debugLogState;
    [SerializeField]
    [ReadOnly]
    private string currentState;
    private string currentStateLast;

    // state machine
    State rootState = new State("root");
    State idle = new State("idle");
    State oneFinger = new State("oneFinger");
    State twoFingers = new State("twoFingers");
    const string selectingFingerDownSignal = "selectingFingerDown";
    const string selectingFingerUpSignal = "selectingFingerUp";
    const string secondFingerDownSignal = "secondFingerDown";
    const string secondFingerUpSignal = "secondFingerUp";

    void Awake()
    {
        positionTarget = rootTransform.position;
        mainCam = Camera.main;

        DefineStateMachine();
        rootState.Start();
    }

    void DefineStateMachine()
    {
        rootState.StartAt(idle);
        rootState.ChangeToSubState(idle).IfSignalCaught(selectingFingerUpSignal);
        idle.ChangeTo(oneFinger).IfSignalCaught(selectingFingerDownSignal);
        idle.ChangeTo(twoFingers).IfSignalCaught(secondFingerDownSignal).AndIf(() => elevationToolToggle.IsOn);
        oneFinger.OnFixedUpdate = delegate
        {
            UpdateRootRaycast();
            UpdateRootPosition();
            UpdateRootRotation();
        };
        oneFinger.OnEnter = CalculateRootOffset;
        oneFinger.ChangeTo(twoFingers).IfSignalCaught(secondFingerDownSignal);
        twoFingers.ChangeTo(oneFinger).IfSignalCaught(secondFingerUpSignal).AndIf(() => elevationToolToggle.IsOn);
        twoFingers.OnFixedUpdate = delegate
        {            
            UpdateRootRaycast();
            UpdateRootPosition();
            UpdateRootRotation();
            UpdatePelvisPosition();
        };
    }

    void Update()
    {
        rootState.Update(Time.deltaTime);
        DebugStateMachineUpdate();
    }

    void UpdatePelvisPosition()
    {
        if (secondFinger != null && elevationToolToggle.IsOn)
        {
            float distanceToPelvis = Vector3.Distance(mainCam.transform.position, pelvisTransform.position);
            Vector3 worldDelta = secondFinger.GetWorldDelta(distanceToPelvis, mainCam);
            pelvisTransform.localPosition += new Vector3(0, worldDelta.y, 0);
        }
    }

    private void CalculateRootOffset()
    {
        Vector2 screenPoint = mainCam.WorldToScreenPoint(rootTransform.position);
        rootPositionScreenSpaceTouchOffset = screenPoint - selectingFinger.ScreenPosition;
    }

    void UpdateRootRaycast()
    {
        if (selectingFinger == null) return;
        // update positionTarget with raycast from selecting finger
        Ray offsetRay = mainCam.ScreenPointToRay(selectingFinger.ScreenPosition + rootPositionScreenSpaceTouchOffset);
        RaycastHit hitGround;
        if (Physics.Raycast(offsetRay, out hitGround, 10000, groundLayerMask))
            positionTarget = hitGround.point;
    }

    void UpdateRootPosition()
    {
        // update position
        Vector3 positionSmooth = Vector3.SmoothDamp(rootTransform.position, positionTarget, ref positionVelocity, movementFollowSmooth);
        rootTransform.position = positionSmooth;

        // update movementDelta from change in position
        movementDelta = rootTransform.position - positionLast;
        movementDelta = Vector3.ProjectOnPlane(movementDelta, Vector3.up);

        positionLast = rootTransform.position;
    }

    void UpdateRootRotation()
    {
        // update rotation
        float movementDeltaMagnitude = movementDelta.magnitude;
        if (movementDeltaMagnitude > 0.01f)
            movementDeltaNormalized = movementDelta.normalized;
        Vector3 movementDeltaSmooth = Vector3.SmoothDamp(rootTransform.forward, movementDeltaNormalized, ref movementDeltaVelocity, movementFacingSmooth);
        rootTransform.forward = movementDeltaSmooth;
    }

    void FixedUpdate()
    {
        // debug draw
        if (debugDraw)
        {
            Debug.DrawRay(rootTransform.position + new Vector3(0, 0.1f, 0), movementDelta * 6, Color.blue);
            Debug.DrawRay(positionTarget, Vector3.up, Color.red);
        }

        rootState.FixedUpdate();
    }

    void DebugStateMachineUpdate()
    {
        // state machine debug        
        currentState = rootState.CurrentStatePath;
        if (debugLogState)
        {
            if (currentState != currentStateLast)
                Debug.Log(currentState);
        }
        currentStateLast = currentState;
    }

    public void OnSelectedFinger(LeanFinger finger)
    {
        #if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isRemoteConnected)
            if (finger.Index == 0)
                return;
        #endif

        // Debug.Log("OnSelectedFinger index: " + finger.Index);
        if (selectingFinger == null)
        {
            // Debug.Log("new selectedFinger index is: " + finger.Index);
            selectingFinger = finger;
            rootState.SendSignal(selectingFingerDownSignal);
        }
    }

    public void OnSelectedFingerUp(LeanFinger finger)
    {
        rootState.SendSignal(selectingFingerUpSignal);
        selectingFinger = null;
    }

    public void OnDeselected()
    {
        OnSelectedFingerUp(null);
    }

    void OnFingerDown(LeanFinger finger) // gets called even when not selected
    {
        #if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isRemoteConnected)
            if (finger.Index == 0)
                return;
        #endif

        // if currently selected
        if (selectingFinger != null)
        {
            if (finger.Index != selectingFinger.Index)
            {
                // print("non-selecting fingers index is: " + finger.Index);
                if (finger.Index == 1 || finger.Index == -2)
                {
                    secondFinger = finger;
                    rootState.SendSignal(secondFingerDownSignal);
                }
            }
        }
    }

    void OnFingerUp(LeanFinger finger)
    {
        if (finger == secondFinger)
        {
            secondFinger = null;
            rootState.SendSignal(secondFingerUpSignal);
        }

    }

    void OnFingerUpdate(LeanFinger finger)
    {

    }

    void OnEnable()
    {
		Lean.Touch.LeanTouch.OnFingerDown += OnFingerDown;
        Lean.Touch.LeanTouch.OnFingerUp += OnFingerUp;
    }

    void OnDisable()
    {
        Lean.Touch.LeanTouch.OnFingerDown -= OnFingerDown;
        Lean.Touch.LeanTouch.OnFingerUp -= OnFingerUp;
    }

    void ICharacterFeatureToggleContainer.GetCharacterFeatureToggles(List<CharacterFeatureToggle> togglesBuffer)
    {
        togglesBuffer.Add(elevationToolToggle);
    }
}
