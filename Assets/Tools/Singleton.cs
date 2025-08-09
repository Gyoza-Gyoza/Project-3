using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A generic Singleton base class for MonoBehaviour-derived types.
// Ensures that only one instance of the specified type exists in the scene.
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            // Ensures that there's always a reference to the singleton instance. 
            // This finds and returns the script if it is in the scene and creates one if it is not.
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance == null)
                {
                    GameObject singletonObject = new GameObject();
                    instance = singletonObject.AddComponent<T>();
                    singletonObject.name = typeof(T).Name + " (Singleton)";
                }
            }
            return instance;
        }
    }
    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
