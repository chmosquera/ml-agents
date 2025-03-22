using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Touch;
using UnityEngine.EventSystems;
using Lean.Common;
using UnityEngine.Events;

public class CameraTouchControl : MonoBehaviour
{
    public bool touchInputEnabled = true;

    [SerializeField] private LeanSelectable selectionProxy = default;
    [SerializeField] private LeanSelectByFinger selectionController = default;

    // movement options
    [Header("Movement Options")]
    [Range(0, 0.05f)]
    public float camPanGestureSpeed = .025f;
    public bool camPanInvertHorizontal = false;
    public bool camPanInvertVertical = false;
    [Range(0, 5)]
    public float camZoomGestureSpeed = 2f;
    public float camZoomGestureThreshold = 0.1f;
    [Range(0, 0.5f)]
    public float camRotateGestureSpeed = 0.1f;
    [Range(0,5f)]
    public float camPositionSmoothSpeed = 3f;
    [Range(0,5f)]
    public float camRotationSmoothSpeed = 3f;

    // private
    [ReadOnly]
    public Vector3 lookPoint;
    float pinchScaleLast = 1;
    // float pinchDelta = 0;
    Camera cam;
    float camLookDistance = 5;
    [SerializeField]
    [ReadOnly]
    Vector3 camLookRotationVector = new Vector3(0,0.5f,-1);
    float camRotationSmoothSpeedUpdating = 3f;

    private Vector3 previousPosition;
    private Vector3 previousForward;

    // events
    delegate void HandleGestureDelegate(List<LeanFinger> fingers);
    HandleGestureDelegate handleGestureDelegate;

    public UnityEvent onLookpointChanged;

    void Awake()
    {
        cam = GetComponent<Camera>();
        lookPoint = cam.transform.position - (camLookRotationVector * camLookDistance);

        previousPosition = cam.transform.position;
        previousForward = cam.transform.forward;
    }

    void Update()
    {
        if (touchInputEnabled)
        {
            // cam position update
            Vector3 camGoalPosition = lookPoint + (camLookRotationVector.normalized * camLookDistance);
            Vector3 camSmoothPosition = Vector3.Lerp(previousPosition, camGoalPosition, camPositionSmoothSpeed * Time.deltaTime);
            cam.transform.position = camSmoothPosition;

            // cam look rotation update
            Vector3 camToLookPointVector = lookPoint - cam.transform.position;
            Vector3 camToLookPointVectorSmooth = Vector3.Lerp(previousForward, camToLookPointVector, camRotationSmoothSpeedUpdating * Time.deltaTime);
            cam.transform.rotation = Quaternion.LookRotation(camToLookPointVectorSmooth.normalized, Vector3.up);
        }

        previousPosition = cam.transform.position;
        previousForward = cam.transform.forward;
    }

    public void ClearPreviousPositionData()
    {
        previousPosition = cam.transform.position;
        previousForward = cam.transform.forward;
    }

    public PlaytimeRecordableCamera.StateFrame GetInternalState()
    {
        return new PlaytimeRecordableCamera.StateFrame()
        {
            lookPoint = this.lookPoint,
            camLookRotationVector = this.camLookRotationVector,
            camLookDistance = this.camLookDistance
        };
    }

    public void SetInternalState(PlaytimeRecordableCamera.StateFrame state, bool setPositionImmediate = false)
    {
        lookPoint = state.lookPoint;
        camLookRotationVector = state.camLookRotationVector;
        camLookDistance = state.camLookDistance;

        if (setPositionImmediate)
        {
            previousPosition = lookPoint + (camLookRotationVector.normalized * camLookDistance);
            previousForward = (lookPoint - previousPosition).normalized;

            cam.transform.position = previousPosition;
            cam.transform.rotation = Quaternion.LookRotation(previousForward, Vector3.up);
        }
    }

    void HandleGesture(List<LeanFinger> fingers)
    {
        if (handleGestureDelegate != null)
            handleGestureDelegate(fingers);
    }

	void HandleFingerTap(LeanFinger finger)
	{
		// Debug.Log("You just tapped the screen with finger " + finger.Index + " at " + finger.ScreenPosition);

        Ray ray = cam.ScreenPointToRay(finger.ScreenPosition);
        RaycastHit hit;
        if (!finger.IsOverGui)    // is the touch on the GUI
        {
            if (Physics.Raycast(ray.origin, ray.direction, out hit, 1000))
            {
                lookPoint = hit.point;
                onLookpointChanged.Invoke();
            }
        }
	}

    public void EnableTouchInput()
    {
        if(!touchInputEnabled)
        {
            previousPosition = cam.transform.position;
            previousForward = cam.transform.forward;
        }

        touchInputEnabled = true;
        handleGestureDelegate = delegate(List<LeanFinger> fingers) {
            Vector2 screenDelta = LeanGesture.GetScreenDelta();
            camRotationSmoothSpeedUpdating = camRotationSmoothSpeed;

            int oneFingerTest  = 0;
            int twoFingersTest = 1;
            #if UNITY_EDITOR
            oneFingerTest = 1;
            twoFingersTest = 2;
            #endif

            foreach(LeanFinger finger in fingers)
                if (finger.StartedOverGui)
                    return;

            if(fingers.Count > oneFingerTest)
            {
                if(!selectionProxy.IsSelected) selectionController.Select(selectionProxy, fingers[0]);
            }
            else
            {
                if(selectionProxy.IsSelected) selectionController.Deselect(selectionProxy);
            }

            // if one finger
            if (fingers.Count > oneFingerTest && fingers.Count <= twoFingersTest && screenDelta.magnitude > 0)
            {
                // rotate when dragging
                camLookRotationVector = Quaternion.AngleAxis(screenDelta.x * camRotateGestureSpeed, Vector3.up) * camLookRotationVector;
                camLookRotationVector = Quaternion.AngleAxis(screenDelta.y * camRotateGestureSpeed, -cam.transform.right) * camLookRotationVector;
            }
            // if two fingers
            else if (fingers.Count > twoFingersTest && screenDelta.magnitude > 0)
            {
                float pinchScale = LeanGesture.GetPinchScale();
                if ((pinchScale < 1 - camZoomGestureThreshold || pinchScale > 1 + camZoomGestureThreshold) && pinchScale != 0)
                {
                    camLookDistance += ((pinchScale-1) * camZoomGestureSpeed)*-1;
                }
                // or pan when dragging
                else
                {
                    camRotationSmoothSpeedUpdating = 0f;
                    float panX = screenDelta.x * camPanGestureSpeed;
                    float panY = screenDelta.y * camPanGestureSpeed;
                    if (camPanInvertHorizontal)
                        panX = -panX;
                    if (camPanInvertVertical)
                        panY = -panY;
                    Vector2 panAmountVector2 = new Vector2(panX, panY);
                    Vector3 panAmountVector3 = new Vector3(panAmountVector2.x, panAmountVector2.y, 0);
                    Vector3 panVector = cam.transform.TransformDirection(panAmountVector3);
                    lookPoint += panVector;
                }
                pinchScaleLast = pinchScale;
            }
        };
    }

    public void DisableTouchInput()
    {
        touchInputEnabled = false;
        handleGestureDelegate = null;
    }

    void OnEnable()
	{
		LeanTouch.OnFingerTap += HandleFingerTap;
        LeanTouch.OnGesture += HandleGesture;
	}

	void OnDisable()
	{
		LeanTouch.OnFingerTap -= HandleFingerTap;
        LeanTouch.OnGesture += HandleGesture;
	}
}
