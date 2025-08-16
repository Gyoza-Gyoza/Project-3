using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int spawnAmount = 3;
    [SerializeField] private float spawnInterval = 5f;

    private float timer; 
    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            for (int i = 0; i < spawnAmount; i++)
            {
                GameObject enemy = GameObjectPool.GetObject(enemyPrefab);
                enemy.transform.position = transform.position;
            }
            timer = 0;
        }
    }
}
