using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// This system acts as a central controller for all cameras
// Use a Camera Object script to work with this system
public class CameraSystem : Singleton<CameraSystem>
{
    [SerializeField] private GameObject cameraPrefab; 

    private Dictionary<string, GameObject> cameraList = new Dictionary<string, GameObject>();
    private string activeCamera;
    /// <summary>
    /// Adds a camera to the list of cameras in the system, names are not case sensitive. Camera named main will be the initial camera
    /// </summary>
    /// <param name="name">Name of the camera</param>
    /// <param name="camera">Camera object</param>
    public void AddCamera(string name, GameObject camera)
    {
        name = name.ToLower();

        if (name == "main")
        {
            activeCamera = name;
            camera.SetActive(true);
        }
        else
        {
            camera.SetActive(false);
        }

        if (cameraList.ContainsKey(name))
        {
            Debug.Log($"Camera list already contains a camera named {name}, camera not added");
        }
        else
        {
            cameraList.Add(name, camera);
            Debug.Log($"Successfully added camera {name} to camera list");
        }
    }

    public void SwitchCameraTo(string name)
    {
        name = name.ToLower();
        if (name == activeCamera) return; 
        if (!cameraList.ContainsKey(name)) Debug.Log("Camera doesn't exist"); 
        else
        {
            foreach (GameObject obj in cameraList.Values)
            {
                obj.SetActive(false);
            }
            cameraList[name].SetActive(true);
        }
    }
}
