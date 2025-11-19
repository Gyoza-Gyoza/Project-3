using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

[System.Serializable]
public class PresetStage
{
    public Transform chargerSpawnPoint;
    public int chargerAmountSpawned;
    public Transform drainerSpawnPoint;
    public int drainerAmountSpawned;
}
public class LevelDirector : Singleton<LevelDirector>
{
    [SerializeField] private Level[] levels;
    [SerializeField] public TextMeshProUGUI testText;
    [SerializeField] private GameObject test;
    [SerializeField] private float spawnSpread;
    [SerializeField] private LayerMask environmentMask;
    public int startLevel;
    public bool lost = false;

    private LineRenderer lineRenderer;

    private List<Transform> spawnMarker = new List<Transform>();
    public Level[] Levels
    { get { return levels; } }
    public Level CurrentLevel
    { get { return levels[currentLevelCounter]; } }
    public Stage[] Stages
    { get { return levels[currentLevelCounter].stages; } }
    public Stage CurrentStage
    { get { return levels[currentLevelCounter].stages[currentStageCounter]; } }
    private int currentLevelCounter;
    public int CurrentLevelCounter { get { return currentLevelCounter; } }
    private int currentStageCounter; 
    public int CurrentStageCounter { get { return currentStageCounter; } }

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject playerEnemyPrefab;
    [SerializeField] private GameObject enemySpawnerPrefab;
    [SerializeField] private PresetStage[] presetStages;
    [SerializeField] private Transform[] specialSpawnPoints;
    private int presetStageCounter;
    [SerializeField] private GameObject[] specialEnemyPrefabs;
    [SerializeField] private float specialEnemyChance = 0.1f;
    [SerializeField] private bool canSpawnEnemies = true;
    public bool CanSpawnEnemies { get { return canSpawnEnemies; } set { canSpawnEnemies = value; } }
    private bool spawnCoolingDown = false;
    private bool startCooldown = false;
    private List<GameObject> specialEnemyPool = new List<GameObject>();
    private List<EnemyBehaviour> enemyList = new List<EnemyBehaviour>();
    private int stageEnemyCount = 0;

    private Vector3 currentPosition; // Used for drawing gizmos
    private PayloadBehaviour payload;

    //[SerializeField] private int enemyCount = 0;
    //public int EnemyCount
    //{ get { return enemyCount; } 
    //  set { enemyCount = value; HUDController.Instance.UpdateTotalEnemyCount(enemyCount); }
    //}

    #region --- Level Length and Progress ---
    private float levelLength = 0f;

    private void CalculateLevelLength()
    {
        for (int i = 1; i < levels[currentLevelCounter].stages.Length; i++)
        {
            levels[currentLevelCounter].stages[i].Length = CalculateCheckpointLength(i);
            //Debug.Log($"Checkpoint Length: {stages[i].Length}");
            //Debug.Log($"Current Length: {levelLength}");
            levelLength += levels[currentLevelCounter].stages[i].Length;
        }
    }

    private float CalculateCheckpointLength(int index)
    {
        if (levels[currentLevelCounter].stages[index - 1] is Escort escort1 && levels[currentLevelCounter].stages[index] is Escort escort2)
        {
            return Vector3.Magnitude(escort2.EscortPosition - escort1.EscortPosition);
        }
        return 0f;
    }

    public float StageProgress
    { 
        get 
        {
            // Subtracting by one to ignore the first escortPosition as it's the starting point
            float calc = 0.0001f;
            //Debug.Log($"calc was {calc}");
            //float progressionPerStage = 1f / (Stages.Length - 1);
            for (int i = 1; i < currentStageCounter; i++)
            {
                if (levels[currentLevelCounter].stages[i].Length > 0f)
                {
                    //Debug.Log($"Length is  {stages[i].Length}");
                    calc += levels[currentLevelCounter].stages[i].Length;
                }
                else
                {
                    //Debug.Log("No length");
                    calc += 0f;
                }
            }

            calc += levels[currentLevelCounter].stages[currentStageCounter].StageProgress * levels[currentLevelCounter].stages[currentStageCounter].Length;
            float result = calc / levelLength;

            //Debug.Log($"Stage StageProgress: {result}, calc was {calc}, levelLength was {levelLength}");

            return Mathf.Clamp01(result); 
        } 
    }
    #endregion

    #region ---- Death Zone Manipulation ----

    [Tooltip("Units per second that the death zone moves at")]
    [SerializeField] private float deathZoneRate = 0f;
    [SerializeField] private NavMeshAgent deathZoneChaser;
    [SerializeField] private float deathZoneStartDelay = 3f;
    [SerializeField] private float deathBuffer = 1f;
    [SerializeField] private Animator deathZoneAnimator;
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
            deathZoneChaser.Warp((stages[0] as Escort).EscortPosition); //This is temp. We need to clean up the code and change stages to something else
            deathZoneChaser.SetDestination(escort.EscortPosition);
        }
        deathZoneChaser.speed = deathZoneRate;
        deathZoneChaser.isStopped = false;
        */

        deathZoneChaser.Warp((levels[currentLevelCounter].stages[0] as Escort).EscortPosition);

        dir = (levels[currentLevelCounter].stages[deathZoneStage] as Escort).EscortPosition - (levels[currentLevelCounter].stages[deathZoneStage - 1] as Escort).EscortPosition;

        StartCoroutine(DeathZoneCoroutine());

        deathZoneAnimator.SetTrigger("StartChasing");
    }

    private void StopDeathZone()
    {
        deathZoneActive = false;
    }

    public void DeathCompletedStage()
    {
        Debug.Log("Death completed called");
        deathZoneStage++;
        deathZoneStage = Mathf.Clamp(currentStageCounter, 0, Stages.Length - 1);

        if (deathZoneStage < levels[currentLevelCounter].stages.Length && levels[currentLevelCounter].stages[deathZoneStage] is Escort nextStage)
        {
            //deathZoneChaser.SetDestination(nextStage.EscortPosition);
            dir = (levels[currentLevelCounter].stages[deathZoneStage] as Escort).EscortPosition - (levels[currentLevelCounter].stages[deathZoneStage - 1] as Escort).EscortPosition;
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

            Escort current = levels[currentLevelCounter].stages[deathZoneStage] as Escort;

            Vector3 currentPos = deathZoneChaser.gameObject.transform.position + (dir.normalized * rate * Time.deltaTime);

            deathZoneChaser.gameObject.transform.position = currentPos;

            float distanceLeft = Vector3.Distance(deathZoneChaser.gameObject.transform.position, current.EscortPosition);

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
                Debug.Log($"Lost! Death progress: {(deathZoneTraveled / levelLength)}, Stage StageProgress: {StageProgress}");
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
    }

    private void Start()
    {
        payload = PayloadBehaviour.instance;
        AssignEscortStages();
        CalculateLevelLength();
        HUDController.Instance.SetUpProgressBar(levels[currentLevelCounter].stages, levelLength);
        //lineRenderer = new LineRenderer();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            if (canSpawnEnemies == true)
            {
                canSpawnEnemies = false;
                HUDController.Instance.EnemiesStopped(true);
            }
            else
            {
                canSpawnEnemies = true;
                HUDController.Instance.EnemiesStopped(false);
            }
        }
        if (canSpawnEnemies)
        {
            SpawnEnemies();
            SpawnSpecialEnemies();
        }
        if (Input.GetKeyDown(KeyCode.J)) SpawnPresetEnemies(-1);
        if (Input.GetKeyDown(KeyCode.K)) SpawnPresetEnemies(0);
        if (Input.GetKeyDown(KeyCode.L)) SpawnPresetEnemies(1);
        if (Input.GetKeyDown(KeyCode.F12)) Application.Quit();
        HUDController.Instance.UpdateTimePassed(Time.time);
    }
    #endregion

    #region ---- Enemy Spawning ----

    private float timer;
    private float chargerTimer;
    private int chargerCount; 
    private float drainerTimer;
    private int drainerCount;
    private void SpawnPresetEnemies(int counter)
    {
        PresetStage chosenStage = presetStages[Mathf.Clamp(presetStageCounter += counter, 0, presetStages.Length - 1)];
        for(int i = 0; i < chosenStage.chargerAmountSpawned;  i++)
        {
            GameObjectPool.GetObject(specialEnemyPrefabs[0]).transform.position 
                = chosenStage.chargerSpawnPoint.position;
        }
        for (int i = 0; i < chosenStage.drainerAmountSpawned; i++)
        {
            GameObjectPool.GetObject(specialEnemyPrefabs[1]).transform.position
                = chosenStage.drainerSpawnPoint.position;
        }
    }
    private void SpawnEnemies()
    {
        if (CanSpawn())
        {
            if (spawners != null)
            {
                foreach (EnemySpawn spawn in spawners)
                {
                    if (spawn.isSpawning == true)
                    {
                        for (int i = 0; i < Stages[currentStageCounter].EnemyPerGroup; i++)
                        {
                            GameObject enemyToSpawn = null;
                            // Spawn player targetting variant
                            if (i < Stages[currentStageCounter].PlayerEnemyPerGroup)
                            {  enemyToSpawn = playerEnemyPrefab; }
                            else
                            {  enemyToSpawn = enemyPrefab; }

                            NavMeshAgent enemy = GameObjectPool.GetObject(enemyToSpawn).GetComponent<NavMeshAgent>();

                            if (enemy != null)
                            {
                                while (true)
                                {
                                    Vector3 spawnPos = spawn.gameObject.transform.position + new Vector3(Random.Range(-spawnSpread, spawnSpread), 0, Random.Range(-spawnSpread, spawnSpread));
                                    if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, spawnSpread, NavMesh.AllAreas))
                                    {
                                        enemy.Warp(hit.position);
                                        enemy.gameObject.GetComponent<EnemyBehaviour>().Kickstart();
                                        break;
                                    }
                                }
                            }

                            EnemyBehaviour enemyBehaviour = enemy.gameObject.GetComponent<EnemyBehaviour>();
                            AddEnemy(enemyBehaviour);

                            //Debug.Log("Enemy count being added");
                        }
                    }
                }
            }
            timer = 0f;
        }
    }
    // Use these to contain all logic for counting enemies
    private void AddEnemy(EnemyBehaviour enemy)
    {
        enemyList.Add(enemy);
        HUDController.Instance.UpdateTotalEnemyCount(enemyList.Count);
        stageEnemyCount++;
    }
    public void RemoveEnemy(EnemyBehaviour enemy)
    {
        if (enemyList.Contains(enemy))
        {
            enemyList.Remove(enemy);
            HUDController.Instance.UpdateTotalEnemyCount(enemyList.Count);
            stageEnemyCount--;
        }
    }
    private bool CanSpawn()
    {
        timer += Time.deltaTime;

        if (spawnCoolingDown) // Checks if the spawn is on cooldown
        {
            if(!startCooldown) // Checks if the cooldown can be started
            {
                if ( stageEnemyCount /*enemyList.Count*/ <= Stages[currentStageCounter].ResumeThreshold) // Checks if the enemy count is below the resume threshold and removes the spawn cooldown
                {
                    startCooldown = true;
                    timer = 0f; // Starts the timer when the enemy goes below the threshold
                    //Debug.Log("Enemy count below threshold, starting spawn cooldown");
                }
            }
            else // If it is cooling down, it starts the timer
            {
                if (timer >= levels[currentLevelCounter].stages[CurrentStageCounter].SpawnCooldown)
                {
                    startCooldown = false;
                    spawnCoolingDown = false;
                    timer = 0f;
                }
            }
            //Debug.Log($"Spawning on cooldown, timer: {timer}");
            return false; 
        }
        if (stageEnemyCount /*enemyList.Count */ >= Stages[currentStageCounter].MaxEnemies) // Checks if the enemy count is at max and sets the spawn on cooldown
        {
            spawnCoolingDown = true;
            //Debug.Log("Enemy cap reached, spawning on cooldown");
            return false;
        }
        if (timer <= levels[currentLevelCounter].stages[currentStageCounter].DurationBetweenSpawns) // Checks if the timer has reached the spawn frequency
        {
            //Debug.Log("Waiting for spawn timer");
            return false;
        }
        timer = 0f;
        return true;
    }
    private void SpawnSpecialEnemies()
    {
        if (chargerCount > CurrentStage.ChargerEnemyCount &&
            drainerCount > CurrentStage.DrainerEnemyCount) return;

        chargerTimer = drainerTimer += Time.deltaTime;

        if (chargerTimer >= CurrentStage.ChargerEnemyInterval)
        {
            chargerCount++;
            chargerTimer = 0f;
            for (int i = 0; i < CurrentStage.ChargerEnemyCount; i++)
            {
                GameObject charger = GameObjectPool.GetObject(specialEnemyPrefabs[0]);
                //charger.GetComponent<NavMeshAgent>().Warp(CurrentStage.SpawnMarkers[CurrentStage.ChargerEnemyLocation]);
                foreach(int index in CurrentStage.ChargerEnemyLocation)
                {
                    charger.GetComponent<NavMeshAgent>().Warp(specialSpawnPoints[index].position);
                }
            }
        }
        if (drainerTimer >= CurrentStage.DrainerEnemyInterval)
        {
            drainerCount++; 
            drainerTimer = 0f;
            for (int i = 0; i < CurrentStage.DrainerEnemyCount; i++)
            {
                // GameObjectPool.GetObject(specialEnemyPrefabs[1]).transform.position =
                //    CurrentStage.SpawnMarkers[CurrentStage.DrainerEnemyLocation];

                foreach (int index in CurrentStage.DrainerEnemyLocation)
                {
                    GameObjectPool.GetObject(specialEnemyPrefabs[1]).transform.position =
                    specialSpawnPoints[index].position;
                }
            }
        }
    }

    #region ############################ NOT IN USE #############################
    // Getting a random spawn location around the player that has line of sight to the player
    private bool GetSpawnLocation(out Vector3 randomPosition)
    {
        Vector3 randomDirection = Vector3.zero;
        randomPosition = Vector3.zero;

        for(int i = 0; i < 100; i++)
        {
            randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            randomPosition = PlayerController3P.instance.transform.position + 
                randomDirection * (levels[currentLevelCounter].stages[currentStageCounter].MinSpawnDistance 
                + Random.Range(0f, levels[currentLevelCounter].stages[currentStageCounter].MaxSpawnDistance));
            
            Vector3 vectorToPlayer = PlayerController3P.instance.transform.position - randomPosition;
            float distanceToPlayer = vectorToPlayer.magnitude;
            Debug.DrawLine(randomPosition, PlayerController3P.instance.transform.position, Color.red, 1f);

            if (Physics.Linecast(randomPosition, PlayerController3P.instance.transform.position, environmentMask))
            {
                Debug.DrawLine(randomPosition, PlayerController3P.instance.transform.position, Color.green, 1f);
                return true;
            }
            else
            {
                Debug.DrawLine(randomPosition, PlayerController3P.instance.transform.position, Color.red, 1f);
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
            if (!(Vector3.Distance(PlayerController3P.instance.transform.position, position.position) <= Stages[currentStageCounter].MaxSpawnDistance) ||
                !(Vector3.Distance(PlayerController3P.instance.transform.position, position.position) >= Stages[currentStageCounter].MinSpawnDistance)) continue;

            if (Physics.Linecast(position.position, PlayerController3P.instance.transform.position, environmentMask))
            {
                Debug.DrawLine(position.position, PlayerController3P.instance.transform.position, Color.green, 1f);
                pos = position.position;
                break;
            }
            else
            {
                Debug.DrawLine(position.position, PlayerController3P.instance.transform.position, Color.red, 1f);
                continue;
            }
        }

        return pos;
    }
    #endregion

    #endregion

    public void CompletedStage()
    {
        currentStageCounter++;
        currentStageCounter = Mathf.Clamp(currentStageCounter, 0, Stages.Length);

        ClearDeadSpawners();



        stageEnemyCount = 0;

        chargerCount = 0;
        drainerCount = 0;

        SpawnSpawners();

        //Hardcode to start death zone on 3rd check point
        //if (currentStageCounter == 2)
        //{
        //    StartDeathZone();
        //}

        if (currentStageCounter >= levels[currentLevelCounter].stages.Length)
        {
            CompleteLevel();
        }
    }

    public void CompleteLevel()
    {
        currentLevelCounter++;
        foreach (EnemyBehaviour enemy in enemyList)
        {
            enemy.TakeDamage(enemy.Health);
        }
        foreach (GameObject specialEnemy in specialEnemyPool)
        {
            EnemyBehaviour sp = specialEnemy.GetComponent<EnemyBehaviour>();
            sp.TakeDamage(sp.Health);
        }
        if (currentLevelCounter >= levels.Length)
        {
            WinGame();
        }
        ScreenEffectsManager.Instance.ScreenFade(() => 
        PlayerController3P.instance.transform.position = CurrentLevel.startPoint.position);
    }

    private void AssignEscortStages() // might become obselete when the baymax stops going backwards
    {
        //Debug.Log("Assign Escorts called");
        for(int i = 1; i < levels[currentLevelCounter].stages.Length; i++)
        {
            if (levels[currentLevelCounter].stages[i] is Escort escort1)
            {
                if (levels[currentLevelCounter].stages[i -1] is Escort escort2)
                {   escort1.PreviousStage = escort2.EscortPosition;}
                else
                {   escort1.PreviousStage = escort1.EscortPosition;}
                //Debug.Log($"Escorts point {i}, Previous: {escort1.PreviousStage}, Current: {escort1.EscortPosition}");
            }
        }
    }

    #region --- Enemy Spawner ---
    private List<EnemySpawn> spawners = new List<EnemySpawn>();
    private int deadSpawners = 0;

    public void AddDeadSpawner()    {   deadSpawners++;     }
    public void ClearDeadSpawners() {   deadSpawners = 0;   }


    public bool AreAllSpawnersGone()
    {
        if (spawners == null || spawners.Count == 0 || deadSpawners >= spawners.Count)
        {
            return true;
        } 
        
        return false;
    }
    private void SpawnSpawners()
    {
        if (spawners != null && spawners.Count > 0)
        {
            foreach (EnemySpawn e in spawners)
            {
                GameObject.Destroy(e.gameObject);
            }
            spawners.Clear();
        }

        foreach (Vector3 spawn in levels[currentLevelCounter].stages[currentStageCounter].SpawnMarkers)
        {
            EnemySpawn just = GameObject.Instantiate(enemySpawnerPrefab, spawn, Quaternion.identity).GetComponent<EnemySpawn>();
            spawners.Add(just);
            just.StartSpawning();
        }

        SpawnEnemies();
        SpawnSpecialEnemies();
    }
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


    [SerializeField] private int respawnDamage = 20;

    public void Respawn()
    {
        Debug.Log("Respawning");
        PlayerController3P.instance.ForceWarp(Stages[currentStageCounter].Respawn);
        PlayerController3P.instance.TakeDamage(respawnDamage);
        //PlayerController3P.instance.gameObject.transform.rotation = Stages[currentStageCounter].Respawn.rotation;
    }

    #endregion

    // REMEMEBER TO COMMENT OUT GIZMOS WHEN BUILDING
    ///*
    private void OnDrawGizmos()
    {

        int[] specialSpawnsSelected_Drainer = null;
        int[] specialSpawnsSelected_Charger = null;

        foreach (Stage stage in Stages)
        {
            switch (stage)
            {
                case Escort escort:
                    currentPosition = escort.EscortPosition;

                    if (Selection.Contains(stage))
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireSphere(currentPosition, 7f);
                    }
                    else
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(currentPosition, 5f);
                    }




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

            if (stage?.SpawnMarkers != null && stage.SpawnMarkers.Length > 0)
            {
                if (Selection.Contains(stage))
                {
                    Gizmos.color = new Color(0, 1, 0, 0.5f);
                }
                else
                {
                    Gizmos.color = new Color(1, 0, 0, 0.5f);
                }

                foreach (Vector3 marker in stage.SpawnMarkers)
                {
                    //Gizmos.color = new Color(0, 1, 0, 0.5f);
                    Gizmos.DrawSphere(marker, spawnSpread);
                }
            }
            if (stage.Respawn != null)
            {
                if (Selection.Contains(stage))
                {
                    Gizmos.color = new Color(0, 0, 1, 0.7f);
                }
                else
                {
                    Gizmos.color = new Color(1, 0, 1, 0.7f);
                }

                Gizmos.DrawCube(stage.Respawn, new Vector3(5f,5f,5f));
            }
            if (stage.DrainerEnemyLocation != null)
            {
                if (Selection.Contains(stage))
                {
                    specialSpawnsSelected_Drainer = stage.DrainerEnemyLocation;
                    specialSpawnsSelected_Charger = stage.ChargerEnemyLocation;
                }
            }
        }

        /*
        if (presetStages != null && presetStages.Length > 0)
        {
            foreach (PresetStage ps in presetStages)
            {
                if (ps.drainerSpawnPoint != null && ps.chargerSpawnPoint != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(ps.chargerSpawnPoint.position, 3f);
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(ps.drainerSpawnPoint.position, 3f);
                }
            }
        }
        */

        if (specialSpawnPoints != null && specialSpawnPoints.Length > 0)
        {
            for (int i = 0; i < specialSpawnPoints.Length; i++)
            {
                if (specialSpawnsSelected_Drainer != null && specialSpawnsSelected_Charger != null)
                {
                    if (specialSpawnsSelected_Drainer.Contains<int>(i) || specialSpawnsSelected_Charger.Contains<int>(i))
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(specialSpawnPoints[i].position, 5f);
                    }
                }
                else
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(specialSpawnPoints[i].position, 3f);
                }

            }
        }

    }

    //*/
}
