using UnityEngine;

[DisallowMultipleComponent]
public class PayloadAnimEventForwarder : MonoBehaviour
{
    [SerializeField] private PayloadBehaviour payload; // drag if needed

    void Awake()
    {
        if (!payload)
            payload = GetComponentInParent<PayloadBehaviour>();
    }

    // Animation Events (match the event names in clips)
    public void StepStart_L() { if (payload) payload.StepStart_L(); }
    public void StepEnd_L() { if (payload) payload.StepEnd_L(); }
    public void StepStart_R() { if (payload) payload.StepStart_R(); }
    public void StepEnd_R() { if (payload) payload.StepEnd_R(); }

    public void PlayStepRightSFX() { if (payload) payload.PlayStepRightSFX(); }
    public void PlayStepLeftSFX() { if (payload) payload.PlayStepLeftSFX(); }
    public void PlayStopSFX() { if (payload) payload.PlayStopSFX(); }
    public void PlayPreStopSFX() { if (payload) payload.PlayPreStopSFX(); }
    
}
