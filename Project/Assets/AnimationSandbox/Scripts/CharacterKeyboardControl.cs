using UnityEngine;
using Stately;

public class CharacterKeyboardControl : MonoBehaviour
{
    public Transform rootTransform;
    public Transform pelvisTransform;
    public LayerMask groundLayerMask;
    [Range(0,0.15f)]
    public float movementFollowSmooth;
    [Range(0,0.3f)]
    public float movementFacingSmooth;
    [Range(0,10f)]
    public float movementSpeed = 5f;
    [Range(0,90f)]
    public float groundRaycastAngle = 45f; // Angle in degrees from vertical

    // private
    Vector3 positionTarget;
    Vector3 positionLast;
    Vector3 positionVelocity;
    Vector3 movementDelta;
    Vector3 movementDeltaNormalized;
    Vector3 movementDeltaVelocity;
    Camera mainCam;
    Rigidbody rb;
    CapsuleCollider capsuleCollider;

    // Debug visualization
    private Vector3 debugRayStart;
    private Vector3 debugRayEnd;
    private bool debugHitSomething;

    void Awake()
    {
        positionTarget = rootTransform.position;
        mainCam = Camera.main;

        // Get or add required components
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
            capsuleCollider = gameObject.AddComponent<CapsuleCollider>();

        // Configure Rigidbody
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void FixedUpdate()
    {
        HandleKeyboardInput();
        UpdateRootPosition();
        UpdateRootRotation();
    }

    void HandleKeyboardInput()
    {
        // // Get input axes
        // float horizontal = Input.GetAxis("Horizontal");
        // float vertical = Input.GetAxis("Vertical");
        //
        //
        // Use WASD input for movement
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            vertical += 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            vertical -= 1f;
        }
        if (Input.GetKey(KeyCode.A))
        {
            horizontal -= 1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            horizontal += 1f;
        }

        // Normalize movement magnitude
        Vector3 normalizedInput = new Vector3(horizontal, 0, vertical).normalized;
        horizontal = normalizedInput.x;
        vertical = normalizedInput.z;

        // Calculate movement direction relative to camera
        Vector3 cameraForward = mainCam.transform.forward;
        Vector3 cameraRight = mainCam.transform.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();


        // Calculate movement direction
        Vector3 moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;

        // Calculate target position
        Vector3 targetPosition = transform.position + moveDirection * movementSpeed *
            Time.fixedDeltaTime;

        // Calculate angled raycast direction
        Vector3 raycastDirection = Quaternion.Euler(45f, 0, 0) * moveDirection;

        // Check for collisions with angled raycast
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * capsuleCollider.height * 0.5f;

        // Store debug information
        bool hitSomething = Physics.Raycast(rayStart, raycastDirection, out hit, 10f, groundLayerMask);

        if (hitSomething)
        {
            positionTarget = hit.point;
            positionTarget.y = hit.point.y;
        }
        else
        {
            positionTarget = targetPosition;
            positionTarget.y = transform.position.y;
        }
    }

    void UpdateRootPosition()
    {

        // Update position with smoothing
        Vector3 positionSmooth = Vector3.SmoothDamp(rootTransform.position, positionTarget, ref positionVelocity, movementFollowSmooth);

        // Apply movement through physics
        Vector3 moveDelta = positionSmooth - rootTransform.position;
        rb.MovePosition(positionSmooth);

        // Update movement delta from change in position
        movementDelta = rootTransform.position - positionLast;
        movementDelta = Vector3.ProjectOnPlane(movementDelta, Vector3.up);

        positionLast = rootTransform.position;
    }

    void UpdateRootRotation()
    {
        // Update rotation to face movement direction
        float movementDeltaMagnitude = movementDelta.magnitude;
        if (movementDeltaMagnitude > 0.01f)
        {
            movementDeltaNormalized = movementDelta.normalized;
            Vector3 movementDeltaSmooth = Vector3.SmoothDamp(rootTransform.forward, movementDeltaNormalized, ref movementDeltaVelocity, movementFacingSmooth);
            rb.MoveRotation(Quaternion.LookRotation(movementDeltaSmooth));
        }
    }

    void OnDrawGizmos()
    {
        // Draw movement raycast
        Gizmos.color = debugHitSomething ? Color.red : Color.green;
        Gizmos.DrawLine(debugRayStart, debugRayEnd);
        Gizmos.DrawWireSphere(debugRayEnd, 0.2f);

        // Draw character's current position and target
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(rootTransform.position, 0.3f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(positionTarget, 0.3f);
    }
}
