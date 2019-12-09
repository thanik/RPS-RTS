using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MessagePack;

[MessagePackObject]
public class NetworkObjectSnapshot
{
    [Key(0)]
    public int objectID { get; set; }
    [Key(1)]
    public int clientOwnerID { get; set; }
    [Key(2)]
    public NetworkObjectType objectType { get; set; }
    [Key(3)]
    public NetworkUnitType unitType { get; set; }
    [Key(4)]
    public NetworkObjectAction currentAction { get; set; }
    [Key(5)]
    public Vector3 positionTarget { get; set; }
    [Key(6)]
    public int objectIDTarget { get; set; }
    [Key(7)]
    public int health { get; set; }
    [Key(8)]
    public int objectLevel { get; set; }
    [Key(9)]
    public float cooldownTime { get; set; }
    [Key(10)]
    public Vector3 currentPosition { get; set; }

    public NetworkObjectSnapshot(int objectID, int clientOwnerID, NetworkObjectType objectType, NetworkUnitType unitType, NetworkObjectAction currentAction, Vector3 positionTarget, int objectIDTarget, int health, int objectLevel, float cooldownTime, Vector3 currentPosition)
    {
        this.objectID = objectID;
        this.clientOwnerID = clientOwnerID;
        this.objectType = objectType;
        this.unitType = unitType;
        this.currentAction = currentAction;
        this.positionTarget = positionTarget;
        this.objectIDTarget = objectIDTarget;
        this.health = health;
        this.objectLevel = objectLevel;
        this.cooldownTime = cooldownTime;
        this.currentPosition = currentPosition;
    }

    public NetworkObjectSnapshot(NetworkObject netObj)
    {
        this.objectID = netObj.objectID;
        this.clientOwnerID = netObj.clientOwnerID;
        this.objectType = netObj.objectType;
        this.unitType = netObj.unitType;
        this.currentAction = netObj.currentAction;
        this.positionTarget = netObj.positionTarget;
        this.objectIDTarget = netObj.objectIDTarget;
        this.health = netObj.health;
        this.objectLevel = netObj.objectLevel;
        this.cooldownTime = netObj.cooldownTime;
        Vector3 currentPosition = netObj.gameObject.transform.position;
        this.currentPosition = new Vector3(Mathf.Round(currentPosition.x * 1000f) / 1000f, Mathf.Round(currentPosition.y * 1000f) / 1000f, Mathf.Round(currentPosition.z * 1000f) / 1000f);
    }

}

public class NetworkObjectSnapshotComparer : IEqualityComparer<NetworkObjectSnapshot>
{
    public int GetHashCode(NetworkObjectSnapshot obj)
    {
        return (obj.objectID + obj.clientOwnerID + (int)obj.objectType + (int)obj.unitType + (int)obj.currentAction + obj.positionTarget.ToString() + obj.objectIDTarget + obj.health + obj.objectLevel + obj.cooldownTime.ToString() + obj.currentPosition.ToString()).GetHashCode();
    }

    public bool Equals(NetworkObjectSnapshot x, NetworkObjectSnapshot y)
    {
        return x.objectID == y.objectID && x.clientOwnerID == y.clientOwnerID && x.objectType == y.objectType && x.unitType == y.unitType && x.currentAction == y.currentAction && x.positionTarget == y.positionTarget && x.objectIDTarget == y.objectIDTarget && x.health == y.health && x.objectLevel == y.objectLevel && x.cooldownTime == y.cooldownTime && x.currentPosition == y.currentPosition;
    }
}
