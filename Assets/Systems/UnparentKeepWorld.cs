using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class UnparentKeepWorld : MonoBehaviour
{
    /*
    [MenuItem("GameObject/Reparent", false, 0)]
    private static void Reparent()
    {
        //List<GameObject> unparented = new List<GameObject>();

        if (Selection.gameObjects.Length > 1)
        {
            return;
        }
        else
        {
            GameObject newParent = new GameObject();
            GameObject oldParent = Selection.gameObjects[0];

            newParent.transform.position = oldParent.transform.position;
            newParent.name = oldParent.name;
            oldParent.name = "OLD " + oldParent.name;

            foreach (Transform child in oldParent.transform)
            {
                GameObject obj = child.gameObject;
                Transform t = obj.transform;
                Vector3 pos = t.position;
                Quaternion rot = t.rotation;
                Vector3 scale = t.lossyScale;

                t.SetParent(newParent.transform);
                t.position = pos;
                t.rotation = rot;
                t.localScale = scale;
            }

            //GameObject.DestroyImmediate(oldParent);
        }
    }
    */
}