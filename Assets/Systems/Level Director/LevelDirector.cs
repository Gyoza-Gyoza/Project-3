using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;


public class LevelDirector : Singleton<LevelDirector>
{
    [SerializeField] private Stage[] stages;
    [SerializeField] public TextMeshProUGUI testText;
    [SerializeField] private GameObject test;
    [SerializeField] private float spawnSpread;
    [SerializeField] private LayerMask environmentMask;

    private LineRenderer lineRenderer;

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
    private bool spawnCoolingDown = false;
    private bool startCooldown = false;

    private Vector3 currentPosition; // Used for drawing gizmos
    private PayloadBehaviour payload;

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
    #region

    [Tooltip("Units per second that the death zone moves at")]
    [SerializeField] private float deathZoneRate = 0f;
    [SerializeField] private float deathZoneStartDelay = 3f;
    private float deathZoneProgress = 0f; 
    private bool deathZoneActive = false;

    private void StartDeathZone()
    {
        StartCoroutine(DeathZoneCoroutine());
    }

    private void StopDeathZone()
    {
        deathZoneActive = false;
    }


    IEnumerator DeathZoneCoroutine()
    {
        //float count = 0f;
        deathZoneActive = true;
        float rate = deathZoneRate / 1f;


        while (deathZoneActive)
        {
            //count += Time.deltaTime;
            
            deathZoneProgress += rate * Time.deltaTime;

            yield return new WaitForSeconds(Time.deltaTime);
        }
        yield break;
    }

    private void UpdateDeathZone()
    {
        HUDController.Instance.SetDeathBar(deathZoneProgress / levelLength);
    }

    #endregion

    #region ----- Defaults (Awake, Start, Update) ------ 
    protected override void Awake()
    {
        base.Awake();
        payload = PayloadBehaviour.Instance;
        AssignEscortStages();
        calculateLevelLength();
        lineRenderer = new LineRenderer();
    }
    private void Update()
    {
        if (spawnEnemies) SpawnEnemies();
    }
    #endregion

    private float timer;

    private void SpawnEnemies()
    {
        if (CanSpawn())
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

            foreach(Vector3 marker in Stages[currentStage].SpawnMarkers)
            {
                for (int i = 0; i < Stages[currentStage].EnemyPerGroup; i++)
                {
                    NavMeshAgent enemy = GameObjectPool.GetObject(enemyPrefab).GetComponent<NavMeshAgent>();
                    enemy.Warp(marker + new Vector3(Random.Range(-spawnSpread, spawnSpread), 0, Random.Range(-spawnSpread, spawnSpread)));
                    EnemyCount += 1;
                }
            }

            timer = 0;
        }
    }
    private bool CanSpawn()
    {
        timer += Time.deltaTime;

        if (spawnCoolingDown) // Checks if the spawn is on cooldown
        {
            if(!startCooldown) // Checks if the cooldown can be started
            {
                if (enemyCount <= Stages[currentStage].ResumeThreshold) // Checks if the enemy count is below the resume threshold and removes the spawn cooldown
                {
                    startCooldown = true;
                    timer = 0f; // Starts the timer when the enemy goes below the threshold
                    Debug.Log("Enemy count below threshold, starting spawn cooldown");
                }
            }
            else // If it is cooling down, it starts the timer
            {
                if (timer >= stages[CurrentStage].SpawnCooldown)
                {
                    startCooldown = false;
                    spawnCoolingDown = false;
                    timer = 0f;
                }
            }
            Debug.Log($"Spawning on cooldown, timer: {timer}");
            return false; 
        }
        if (enemyCount >= Stages[currentStage].MaxEnemies) // Checks if the enemy count is at max and sets the spawn on cooldown
        {
            spawnCoolingDown = true;
            Debug.Log("Enemy cap reached, spawning on cooldown");
            return false;
        }
        if (timer <= stages[currentStage].DurationBetweenSpawns) // Checks if the timer has reached the spawn frequency
        {
            Debug.Log("Waiting for spawn timer");
            return false;
        }
        timer = 0f;
        return true;
    }
    // Getting a random spawn location around the player that has line of sight to the player
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
    // Gets a spawn location from the list of spawn markers that has line of sight to the player
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
        //testText.text = $"Level Completed! Total Progress: 100%";
        Debug.Log("Complete level called");
        WinGame();
    }

    private void AssignEscortStages()
    {
        //Debug.Log("Assign Escorts called");
        for(int i = 1; i < stages.Length; i++)
        {
            if (stages[i] is Escort escort1)
            {
                if (stages[i -1] is Escort escort2)
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

    #region ------ Game State Manipulation ------
    public void WinGame()
    {
        HUDController.Instance.WinScreen();
    }

    public void LoseGame()
    {
        HUDController.Instance.LoseScreen();
    }

    public void Restart()
    {
        SceneManager.LoadScene("Level 1 White Box", LoadSceneMode.Single);
    }

    public void Quit()
    {
        Application.Quit();
    }
    #endregion

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
            foreach (Vector3 marker in stage.SpawnMarkers)
            {
                Gizmos.color = new Color(0, 1, 0, 0.5f);
                Gizmos.DrawSphere(marker, spawnSpread);
            }
        }
    }
}
