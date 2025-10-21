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
    public bool lost = false;

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
    [SerializeField] private GameObject[] specialEnemyPrefabs;
    [SerializeField] private float specialEnemyChance = 0.1f;
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
            stages[i].Distance = calculateCheckpointLength(i);
            levelLength += stages[i].Distance;
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
                calc += stages[i].Distance;
            }

            calc += stages[currentStage].Progress * stages[currentStage].Distance;
            float result = calc / levelLength;

            //Debug.Log($"Stage Progress: {result}");

            return Mathf.Clamp01(result); 
        } 
    }

    #region ---- Death Zone Manipulation ----

    [Tooltip("Units per second that the death zone moves at")]
    [SerializeField] private float deathZoneRate = 0f;
    [SerializeField] private NavMeshAgent deathZoneChaser;
    [SerializeField] private float deathZoneStartDelay = 3f;
    [SerializeField] private float deathBuffer = 1f;
    private Vector3 dir = Vector3.zero;
    private float deathZoneTraveled = 0f; 
    private bool deathZoneActive = false;
    private bool deathCountdown = false;
    private int deathZoneStage = 1; 


    private void StartDeathZone()
    {
        /*
        if (stages[1] is Escort escort)
        {
            deathZoneChaser.Warp((stages[0] as Escort).Checkpoint); //This is temp. We need to clean up the code and change stages to something else
            deathZoneChaser.SetDestination(escort.Checkpoint);
        }
        deathZoneChaser.speed = deathZoneRate;
        deathZoneChaser.isStopped = false;
        */

        deathZoneChaser.Warp((stages[0] as Escort).Checkpoint);

        dir = (stages[deathZoneStage] as Escort).Checkpoint - (stages[deathZoneStage - 1] as Escort).Checkpoint;

        StartCoroutine(DeathZoneCoroutine());
    }

    private void StopDeathZone()
    {
        deathZoneActive = false;
    }

    public void DeathCompletedStage()
    {
        Debug.Log("Death completed called");
        deathZoneStage++;
        deathZoneStage = Mathf.Clamp(currentStage, 0, Stages.Length - 1);

        if (deathZoneStage < stages.Length && stages[deathZoneStage] is Escort nextStage)
        {
            //deathZoneChaser.SetDestination(nextStage.Checkpoint);
            dir = (stages[deathZoneStage] as Escort).Checkpoint - (stages[deathZoneStage - 1] as Escort).Checkpoint;
        }
        else
        {
            //LoseGame();
        }
    }

    IEnumerator DeathZoneCoroutine()
    {
        //float count = 0f;
        deathZoneActive = true;
        float rate = deathZoneRate / 1f;
        float previousRemaining = deathZoneChaser.remainingDistance;

        //Debug.Log($"DeathZone STARTED: previous is {previousRemaining}");

        while (deathZoneActive)
        {
            //count += Time.deltaTime;

            deathZoneTraveled += rate * Time.deltaTime;

            //Escort previous = stages[deathZoneStage - 1] as Escort;

            Escort current = stages[deathZoneStage] as Escort;

            Vector3 currentPos = deathZoneChaser.gameObject.transform.position + (dir.normalized * rate * Time.deltaTime);

            deathZoneChaser.gameObject.transform.position = currentPos;

            float distanceLeft = Vector3.Distance(deathZoneChaser.gameObject.transform.position, current.Checkpoint);

            //Debug.Log($"Death distance left: {distanceLeft}");

            if ( distanceLeft <= 0.6f)
            {
                DeathCompletedStage();
            }

            float progress = Mathf.Clamp( (deathZoneTraveled / levelLength), 0f, 1f);

            if (progress != 0f && progress != 1f &&  progress >= StageProgress && !deathCountdown)
            {
                StartCoroutine(DeathCountdown());
            }
            else
            {
                deathCountdown = false;
            }

            UpdateDeathZone();

            yield return new WaitForSeconds(Time.deltaTime);
        }
        yield break;
    }

    IEnumerator DeathCountdown()
    {
        float count = 0f;
        deathCountdown = true;

        while(deathCountdown)
        {
            count += Time.deltaTime;
            if (count >= deathBuffer)
            {
                Debug.Log($"Lost! Death progress: {(deathZoneTraveled / levelLength)}, Stage Progress: {StageProgress}");
                LoseGame();
            }
            yield return new WaitForSeconds(Time.deltaTime);
        }


        yield break;
    }


    private void UpdateDeathZone()
    {
        HUDController.Instance.SetDeathBar(deathZoneTraveled / levelLength);
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

    #region ---- Enemy Spawning ----

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
                    GameObject enemyToSpawn = enemyPrefab;
                    if (Random.Range(0, 1f) <= specialEnemyChance) enemyToSpawn = specialEnemyPrefabs[Random.Range(0, specialEnemyPrefabs.Length - 1)];
                    NavMeshAgent enemy = GameObjectPool.GetObject(enemyToSpawn).GetComponent<NavMeshAgent>();
                    enemy.Warp(marker + new Vector3(Random.Range(-spawnSpread, spawnSpread), 0, Random.Range(-spawnSpread, spawnSpread)));
                    EnemyCount += 1;
                    Debug.Log("Enemy count being added");
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
                    //Debug.Log("Enemy count below threshold, starting spawn cooldown");
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
            //Debug.Log($"Spawning on cooldown, timer: {timer}");
            return false; 
        }
        if (enemyCount >= Stages[currentStage].MaxEnemies) // Checks if the enemy count is at max and sets the spawn on cooldown
        {
            spawnCoolingDown = true;
            //Debug.Log("Enemy cap reached, spawning on cooldown");
            return false;
        }
        if (timer <= stages[currentStage].DurationBetweenSpawns) // Checks if the timer has reached the spawn frequency
        {
            //Debug.Log("Waiting for spawn timer");
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
    #endregion

    public void CompletedStage()
    {
        currentStage++;
        currentStage = Mathf.Clamp(currentStage, 0, Stages.Length);
        if (currentStage == 2)
        {
            StartDeathZone();
        }
    }

    public void CompleteLevel()
    {
        // Handle level completion logic here
        //testText.text = $"Level Completed! Total Progress: 100%";
        Debug.Log("Complete level called");
        WinGame();
    }

    private void AssignEscortStages() // might become obselete when the baymax stops going backwards
    {
        //Debug.Log("Assign Escorts called");
        for(int i = 1; i < stages.Length; i++)
        {
            if (stages[i] is Escort escort1)
            {
                if (stages[i -1] is Escort escort2)
                {   escort1.PreviousCheckpoint = escort2.Checkpoint;}
                else
                {   escort1.PreviousCheckpoint = escort1.Checkpoint;}
                //Debug.Log($"Escorts point {i}, Previous: {escort1.PreviousCheckpoint}, Current: {escort1.Checkpoint}");
            }
        }
    }

    #region 



    #endregion

    #region ------ Game State Manipulation ------
    public void WinGame()
    {
        HUDController.Instance.WinScreen();
    }

    public void LoseGame()
    {
        StopDeathZone();
        HUDController.Instance.LoseScreen();
        lost = true;
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
    }

    public void Restart()
    {
        lost = false;
        SceneManager.LoadScene("Level 1 White Box", LoadSceneMode.Single);
    }

    public void Quit()
    {
        Debug.Log("Quitted game");
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

            if (stage?.SpawnMarkers != null)
            {
                if (stage.SpawnMarkers.Length > 0)
                {
                    foreach (Vector3 marker in stage.SpawnMarkers)
                    {
                        Gizmos.color = new Color(0, 1, 0, 0.5f);
                        Gizmos.DrawSphere(marker, spawnSpread);
                    }
                }
            }

        }
    }
}
