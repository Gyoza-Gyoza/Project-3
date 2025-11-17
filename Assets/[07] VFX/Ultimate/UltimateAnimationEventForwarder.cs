using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UltimateAnimationEventForwarder : MonoBehaviour
{
    [SerializeField] private GameObject playerHolder;
    [SerializeField] private Volume volume;
    private DepthOfField depthOfField;
    private ChromaticAberration aberration;
    private ChromaticAberration initialAberrationValues;
    private Coroutine aberrationCoroutine;
    [SerializeField] private Transform ultimateCamera;
    [SerializeField] private float duration = 0.5f, longerDuration;
    [SerializeField] private float damageInterval = 0.1f;
    [SerializeField] private FullScreenPassRendererFeature renderFeature;
    private Material renderFeatureMat;
    private Vector3 originalLocalPos;
    private Coroutine shakeCoroutine;
    private Coroutine dealDamageCoroutine;
    private bool shakeRoutineOn;
    private bool dealDamageOn;
    private GameObject thirdPersonCamera;
    private List<EnemyBehaviour> enemies = new List<EnemyBehaviour>();
    private static bool isUlting = false;
    public static bool IsUlting 
    { get { return isUlting; } }
    void Awake()
    {
        originalLocalPos = transform.localPosition;

        if (volume.profile.TryGet<DepthOfField>(out var dof)) depthOfField = dof;
        if (volume.profile.TryGet<ChromaticAberration>(out var ca)) initialAberrationValues = aberration = ca;

        thirdPersonCamera = ThirdPersonCamera.instance.gameObject;
        renderFeatureMat = renderFeature.passMaterial;
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
        isUlting = !state;
    }
    public void SetPlayerInputActive(bool state)
    {
        PlayerController3P.instance.canInput = state;
        playerHolder.SetActive(state);
    }
    public void SetLetterboxingActive(bool state)
    {
        ScreenEffectsManager.Instance.SetLetterboxActive(state, 0.2f, 0.2f);
    }
    public void SetImpactFrameActive(bool state)
    {
        renderFeatureMat.SetInteger("_Enabled", state ? 1 : 0);
    }
    /// <summary>
    /// Sets the chromatic aberration values
    /// </summary>
    /// <param name="str">string will be parsed through this "0.5,0", first number: intensity value from 0-1, second number: duration of transition, enter no duration like this "0.5," for infinite duration</param>
    public void SetChromaticAberration(string str)
    {
        if (aberrationCoroutine != null) StopCoroutine(aberrationCoroutine);
        aberrationCoroutine = StartCoroutine(SetChromaticAberrationCoroutine(str));
    }
    public void ResetChromaticAberration()
    {
        if (aberrationCoroutine != null) StopCoroutine(aberrationCoroutine);
        aberration = initialAberrationValues;
    }
    private IEnumerator SetChromaticAberrationCoroutine(string str)
    {
        float timer = 0f;
        string[] values = str.Split(",");

        if (values[1] != "")
        {
            float duration = float.Parse(values[1]);
            float start = aberration.intensity.value;
            float end = float.Parse(values[0]);

            while (timer <= duration)
            {
                timer += Time.deltaTime;

                float progress = Mathf.Lerp(start, end, timer / duration);
                aberration.intensity.value = progress;
                yield return null;
            }
        }
        else
        {
            aberration.intensity.value = float.Parse(values[0]);
            yield break;
        }
    }
    public void FlashChromaticAberration(float duration)
    {
        if (aberrationCoroutine != null) StopCoroutine(aberrationCoroutine);
        aberrationCoroutine = StartCoroutine(FlashChromaticAberrationCoroutine(duration));
    }
    private IEnumerator FlashChromaticAberrationCoroutine(float duration)
    {
        float timer = 0f;

        while (timer <= duration)
        {
            timer += Time.deltaTime;
            aberration.intensity.value = Mathf.Lerp(1, 0, timer / duration);
            yield return null;
        }
    }
    public void DealDamageNoKnockback(bool state)
    {
        dealDamageOn = state;

        if (state)
        {
            if (dealDamageCoroutine != null) StopCoroutine(dealDamageCoroutine);
            dealDamageCoroutine = StartCoroutine(DealDamageNoKnockbackCoroutine());
        }
    }
    private IEnumerator DealDamageNoKnockbackCoroutine()
    {
        while (true)
        {
            foreach (EnemyBehaviour enemy in enemies)
            {
                enemy.TakeDamageNoKnockback();
            }
            yield return new WaitForSeconds(damageInterval);

            if (!dealDamageOn)
            {
                break;
            }
        }
    }
    public void DealDamage()
    {
        foreach (EnemyBehaviour enemy in enemies)
        {
            enemy.TakeDamage(enemy.Health);
        }
    }
    public void End()
    {
        gameObject.SetActive(false);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<EnemyBehaviour>(out EnemyBehaviour enemy))
        {
            enemies.Add(enemy);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<EnemyBehaviour>(out EnemyBehaviour enemy))
        {
            enemies.Remove(enemy);
        }
    }
}
