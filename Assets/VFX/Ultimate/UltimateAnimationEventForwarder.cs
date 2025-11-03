using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UltimateAnimationEventForwarder : MonoBehaviour
{
    [SerializeField] private GameObject playerHolder;
    [SerializeField] private Volume volume;
    private DepthOfField depthOfField;
    [SerializeField] private Transform ultimateCamera;
    [SerializeField] private float duration = 0.5f, longerDuration;
    private Vector3 originalLocalPos;
    private Coroutine shakeCoroutine;
    private bool shakeRoutineOn;
    private GameObject thirdPersonCamera;

    void Awake()
    {
        originalLocalPos = transform.localPosition;
        if (volume.profile.TryGet<DepthOfField>(out var dof))
        {
            depthOfField = dof;
        }
        thirdPersonCamera = ThirdPersonCamera.Instance.gameObject;
    }

    public void CameraShake(float intensity = 0.1f)
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, duration));
    }
    public void LongerCameraShake(float intensity = 0.1f)
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, longerDuration));
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            transform.localPosition = originalLocalPos + Random.insideUnitSphere * intensity;
            yield return null;
        }

        transform.localPosition = originalLocalPos;
        shakeCoroutine = null;
    }
    private IEnumerator ShakeRoutineOnCoroutine(float intensity)
    {
        float timer = 0f;

        while (true)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            transform.localPosition = originalLocalPos + Random.insideUnitSphere * intensity;

            if(!shakeRoutineOn)
                break;
            yield return null;
        }

        transform.localPosition = originalLocalPos;
        shakeCoroutine = null;
    }
    public void ShakeRoutineOn(float intensity)
    {
        shakeRoutineOn = true;

        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutineOnCoroutine(intensity));
    }
    public void ShakeRoutineOff()
    {
        shakeRoutineOn = false;
    }
    public void SetCameraActive(bool state)
    {
        thirdPersonCamera.SetActive(state);
        if (state) thirdPersonCamera.transform.position = ultimateCamera.position;
        //Camera.main.enabled = state;
        HUDController.Instance.SetUIVisible(state);
        depthOfField.active = state;
    }
    public void SetPlayerInputActive(bool state)
    {
        PlayerController3P.Instance.canInput = state;
        playerHolder.SetActive(state);
    }
    public void SetLetterboxingActive(bool state)
    {
        ScreenEffectsManager.Instance.SetLetterboxActive(state, 0.2f, 0.2f);
    }
    public void End()
    {
        gameObject.SetActive(false);
    }
}
