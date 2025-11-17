using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Collect", menuName = "ScriptableObjects/Stages/Collect", order = 3)]
public class Collect : Stage
{
    [SerializeField] private Vector3[] itemSpawnLocations;
    private List<Vector3> validSpawnLocations = new();
    [Tooltip("Item that needs to be collected")]
    [SerializeField] private GameObject itemToCollect;
    private List<GameObject> itemsToCollect = new List<GameObject>();
    [Tooltip("Amount of items to collect")]
    [SerializeField] private int amountToCollect;
    [Tooltip("Amount of items to spawn")]
    [SerializeField] private int amountToSpawn;

    private int amountCollected = 0;
    public int AmountToCollect
    { get { return amountToCollect; } }
    public int AmountCollected
    { get { return amountCollected; } private set { amountCollected = value; } }
    public override float StageProgress
    { get { return AmountCollected / AmountToCollect; } }
    public override void StartStage()
    {
        AmountCollected = 0;
        PayloadBehaviour.instance.Agent.isStopped = true; // Stops the payload from moving

        foreach (Vector3 vector3 in itemSpawnLocations) validSpawnLocations.Add(vector3);

        SpawnItems(amountToSpawn);
    }
    private void SpawnItems(int amount)
    {
        for (int i = 0; i < amountToSpawn; i++)
        {
            GameObject item = GameObjectPool.GetObject(itemToCollect);
            item.transform.position = GetLocation();
            itemsToCollect.Add(item);
        }
    }
    private Vector3 GetLocation()
    {
        Vector3 location = new();
        int randomIndex = Random.Range(0, itemSpawnLocations.Length);
        location = validSpawnLocations[randomIndex];
        validSpawnLocations.RemoveAt(randomIndex);
        
        return location;
    }
    public void Collected(int amount)
    {
        // Add in sound effects, screen effects, etc.
        AmountCollected = Mathf.Clamp(AmountCollected + amount, 0, amountToCollect);
    }
    public override void DoPayloadBehaviour()
    {
        LevelDirector.Instance.testText.text =
        $"Current Stage is {LevelDirector.Instance.CurrentStageCounter} " +
        $"\n {AmountCollected} / {AmountToCollect} collected " +
        $"\n Total progress is {LevelDirector.Instance.StageProgress * 100}%";
        if (AmountCollected >= AmountToCollect)
        {
            foreach (GameObject item in itemsToCollect)
            {
                GameObjectPool.ReturnObject(item);
            }
            itemsToCollect.Clear();
            PayloadBehaviour.instance.CompleteStage();
        }
    }
    public override void PlayerInRange()
    {
        AmountCollected += PlayerController3P.instance.DropOffItems();
    }
    public override void PlayerOutOfRange()
    {

    }
}