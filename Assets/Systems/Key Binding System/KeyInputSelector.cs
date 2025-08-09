using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KeyInputSelector : MonoBehaviour
{
    public KeyInput keyInput;
    private KeyCode keyCode;
    public KeyCode KeyCode
    {  
        get { return keyCode; } 
        set 
        { 
            keyCode = value;
            keyText.text = value.ToString();
        }
    }
    [HideInInspector]
    public TextMeshProUGUI keyText;

    private bool setKeyBindMode;
    private void Awake()
    {
        keyText = GetComponentInChildren<TextMeshProUGUI>();
    }
    private void Start()
    {
        InitializeSelector();
    }
    private void Update()
    {
        if (setKeyBindMode)
        {
            foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
            {
                //Gets the current pressed key 
                if (Input.GetKey(kcode))
                {
                    if(IsKeycodeAvailable(kcode))
                    {
                        KeyCode = kcode;
                        setKeyBindMode = false;
                        break;
                    }
                    else
                    {
                        KeyCode = keyCode;
                        setKeyBindMode = false;
                        break;
                    }
                }
            }
        }
    }
    private void InitializeSelector()
    {
        InputManager.Instance.GetSelectorReference(this);
        KeyCode = InputManager.Instance.Keys[keyInput];
    }
    /// <summary>
    /// Start key binding mode, for use with Unity Buttons
    /// </summary>
    public void StartKeyBindMode()
    {
        setKeyBindMode = true;
        keyText.text = "Enter key";
    }

    /// <summary>
    /// Checks to see if the keycode is not being used in the keybind list
    /// </summary>
    /// <param name="keycodeToCheck">The keycode that is being checked</param>
    /// <returns></returns>
    private bool IsKeycodeAvailable(KeyCode keycodeToCheck)
    {
        foreach(KeyInputSelector keyInputSelector in InputManager.Instance.KeyInputSelectors)
        {
            if (keycodeToCheck == keyInputSelector.KeyCode)
            {
                return false;
            }
        }
        return true;
    }
}
