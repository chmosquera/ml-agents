using UnityEngine;

public class ButtDetect : MonoBehaviour
{
    // Reference to the parent agent
    public SittingAgent agent;

    private void OnTriggerEnter(Collider other)
    {
        // Check if collided with the block
        if (other.CompareTag("block"))
        {
            // Make sure we have a reference to the agent
            if (agent == null)
            {
                Debug.LogError("ButtDetect: No agent reference set!");
                return;
            }

            // Notify the agent that the butt has sat on the block
            agent.ButtSatOnBlock();
        }
    }
}
