using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelDirector : Singleton<LevelDirector>
{
    [SerializeField] private Level currentLevel;
    public Level CurrentLevel
    { get { return currentLevel; } }
    private int currentStage; 
    public int CurrentStage { get { return currentStage; } }

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private bool spawnEnemies = true;

    public float StageProgress
    { 
        get 
        {
            // Minus ones are to ignore the first checkpoint as it's the starting point
            float result;
            float progressionPerStage = 1f / (currentLevel.Stages.Count - 1);
            result = progressionPerStage * (currentStage - 1) + PayloadBehaviour.Instance.CheckpointProgress * progressionPerStage;

            if (CurrentStage == currentLevel.Stages.Count) result = 1f;

            return Mathf.Clamp01(result); 
        } 
    }

    private float timer;

    private void Update()
    {
        if (spawnEnemies) Spawning();
        Debug.Log($"Current Stage is {currentStage} \n Checkpoint progress is {PayloadBehaviour.Instance.CheckpointProgress * 100}% \n Total progress is {StageProgress * 100}%");
    }
    private void Spawning()
    {
        timer += currentLevel.Stages[currentStage].SpawnFrequency * Time.deltaTime; 

        if (timer >= 1f)
        {
            for (int i = 0; i < currentLevel.Stages[currentStage].SpawnAmount; i++)
            {
                GameObject enemy = GameObjectPool.GetObject(enemyPrefab);
                enemy.transform.position = transform.position;
            }
            timer = 0;
        }
    }
    public void ReachedCheckpoint()
    {
        currentStage++;
        currentStage = Mathf.Clamp(currentStage, 0, currentLevel.Stages.Count);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach(Stage stage in currentLevel.Stages)
        {
            Gizmos.DrawSphere(stage.Checkpoint, 1f);
        }
    }
}
