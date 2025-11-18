using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public abstract class Stage : ScriptableObject
{
    [Tooltip("If this is a escortPosition")]
    [SerializeField] private bool isCheckpoint;
    [Tooltip("The amount of time the director will wait before spawning the next group")]
    [SerializeField] private float durationBetweenSpawns;
    [Tooltip("Amount of enemies that the director will spawn each time")]
    [SerializeField] private int enemyPerGroup;
    [Tooltip("Amongst the group, how many will be player directed")]
    [SerializeField] private int playerEnemyPerGroup;
    [Tooltip("List of spawn points for the escortPosition")]
    [SerializeField] private Vector3[] spawnMarkers;
    [Tooltip("Cooldown for spawning after hitting cap")]
    [SerializeField] private float spawnCooldown;
    [Tooltip("Maximum amount of enemies allowed in the scene before stopping spawns")]
    [SerializeField] private int maxEnemies;
    [Tooltip("Amount of enemies allowed in the scene before resuming spawns")]
    [SerializeField] private int resumeThreshold;
    [SerializeField] private float minSpawnDistance;
    [SerializeField] private float maxSpawnDistance;
    [SerializeField] private float chargerEnemyInterval;
    [SerializeField] private int chargerEnemyCount, chargerEnemyWaves;
    [SerializeField] private int[] chargerEnemyLocation;
    [SerializeField] private float drainerEnemyInterval;
    [SerializeField] private int drainerEnemyCount, drainerEnemyWaves;
    [SerializeField] private int[] drainerEnemyLocation;
    [SerializeField] private Vector3 respawn;
    private float progress = 0f;
    private float lengthFromPrevious;

    public float DurationBetweenSpawns
    { get { return durationBetweenSpawns; } }
    public int EnemyPerGroup
    { get { return enemyPerGroup; } }
    public int PlayerEnemyPerGroup
    { get {return playerEnemyPerGroup ; } }
    public Vector3[] SpawnMarkers
    { get { return spawnMarkers; } }
    public float SpawnCooldown
    { get { return spawnCooldown; } }
    public int MaxEnemies
    { get { return maxEnemies; } }
    public int ResumeThreshold
    { get { return resumeThreshold; } }
    public float MaxSpawnDistance
    { get { return maxSpawnDistance; } }
    public float MinSpawnDistance
    { get { return minSpawnDistance; } }
    public virtual float StageProgress
    { get { return progress; } private set { progress = Mathf.Clamp01(value); } } // Default progression, can be overridden by specific stages
    public virtual float Length
    { get { return lengthFromPrevious; } set { lengthFromPrevious = value; }}
    public bool IsCheckpoint
    { get { return isCheckpoint; }}
    public float ChargerEnemyInterval
    {  get { return chargerEnemyInterval; }}
    public int ChargerEnemyCount
    { get { return chargerEnemyCount; }}
    public int ChargerEnemyWaves
    {  get { return chargerEnemyWaves; }}
    public int[] ChargerEnemyLocation
    {  get { return chargerEnemyLocation; }}
    public float DrainerEnemyInterval
    {  get { return drainerEnemyInterval; }}
    public int DrainerEnemyCount
    { get { return drainerEnemyCount; }}
    public int DrainerEnemyWaves
    {  get { return drainerEnemyWaves; }}
    public int[] DrainerEnemyLocation
    {  get { return drainerEnemyLocation; }}
    public Vector3 Respawn
    { get { return respawn; } }

    public abstract void StartStage();
    public abstract void DoPayloadBehaviour();
    public abstract void PlayerInRange();
    public abstract void PlayerOutOfRange();
}
