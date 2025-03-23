using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class SittingAgent : Agent
{
    [Range(0, 0.3f)] public float movementFacingSmooth;

    [Header("Environment")]
    /// <summary>
    /// The ground. The bounds are used to spawn the elements.
    /// </summary>
    public GameObject ground;

    public GameObject area;

    /// <summary>
    /// The area bounds.
    /// </summary>
    [HideInInspector] public Bounds areaBounds;

    PushBlockSettings m_PushBlockSettings;

    /// <summary>
    /// The block to be interacted with.
    /// </summary>
    public GameObject block;

    /// <summary>
    /// Detects when the agent touches the block.
    /// </summary>
    [HideInInspector] public SittableDetect sittableDetect;

    public bool useVectorObs;

    [Header("Debug")] public TMPro.TextMeshProUGUI rewardText; // Assign in inspector
    public bool showDebugGUI = true;

    Rigidbody m_BlockRb; //cached on initialization
    Rigidbody m_AgentRb; //cached on initialization
    CapsuleCollider m_AgentCollider;
    Material m_GroundMaterial; //cached on Awake()

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>
    Renderer m_GroundRenderer;

    EnvironmentParameters m_ResetParams;

    [Header("Internal Variables")] private Vector3 positionTarget;
    private Vector3 positionLast;
    private Vector3 positionVelocity;
    private Vector3 movementDelta;
    private Vector3 movementDeltaNormalized;
    private Vector3 movementDeltaVelocity;
    private Camera mainCam;
    private Vector3 lastDistanceToBlock;

    protected override void Awake()
    {
        base.Awake();

        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();
    }

    public override void Initialize()
    {
        sittableDetect = block.GetComponent<SittableDetect>();
        sittableDetect.agent = this;

        // Cache the agent rigidbody
        m_AgentRb = GetComponent<Rigidbody>();
        m_AgentCollider = GetComponent<CapsuleCollider>();
        if (m_AgentCollider == null)
        {
            m_AgentCollider = gameObject.AddComponent<CapsuleCollider>();
        }

        // Cache the block rigidbody
        m_BlockRb = block.GetComponent<Rigidbody>();
        // Get the ground's bounds
        areaBounds = ground.GetComponent<Collider>().bounds;
        // Get the ground renderer so we can change the material when a goal is scored
        m_GroundRenderer = ground.GetComponent<Renderer>();
        // Starting material
        m_GroundMaterial = m_GroundRenderer.material;

        positionTarget = transform.position;
        mainCam = Camera.main;

        // Configure Rigidbody
        m_AgentRb.constraints = RigidbodyConstraints.FreezeRotation;
        m_AgentRb.interpolation = RigidbodyInterpolation.Interpolate;
        m_AgentRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        SetResetParameters();
    }

    /// <summary>
    /// Use the ground's bounds to pick a random spawn position.
    /// </summary>
    public Vector3 GetRandomSpawnPos()
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier);
            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }

        return randomSpawnPos;
    }

    /// <summary>
    /// Called when the agent touches the block.
    /// </summary>
    public void BlockTouched()
    {
        // We use a reward of 5 for touching the block
        AddReward(5f);

        Debug.Log($"<color=yellow>{transform.parent.name}: Block touched! Reward: </color>" + GetCumulativeReward());

        // By marking an agent as done AgentReset() will be called automatically.
        EndEpisode();

        // Swap ground material for a bit to indicate success.
        StartCoroutine(GoalScoredSwapGroundMaterial(m_PushBlockSettings.goalScoredMaterial, 0.5f));

    }

    /// <summary>
    /// Swap ground material, wait time seconds, then swap back to the regular material.
    /// </summary>
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time); // Wait for 2 sec
        m_GroundRenderer.material = m_GroundMaterial;
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
    public void MoveAgent(ActionSegment<int> act)
    {
        var action = act[0];

        float horizontal = 0f;
        float vertical = 0f;

        switch (action)
        {
            case 1:
                vertical += 1f;
                break;
            case 2:
                vertical -= 1f;
                break;
            case 3:
                horizontal -= 1f;
                break;
            case 4:
                horizontal += 1f;
                break;
        }

        // Normalize movement magnitude
        Vector3 dirToGo = new Vector3(horizontal, 0, vertical).normalized;

        // Calculate movement direction relative to camera
        Vector3 cameraForward = mainCam.transform.forward;
        Vector3 cameraRight = mainCam.transform.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calculate movement direction
        Vector3 moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;

        // Apply the movement force
        if (moveDirection.magnitude > 0.1f)
        {
            m_AgentRb.AddForce(moveDirection * m_PushBlockSettings.agentRunSpeed,
                ForceMode.VelocityChange);
        }

        // Update the rotation and position tracking
        UpdateRootPosition();
        UpdateRootRotation();
    }

    public void UpdateRootPosition()
    {
        // Update movement delta from change in position
        movementDelta = transform.position - positionLast;
        movementDelta = Vector3.ProjectOnPlane(movementDelta, Vector3.up);

        positionLast = transform.position;
    }

    void UpdateRootRotation()
    {
        // Update rotation to face movement direction
        float movementDeltaMagnitude = movementDelta.magnitude;
        if (movementDeltaMagnitude > 0.01f)
        {
            movementDeltaNormalized = movementDelta.normalized;
            Vector3 movementDeltaSmooth = Vector3.SmoothDamp(transform.forward, movementDeltaNormalized,
                ref movementDeltaVelocity, movementFacingSmooth);
            m_AgentRb.MoveRotation(Quaternion.LookRotation(movementDeltaSmooth));
        }
    }

    private void Update()
    {
        // Update reward display if UI element is assigned
        if (rewardText != null)
        {
            rewardText.text = $"Reward: {GetCumulativeReward():F2}";
        }
    }

    private void OnGUI()
    {
        // if (showDebugGUI)
        // {
        //     GUI.Label(new Rect(10, 10, 200, 20), $"Reward: {GetCumulativeReward():F2}");
        //     GUI.Label(new Rect(10, 30, 200, 20), $"Distance to Block: {Vector3.Distance(transform.position, block.transform.position):F2}");
        //     GUI.Label(new Rect(10, 50, 200, 20), $"Steps: {StepCount}");
        // }
    }

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Move the agent using the action.
        MoveAgent(actionBuffers.DiscreteActions);

        // Reward for getting closer to the block
        Vector3 currentDistanceToBlock = block.transform.position - transform.position;
        if (lastDistanceToBlock != Vector3.zero)
        {
            float movingCloserReward = lastDistanceToBlock.magnitude - currentDistanceToBlock.magnitude;
            AddReward(movingCloserReward * 0.1f); // Small reward for progress
        }

        lastDistanceToBlock = currentDistanceToBlock;

        // Penalty given each step to encourage agent to finish task quickly.
        AddReward(-1f / MaxStep);

        if (StepCount == MaxStep - 1)
            Debug.Log($"<color=red>********* Reaching end of episode ********** {GetCumulativeReward()}</color>");
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0; // Default to "do nothing"

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            discreteActionsOut[0] = 1; // Forward
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            discreteActionsOut[0] = 2; // Backward
        }
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            discreteActionsOut[0] = 3; // Left
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            discreteActionsOut[0] = 4; // Right
        }
    }

    /// <summary>
    /// Resets the block position and velocities.
    /// </summary>
    void ResetBlock()
    {
        // Get a random position for the block.
        block.transform.position = GetRandomSpawnPos();

        // Reset block velocity back to zero.
        m_BlockRb.linearVelocity = Vector3.zero;

        // Reset block angularVelocity back to zero.
        m_BlockRb.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// In the editor, if "Reset On Done" is checked then AgentReset() will be
    /// called automatically anytime we mark done = true in an agent script.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        var rotation = Random.Range(0, 4);
        var rotationAngle = rotation * 90f;
        area.transform.Rotate(new Vector3(0f, rotationAngle, 0f));

        ResetBlock();
        transform.position = GetRandomSpawnPos();
        m_AgentRb.linearVelocity = Vector3.zero;
        m_AgentRb.angularVelocity = Vector3.zero;

        lastDistanceToBlock = Vector3.zero;
        SetResetParameters();
    }


    public void SetGroundMaterialFriction()
    {
        var groundCollider = ground.GetComponent<Collider>();

        groundCollider.material.dynamicFriction = m_ResetParams.GetWithDefault("dynamic_friction", 0);
        groundCollider.material.staticFriction = m_ResetParams.GetWithDefault("static_friction", 0);
    }

    public void SetBlockProperties()
    {
        var scale = m_ResetParams.GetWithDefault("block_scale", 2);
        //Set the scale of the block
        m_BlockRb.transform.localScale = new Vector3(scale, 0.75f, scale);

        // Set the drag of the block
        m_BlockRb.linearDamping = m_ResetParams.GetWithDefault("block_drag", 0.5f);
    }

    void SetResetParameters()
    {
        SetGroundMaterialFriction();
        SetBlockProperties();
    }
}
