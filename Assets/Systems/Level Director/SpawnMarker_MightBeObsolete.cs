using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnMarker_MightBeObsolete : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        LevelDirector.Instance.MarkLocation(transform);
    }

    [ExecuteInEditMode]    
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(this.transform.position, new Vector3(5f, 5f, 5f));
    }

}
