using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Device : MonoBehaviour
{
    [SerializeField] private float duration;
    [SerializeField] private float range;
    protected float timer = 0f;
    protected bool isActive = false;

    public float Duration 
    { get { return duration; } }
    public float Range 
    { get { return range; } }

    public abstract void ActivateDevice();
}
