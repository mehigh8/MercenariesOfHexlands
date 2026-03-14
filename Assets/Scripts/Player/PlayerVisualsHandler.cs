using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVisualsHandler : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float playerHeight;
    [SerializeField] private float rayLength;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float heightChangeDuration;
    [SerializeField] private float heightChangeEpsilon;

    private float targetHeight = float.MaxValue;
    private Coroutine heightChangeCoroutine;
    private Vector3 velocity;

    void Update()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, rayLength, groundLayer))
        {
            float height = hit.point.y + playerHeight;
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

    IEnumerator ChangeHeight()
    {
        while (Mathf.Abs(transform.position.y - targetHeight) > heightChangeEpsilon)
        {
            transform.position = Vector3.SmoothDamp(transform.position, new Vector3(transform.position.x, targetHeight, transform.position.z), ref velocity, heightChangeDuration);
            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.up * playerHeight);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, Vector3.down * rayLength);
    }
}
