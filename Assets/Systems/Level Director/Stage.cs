using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Stage : ScriptableObject
{
    [Tooltip("Speed that the director will try spawning new enemies")]
    [SerializeField] private float spawnFrequency;
    [Tooltip("Amount of enemies that the director will spawn each time")]
    [SerializeField] private float maxSpawnAmount;
    private float progress = 0f;

    public float SpawnFrequency
    { get { return spawnFrequency; } }
    public float MaxSpawnAmount
    { get { return maxSpawnAmount; } }
    public virtual float Progress
    { get { return progress; } private set { progress = Mathf.Clamp01(value); } } // Default progression, can be overridden by specific stages
    public abstract void StartStage();
    public abstract void DoPayloadBehaviour();
    public abstract void PlayerInRange();
    public abstract void PlayerOutOfRange();
}