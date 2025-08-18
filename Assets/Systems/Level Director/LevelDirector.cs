using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelDirector : Singleton<LevelDirector>
{
    [SerializeField] private Stage[] stages;
    [SerializeField] public TextMeshProUGUI testText;
    public Stage[] Stages
    {
        get { return stages; }
    }
    private int currentStage; 
    public int CurrentStage { get { return currentStage; } }

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private bool spawnEnemies = true;

    private Vector3 currentPosition; // Used for drawing gizmos

    public float StageProgress
    { 
        get 
        {
            // Subtracting by one to ignore the first checkpoint as it's the starting point
            float result;
            float progressionPerStage = 1f / (Stages.Length - 1);
            result = progressionPerStage * (currentStage - 1) + stages[currentStage].Progress * progressionPerStage;

            return Mathf.Clamp01(result); 
        } 
    }

    private float timer;

    private void Update()
    {
        if (spawnEnemies) Spawning();
    }
    private void Spawning()
    {
        timer += Stages[currentStage].SpawnFrequency * Time.deltaTime; 

        if (timer >= 1f)
        {
            for (int i = 0; i < Stages[currentStage].MaxSpawnAmount; i++)
            {
                GameObject enemy = GameObjectPool.GetObject(enemyPrefab);
                enemy.transform.position = transform.position;
            }
            timer = 0;
        }
    }
    public void CompletedStage()
    {
        currentStage++;
        currentStage = Mathf.Clamp(currentStage, 0, Stages.Length);
    }
    public void CompleteLevel()
    {
        // Handle level completion logic here
        testText.text = $"Level Completed! Total Progress: 100%";
    }

    private void OnDrawGizmos()
    {
        foreach (Stage stage in Stages)
        {
            switch (stage)
            {
                case Escort escort:
                    Gizmos.color = Color.red;
                    currentPosition = escort.Checkpoint;
                    Gizmos.DrawWireSphere(currentPosition, 1f);
                    break;
                case Defend defend:
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(currentPosition, 5f);
                    break;
                case Collect collect:
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(currentPosition, 3f);
                    break;
                default:
                    Gizmos.color = Color.white;
                    break;
            }
        }
    }
}
