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

            if (PayloadBehaviour.instance.Agent.destination == escortPosition)
            {
                //Debug.Log("Position is correct");
                return PayloadBehaviour.instance.Agent.remainingDistance;
            }

            else
            {
                //Debug.Log($"Position is incorrect, {escortDistance}, {PayloadBehaviour.instance.Agent.remainingDistance}");

                return (escortDistance - PayloadBehaviour.instance.Agent.remainingDistance) / escortDistance; 

            }
        } 
    }

    //private float storedProgress;

    public override void StartStage()
    {
        //FaceForward();
        PayloadBehaviour.instance.Agent.SetDestination(EscortPosition);
        PayloadBehaviour.instance.MovementSpeed = payloadSpeed;
    }
    public override void DoPayloadBehaviour()
    {
        HUDController.Instance.SetProgressBar(LevelDirector.Instance.StageProgress);
        //Basically check if the 
        if (PayloadBehaviour.instance.Agent.hasPath && PayloadBehaviour.instance.Agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            //Debug.Log("Remaining distance: " + PayloadBehaviour.instance.Agent.remainingDistance);

            if (!PayloadBehaviour.instance.Agent.pathPending && PayloadBehaviour.instance.Agent.remainingDistance <= 1f)
            {
                //Debug.Log($"Super Close to point, completing point");
                PayloadBehaviour.instance.CompleteStage();
            }

        }
        else
        {
            Debug.LogWarning("Path incomplete or invalid! (Do Payload Behaviour)");
        }

    }

    public override void PlayerInRange()
    {
        /* //=== EZE'S GAS REMOVE EDIT ===
        PayloadBehaviour.instance.StartFillingGas();
        */
    }
    public override void PlayerOutOfRange()
    {
        /* //=== EZE'S GAS REMOVE EDIT ===
        PayloadBehaviour.instance.StopFillingGas();
        */
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
    //    PayloadBehaviour.instance.Agent.SetDestination(EscortPosition);
    //}

    //public void FaceBackwards()
    //{
    //    Debug.Log("Facing Backward");
    //    storedProgress = (escortDistance - PayloadBehaviour.instance.Agent.remainingDistance) / escortDistance;
    //    //Eze's note to self
    //    //This stored progress is very wrong btw. It does not go down when the player goes backwards.
    //    PayloadBehaviour.instance.Agent.SetDestination(previousStage);
    //}
    //#endregion

}
