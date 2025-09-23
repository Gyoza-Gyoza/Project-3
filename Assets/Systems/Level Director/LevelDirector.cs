using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;


public class LevelDirector : Singleton<LevelDirector>
{
    [SerializeField] private Stage[] stages;
    [SerializeField] public TextMeshProUGUI testText;
    [SerializeField] private GameObject test;
    [SerializeField] private float spawnSpread;
    [SerializeField] private LayerMask environmentMask;
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
        AssignEscortStages();
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
            if (GetSpawnLocation(out Vector3 spawnPosition))
            {
                for (int i = 0; i < Stages[currentStage].MaxSpawnAmount; i++)
                {
                    GameObject enemy = GameObjectPool.GetObject(enemyPrefab);
                    enemy.transform.position = spawnPosition + new Vector3(Random.Range(-spawnSpread, spawnSpread), 0f, Random.Range(-spawnSpread, spawnSpread));
                }
            }
            else
            {
                Debug.Log("Failed to find a location, spawn cancelled");
            }
            timer = 0;
        }
    }
    private bool GetSpawnLocation(out Vector3 randomPosition)
    {
        Vector3 randomDirection = Vector3.zero;
        randomPosition = Vector3.zero;

        for(int i = 0; i < 100; i++)
        {
            randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            randomPosition = Discon_PlayerController.Instance.transform.position + randomDirection * (stages[currentStage].MinSpawnDistance + Random.Range(0f, stages[currentStage].MaxSpawnDistance));
            
            Vector3 vectorToPlayer = Discon_PlayerController.Instance.transform.position - randomPosition;
            float distanceToPlayer = vectorToPlayer.magnitude;
            Debug.DrawLine(randomPosition, Discon_PlayerController.Instance.transform.position, Color.red, 1f);

            if (Physics.Linecast(randomPosition, Discon_PlayerController.Instance.transform.position, environmentMask))
            {
                Debug.DrawLine(randomPosition, Discon_PlayerController.Instance.transform.position, Color.green, 1f);
                return true;
            }
            else
            {
                Debug.DrawLine(randomPosition, Discon_PlayerController.Instance.transform.position, Color.red, 1f);
                continue;
            }
        }
        return false;
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

    private void AssignEscortStages()
    {
        //Debug.Log("Assign Escorts called");
        for(int i = 1; i < stages.Length; i++)
        {
            if (stages[i] is Escort escort1)
            {
                if (stages[i -1 ] is Escort escort2)
                {
                    escort1.PreviousCheckpoint = escort2.Checkpoint;
                }
                else
                {
                    escort1.PreviousCheckpoint = escort1.Checkpoint;

                }

                //Debug.Log($"Escorts point {i}, Previous: {escort1.PreviousCheckpoint}, Current: {escort1.Checkpoint}");
            }
        }
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
