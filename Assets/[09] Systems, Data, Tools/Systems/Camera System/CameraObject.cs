using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraObject : MonoBehaviour
{
    [SerializeField] private string cameraName;

    private void Start()
    {
        CameraSystem.Instance.AddCamera(cameraName, gameObject);
    }
}
