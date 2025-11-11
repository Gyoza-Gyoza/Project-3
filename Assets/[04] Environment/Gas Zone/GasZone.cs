using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GasZone : MonoBehaviour
{

    [SerializeField] private float addRate; //per second
    [SerializeField] private float regenRate = 0.5f; //per second
    private bool addingGas = false;
    private bool addingHealth = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            addingGas = true;
            StartCoroutine(addGas());

            addingHealth = true;
            StartCoroutine(addHealth());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            
            addingGas = false;

            addingHealth = false;
        }
    }

    IEnumerator addGas()
    {
        float count = 0f;

        while (addingGas)
        {
            count += Time.deltaTime;
            if (count > 1f / addRate)
            {
                count -= (1f / addRate);
                if (PlayerController3P.Instance.AddGas(1) == false)
                {
                    addingGas = false;
                }
            }
            yield return new WaitForSeconds(Time.deltaTime);
        }

        yield break;
    }

    IEnumerator addHealth()
    {
        float count = 0f;

        while (addingHealth)
        {
            count += Time.deltaTime;
            if (count > 1f / regenRate)
            {
                count -= (1f / regenRate);
                PlayerController3P.Instance.Heal(1);
            }
            yield return new WaitForSeconds(Time.deltaTime);
        }

        yield break;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
}
