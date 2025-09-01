using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : Singleton<HUDController>
{
    [SerializeField] private Slider health;

    // Start is called before the first frame update
    void Start()
    {
        health.value = 1;
    }

    public void SetHealth(float input)
    {
        health.value += Mathf.Clamp(input, 0f, 1f);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
