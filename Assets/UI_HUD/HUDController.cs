using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : Singleton<HUDController>
{
    [Header("Health")]
    [SerializeField] private Slider health;
    [SerializeField] private Image healthFill;
    [SerializeField] private Slider healthCatchUp;
    [SerializeField] private float healthCatchUpTiming;
    private float healthOrigin;
    private float healthTemp;
    private float healthTarget;
    private bool updatingHealth = false;

    [Header("Gas")]
    [SerializeField] private Slider gas;
    [SerializeField] private Slider gasCatchUp;
    [SerializeField] private float gasCatchUpTiming;
    private float gasOrigin;
    private float gasTemp;
    private float gasTarget;
    private bool updatingGas = false;

    [Header("Payload Gas")]
    [SerializeField] private Slider payloadGas;
    [SerializeField] private Image payloadGasFill;
    [SerializeField] private Slider payloadGasCatchUp;
    [SerializeField] private float payloadGasCatchUpTiming;
    [SerializeField] private Color flickerColour;
    [SerializeField] private Color originalColour;
    [Tooltip("Seconds between each flicker")]
    [SerializeField] private float flickerRate;
    [SerializeField] private float warningThreshold;

    [SerializeField] private CanvasGroup highlight;
    private bool highlighting = false;
    private bool highlightSwitch = false;
    public float highlightSpeed = 1f;
    private int enemiesPushing = 0;
    public float highlightSpeedUpPer = 0.1f;


    private float payloadGasOrigin;
    private float payloadGasTemp;
    private float payloadGasTarget;
    private bool updatingPayloadGas = false;

    private bool flickering = false;
    private bool flickerSwitch = false;

    [Header("Progress Bar")]
    [SerializeField] private Slider progress;
    [SerializeField] private TextMeshProUGUI progressText;


    [Header("Conditions")]
    [SerializeField] private CanvasGroup winGroup;
    [SerializeField] private CanvasGroup loseGroup;

    [Header("Debug")]
    [SerializeField] private TextMeshProUGUI enemyCount;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    #region Health

    public void SetHealth(float input)
    {

        if (updatingHealth == false)
        {
            healthOrigin = health.value;
            healthTarget = Mathf.Clamp(input, 0f, 1f);
            health.value = healthTarget;
            StartCoroutine(UpdateHealthSequence());
        }
        else
        {
            healthTarget = Mathf.Clamp(input, 0f, 1f);
            health.value = healthTarget;
        }
        //health.value = 
    }

    IEnumerator UpdateHealthSequence()
    {
        updatingHealth = true;
        //health.value = healthTarget;

        float catchupCount = 0f;

        while (updatingHealth)
        {
            if (catchupCount < healthCatchUpTiming)
            {
                catchupCount += Time.deltaTime;

                healthCatchUp.value = Mathf.SmoothStep(healthOrigin, healthTarget, (float)(catchupCount / healthCatchUpTiming));
                yield return new WaitForSeconds(Time.deltaTime);
            }
            else
            {
                updatingHealth = false;
                healthCatchUp.value = healthTarget;
            }
        }
        yield break;
    }

    public void DamageFlicker()
    {
        StartCoroutine(DamageFlickerSequence());
    }
    IEnumerator DamageFlickerSequence()
    {
        Color temp = healthFill.color;

        healthFill.color = Color.red;

        yield return new WaitForSeconds(.5f);

        healthFill.color = temp;

        yield break;
    }

    #endregion

    #region Gas
    public void SetGas(float input)
    {
        if (updatingGas == false)
        {
            gasOrigin = gas.value;
            gasTarget = Mathf.Clamp(input, 0f, 1f);
            gas.value = gasTarget;
            StartCoroutine(UpdateHealthSequence());
        }
        else
        {
            gasTarget = Mathf.Clamp(input, 0f, 1f);
            gas.value = gasTarget;
        }
    }

    IEnumerator UpdateGasSequence()
    {
        updatingGas = true;
        //health.value = healthTarget;

        float catchupCount = 0f;

        while (updatingGas)
        {
            if (catchupCount < gasCatchUpTiming)
            {
                catchupCount += Time.deltaTime;

                gasCatchUp.value = Mathf.SmoothStep(gasOrigin, gasTarget, (float)(catchupCount / gasCatchUpTiming));
                yield return new WaitForSeconds(Time.deltaTime);
            }
            else
            {
                updatingGas = false;
                gasCatchUp.value = gasTarget;
            }
        }
        yield break;
    }
    #endregion

    #region Payload Gas
    public void SetPayloadGas(float input)
    {

        //Debug.Log($"Updating Payload Gas Input is {input}");

        if (updatingPayloadGas == false)
        {
            payloadGasOrigin = payloadGas.value;
            payloadGasTarget = Mathf.Clamp(input, 0f, 1f);
            payloadGas.value = payloadGasTarget;
            StartCoroutine(UpdatePayloadGasSequence());
        }
        else
        {
            payloadGasTarget = Mathf.Clamp(input, 0f, 1f);
            payloadGas.value = payloadGasTarget;
        }

        if (input <= warningThreshold)
        {
            if (flickering == false)
            {
                StartCoroutine(LowGasWarningFlicker());
            }
        }
        else
        {
            flickering = false;
        }
    }

    IEnumerator UpdatePayloadGasSequence()
    {
        //Debug.Log("Start Setting Payload Gas");
        updatingPayloadGas = true;

        float catchupCount = 0f;

        while (updatingPayloadGas)
        {
            if (catchupCount < payloadGasCatchUpTiming)
            {
                catchupCount += Time.deltaTime;

                payloadGasCatchUp.value = Mathf.SmoothStep(payloadGasOrigin, payloadGasTarget, (float)(catchupCount / payloadGasCatchUpTiming));
                yield return new WaitForSeconds(Time.deltaTime);
            }
            else
            {
                updatingPayloadGas = false;
                payloadGasCatchUp.value = payloadGasTarget;
            }
        }

        //Debug.Log("Stop Setting Payload Gas");
        yield break;
    }

    IEnumerator LowGasWarningFlicker()
    {
        //Debug.Log("Start flickering");
        flickering = true;

        while(flickering)
        {
            if (flickerSwitch)
            {
                payloadGasFill.color = originalColour;
                flickerSwitch = false;
                //Debug.Log("Original color");
            }
            else
            {
                payloadGasFill.color = flickerColour;
                flickerSwitch = true;
                //Debug.Log("Flicker color");
            }
            yield return new WaitForSeconds(flickerRate);
        }

        payloadGasFill.color = originalColour;

        //Redundancy
        flickering = false;
        flickerSwitch = false;

        yield break;
    }

    public void StartHighlight()
    {
        Debug.Log("Start Highling called");
        enemiesPushing += 1;
        if (!highlighting)
        {
            highlighting = true;
            StartCoroutine(EnemyPushingWarning());
        }
    }

    public void StopHighlight()
    {
        enemiesPushing -= 1;
        if (enemiesPushing <= 0)
        {
            highlighting = false;
        }
    }

    IEnumerator EnemyPushingWarning()
    {
        Debug.Log("Enemy Push Coroutine called");
        while (highlighting)
        {
            // Fade In
            yield return StartCoroutine(HighlightFadeTo(1f));
            // Fade Out
            yield return StartCoroutine(HighlightFadeTo(0f));
        }
        highlight.alpha = 0f;
    }


    IEnumerator HighlightFadeTo(float target)
    {
        Debug.Log("Highlight fade done");
        while (!Mathf.Approximately(highlight.alpha, target))
        {
            highlight.alpha = Mathf.MoveTowards(
                highlight.alpha,
                target,
                (highlightSpeed + (enemiesPushing * highlightSpeedUpPer)) * Time.deltaTime // uses current fadeSpeed continuously
            );
            yield return null;
        }
    }

    #endregion

    #region Progress Bar
    public void SetProgressBar(float input)
    {
        //Debug.Log($"Setting progress to {input}");
        float clamped = Mathf.Clamp(input, 0f, 1f);
        progressText.text = $"{Mathf.Round(clamped * 10000)/100}%";
        progress.value = clamped;
    }
    #endregion

    #region Win Lose
    public void WinScreen()
    {
        StartCoroutine(ScreenEffectsManager.Instance.Fade(winGroup, 0f, 1f, 1f));
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoseScreen()
    {
        StartCoroutine(ScreenEffectsManager.Instance.Fade(loseGroup, 0f, 1f, 1f));
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartPressed()
    {
        LevelDirector.Instance.Restart();
    }

    public void QuitPressed()
    {
        LevelDirector.Instance.Quit();
    }
    #endregion

    #region Debug
    public void UpdateTotalEnemyCount(int input)
    {
        if (enemyCount!= null && enemyCount.isActiveAndEnabled)
        {
            enemyCount.text = input.ToString();
        }
    }

    #endregion

    // Update is called once per frame
    void Update()
    {
        
    }
}
