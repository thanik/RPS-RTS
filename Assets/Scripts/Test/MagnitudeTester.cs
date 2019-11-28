using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MagnitudeTester : MonoBehaviour
{
    // Start is called before the first frame update
    //public GameObject targetBuilding;
    Vector3 target;
    public float magnitude;
    void Start()
    {
        target = GetComponent<NavMeshAgent>().destination;
    }

    // Update is called once per frame
    void Update()
    {
        target = GetComponent<NavMeshAgent>().destination;
        Vector3 directionToTarget = target - transform.position;
        magnitude = directionToTarget.sqrMagnitude;

    }
}
