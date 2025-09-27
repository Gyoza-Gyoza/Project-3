using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GasZone : MonoBehaviour
{

    [SerializeField] private float addRate; //per second
    private bool addingGas = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            addingGas = true;
            StartCoroutine(addGas());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            
            addingGas = false;
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
               if ( Discon_PlayerController.Instance.AddGas(1) == false)
                {
                    addingGas = false;
                }
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
