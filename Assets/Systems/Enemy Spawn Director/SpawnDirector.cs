using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnDirector : MonoBehaviour
{
    [SerializeField] private Level currentLevel;
    public Level CurrentLevel
    { get { return currentLevel; } }
    private int currentStage; 
    public int CurrentStage { get { return currentStage; } }

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject enemyPrefab;

    private float timer;

    private static SpawnDirector instance;
    public static SpawnDirector Instance
    {
        get
        {
            // Ensures that there's always a reference to the singleton instance. 
            // This finds and returns the script if it is in the scene and creates one if it is not.
            if (instance == null)
            {
                instance = FindObjectOfType<SpawnDirector>();
                if (instance == null)
                {
                    GameObject singletonObject = new GameObject();
                    instance = singletonObject.AddComponent<SpawnDirector>();
                    singletonObject.name = typeof(SpawnDirector).Name + " (Singleton)";
                }
            }
            return instance;
        }
    }
    private void Awake()
    {
        if (instance == null)
        {
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Update()
    {
        timer += Time.deltaTime * currentLevel.Stages[currentStage].SpawnFrequency;

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
        currentStage = Mathf.Clamp(currentStage, 0, currentLevel.Stages.Count - 1);
        Debug.Log($"Reached checkpoint, stage count is {currentStage}");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach(Stage stage in currentLevel.Stages)
        {
            Gizmos.DrawSphere(stage.Checkpoint, 1f);
        }
    }
}
