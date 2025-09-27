using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Escort", menuName = "ScriptableObjects/Stages/Escort", order = 1)]
public class Escort : Stage
{
    [Tooltip("Location of the checkpoint that the payload will travel to")]
    [SerializeField] private Vector3 checkpoint;
    [Tooltip("Speed of the payload")]
    [SerializeField] private float payloadSpeed;
    private float escortDistance;
    private Vector3 previousCheckpoint;
    public Vector3 PreviousCheckpoint
    {
        get { return previousCheckpoint; }
        set { previousCheckpoint = value; }
    }
    public Vector3 Checkpoint
    { get { return checkpoint; } }
    public float PayloadSpeed
    { get { return payloadSpeed; } }
    public float EscortDistance
    {
        get { return escortDistance; }
        set { escortDistance = value; }
    }
    public override float Progress
    { get 
        {
            if (PayloadBehaviour.Instance.Agent.destination == checkpoint)
                return (escortDistance - PayloadBehaviour.Instance.Agent.remainingDistance) / escortDistance;
            else
                return storedProgress;
        } 
    }

    private float storedProgress;

    public override void StartStage()
    {
        FaceForward();
    }
    public override void DoPayloadBehaviour()
    {
        HUDController.Instance.SetProgressBar(LevelDirector.Instance.StageProgress);
        if (PayloadBehaviour.Instance.Agent.remainingDistance <= 0.05f)
        {
            PayloadBehaviour.Instance.CompleteStage();
        }
    }

    public override void PlayerInRange()
    {
        PayloadBehaviour.Instance.StartFillingGas();
    }
    public override void PlayerOutOfRange()
    {
        PayloadBehaviour.Instance.StopFillingGas();
    }


    private void Update()
    {

    }

    #region Facing
    public void FaceForward()
    {
        Debug.Log("Facing Forward");
        storedProgress = 0;
        PayloadBehaviour.Instance.Agent.SetDestination(Checkpoint);
    }

    public void FaceBackwards()
    {
        Debug.Log("Facing Backward");
        storedProgress = (escortDistance - PayloadBehaviour.Instance.Agent.remainingDistance) / escortDistance;
        PayloadBehaviour.Instance.Agent.SetDestination(previousCheckpoint);
    }
    #endregion
}
