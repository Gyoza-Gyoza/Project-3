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
    [SerializeField] private float specialEnemyChance;
    [SerializeField] private bool[] specialEnemiesIncluded;
    [SerializeField] private Vector3 respawn;
    private float progress = 0f;
    private float lengthFromPrevious;

    public float DurationBetweenSpawns
    { get { return durationBetweenSpawns; } }
    public int EnemyPerGroup
    { get { return enemyPerGroup; } }
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
    public float SpecialEnemyChance
    { get { return specialEnemyChance; } }
    public bool[] SpecialEnemiesIncluded
    { get { return specialEnemiesIncluded; } }
    public Vector3 Respawn
    { get { return respawn; } }

    public abstract void StartStage();
    public abstract void DoPayloadBehaviour();
    public abstract void PlayerInRange();
    public abstract void PlayerOutOfRange();
}
