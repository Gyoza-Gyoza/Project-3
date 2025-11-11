using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

//Directory to all the singleton scripts.
public class Singles
{
    public PlayerController pc { get; private set; }
    public bool SetPC(PlayerController toSet)
    {
        if (pc == null) 
        { 
            pc = toSet; 
            Debug.Log("PC Single was empty. Setting."); 
            return true; 
        }
        else if (pc == toSet) 
        {
            Debug.Log("PC Single was the same as input. No change."); 
            return true; 
        }
        else 
        { 
            Debug.Log("PC Single already filled. Destroying Extra.");
            GameObject.Destroy(toSet.gameObject);
            return false; 
        }
    }
    public HUDController hud   { get; private set; }
    public bool SetHUD(HUDController toSet)
    {
        if (hud == null)
        {
            hud = toSet;
            Debug.Log("PC Single was empty. Setting.");
            return true;
        }
        else if (hud == toSet)
        {
            Debug.Log("PC Single was the same as input. No change.");
            return true;
        }
        else
        {
            Debug.Log("PC Single already filled. Destroying Extra.");
            GameObject.Destroy(toSet.gameObject);
            return false;
        }
    }


}
