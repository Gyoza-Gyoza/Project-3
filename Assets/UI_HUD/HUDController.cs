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
    [SerializeField] private Slider playerEmber;
    [SerializeField] private Slider playerEmberCatchUp;
    [SerializeField] private float playerEmberCatchUpTiming;
    private float emberOrigin;
    private float gasTemp;
    private float playerEmberTarget;
    private bool updatingPlayerEmber = false;

    [Header("Payload Gas")]
    [SerializeField] private Slider payloadEmber;
    [SerializeField] private Image payloadGasFill;
    [SerializeField] private Slider payloadGasCatchUp;
    [SerializeField] private float payloadEmberCatchUpTiming;
    //-------------------  Flicker ------------------------
    [SerializeField] private Color flickerColour;
    [SerializeField] private Color originalColour;
    [Tooltip("Seconds between each flicker")]
    [SerializeField] private float flickerRate;
    [SerializeField] private float warningThreshold;
    //-------------------  Being attacked ------------------------
    [SerializeField] private CanvasGroup highlight;
    private bool highlighting = false;
    private bool highlightSwitch = false;
    public float highlightSpeed = 1f;
    private int enemiesPushing = 0;
    public float highlightSpeedUpPer = 0.1f;


    private float payloadEmberOrigin;
    private float payloadGasTemp;
    private float payloadEmberTarget;
    private bool updatingPayloadEmber = false;

    private bool flickering = false;
    private bool flickerSwitch = false;

    [Header("Progress Bar")]
    [SerializeField] private Slider progress;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Death Bar")]
    [SerializeField] private GameObject deathObject;
    [SerializeField] private Slider death;
    [SerializeField] private TextMeshProUGUI deathText;

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

    #region Player Ember
    public void SetPlayerEmber(float input)
    {
        Debug.Log($"Inputing Player Ember as {input}");
        if (updatingPlayerEmber == false)
        {
            emberOrigin = playerEmber.value;
            playerEmberTarget = Mathf.Clamp(input, 0f, 1f);
            playerEmber.value = playerEmberTarget;
            StartCoroutine(UpdateHealthSequence());
        }
        else
        {
            playerEmberTarget = Mathf.Clamp(input, 0f, 1f);
            playerEmber.value = playerEmberTarget;
        }
    }

    IEnumerator UpdatePlayerEmberSequence()
    {
        updatingPlayerEmber = true;
        //health.value = healthTarget;

        float catchupCount = 0f;

        while (updatingPlayerEmber)
        {
            if (catchupCount < playerEmberCatchUpTiming)
            {
                catchupCount += Time.deltaTime;

                playerEmberCatchUp.value = Mathf.SmoothStep(emberOrigin, playerEmberTarget, (float)(catchupCount / playerEmberCatchUpTiming));
                yield return new WaitForSeconds(Time.deltaTime);
            }
            else
            {
                updatingPlayerEmber = false;
                playerEmberCatchUp.value = playerEmberTarget;
            }
        }
        yield break;
    }
    #endregion

    #region Payload Gas
    public void SetPayloadEmber(float input)
    {

        //Debug.Log($"Updating Payload Gas Input is {input}");

        if (updatingPayloadEmber == false)
        {
            payloadEmberOrigin = payloadEmber.value;
            payloadEmberTarget = Mathf.Clamp(input, 0f, 1f);
            payloadEmber.value = payloadEmberTarget;
            StartCoroutine(UpdatePayloadEmberSequence());
        }
        else
        {
            payloadEmberTarget = Mathf.Clamp(input, 0f, 1f);
            payloadEmber.value = payloadEmberTarget;
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

    IEnumerator UpdatePayloadEmberSequence()
    {
        //Debug.Log("Start Setting Payload Gas");
        updatingPayloadEmber = true;

        float catchupCount = 0f;

        while (updatingPayloadEmber)
        {
            if (catchupCount < payloadEmberCatchUpTiming)
            {
                catchupCount += Time.deltaTime;

                payloadGasCatchUp.value = Mathf.SmoothStep(payloadEmberOrigin, payloadEmberTarget, (float)(catchupCount / payloadEmberCatchUpTiming));
                yield return new WaitForSeconds(Time.deltaTime);
            }
            else
            {
                updatingPayloadEmber = false;
                payloadGasCatchUp.value = payloadEmberTarget;
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

    #region Death Zone Progress

    public void SetDeathBar(float input)
    {
        //Debug.Log($"Setting Death Progress called, is active: {death.gameObject.activeSelf}");
        if (!deathObject.activeSelf)
        {
            deathObject.SetActive(true);
        }
        float clamped = Mathf.Clamp(input, 0f, 1f);
        deathText.text = $"{Mathf.Round(clamped * 10000) / 100}%";
        death.value = clamped;

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
