using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
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
    [SerializeField] private Slider payloadHealth;
    [SerializeField] private Image payloadHealthFill;
    [SerializeField] private Slider payloadHealthCatchUp;
    [SerializeField] private float payloadHealthCatchUpTiming;
    [SerializeField] private GameObject golemDefaultImage;
    [SerializeField] private GameObject golemWarningImage;
    //-------------------  Flicker ------------------------
    // [SerializeField] private Color flickerColour;
    // [SerializeField] private Color originalColour;
    // [Tooltip("Seconds between each flicker")]
    // [SerializeField] private float flickerRate;
    [SerializeField] private float warningThreshold;
    //-------------------  Being attacked ------------------------
    [SerializeField] private CanvasGroup highlight;
    private bool highlighting = false;
    private bool highlightSwitch = false;
    public float highlightSpeed = 1f;
    private int enemiesAttacking = 0;
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
    [SerializeField] private GameObject checkpointMarker;

    [Header("Death Bar")]
    [SerializeField] private GameObject deathObject;
    [SerializeField] private Slider death;
    [SerializeField] private TextMeshProUGUI deathText;

    [Header("Conditions")]
    [SerializeField] private CanvasGroup winGroup;
    [SerializeField] private CanvasGroup loseGroup;

    [Header("Devices / Abilities")]
    [SerializeField] private Slider tauntSlider;
    [SerializeField] private Sprite tauntActive;
    [SerializeField] private Sprite tauntInactive;
    [SerializeField] private Image tauntAbility;

    [Header("Ultimate")]
    [SerializeField] private Slider ultimateSlider;
    [SerializeField] private GameObject ultimateReady;
    [SerializeField] private GameObject ultimateButtonReady;

    [Header("Debug")]
    [SerializeField] private TextMeshProUGUI enemyCount;
    [SerializeField] private TextMeshProUGUI enemiesStopped;
    [SerializeField] private TextMeshProUGUI timepassed;

    public CanvasGroup hideableUIs;

    // Start is called before the first frame update
    void Start()
    {
        if (golemDefaultImage != null && golemWarningImage != null)
        {
            golemDefaultImage.SetActive(false);
            golemWarningImage.SetActive(true);
        }
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
        //Debug.Log($"Inputing Player Ember as {input}");
        if (updatingPlayerEmber == false)
        {
            emberOrigin = playerEmber.value;
            playerEmberTarget = Mathf.Clamp(input, 0f, 1f);
            playerEmber.value = playerEmberTarget;
            StartCoroutine(UpdatePlayerEmberSequence());
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

    #region Payload Health
    public void SetPayloadHealth(float input)
    {

        //Debug.Log($"Updating Payload Gas Input is {input}");

        if (updatingPayloadEmber == false)
        {
            payloadEmberOrigin = payloadHealth.value;
            payloadEmberTarget = Mathf.Clamp(input, 0f, 1f);
            payloadHealth.value = payloadEmberTarget;
            StartCoroutine(UpdatePayloadHealthSequence());
        }
        else
        {
            payloadEmberTarget = Mathf.Clamp(input, 0f, 1f);
            payloadHealth.value = payloadEmberTarget;
        }

        // if (input <= warningThreshold)
        // {
        //     if (flickering == false)
        //     {
        //         StartCoroutine(LowGasWarningFlicker());
        //     }
        // }
        // else
        // {
        //     flickering = false;
        // }

        bool isWarning = input <= warningThreshold;
        if (golemWarningImage != null && golemDefaultImage != null)
        {
            golemDefaultImage.SetActive(!isWarning);
            golemWarningImage.SetActive(isWarning);
        }
    }

    IEnumerator UpdatePayloadHealthSequence()
    {
        //Debug.Log("Start Setting Payload Gas");
        updatingPayloadEmber = true;

        float catchupCount = 0f;

        while (updatingPayloadEmber)
        {
            if (catchupCount < payloadHealthCatchUpTiming)
            {
                catchupCount += Time.deltaTime;

                payloadHealthCatchUp.value = Mathf.SmoothStep(payloadEmberOrigin, payloadEmberTarget, (float)(catchupCount / payloadHealthCatchUpTiming));
                yield return new WaitForSeconds(Time.deltaTime);
            }
            else
            {
                updatingPayloadEmber = false;
                payloadHealthCatchUp.value = payloadEmberTarget;
            }
        }

        //Debug.Log("Stop Setting Payload Gas");
        yield break;
    }

    // IEnumerator LowGasWarningFlicker()
    // {
    //     //Debug.Log("Start flickering");
    //     flickering = true;

    //     while(flickering)
    //     {
    //         if (flickerSwitch)
    //         {
    //             payloadHealthFill.color = originalColour;
    //             flickerSwitch = false;
    //             //Debug.Log("Original color");
    //         }
    //         else
    //         {
    //             payloadHealthFill.color = flickerColour;
    //             flickerSwitch = true;
    //             //Debug.Log("Flicker color");
    //         }
    //         yield return new WaitForSeconds(flickerRate);
    //     }

    //     payloadHealthFill.color = originalColour;

    //     //Redundancy
    //     flickering = false;
    //     flickerSwitch = false;

    //     yield break;
    // }

    public void StartHighlight()
    {
        //Debug.Log("Start Highling called");
        enemiesAttacking += 1;
        if (!highlighting)
        {
            highlighting = true;
            StartCoroutine(EnemyPushingWarning());
        }
    }

    public void StopHighlight()
    {
        enemiesAttacking -= 1;
        if (enemiesAttacking <= 0)
        {
            highlighting = false;
        }
    }

    IEnumerator EnemyPushingWarning()
    {
        //Debug.Log("Enemy Push Coroutine called");
        while (highlighting)
        {
            // Fade In
            yield return StartCoroutine(HighlightFadeTo(1f));
            // Fade Out
            yield return StartCoroutine(HighlightFadeTo(0f));
        }
        highlight.alpha = 0f;
        yield break;
    }

    IEnumerator HighlightFadeTo(float target)
    {
        //Debug.Log("Highlight fade done");
        while (!Mathf.Approximately(highlight.alpha, target))
        {
            highlight.alpha = Mathf.MoveTowards(
                highlight.alpha,
                target,
                (highlightSpeed + (enemiesAttacking * highlightSpeedUpPer)) * Time.deltaTime // uses current fadeSpeed continuously
            );
            yield return null;
        }
    }

    #endregion

    #region Progress Bar

    public void SetUpProgressBar(Stage[] stages, float levelLength)
    {
        // float count = 0f;
        // foreach (Stage stage in stages)
        // {
        //     count += stage.Length;
        //     if (stage != null && stage.IsCheckpoint == true)
        //     {
        //         SpawnCheckpointMarker(count / levelLength);
        //     }
        // }
    }

    public void SpawnCheckpointMarker(float percentage)
    {
        GameObject justSpawned = GameObject.Instantiate(checkpointMarker, progress.transform);

        float width = progress.GetComponent<RectTransform>().rect.width;

        float calc = -(width / 2) + (width * percentage);

        justSpawned.GetComponent<RectTransform>().anchoredPosition = new Vector3(calc, 0f, 0f);
    }

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
        //Debug.Log($"Setting Death StageProgress called, is active: {death.gameObject.activeSelf}");
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
        StartCoroutine(Fade(winGroup, 0f, 1f, 1f));
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoseScreen()
    {
        StartCoroutine(Fade(loseGroup, 0f, 1f, 1f));
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

    #region Abilities
    public void ToggleTaunt(bool input)
    {
        if (input)
        { tauntAbility.sprite = tauntActive; }
        else 
        { tauntAbility.sprite = tauntInactive; }
    }

    public void SetTauntSlider(float input)
    {
        tauntSlider.value = input;
    }
    #endregion

    #region Ultimate

    public void SetUltimateSlider(float input)
    {
        ultimateSlider.value = input;
    }

    public void UltimateReady()
    {
        ultimateReady.SetActive(true);
        ultimateButtonReady.SetActive(true);
    }

    public void UltimateUsed()
    {
        ultimateReady.SetActive(false);
        ultimateButtonReady.SetActive(false);
    }

    #endregion

    #region Debug

    public void EnemiesStopped( bool input)
    {
        if (input == true)
        {
            enemiesStopped.gameObject.SetActive(true);
        }
        else
        { enemiesStopped.gameObject.SetActive(false); }
    }

    public void UpdateTotalEnemyCount(int input)
    {
        if (enemyCount!= null && enemyCount.isActiveAndEnabled)
        {
            enemyCount.text = input.ToString();
        }
    }

    public void UpdateTimePassed(float input)
    {
        //Debug.Log($"Time passed called, {input}");
        if (timepassed != null && timepassed.isActiveAndEnabled)
        {
            timepassed.text = ((int)(input / 60)).ToString() + " Min " + ((int)(input % 60)).ToString() + "Sec";
        }
    }
    #endregion


    public IEnumerator Fade(CanvasGroup target, float start, float end, float duration)
    {
        float timer = 0f;

        while (timer <= duration)
        {
            timer += Time.deltaTime;
            target.alpha = Mathf.Lerp(start, end, timer / duration);
            yield return null;
        }
    }
    public void SetUIVisible(bool state)
    {
        StartCoroutine(SetUIVisibleCoroutine(state));
    }
    private IEnumerator SetUIVisibleCoroutine(bool state)
    {
        float start = hideableUIs.alpha;
        float end = state ? 1f : 0f;
        float duration = 0.5f;
        float timer = 0f;

        while (timer <= duration)
        {
            timer += Time.deltaTime;
            hideableUIs.alpha = Mathf.Lerp(start, end, timer / duration);
            yield return null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDestroy()
    {
        Debug.Log("HUD Manager Destoyed");
    }
}
