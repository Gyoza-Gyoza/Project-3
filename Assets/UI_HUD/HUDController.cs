using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : Singleton<HUDController>
{
    [Header("Health")]
    [SerializeField] private Slider health;
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

    [Header("Progress Bar")]
    [SerializeField] private Slider progress;
    [SerializeField] private TextMeshProUGUI progressText;
    //[SerializeField] private Slider gasCatchUp;
    //[SerializeField] private float gasCatchUpTiming;
    //private float gasOrigin;
    //private float gasTemp;
    //private float gasTarget;
    //private bool updatingGas = false;



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


    #region Progress Bar
    public void SetProgressBar(float input)
    {
        Debug.Log($"Setting progress to {input}");
        float clamped = Mathf.Clamp(input, 0f, 1f);
        progressText.text = $"{Mathf.Round(clamped * 10000)/100}%";
        progress.value = clamped;
    }
    #endregion
    // Update is called once per frame
    void Update()
    {
        
    }
}
