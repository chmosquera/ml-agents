using UnityEngine;

public class SittableDetect : MonoBehaviour
{
    public SittingAgent agent;

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the agent collided with this block
        if (collision.gameObject.CompareTag("agent") ||
            collision.gameObject == agent.gameObject)
        {
            // Notify the agent
            agent.BlockTouched();

            // Optional: Visual feedback
            Debug.Log("Block touched by agent!");
        }
    }
}
