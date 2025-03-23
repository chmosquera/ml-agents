using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizeAssetPlacement : MonoBehaviour
{
    [Header("Randomization Settings")]
    public float radius = 10f;
    public int maxPlacementAttempts = 50;
    public bool isDebug = false;
    public LayerMask collisionLayers;
    public float minDistanceBetweenAssets = 0.1f;

    private Transform[] childAssets;
    private List<Collider> assetColliders = new List<Collider>();

    void Start()
    {
        // Store all children transforms
        GetChildAssets();

        // Initial randomization
        Randomize();
    }

    void Update()
    {
        // Debug mode - press SPACE to randomize
        if (isDebug && Input.GetKeyDown(KeyCode.Space))
        {
            Randomize();
        }
    }

    private void GetChildAssets()
    {
        // Get all children transforms
        childAssets = new Transform[transform.childCount];
        assetColliders.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            childAssets[i] = transform.GetChild(i);

            // Get colliders from each child
            Collider[] colliders = childAssets[i].GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                assetColliders.Add(col);
            }
        }

        Debug.Log($"Found {childAssets.Length} child assets with a total of {assetColliders.Count} colliders");
    }

    /// <summary>
    /// Randomizes the position and rotation of all child assets within a circle
    /// </summary>
    public void Randomize()
    {
        // Make sure we have the latest child references
        if (childAssets == null || childAssets.Length != transform.childCount)
        {
            GetChildAssets();
        }

        // Reset all positions first
        foreach (Transform child in childAssets)
        {
            // Move the asset far away during randomization
            child.position = new Vector3(0, -1000, 0);
        }

        // Place each asset one by one
        foreach (Transform child in childAssets)
        {
            PlaceAsset(child);
        }
    }

    private void PlaceAsset(Transform asset)
    {
        bool validPlacement = false;
        int attempts = 0;

        // Get the bounds of this asset's colliders
        Bounds assetBounds = CalculateBounds(asset);
        float assetRadius = Mathf.Max(assetBounds.extents.x, assetBounds.extents.z);

        while (!validPlacement && attempts < maxPlacementAttempts)
        {
            attempts++;

            // Generate random angle and distance
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(0f, radius);

            // Convert to position
            Vector3 randomPos = new Vector3(
                distance * Mathf.Cos(angle * Mathf.Deg2Rad),
                0f,
                distance * Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            // Apply position (local to this transform)
            asset.position = transform.position + randomPos;

            // Random rotation (Y-axis only)
            asset.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Check for collisions with already placed assets
            if (!CheckForCollisions(asset))
            {
                validPlacement = true;
            }
        }

        if (!validPlacement)
        {
            Debug.LogWarning($"Could not find valid placement for {asset.name} after {maxPlacementAttempts} attempts");
        }
    }

    private bool CheckForCollisions(Transform asset)
    {
        // Get all colliders for this asset
        Collider[] assetColliders = asset.GetComponentsInChildren<Collider>();

        foreach (Collider assetCol in assetColliders)
        {
            // Skip if the collider is not enabled
            if (!assetCol.enabled) continue;

            // Check if out of bounds
            Vector3 localPos = transform.InverseTransformPoint(assetCol.bounds.center);
            float distanceFromCenter = new Vector2(localPos.x, localPos.z).magnitude;

            // If the collider is outside the circle radius
            if (distanceFromCenter + assetCol.bounds.extents.magnitude > radius)
            {
                return true; // Collision (out of bounds)
            }

            // Check against all other placed assets
            foreach (Transform otherAsset in childAssets)
            {
                // Skip self
                if (otherAsset == asset) continue;

                // Skip assets that haven't been placed yet (still at temporary position)
                if (otherAsset.position.y < -999) continue;

                // Get all colliders for the other asset
                Collider[] otherColliders = otherAsset.GetComponentsInChildren<Collider>();

                // Check each collider pair
                foreach (Collider otherCol in otherColliders)
                {
                    // Skip if the collider is not enabled
                    if (!otherCol.enabled) continue;

                    // Check if colliders are in contact
                    if (Physics.ComputePenetration(
                        assetCol, assetCol.transform.position, assetCol.transform.rotation,
                        otherCol, otherCol.transform.position, otherCol.transform.rotation,
                        out Vector3 direction, out float distance))
                    {
                        // If closer than minimum distance, consider it a collision
                        if (distance > minDistanceBetweenAssets)
                        {
                            return true; // Collision detected
                        }
                    }
                }
            }
        }

        return false; // No collisions
    }

    private Bounds CalculateBounds(Transform asset)
    {
        Bounds bounds = new Bounds(asset.position, Vector3.zero);
        Collider[] colliders = asset.GetComponentsInChildren<Collider>();

        if (colliders.Length > 0)
        {
            // Initialize with the first collider
            bounds = colliders[0].bounds;

            // Expand to include all other colliders
            for (int i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
        }

        return bounds;
    }

    // Optional: Gizmo to visualize the placement area

}
