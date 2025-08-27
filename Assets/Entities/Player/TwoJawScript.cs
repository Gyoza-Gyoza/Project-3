using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class TwoJawScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject leftJaw;
    private GameObject leftJawIni;
    [SerializeField] private GameObject rightJaw;
    private GameObject rightJawIni;
    [SerializeField] private GameObject leftJawAttackEnd;
    [SerializeField] private GameObject rightJawAttackEnd;


    [Header("Adjustment Numbers")]
    [SerializeField] private float attackSpeed;
    [SerializeField] private float maxThrowRange;
    [SerializeField] private float throwSpeed;
    
    private bool leftJawThrown = false;
    private bool rightJawThrown = false;
    private bool rightSideCurrently = true;
    private bool meleeAttacking = false;

    public void Attack()
    {
        //Basic Attack but they will always attack with whichever is free

        if (meleeAttacking == true || rightJawThrown == true && leftJawThrown == true)
        {
            return ;
        }


        if (rightSideCurrently ==  true)
        {
            if (rightJawThrown == false)
            {
                rightJawThrown = true;
                StartCoroutine(AttackCoroutine(rightJaw));
            }
            else
            {
                rightJawThrown = false;
                StartCoroutine(AttackCoroutine(leftJaw));
            }
        }
        else
        {
            if (leftJawThrown == false)
            {
                rightJawThrown = false;
                StartCoroutine(AttackCoroutine(leftJaw));
            }
            else
            {
                rightJawThrown = true;
                StartCoroutine(AttackCoroutine(rightJaw));
            }
        }
    }

    IEnumerator AttackCoroutine(GameObject toAttack)
    {
        meleeAttacking = true;
        float count = 0f;
        bool forward = true;
        Transform iniTransform;
        Transform finalTransform;

        //Assign Left/Right coords
        if (rightSideCurrently == true)
        {
            iniTransform = rightJawIni.transform;
            finalTransform = rightJawAttackEnd.transform;
        }
        else
        {
            iniTransform = leftJawIni.transform;
            finalTransform = leftJawAttackEnd.transform;
        }

        //forward loop
        while (forward ==  true)
        {
            count += Time.deltaTime;

            if (count >= attackSpeed)
            {
                count = attackSpeed;
                forward = false;
            }

            toAttack.transform.position = Vector3.Lerp(iniTransform.position, finalTransform.transform.position, count / attackSpeed);
            toAttack.transform.rotation = Quaternion.Lerp(iniTransform.rotation, finalTransform.transform.rotation, count / attackSpeed);

            yield return new WaitForSeconds(Time.deltaTime);
            
        }
        Debug.Log("Forward Loop done");
        //backward loop
        while (forward == false)
        {
            count -= Time.deltaTime;

            if (count <= 0)
            {
                count = 0;
                forward = true;
                Debug.Log("Back exit");
            }

            toAttack.transform.position = Vector3.Lerp(iniTransform.position, finalTransform.transform.position, count / attackSpeed);
            toAttack.transform.rotation = Quaternion.Lerp(iniTransform.rotation, finalTransform.transform.rotation, count / attackSpeed);

            yield return new WaitForSeconds(Time.deltaTime);
            Debug.Log("Back Looping");

        }
        Debug.Log("Back Loop done");

        //flip sides
        if (rightSideCurrently == true)
        {
            rightSideCurrently = false;
        }
        else
        {
            rightSideCurrently = true;
        }

        meleeAttacking = false;

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
        leftJawIni = GameObject.Instantiate(leftJawAttackEnd,leftJaw.transform.position , leftJaw.transform.rotation, leftJaw.transform.parent);
        rightJawIni = GameObject.Instantiate(rightJawAttackEnd, rightJaw.transform.position, rightJaw.transform.rotation, rightJaw.transform.parent);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
