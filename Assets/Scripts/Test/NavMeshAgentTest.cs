using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshAgentTest : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform proxy;
    public Transform model;
    public NetworkObject netObj;
    NavMeshAgent agent;
    NavMeshObstacle obstacle;
    void Start()
    {
        agent = proxy.GetComponent<NavMeshAgent>();
        obstacle = proxy.GetComponent<NavMeshObstacle>();
    }

    // Update is called once per frame
    void Update()
    {
        // Test if the distance between the agent and the player
        // is less than the attack range (or the stoppingDistance parameter)
        Debug.Log((netObj.positionTarget - proxy.position).sqrMagnitude + " " + Mathf.Pow(agent.stoppingDistance, 2));
        if ((netObj.positionTarget - proxy.position).sqrMagnitude < Mathf.Pow(agent.stoppingDistance, 2))
        {
            // If the agent is in attack range, become an obstacle and
            // disable the NavMeshAgent component
            
            obstacle.enabled = true;
            agent.enabled = false;

        }
        else
        {
            // If we are not in range, become an agent again
            obstacle.enabled = false;
            agent.enabled = true;
            agent.destination = netObj.positionTarget;
        }

        model.position = Vector3.Lerp(model.position, proxy.position, Time.deltaTime * 2);
    }
}
