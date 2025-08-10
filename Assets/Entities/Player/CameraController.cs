using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Transform testTarget;
    [SerializeField]
    private PlayerController player;
    // Variables for controlling the camera 
    [SerializeField]
    private float xSensitivity = 5f, ySensitivity = 5f;
    // Movement variables
    private float xCursor, yCursor, upDownRotation,
        yLookMin = -80f, yLookMax = 80f;

    [SerializeField]
    private Vector3 cameraShakeMagnitude;

    private GameObject target;
    private TimerManager timerManager;
    private bool canControl = true; 
    public bool CanControl
    {
        get { return canControl; }
        set { canControl = value; }
    }
    private bool cameraMoving;

    private void Start()
    {
        timerManager = TimerManager.Instance;
        target = player.gameObject;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O) && !cameraMoving) SwitchTarget(1f, testTarget);
        if (Input.GetKeyDown(KeyCode.P) && !cameraMoving) ReturnToPlayer(1f);
        if (canControl) MouseInput();
    }
    private void MouseInput() //Gets the mouse position 
    {
        xCursor = Input.GetAxis("Mouse X") * xSensitivity; // Gets mouse X input
        yCursor = Input.GetAxis("Mouse Y") * ySensitivity; // Gets mouse Y input

        // Performs vertical rotation
        upDownRotation += -yCursor;
        upDownRotation = Mathf.Clamp(upDownRotation, yLookMin, yLookMax);
        transform.localRotation = Quaternion.Euler(upDownRotation, 0f, 0f);

        player.transform.Rotate(Vector3.up, xCursor); // Performs horizontal rotation
    }
    public void CameraShake(float duration, Vector2 magnitude)
    {
        Vector3 originalPosition = GetCameraPosition();
        timerManager.CreateRoutineTask(duration, () =>
        {
            transform.position = new Vector3(originalPosition.x + Random.Range(-magnitude.x, magnitude.x), originalPosition.y + Random.Range(-magnitude.y, magnitude.y), originalPosition.z);
        });
    }
    public void SwitchTarget(float duration, Transform target)
    {
        float timer = 0f;
        canControl = false;
        cameraMoving = true;
        timerManager.CreateRoutineTask(duration, () =>
        {
            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(this.target.transform.position, target.position, timer / duration);
        }, () => 
        {
            this.target = target.gameObject;
            cameraMoving = false;
        });
    }
    public void ReturnToPlayer(float duration)
    {
        float timer = 0f;
        cameraMoving = true;
        timerManager.CreateRoutineTask(duration, () =>
        {
            timer += Time.deltaTime;
            Debug.Log(timer);
            transform.position = Vector3.Lerp(target.transform.position, GetCameraPosition(), timer / duration);
        },
        () =>
        {
            target = player.gameObject;
            canControl = true;
            cameraMoving = false;
        });
    }
    public Vector3 GetCameraPosition() // Returns player position after all calculations
    {
        return player.transform.position;
    }
}
