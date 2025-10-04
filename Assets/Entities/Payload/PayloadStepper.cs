using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Non-root-motion stepper for payload/golem.
/// Attach to same GameObject as NavMeshAgent and Animator.
/// It alternates left/right steps (triggering animator) and lerps the transform to the sampled navmesh position.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class PayloadStepper : MonoBehaviour
{
    [Header("Step tuning")]
    public float stepLength = 1.0f;         // desired step length in world units
    public float stepDuration = 0.35f;      // how long the physical step interpolation takes
    public float stepCooldown = 0.18f;      // pause after completing a step (golem delay)
    public float sampleMaxDistance = 1.2f;  // NavMesh.SamplePosition fallback radius
    public float minSteerDistance = 0.15f;  // if steering target is closer than this, skip stepping

    [Header("Animator")]
    public Animator animator;               // assign Animator (child or same object)
    public string leftTrigger = "StepLeft";
    public string rightTrigger = "StepRight";
    public bool setWalkingBool = true;
    public string walkingBoolName = "Walking";

    [Header("Behavior")]
    public bool alternateSteps = true;      // alternate left / right; if false will always use leftTrigger
    public int navAreaMask = -1;            // NavMesh.AllAreas by default (-1)

    NavMeshAgent agent;
    bool isStepping = false;
    bool nextLeft = true;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // We'll handle transform movement ourselves.
        agent.updatePosition = false;
        agent.updateRotation = false;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (setWalkingBool && animator != null)
            animator.SetBool(walkingBoolName, false);
    }

    void Update()
    {
        // Only step when agent has a path and it's allowed (not paused/stopped)
        if (isStepping) return;
        if (agent == null) return;
        if (agent.pathPending) return;
        if (!agent.hasPath) 
        {
            // clear walking bool if nothing to do
            if (setWalkingBool && animator != null) animator.SetBool(walkingBoolName, false);
            return;
        }

        // Do not step if agent is explicitly stopped (PayloadBehaviour controls isStopped)
        if (agent.isStopped) 
        {
            if (setWalkingBool && animator != null) animator.SetBool(walkingBoolName, false);
            return;
        }

        // Enough distance to the steering target?
        Vector3 toSteer = agent.steeringTarget - transform.position;
        toSteer.y = 0f;
        if (toSteer.sqrMagnitude < minSteerDistance * minSteerDistance) return;

        // Start the step toward steering target
        StartCoroutine(DoStep(toSteer.normalized));
    }

    IEnumerator DoStep(Vector3 dir)
    {
        isStepping = true;

        // set walking flag
        if (setWalkingBool && animator != null) animator.SetBool(walkingBoolName, true);

        // compute a desired point and clamp to navmesh
        Vector3 desired = transform.position + dir * stepLength;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(desired, out hit, sampleMaxDistance, navAreaMask))
        {
            desired = hit.position;
        }
        else
        {
            // fallback to a shorter step if sample fails
            if (NavMesh.SamplePosition(transform.position + dir * (stepLength * 0.35f), out hit, sampleMaxDistance, navAreaMask))
                desired = hit.position;
            else
            {
                // can't find a valid step
                if (setWalkingBool && animator != null) animator.SetBool(walkingBoolName, false);
                isStepping = false;
                yield break;
            }
        }

        // determine which trigger to use
        string trigger = leftTrigger;
        if (alternateSteps)
        {
            trigger = nextLeft ? leftTrigger : rightTrigger;
            nextLeft = !nextLeft;
        }

        // trigger animation
        if (animator != null && !string.IsNullOrEmpty(trigger))
            animator.SetTrigger(trigger);

        // interpolate transform position + rotation (non-root motion)
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = startRot;
        Vector3 look = dir; look.y = 0f;
        if (look.sqrMagnitude > 0.001f) targetRot = Quaternion.LookRotation(look);

        Vector3 startPos = transform.position;
        float elapsed = 0f;
        while (elapsed < stepDuration)
        {
            float t = elapsed / stepDuration;
            transform.position = Vector3.Lerp(startPos, desired, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // finalize
        transform.position = desired;
        transform.rotation = targetRot;

        // sync navmesh agent internal state so pathfinding continues properly
        agent.nextPosition = transform.position;

        // step cooldown for that clunky feel
        yield return new WaitForSeconds(stepCooldown);

        // optional: if path finished or agent is stopped, clear walking bool
        if (setWalkingBool && animator != null)
            animator.SetBool(walkingBoolName, !agent.isStopped && agent.hasPath);

        isStepping = false;
    }

    // Allow external code to set destination; this keeps stage code unchanged
    public void SetDestination(Vector3 worldTarget)
    {
        agent.SetDestination(worldTarget);
        // make sure walking bool is set (it will be used when stepping)
        if (setWalkingBool && animator != null) animator.SetBool(walkingBoolName, true);
    }

    public void StopStepperImmediate()
    {
        // stop stepping and pause the agent
        agent.isStopped = true;
        isStepping = false;
        if (setWalkingBool && animator != null) animator.SetBool(walkingBoolName, false);
    }

    public void ResumeStepper()
    {
        agent.isStopped = false;
        if (setWalkingBool && animator != null) animator.SetBool(walkingBoolName, true);
    }
}
