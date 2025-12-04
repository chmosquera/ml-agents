using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class SittingAgent : Agent
{
    [Range(0, 0.3f)] public float movementFacingSmooth;
    [Range(0, 1f)] public float squatSpeed = 1f;

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
    public Transform pelvicTransform;
    [FormerlySerializedAs("pelvicOriginalPosition")] public Vector3 m_pelvicOriginalPosition;

    public RandomizeAssetPlacement assetPlacer;
    public bool useVectorObs;

    [Header("Debug")]
    public TMPro.TextMeshProUGUI rewardText; // Assign in inspector
    public bool showDebugGUI = true;
    public bool verboseLogging = true; // Set to true for detailed logs

    Rigidbody m_BlockRb; //cached on initialization
    Rigidbody m_AgentRb; //cached on initialization
    CapsuleCollider m_AgentCollider;
    Material m_GroundMaterial; //cached on Awake()

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>
    Renderer m_GroundRenderer;

    EnvironmentParameters m_ResetParams;

    [Header("Internal Variables")]
    private Vector3 positionTarget;
    private Vector3 positionLast;
    private Vector3 positionVelocity;
    private Vector3 movementDelta;
    private Vector3 movementDeltaNormalized;
    private Vector3 movementDeltaVelocity;
    private Camera mainCam;
    private Vector3 lastDistanceToBlock;

    // Variable to track proximity-based reward scaling
    private float closerScalar = 0.1f; // Default scaling factor

    // Track if agent is in different proximity zones
    private bool isInMediumZone = false;
    private bool isInHotZone = false;

    // Flag to prevent multiple episode endings
    private bool isEpisodeEnding = false;

    // For debugging - track episode and step count
    private int episodeCount = 0;
    private float lastRewardValue = 0f;

    // Pelvis height limits
    public float pelvisMin = 0.25f;
    public float pelvisMax = 0.83f;

    protected override void Awake()
    {
        base.Awake();
        DebugLog("Awake", "SittingAgent is initializing");
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();
        m_pelvicOriginalPosition = pelvicTransform.localPosition;
    }

    public override void Initialize()
    {
        DebugLog("Initialize", "Starting agent initialization");

        sittableDetect = block.GetComponent<SittableDetect>();
        sittableDetect.agent = this;

        // Cache the agent rigidbody
        m_AgentRb = GetComponent<Rigidbody>();
        m_AgentCollider = GetComponent<CapsuleCollider>();
        if (m_AgentCollider == null)
        {
            m_AgentCollider = gameObject.AddComponent<CapsuleCollider>();
            DebugLog("Initialize", "Added CapsuleCollider to agent");
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
        DebugLog("Initialize", "Agent initialization complete");
    }

    /// <summary>
    /// Use the ground's bounds to pick a random spawn position.
    /// </summary>
    public Vector3 GetRandomSpawnPos()
    {
        DebugLog("GetRandomSpawnPos", "Finding spawn position");
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        var attempts = 0;

        // Define an appropriate height above ground
        float spawnHeight = 0.1f;

        while (foundNewSpawnLocation == false && attempts < 100)
        {
            attempts++;

            // Get random X and Z coordinates
            var randomPosX = Random.Range(-areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier);
            var randomPosZ = Random.Range(-areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier);

            // Create position using ground's X,Z with consistent Y height
            Vector3 groundPosition = ground.transform.position;
            randomSpawnPos = new Vector3(groundPosition.x + randomPosX, groundPosition.y + spawnHeight, groundPosition.z + randomPosZ);

            // Optional: Use raycast to find the exact ground height at this X,Z coordinate
            // This ensures proper placement even on uneven terrain
            RaycastHit hit;
            if (Physics.Raycast(randomSpawnPos + Vector3.up * 5f, Vector3.down, out hit, 10f, LayerMask.GetMask("Ground")))
            {
                // Use the hit point's Y value plus a small offset for the agent's height
                randomSpawnPos = new Vector3(randomSpawnPos.x, hit.point.y + spawnHeight, randomSpawnPos.z);
            }

            // Check if the position is valid (no collisions)
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
                DebugLog("GetRandomSpawnPos", $"Found position after {attempts} attempts: {randomSpawnPos}");
            }
        }

        if (!foundNewSpawnLocation)
        {
            DebugLog("GetRandomSpawnPos", "WARNING: Failed to find spawn location after max attempts!", LogType.Warning);
        }

        return randomSpawnPos;
    }

    /// <summary>
    /// Called when the agent touches the block.
    /// </summary>
    public void BlockTouched()
    {
        DebugLog("BlockTouched", "Agent touched block!");

        if (isEpisodeEnding)
        {
            DebugLog("BlockTouched", "Ignoring block touch because episode is already ending");
            return;
        }

        // We now give a reward of 10 for touching the block but DON'T end the episode
        float previousReward = GetCumulativeReward();
#if ADD_REWARD
        AddReward(10f);
#endif
        DebugLog("BlockTouched", $"Reward added: +10.0, Previous: {previousReward}, New: {GetCumulativeReward()}");

        Debug.Log($"<color=yellow>{transform.parent.name}: Block touched! Reward: </color>" + GetCumulativeReward());

        // We no longer end the episode here
        // SafeEndEpisode("Block touched - success!");

        // Still swap the material
        StartCoroutine(GoalScoredSwapGroundMaterial(m_PushBlockSettings.goalScoredMaterial, 0.5f));
    }

    /// <summary>
    /// Called when the butt trigger collides with the block - this is the success condition
    /// This will be called from the ButtDetect class
    /// </summary>
    public void ButtSatOnBlock()
    {
        DebugLog("ButtSatOnBlock", "Butt sat on block!");

        if (isEpisodeEnding)
        {
            DebugLog("ButtSatOnBlock", "Ignoring butt sitting because episode is already ending");
            return;
        }

        // We use a reward of 50 for sitting on the block
        float previousReward = GetCumulativeReward();
#if ADD_REWARD
        AddReward(50f);
#endif
        DebugLog("ButtSatOnBlock", $"Reward added: +50.0, Previous: {previousReward}, New: {GetCumulativeReward()}");

        Debug.Log($"<color=green>{transform.parent.name}: Successfully sat on block! Reward: </color>" + GetCumulativeReward());

        // Now we end the episode on successful sitting
        // SafeEndEpisode("Butt sat on block - success!");

        // Swap material for visual feedback
        StartCoroutine(GoalScoredSwapGroundMaterial(m_PushBlockSettings.goalScoredMaterial, 0.5f));
    }

    /// <summary>
    /// Called when agent collides with props - penalizes the agent for bumping into obstacles
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        DebugLog("OnCollisionEnter", $"Collision with {collision.gameObject.name}, tag: {collision.gameObject.tag}");

        if (isEpisodeEnding)
        {
            DebugLog("OnCollisionEnter", "Ignoring collision because episode is already ending");
            return;
        }

        // Check if the agent collided with a prop object
        if (collision.gameObject.CompareTag("prop"))
        {
            // Apply negative reward for hitting a prop
            float previousReward = GetCumulativeReward();
#if ADD_REWARD
            AddReward(-1f);
#endif
            DebugLog("OnCollisionEnter", $"Prop collision penalty: -1.0, Previous: {previousReward}, New: {GetCumulativeReward()}");

            Debug.Log($"<color=red>{transform.parent.name}: Hit prop! Penalty applied. Reward: </color>" + GetCumulativeReward());

            // Check if the agent's reward has fallen below the threshold after this penalty
            CheckIfRewardBelowThreshold();
        }
    }

    /// <summary>
    /// Track when agent enters the medium/hot proximity zones - increases reward scaling
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        DebugLog("OnTriggerEnter", $"Entered trigger: {other.name}, tag: {other.tag}");

        if (isEpisodeEnding)
        {
            DebugLog("OnTriggerEnter", "Ignoring trigger entry because episode is already ending");
            return;
        }

        // Check for medium proximity zone entry
        if (other.CompareTag("medium") && !isInMediumZone)
        {
            float oldScalar = closerScalar;
            isInMediumZone = true;
            closerScalar = 0.4f; // Increase reward scaling in medium zone
            DebugLog("OnTriggerEnter", $"Entered medium zone! Reward scaling changed from {oldScalar} to {closerScalar}");

            Debug.Log($"<color=cyan>{transform.parent.name}: Entered medium zone! Reward scaling: {closerScalar}</color>");
        }
        // Check for hot zone entry (inner zone) - highest reward scaling
        else if (other.CompareTag("hot") && !isInHotZone)
        {
            float oldScalar = closerScalar;
            isInHotZone = true;
            closerScalar = 1.0f; // Maximum reward scaling in hot zone
            DebugLog("OnTriggerEnter", $"Entered hot zone! Reward scaling changed from {oldScalar} to {closerScalar}");

            Debug.Log($"<color=green>{transform.parent.name}: Entered hot zone! Reward scaling: {closerScalar}</color>");
        }
    }

    /// <summary>
    /// Track when agent exits proximity zones - adjusts reward scaling accordingly
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        DebugLog("OnTriggerExit", $"Exited trigger: {other.name}, tag: {other.tag}");

        if (isEpisodeEnding)
        {
            DebugLog("OnTriggerExit", "Ignoring trigger exit because episode is already ending");
            return;
        }

        // Handle exiting the hot zone
        if (other.CompareTag("hot") && isInHotZone)
        {
            float oldScalar = closerScalar;
            isInHotZone = false;
            // If still in medium zone, use medium scaling, otherwise default
            closerScalar = isInMediumZone ? 0.4f : 0.1f;
            DebugLog("OnTriggerExit", $"Exited hot zone! Reward scaling changed from {oldScalar} to {closerScalar}");

            Debug.Log($"<color=yellow>{transform.parent.name}: Exited hot zone! Reward scaling: {closerScalar}</color>");
        }
        // Handle exiting the medium zone
        else if (other.CompareTag("medium") && isInMediumZone)
        {
            float oldScalar = closerScalar;
            isInMediumZone = false;
            // If somehow still in hot zone, keep high scaling (shouldn't happen with proper collider setup)
            closerScalar = isInHotZone ? 1.0f : 0.1f;
            DebugLog("OnTriggerExit", $"Exited medium zone! Reward scaling changed from {oldScalar} to {closerScalar}");

            Debug.Log($"<color=orange>{transform.parent.name}: Exited medium zone! Reward scaling: {closerScalar}</color>");
        }
    }

    /// <summary>
    /// Safely ends the episode to prevent multiple calls to EndEpisode()
    /// </summary>
    private void SafeEndEpisode(string reason)
    {
        if (isEpisodeEnding)
        {
            DebugLog("SafeEndEpisode", $"Episode already ending, ignoring new end request: {reason}");
            return;
        }

        isEpisodeEnding = true;
        Debug.Log($"<color=green>** SafeEndEpisode! - Ending episode. Reason: {reason}</color>");

        // Stop all coroutines to prevent any ongoing processes
        StopAllCoroutines();

        // Call the actual EndEpisode method
        EndEpisode();
    }

    /// <summary>
    /// Checks if the cumulative reward has fallen below the threshold (-1.5)
    /// If so, applies a large penalty and ends the episode
    /// </summary>
    private void CheckIfRewardBelowThreshold()
    {
        float currentReward = GetCumulativeReward();
        DebugLog("CheckIfRewardBelowThreshold", $"Checking reward threshold: Current reward = {currentReward}");

        if (isEpisodeEnding)
        {
            DebugLog("CheckIfRewardBelowThreshold", "Episode already ending, skipping check");
            return;
        }

        if (currentReward < -1.5f)
        {
            DebugLog("CheckIfRewardBelowThreshold", $"Reward below threshold ({currentReward} < -1.5)! Applying severe penalty.");

            float previousReward = GetCumulativeReward();
#if ADD_REWARD
            AddReward(-10f);
#endif
            DebugLog("CheckIfRewardBelowThreshold", $"Added penalty: -10.0, Previous: {previousReward}, New: {GetCumulativeReward()}");

            Debug.Log($"<color=red>{transform.parent.name}: Reward below threshold! Applying severe penalty.</color>");

            // SafeEndEpisode("Reward fell below threshold - applying penalty");
        }
    }

    /// <summary>
    /// Swap ground material, wait time seconds, then swap back to the regular material.
    /// </summary>
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        DebugLog("GoalScoredSwapGroundMaterial", "Starting material swap");
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time);

        // Check if we've been destroyed during the wait
        if (this == null || m_GroundRenderer == null)
        {
            DebugLog("GoalScoredSwapGroundMaterial", "Object destroyed during coroutine! Aborting material swap.");
            yield break;
        }

        m_GroundRenderer.material = m_GroundMaterial;
        DebugLog("GoalScoredSwapGroundMaterial", "Material swap complete");
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
    public void MoveAgent(ActionSegment<int> act)
    {
        if (isEpisodeEnding)
        {
            DebugLog("MoveAgent", "Skipping movement because episode is ending");
            return;
        }

        var action = act[0];
        int squatAction = act[1];
        DebugLog("MoveAgent", $"Processing moveAction: {action}; squatAction {squatAction}");

        float horizontal = 0f;
        float vertical = 0f;
        float squat = 0f;

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
        // if (moveDirection.magnitude > 0.1f)
        // {
        //     DebugLog("MoveAgent", $"Adding force: {moveDirection * m_PushBlockSettings.agentRunSpeed}");
        //     m_AgentRb.AddForce(moveDirection * m_PushBlockSettings.agentRunSpeed,
        //         ForceMode.VelocityChange);
        // }

        // SQUAT
        switch (squatAction)
        {
            case 1:
                squat += 1f;  // Squat down
                break;
            case 2:
                squat -= 1f;  // Stand up
                break;
        }

        var currentPelvisPosition = pelvicTransform.localPosition;

        // Apply squat action - invert the direction so positive value lowers the pelvis (squats down)
        float newPelvisY = currentPelvisPosition.y - squat * squatSpeed * Time.deltaTime;
        newPelvisY = Mathf.Clamp(newPelvisY, pelvisMin, pelvisMax);

        var pelvisYChange = newPelvisY - currentPelvisPosition.y;
        if (Mathf.Abs(pelvisYChange) > 0.001f)
        {
            DebugLog("MoveAgent", $"Adjusting pelvis height: {currentPelvisPosition.y:F3} -> {newPelvisY:F3}");
        }

        pelvicTransform.localPosition = new Vector3(currentPelvisPosition.x, newPelvisY, currentPelvisPosition.z);

        // Update the rotation and position tracking
        UpdateRootPosition();
        UpdateRootRotation();
        CalculatePelvisReward();
    }

    public void UpdateRootPosition()
    {
        if (isEpisodeEnding) return;

        // Update movement delta from change in position
        movementDelta = transform.position - positionLast;
        movementDelta = Vector3.ProjectOnPlane(movementDelta, Vector3.up);

        positionLast = transform.position;

        // Log if significant movement
        if (movementDelta.magnitude > 0.5f)
        {
            DebugLog("UpdateRootPosition", $"Large movement detected: {movementDelta.magnitude}");
        }
    }

    void UpdateRootRotation()
    {
        if (isEpisodeEnding) return;

        // Update rotation to face movement direction
        float movementDeltaMagnitude = movementDelta.magnitude;
        if (movementDeltaMagnitude > 0.01f)
        {
            movementDeltaNormalized = movementDelta.normalized;
            Vector3 movementDeltaSmooth = Vector3.SmoothDamp(transform.forward, movementDeltaNormalized,
                ref movementDeltaVelocity, movementFacingSmooth);

            DebugLog("UpdateRootRotation", $"Rotating to face direction: {movementDeltaSmooth}");
            m_AgentRb.MoveRotation(Quaternion.LookRotation(movementDeltaSmooth));
        }
    }

    private void CalculatePelvisReward()
    {
        // Get normalized pelvis height (0 = fully squatted, 1 = standing)
        float pelvisHeightNormalized = Mathf.InverseLerp(pelvisMin, pelvisMax, pelvicTransform.localPosition.y);

        // Distance to chair/block
        float distanceToChair = Vector3.Distance(transform.position, block.transform.position);
        float maxRewardDistance = 2.0f; // Adjust based on your environment scale

        // Calculate distance factor (1 when very close, 0 when far)
        float proximityFactor = Mathf.Clamp01(1.0f - (distanceToChair / maxRewardDistance));

        // When close to chair: reward squatting (lower pelvis)
        // When far from chair: reward standing (higher pelvis)
        float squatReward = 0;

        if (isInHotZone)
        {
            // In hot zone - strongly reward squatting
            // More reward the lower the pelvis is
            squatReward = (1.0f - pelvisHeightNormalized) * 0.1f;
            Debug.Log($"<color=green>Hot zone squat reward: {squatReward:F3}, height: {pelvisHeightNormalized:F2}</color>");
        }
        else if (isInMediumZone)
        {
            // In medium zone - moderately reward squatting
            squatReward = (1.0f - pelvisHeightNormalized) * 0.05f;
            Debug.Log($"<color=yellow>Medium zone squat reward: {squatReward:F3}, height: {pelvisHeightNormalized:F2}</color>");
        }
        else
        {
            // Far from chair - penalize squatting
            // More penalty the lower the pelvis is
            squatReward = pelvisHeightNormalized * 0.02f - 0.01f;
            Debug.Log($"<color=red>Far from chair - stand up reward: {squatReward:F3}, height: {pelvisHeightNormalized:F2}</color>");
        }

#if ADD_REWARD
        AddReward(squatReward);
#endif
    }

    private void Update()
    {
        // Update reward display if UI element is assigned
        if (rewardText != null)
        {
            float currentReward = GetCumulativeReward();
            float pelvisHeightPercent = Mathf.InverseLerp(pelvisMin, pelvisMax, pelvicTransform.localPosition.y) * 100f;
            rewardText.text = $"Reward: {currentReward:F2} | Scaling: {closerScalar:F1} | Squat: {100-pelvisHeightPercent:F0}% | Step: {StepCount}/{MaxStep}";

            // Log significant reward changes
            if (Mathf.Abs(currentReward - lastRewardValue) > 0.5f)
            {
                DebugLog("Update", $"Significant reward change: {lastRewardValue:F2} -> {currentReward:F2}");
                lastRewardValue = currentReward;
            }
        }
    }

    private void OnGUI()
    {
        if (showDebugGUI)
        {
            float currentReward = GetCumulativeReward();
            float pelvisHeightPercent = Mathf.InverseLerp(pelvisMin, pelvisMax, pelvicTransform.localPosition.y) * 100f;

            GUI.Label(new Rect(10, 10, 300, 20), $"Agent: {name}, Episode: {episodeCount}, Ending: {isEpisodeEnding}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Reward: {currentReward:F2}, Zone Scale: {closerScalar:F1}");
            GUI.Label(new Rect(10, 50, 300, 20), $"Steps: {StepCount}/{MaxStep}, Zones: Medium:{isInMediumZone}, Hot:{isInHotZone}");
            GUI.Label(new Rect(10, 70, 300, 20), $"Distance to Block: {Vector3.Distance(transform.position, block.transform.position):F2}");
            GUI.Label(new Rect(10, 90, 300, 20), $"Pelvis Height: {pelvisHeightPercent:F1}% (min:{pelvisMin}, max:{pelvisMax})");
        }
    }

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (isEpisodeEnding)
        {
            DebugLog("OnActionReceived", "Skipping action because episode is ending");
            return;
        }

        DebugLog("OnActionReceived", $"Step {StepCount}: Processing action");

        // Move the agent using the action.
        MoveAgent(actionBuffers.DiscreteActions);

        // Reward for getting closer to the block, scaled by proximity zone factor
        Vector3 currentDistanceToBlock = block.transform.position - transform.position;
        if (lastDistanceToBlock != Vector3.zero)
        {
            float movingCloserReward = lastDistanceToBlock.magnitude - currentDistanceToBlock.magnitude;

            // Apply the dynamic reward scaling based on which zone the agent is in
            float scaledReward = movingCloserReward * closerScalar;
            float prevReward = GetCumulativeReward();
#if ADD_REWARD
            AddReward(scaledReward);
#endif

            DebugLog("OnActionReceived",
                $"Distance change: {movingCloserReward:F3}, Scaled reward: {scaledReward:F3}, " +
                $"Previous: {prevReward:F2}, New: {GetCumulativeReward():F2}");

            // Debug info when reward scaling changes significantly
            if (scaledReward > 0.1f)
            {
                Debug.Log($"{transform.parent.name}: Good progress! Reward: {scaledReward:F2}, Zone scale: {closerScalar:F1}");
            }
        }

        lastDistanceToBlock = currentDistanceToBlock;

        // Penalty given each step to encourage agent to finish task quickly.
        float stepPenalty = -1f / MaxStep;
        float previousReward = GetCumulativeReward();
#if ADD_REWARD
        AddReward(stepPenalty);
#endif

        DebugLog("OnActionReceived", $"Step penalty: {stepPenalty:F5}, Previous: {previousReward:F2}, New: {GetCumulativeReward():F2}");

        // Check if cumulative reward has fallen below threshold
        CheckIfRewardBelowThreshold();

        // Log when approaching max steps
        if (StepCount >= MaxStep - 10)
        {
            DebugLog("OnActionReceived", $"Approaching max steps: {StepCount}/{MaxStep}", LogType.Warning);
        }

        if (StepCount == MaxStep - 1)
        {
            DebugLog("OnActionReceived", "Final step before max steps reached!", LogType.Warning);
            Debug.Log($"<color=red>********* Reaching end of episode ********** {GetCumulativeReward()}</color>");
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        // Movement (Branch 0)
        discreteActionsOut[0] = 0; // Default: no movement
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            discreteActionsOut[0] = 1; // Forward
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            discreteActionsOut[0] = 2; // Backward
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            discreteActionsOut[0] = 3; // Left
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            discreteActionsOut[0] = 4; // Right

        // Squatting (Branch 1)
        discreteActionsOut[1] = 0; // Default: no squat change
        if (Input.GetKey(KeyCode.Q)) // Squat down
            discreteActionsOut[1] = 1;
        else if (Input.GetKey(KeyCode.E)) // Stand up
            discreteActionsOut[1] = 2;
    }

    /// <summary>
    /// Resets the block position and velocities.
    /// </summary>
    void ResetBlock()
    {
        DebugLog("ResetBlock", "Resetting block position");
        // Get a random position for the block.
        block.transform.position = GetRandomSpawnPos();
    }

    /// <summary>
    /// In the editor, if "Reset On Done" is checked then AgentReset() will be
    /// called automatically anytime we mark done = true in an agent script.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        episodeCount++;
        Debug.Log($"Starting episode {episodeCount}");

        // Reset ending flag
        isEpisodeEnding = false;

        var rotation = Random.Range(0, 4);
        var rotationAngle = rotation * 90f;
        area.transform.Rotate(new Vector3(0f, rotationAngle, 0f));
        DebugLog("OnEpisodeBegin", $"Area rotated by {rotationAngle} degrees");

        // ResetBlock();

        // if (assetPlacer != null)
        // {
        //     DebugLog("OnEpisodeBegin", "Randomizing asset placement");
        //     assetPlacer.Randomize();
        // }
        // else
        // {
        //     DebugLog("OnEpisodeBegin", "WARNING: assetPlacer is null!", LogType.Warning);
        // }

        // Vector3 agentSpawnPos = GetRandomSpawnPos();
        // DebugLog("OnEpisodeBegin", $"Setting agent position to {agentSpawnPos}");
        // transform.position = agentSpawnPos;

        DebugLog("OnEpisodeBegin", "Resetting agent velocity");
        m_AgentRb.linearVelocity = Vector3.zero;
        m_AgentRb.angularVelocity = Vector3.zero;

        // Reset pelvis to standing position
        Vector3 resetPelvisPos = pelvicTransform.localPosition;
        resetPelvisPos.y = pelvisMax; // Default to standing up
        pelvicTransform.localPosition = resetPelvisPos;

        // Reset zone trackers and reward scaling
        isInMediumZone = false;
        isInHotZone = false;
        closerScalar = 0.1f;
        lastRewardValue = 0f;

        lastDistanceToBlock = Vector3.zero;
        DebugLog("OnEpisodeBegin", "Setting reset parameters");
        SetResetParameters();

        DebugLog("OnEpisodeBegin", $"Initial distance to block: {Vector3.Distance(transform.position, block.transform.position):F2}");
        DebugLog("OnEpisodeBegin", "Episode setup complete");
    }

    void SetResetParameters()
    {
        // Reset parameters as needed
        DebugLog("SetResetParameters", "Reset parameters applied");
    }

    /// <summary>
    /// Helper method for consistent debug logging
    /// </summary>
    private void DebugLog(string method, string message, LogType logType = LogType.Log)
    {
        if (!verboseLogging && logType == LogType.Log) return;

        string agentName = transform.parent != null ? transform.parent.name : gameObject.name;
        string prefix = $"[{agentName}:{episodeCount}:{StepCount}] {method}: ";

        switch (logType)
        {
            case LogType.Warning:
                Debug.LogWarning(prefix + message);
                break;
            case LogType.Error:
                Debug.LogError(prefix + message);
                break;
            default:
                Debug.Log(prefix + message);
                break;
        }
    }

    // Ensure we properly handle object destruction
    private void OnDestroy()
    {
        DebugLog("OnDestroy", "Agent being destroyed", LogType.Warning);
        StopAllCoroutines();
    }
}
