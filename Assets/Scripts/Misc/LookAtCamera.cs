using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LookAtCamera : MonoBehaviour
{
    public GameObject canvas;
    public TMP_Text nameText;
    private GameObject cameraObj;
    void Start()
    {
        cameraObj = Camera.main.gameObject;
    }
    void Update()
    {
        canvas.transform.LookAt(cameraObj.transform.position);
    }
}
