using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StatTracking 
{
    private static float payloadProgression;
    public static float PayloadProgression
    { 
        get { return payloadProgression; } 
        set { payloadProgression = Mathf.Clamp01(value); }
    }
}
