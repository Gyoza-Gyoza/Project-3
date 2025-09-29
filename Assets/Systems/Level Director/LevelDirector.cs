using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
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
    private List<Transform> spawnMarker = new List<Transform>(); 
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

    [SerializeField] private int maxEnemies = 0;
    [SerializeField] private int enemyCount = 0;
    public int EnemyCount
    { get { return enemyCount; } 
      set { enemyCount = value; HUDController.Instance.UpdateTotalEnemyCount(enemyCount); }
    }


    private float levelLength = 0f;

    private void calculateLevelLength()
    {
        for (int i = 1; i < stages.Length; i++)
        {
            levelLength += calculateCheckpointLength(i);
        }
    }

    private float calculateCheckpointLength(int index)
    {
        if (stages[index - 1] is Escort escort1 && stages[index] is Escort escort2)
        {
            return Vector3.Magnitude(escort2.Checkpoint - escort1.Checkpoint);
        }

        return 0f;
    }

    public float StageProgress
    { 
        get 
        {
            // Subtracting by one to ignore the first checkpoint as it's the starting point
            float calc = 0f;
            //float progressionPerStage = 1f / (Stages.Length - 1);
            for (int i = 1; i < currentStage; i++)
            {
                calc += calculateCheckpointLength(i);
            }
            calc += stages[currentStage].Progress * calculateCheckpointLength(currentStage);

            float result = calc / levelLength;

            //Debug.Log($"Stage Progress: {result}");

            return Mathf.Clamp01(result); 
        } 
    }

    private float timer;

    protected override void Awake()
    {
        base.Awake();
        payload = PayloadBehaviour.Instance;
        AssignEscortStages();
        calculateLevelLength();
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
            //if (GetSpawnLocation(out Vector3 spawnPosition))
            //{
            //    for (int i = 0; i < Stages[currentStage].EnemyPerSpawn; i++)
            //    {
            //        GameObject enemy = GameObjectPool.GetObject(enemyPrefab);
            //        enemy.transform.position = spawnPosition + new Vector3(Random.Range(-spawnSpread, spawnSpread), 0f, Random.Range(-spawnSpread, spawnSpread));
            //    }
            //}
            //else
            //{
            //    Debug.Log("Failed to find a location, spawn cancelled");
            //}

            Vector3 pos = GetSpawnMarkerLocation();

            for (int i = 0; i < Stages[currentStage].EnemyPerSpawn; i++)
            {
                if (enemyCount > maxEnemies)
                {
                    break;
                }
                NavMeshAgent enemy = GameObjectPool.GetObject(enemyPrefab).GetComponent<NavMeshAgent>();
                enemy.Warp(pos + new Vector3(Random.Range(-spawnSpread, spawnSpread), 0, Random.Range(-spawnSpread, spawnSpread)));
                EnemyCount += 1;
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
            randomPosition = PlayerController3P.Instance.transform.position + randomDirection * (stages[currentStage].MinSpawnDistance + Random.Range(0f, stages[currentStage].MaxSpawnDistance));
            
            Vector3 vectorToPlayer = PlayerController3P.Instance.transform.position - randomPosition;
            float distanceToPlayer = vectorToPlayer.magnitude;
            Debug.DrawLine(randomPosition, PlayerController3P.Instance.transform.position, Color.red, 1f);

            if (Physics.Linecast(randomPosition, PlayerController3P.Instance.transform.position, environmentMask))
            {
                Debug.DrawLine(randomPosition, PlayerController3P.Instance.transform.position, Color.green, 1f);
                return true;
            }
            else
            {
                Debug.DrawLine(randomPosition, PlayerController3P.Instance.transform.position, Color.red, 1f);
                continue;
            }
        }
        return false;
    }
    private Vector3 GetSpawnMarkerLocation()
    {
        Vector3 pos = Vector3.zero;

        foreach (Transform position in spawnMarker)
        {
            // Checks if position is within range
            if (!(Vector3.Distance(PlayerController3P.Instance.transform.position, position.position) <= Stages[currentStage].MaxSpawnDistance) ||
                !(Vector3.Distance(PlayerController3P.Instance.transform.position, position.position) >= Stages[currentStage].MinSpawnDistance)) continue;

            if (Physics.Linecast(position.position, PlayerController3P.Instance.transform.position, environmentMask))
            {
                Debug.DrawLine(position.position, PlayerController3P.Instance.transform.position, Color.green, 1f);
                pos = position.position;
                break;
            }
            else
            {
                Debug.DrawLine(position.position, PlayerController3P.Instance.transform.position, Color.red, 1f);
                continue;
            }
        }
        //Debug.Log($"{pos.x}, {pos.y}, {pos.z}");
        return pos;
    }
    public void MarkLocation(Transform pos) => spawnMarker.Add(pos);
    public void CompletedStage()
    {
        currentStage++;
        currentStage = Mathf.Clamp(currentStage, 0, Stages.Length - 1);
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

    public void WinGame()
    {

    }

    public void LoseGame()
    {

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
                    Gizmos.DrawWireSphere(currentPosition, 5f);
                    break;
                case Defend defend:
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(currentPosition, 5f);
                    break;
                case Collect collect:
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(currentPosition, 5f);
                    break;
                default:
                    Gizmos.color = Color.white;
                    break;
            }
        }
    }
}
