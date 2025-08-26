using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Defend", menuName = "ScriptableObjects/Stages/Defend", order = 2)]
public class Defend : Stage
{
    [Tooltip("Time to defend the payload")]
    [SerializeField] private float defendDuration;
    private bool playerInRange = false;
    private float timer;
    public float DefendDuration
    { get { return defendDuration; } }
    public override float Progress     
    { get { return Mathf.Clamp01(timer / defendDuration); } }
    public override void StartStage()
    {
        timer = 0f;
        PayloadBehaviour.Instance.Agent.isStopped = true; // Stops the payload from moving
    }
    public override void DoPayloadBehaviour()
    {
        LevelDirector.Instance.testText.text =
        $"Current Stage is {LevelDirector.Instance.CurrentStage} " +
        $"\n Defense progress is {Progress * 100}% " +
        $"\n Total progress is {LevelDirector.Instance.StageProgress * 100}%"; 
        if (playerInRange)
        {
            timer += Time.deltaTime;
            if (timer >= defendDuration)
            {
                PayloadBehaviour.Instance.CompleteStage();
            }
        }
    }
    public override void PlayerInRange()
    {
        playerInRange = true;
    }
    public override void PlayerOutOfRange()
    {
        playerInRange = false;
    }
}
