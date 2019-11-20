using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkObjectSnapshot
{
    public int objectID;
    public NetworkObjectAction currentAction;
    public Vector3 positionTarget;
    public int objectIDTarget;
    public int health;
    public int objectLevel;
}
