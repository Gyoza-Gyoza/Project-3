using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "Escort", menuName = "ScriptableObjects/Stages/Escort", order = 1)]
public class Escort : Stage
{
    [Tooltip("Location of the checkpoint that the payload will travel to")]
    [SerializeField] private Vector3 checkpoint;
    [Tooltip("Speed of the payload")]
    [SerializeField] private float payloadSpeed;
    [Tooltip("If this is a checkpoint")]
    [SerializeField] private bool isCheckpoint;
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
                return PayloadBehaviour.Instance.Agent.remainingDistance;
            else
                return (escortDistance - PayloadBehaviour.Instance.Agent.remainingDistance) / escortDistance; 
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
        //Basically check if the 
        if (PayloadBehaviour.Instance.Agent.hasPath && PayloadBehaviour.Instance.Agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            //Debug.Log("Remaining distance: " + PayloadBehaviour.Instance.Agent.remainingDistance);

            if (!PayloadBehaviour.Instance.Agent.pathPending && PayloadBehaviour.Instance.Agent.remainingDistance <= 0.05f)
            {
                //Debug.Log($"Super Close to point, completing point");
                PayloadBehaviour.Instance.CompleteStage();
            }

        }
        else
        {
            //Debug.LogWarning("Path incomplete or invalid!");
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
        //Eze's note to self
        //This stored progress is very wrong btw. It does not go down when the player goes backwards.
        PayloadBehaviour.Instance.Agent.SetDestination(previousCheckpoint);
    }
    #endregion
}
