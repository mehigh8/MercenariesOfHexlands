using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script should be used to update the height of the visuals from player or other objects
/// TODO: Maybe change the name since this script is not limited to player
/// </summary>
public class PlayerVisualsHandler : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float playerHeight; // Height of the player
    [SerializeField] private float rayLength; // Length of the ray searching for a tile
    [SerializeField] private LayerMask groundLayer; // Layer of the ground tiles
    [SerializeField] private float heightChangeDuration; // Duration of the height change animation
    [SerializeField] private float heightChangeEpsilon; // Acceptable difference between current height and target height in animation

    private float targetHeight = float.MaxValue; // Height we want to go to
    private Coroutine heightChangeCoroutine; // Reference to animation coroutine
    private Vector3 velocity; // Velocity of the SmoothDamp

    #region Unity Functions
    void Update()
    {
        // Check if there is a hex tile underneath the object
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, rayLength, groundLayer))
        {
            // Calculate the new height
            float height = hit.point.y + playerHeight;
            // If there is a noticeable difference, stop the coroutine in case it is still running and start it with the new height
            if (Mathf.Abs(height - targetHeight) > heightChangeEpsilon)
            {
                if (heightChangeCoroutine != null)
                    StopCoroutine(heightChangeCoroutine);

                targetHeight = height;
                velocity = Vector3.zero;
                heightChangeCoroutine = StartCoroutine(ChangeHeight());
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw a red ray signifying the player height
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.up * playerHeight);

        // Draw a green ray signifying the checking ray for hex tiles
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, Vector3.down * rayLength);
    }
    #endregion

    #region Animation
    /// <summary>
    /// IEnumerator used to animate the change in height
    /// </summary>
    /// <returns>-</returns>
    private IEnumerator ChangeHeight()
    {
        // Use SmoothDamp to change the height of the object until it gets close enough
        while (Mathf.Abs(transform.position.y - targetHeight) > heightChangeEpsilon)
        {
            transform.position = Vector3.SmoothDamp(transform.position, new Vector3(transform.position.x, targetHeight, transform.position.z), ref velocity, heightChangeDuration);
            yield return null;
        }
    }
    #endregion
}
