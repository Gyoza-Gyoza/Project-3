using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Collect", menuName = "ScriptableObjects/Stages/Collect", order = 3)]
public class Collect : Stage
{
    [Tooltip("Item that needs to be collected")]
    [SerializeField] private GameObject itemToCollect;
    private List<GameObject> itemsToCollect = new List<GameObject>();
    [Tooltip("Amount of items to collect")]
    [SerializeField] private int amountToCollect;
    [Tooltip("Amount of items to spawn")]
    [SerializeField] private int amountToSpawn;
    [Tooltip("Max distance from the payload to spawn")]
    [SerializeField] private float maxDistanceToSpawn;

    private int amountCollected = 0;
    public int AmountToCollect
    { get { return amountToCollect; } }
    public int AmountCollected
    { get { return amountCollected; } private set { amountCollected = value; } }
    public override float Progress
    { get { return AmountCollected / AmountToCollect; } }
    public override void StartStage()
    {
        AmountCollected = 0;
        PayloadBehaviour.Instance.Agent.isStopped = true; // Stops the payload from moving

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
        float interactRadius = PayloadBehaviour.Instance.InteractRadius;

        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        float randomDistance = Random.Range(interactRadius, interactRadius + maxDistanceToSpawn);
        Vector3 randomOffset = randomDirection * randomDistance;
        return PayloadBehaviour.Instance.transform.position + randomOffset;
    }
    public void Collected(int amount)
    {
        // Add in sound effects, screen effects, etc.
        AmountCollected = Mathf.Clamp(AmountCollected + amount, 0, amountToCollect);
    }
    public override void DoPayloadBehaviour()
    {
        LevelDirector.Instance.testText.text =
        $"Current Stage is {LevelDirector.Instance.CurrentStage} " +
        $"\n {AmountCollected} / {AmountToCollect} collected " +
        $"\n Total progress is {LevelDirector.Instance.StageProgress * 100}%";
        if (AmountCollected >= AmountToCollect)
        {
            foreach (GameObject item in itemsToCollect)
            {
                GameObjectPool.ReturnObject(item);
            }
            itemsToCollect.Clear();
            PayloadBehaviour.Instance.CompleteStage();
        }
    }
    public override void PlayerInRange()
    {
        AmountCollected += Discon_PlayerController.Instance.DropOffItems();
    }
    public override void PlayerOutOfRange()
    {

    }
}