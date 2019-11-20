using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NetworkObjectType
{
    BUILDING,
    ROCK,
    PAPER,
    SCISSORS
}
public enum NetworkObjectAction
{
    NOTHING,
    WALKING,
    WALKTHENATTACK,
    ATTACK,
    TRAINING
}

public class NetworkObject : MonoBehaviour
{
    public int objectID;
    public int clientOwnerID;
    public NetworkObjectType objectType;
    public NetworkObjectAction currentAction;
    public Vector3 positionTarget;
    public int objectIDTarget;
    public int health;
    public int objectLevel;
    public Vector3[] positionQueue = new Vector3[2];
    public float[] networkTimeQueue = new float[2];
    void Start()
    {
        objectID = NetworkManager.Instance.addNetworkObject(this);

    }

    // Update is called once per frame
    void Update()
    {

    }
}
