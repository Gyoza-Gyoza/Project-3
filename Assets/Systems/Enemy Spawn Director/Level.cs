using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "ScriptableObjects/Level", order = 1)]
public class Level : ScriptableObject
{
    private string levelId;
    [SerializeField] private List<Stage> stages;
    public List<Stage> Stages
    {  get { return stages; } }
}
[System.Serializable]
public class Stage
{
    [Tooltip("Location of the checkpoint that the payload will travel to")]
    [SerializeField] private Vector3 checkpoint;
    [Tooltip("Speed of the payload")]
    [SerializeField] private float payloadSpeed;
    [Tooltip("Speed that the director will try spawning new enemies")]
    [SerializeField] private float spawnFrequency;
    [Tooltip("Amount of enemies that the director will spawn each time")]
    [SerializeField] private float spawnAmount;

    public Vector3 Checkpoint
    { get { return checkpoint; } }
    public float PayloadSpeed
    { get { return payloadSpeed; } }
    public float SpawnFrequency
    { get { return spawnFrequency; } }
    public float SpawnAmount
    { get { return spawnAmount; } }
}
