using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "Escort", menuName = "ScriptableObjects/Stages/Escort", order = 1)]
public class Escort : Stage
{
    [Tooltip("Location of the escortPosition that the payload will travel to")]
    [SerializeField] private Vector3 escortPosition;
    [Tooltip("Speed of the payload")]
    [SerializeField] private float payloadSpeed;
    private float escortDistance;
    private Vector3 previousStage;
    public Vector3 PreviousStage
    {
        get { return previousStage; }
        set { previousStage = value; }
    }
    public Vector3 EscortPosition
    { get { return escortPosition; } }
    public float PayloadSpeed
    { get { return payloadSpeed; } }
    public float EscortDistance
    {
        get { return escortDistance; }
        set { escortDistance = value; }
    }
    public override float StageProgress
    { get 
        {
            //I think its inefficient since this is being called like almost every tick.
            //Its either calculating like crazy or storing the progress like crazy

            if (PayloadBehaviour.Instance.Agent.destination == escortPosition)
            {
                Debug.Log("Position is correct");
                return PayloadBehaviour.Instance.Agent.remainingDistance;
            }

            else
            {
                Debug.Log($"Position is incorrect, {escortDistance}, {PayloadBehaviour.Instance.Agent.remainingDistance}");

                return (escortDistance - PayloadBehaviour.Instance.Agent.remainingDistance) / escortDistance; 

            }
        } 
    }

    //private float storedProgress;

    public override void StartStage()
    {
        //FaceForward();
        PayloadBehaviour.Instance.Agent.SetDestination(EscortPosition);
    }
    public override void DoPayloadBehaviour()
    {
        HUDController.Instance.SetProgressBar(LevelDirector.Instance.StageProgress);
        //Basically check if the 
        if (PayloadBehaviour.Instance.Agent.hasPath && PayloadBehaviour.Instance.Agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            Debug.Log("Remaining distance: " + PayloadBehaviour.Instance.Agent.remainingDistance);

            if (!PayloadBehaviour.Instance.Agent.pathPending && PayloadBehaviour.Instance.Agent.remainingDistance <= 1f)
            {
                //Debug.Log($"Super Close to point, completing point");
                PayloadBehaviour.Instance.CompleteStage();
            }

        }
        else
        {
            Debug.LogWarning("Path incomplete or invalid!");
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

    //Kind of obsolete

    //#region Facing
    //public void FaceForward()
    //{
    //    Debug.Log("Facing Forward");
    //    storedProgress = 0;
    //    PayloadBehaviour.Instance.Agent.SetDestination(EscortPosition);
    //}

    //public void FaceBackwards()
    //{
    //    Debug.Log("Facing Backward");
    //    storedProgress = (escortDistance - PayloadBehaviour.Instance.Agent.remainingDistance) / escortDistance;
    //    //Eze's note to self
    //    //This stored progress is very wrong btw. It does not go down when the player goes backwards.
    //    PayloadBehaviour.Instance.Agent.SetDestination(previousStage);
    //}
    //#endregion

}
