using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// This system works together with the KeyInputSelector class to allow players to map keys to different key inputs.
// The KeyInputSelector class contains the key input, the corresponding key code, and the function to switch key codes. 
// Use the ConfirmKeyBinds function to get the keycodes from each of the KeyInputSelector classes and assign them in the Keys dictionary.
// Use the ResetKeyBinds function to reset the changes made in all the KeyInputSelector classes. 
// The Keys dictionary is referenced by other scripts to get the right keycode for a particular action. 

[System.Serializable]
public enum KeyInput //Contains all available inputs
{
    Forward,
    Backward,
    Left,
    Right, 
    Sprint,
    Jump
}
public class InputManager : Singleton<InputManager>
{
    private string fileName = "Keybinds";

    private List<KeyInputSelector> keyInputSelectors = new List<KeyInputSelector>();
    public List<KeyInputSelector> KeyInputSelectors
    {  get { return keyInputSelectors; } }

    //Contains the available inputs with their respective keybinds 
    private Dictionary<KeyInput, KeyCode> keys = new Dictionary<KeyInput, KeyCode>()
    {
        { KeyInput.Forward, KeyCode.W },
        { KeyInput.Backward, KeyCode.S },
        { KeyInput.Left, KeyCode.A },
        { KeyInput.Right, KeyCode.D },
        { KeyInput.Sprint, KeyCode.LeftShift},
        { KeyInput.Jump, KeyCode.Space }
    };
    public Dictionary<KeyInput, KeyCode> Keys
    { get { return keys; } }

    protected override void Awake()
    {
        base.Awake();

        //Tries to import saved keybindings
        if (DatabaseIO.ImportDatabase(fileName, out string[] csv)) ImportKeybinds(csv);
        //Else exports a new copy 
        else DatabaseIO.ExportDatabase(fileName, ExportKeyBinds());

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    private string ExportKeyBinds()
    {
        //Intialize string with headers
        string result = "Inputs,Keycodes\n";

        //Writes each row 
        foreach(KeyValuePair<KeyInput, KeyCode> kvp in keys)
        {
            result += $"{kvp.Key},{kvp.Value}\n";
        }

        return result;
    }
    private void ImportKeybinds(string[] csv)
    {
        foreach (string key in csv)
        {
            string[] cells = key.Split(',');
            if (Enum.TryParse<KeyInput>(cells[0], out var inputKeyInput))
            {
                if (Enum.TryParse<KeyCode>(cells[1], out var inputKeyCode))
                {
                    keys[inputKeyInput] = inputKeyCode;
                }
            }
        }
    }

    /// <summary>
    /// Used to get a reference from the KeyInputSelector class
    /// </summary>
    /// <param name="keyInputSelector">KeyInputSelector class reference</param>
    public void GetSelectorReference(KeyInputSelector keyInputSelector) => keyInputSelectors.Add(keyInputSelector);
    public KeyCode GetKey(KeyInput key) => keys[key]; 

    /// <summary>
    /// Goes through the list of key input selectors and sets the key bindings to the new keycodes
    /// </summary>
    public void ConfirmKeyBinds()
    {
        foreach(KeyInputSelector keyInputSelector in keyInputSelectors)
        {
            keys[keyInputSelector.keyInput] = keyInputSelector.KeyCode;
        }

        DatabaseIO.ExportDatabase(fileName, ExportKeyBinds());
    }
    /// <summary>
    /// Resets changes to the keybinds
    /// </summary>
    public void ResetKeyBinds()
    {
        foreach(KeyInputSelector keyInputSelector in keyInputSelectors)
        {
            keyInputSelector.KeyCode = keys[keyInputSelector.keyInput];
        }
    }
}