using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnMarker : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        LevelDirector.Instance.MarkLocation(transform);
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawCube(this.transform.position, new Vector3(5f, 5f, 5f));
    }
}
