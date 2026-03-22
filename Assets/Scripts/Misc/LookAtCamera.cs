using UnityEngine;

/// <summary>
/// This script will generally be used for canvas objects to make them look at the main camera
/// </summary>
public class LookAtCamera : MonoBehaviour 
{
    private GameObject cameraObj; // Camera to point our object at
    void Start()
    {
        cameraObj = Camera.main.gameObject;
    }
    void Update()
    {
        transform.LookAt(cameraObj.transform.position);
    }
}
