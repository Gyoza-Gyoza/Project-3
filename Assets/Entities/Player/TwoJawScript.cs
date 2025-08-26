using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class TwoJawScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject leftJaw;
    private Transform leftJawIni;
    [SerializeField] private GameObject rightJaw;
    private Transform rightJawIni;
    [SerializeField] private GameObject leftJawAttackEnd;
    [SerializeField] private GameObject rightJawAttackEnd;


    [Header("Adjustment Numbers")]
    [SerializeField] private float attackSpeed;
    [SerializeField] private float maxThrowRange;
    [SerializeField] private float throwSpeed;
    
    private bool leftJawThrown = false;
    private bool rightJawThrown = false;
    private bool rightSideCurrently = true;

    public void Attack()
    {
        //Basic Attack but they will always attack with whichever is free

    }

    IEnumerator AttackCoroutine(GameObject toAttack)
    {
        float count = 0f;
        //bool forward = true;
        while (leftJawThrown == true || rightJawThrown == true)
        {
            if (count > attackSpeed)
            {
                break;
            }

            count += Time.deltaTime;
            if (rightSideCurrently == true)
            {
                toAttack.transform.position = Vector3.Lerp(rightJawIni.position, rightJawAttackEnd.transform.position, count / attackSpeed);
                toAttack.transform.rotation = Quaternion.Lerp(rightJawIni.rotation, rightJawAttackEnd.transform.rotation, count / attackSpeed);
            }
            else
            {
                toAttack.transform.position = Vector3.Lerp(leftJawIni.position, leftJawAttackEnd.transform.position, count / attackSpeed);
                toAttack.transform.rotation = Quaternion.Lerp(leftJawIni.rotation, leftJawAttackEnd.transform.rotation, count / attackSpeed);
            }
            yield return new WaitForSeconds(Time.deltaTime);


        }

        
        //Flips the side
        if (rightSideCurrently == true)
        {
            rightSideCurrently = false;
        }
        else
        {
            rightSideCurrently = true;
        }

        yield break;
    }

    public bool Throw()
    {
        if (rightJawThrown == false)
        {

            return true;
        }
        else if (leftJawThrown == false)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    public bool Recover()
    {
        if (rightJawThrown == true)
        {
            return true;
        }
        else if (leftJawThrown == true)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        leftJawIni = leftJaw.transform;
        rightJawIni = rightJaw.transform;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
