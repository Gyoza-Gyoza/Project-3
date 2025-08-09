using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script manages an object pool to reuse GameObjects instead of creating and destroying them frequently.
// Use GetObject() to spawn an object at a location, and DestroyObject() to return it to the pool for reuse.
public static class GameObjectPool
{
    private static Dictionary<string, Stack<GameObject>> pools = new Dictionary<string, Stack<GameObject>>();

    public static GameObject GetObject(GameObject obj)
    {
        if (!pools.TryGetValue(obj.name, out Stack<GameObject> pool))
        {
            pool = new Stack<GameObject>(); 
            pools[obj.name] = pool;
        }

        if (pool.Count > 0)
        {
            GameObject result = pool.Pop();
            result.SetActive(true);
            return result;
        }
        else
        {
            GameObject instantiatedResult = Object.Instantiate(obj);
            instantiatedResult.name = obj.name;
            return instantiatedResult;
        }
    }
    public static void ReturnObject(GameObject obj)
    {
        if (pools.TryGetValue(obj.name, out Stack<GameObject> pool))
        {
            pool.Push(obj);
            obj.SetActive(false);
        }
        else
        {
            pools.Add(obj.name, new Stack<GameObject>());
        }
    }
}