using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;


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
    private PayloadBehaviour payload;

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

    protected override void Awake()
    {
        base.Awake();
        payload = PayloadBehaviour.Instance;
    }
    private void Update()
    {
        if (spawnEnemies) SpawnEnemies();
    }
    private void SpawnEnemies()
    {
        timer += Stages[currentStage].SpawnFrequency * Time.deltaTime; 

        if (timer >= 1f)
        {
            for (int i = 0; i < Stages[currentStage].MaxSpawnAmount; i++)
            {
                GameObject enemy = GameObjectPool.GetObject(enemyPrefab);
                enemy.transform.position = GetSpawnLocation();
            }
            timer = 0;
        }
    }
    private Vector3 GetSpawnLocation()
    {
        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        Vector3 randomPosition = payload.transform.position + randomDirection * Random.Range(payload.InteractRadius + 5f, payload.InteractRadius + 15f);
        NavMesh.SamplePosition
        return Vector3.zero; // Placeholder
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
                    Gizmos.DrawWireSphere(currentPosition, 3f);
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
