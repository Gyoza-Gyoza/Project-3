using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemToCollect : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.OnCollect();
                GameObjectPool.ReturnObject(gameObject);
            }
        }
    }
}
